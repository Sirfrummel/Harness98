using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Harness98
{
    public sealed class ToolRegistry
    {
        private readonly CommandTool commandTool;

        public ToolRegistry(string defaultWorkingDirectory)
        {
            commandTool = new CommandTool(defaultWorkingDirectory);
        }

        public string DefinitionsJson
        {
            get { return "[" + commandTool.DefinitionJson + "]"; }
        }

        public string Execute(ToolCall call)
        {
            if (String.Compare(call.Name, "run_command", true) == 0)
                return commandTool.Execute(call.Arguments);
            return "{\"error\":" + Json.Quote("Unknown tool: " + call.Name) + "}";
        }
    }

    public sealed class CommandTool
    {
        private const int TimeoutMilliseconds = 30000;
        private const int MaximumStandardOutputCharacters = 8192;
        private const int MaximumStandardErrorCharacters = 2048;
        private readonly string defaultWorkingDirectory;

        public CommandTool(string workingDirectory)
        {
            defaultWorkingDirectory = Path.GetFullPath(workingDirectory);
        }

        public string DefinitionJson
        {
            get
            {
                return "{\"type\":\"function\",\"function\":{" +
                    "\"name\":\"run_command\"," +
                    "\"description\":\"Run a Windows 98 COMMAND.COM-compatible " +
                    "command and return its exit code, standard output, and error output.\"," +
                    "\"parameters\":{\"type\":\"object\",\"properties\":{" +
                    "\"command\":{\"type\":\"string\",\"description\":" +
                    "\"The command line to run.\"}," +
                    "\"working_directory\":{\"type\":\"string\",\"description\":" +
                    "\"Optional absolute or application-relative working directory.\"}" +
                    "},\"required\":[\"command\"]}}}";
            }
        }

        public string Execute(string argumentsJson)
        {
            string command;
            string workingDirectory;
            try
            {
                Hashtable arguments = Json.AsObject(Json.Parse(argumentsJson));
                if (arguments == null)
                    return Error("Tool arguments must be a JSON object.");
                command = Json.GetString(arguments, "command");
                string requestedDirectory = Json.GetString(arguments,
                    "working_directory");
                if (command == null || command.Trim().Length == 0)
                    return Error("The command argument is required.");
                workingDirectory = ResolveWorkingDirectory(requestedDirectory);
                if (!Directory.Exists(workingDirectory))
                    return Error("Working directory does not exist: " +
                        workingDirectory);
            }
            catch (Exception ex)
            {
                return Error("Could not read tool arguments: " + ex.Message);
            }

            BoundedText standardOutput = new BoundedText(
                MaximumStandardOutputCharacters);
            BoundedText standardError = new BoundedText(
                MaximumStandardErrorCharacters);
            int exitCode = -1;
            bool timedOut = false;
            try
            {
                string commandInterpreter = Environment.GetEnvironmentVariable(
                    "COMSPEC");
                if (commandInterpreter == null || commandInterpreter.Length == 0)
                    commandInterpreter = "COMMAND.COM";

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = commandInterpreter;
                start.Arguments = "/C " + command;
                start.WorkingDirectory = workingDirectory;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.CreateNoWindow = true;

                using (Process process = Process.Start(start))
                {
                    ReaderWorker outputWorker = new ReaderWorker(
                        process.StandardOutput, standardOutput);
                    ReaderWorker errorWorker = new ReaderWorker(
                        process.StandardError, standardError);
                    Thread outputThread = new Thread(new ThreadStart(outputWorker.Read));
                    Thread errorThread = new Thread(new ThreadStart(errorWorker.Read));
                    outputThread.IsBackground = true;
                    errorThread.IsBackground = true;
                    outputThread.Start();
                    errorThread.Start();

                    if (!process.WaitForExit(TimeoutMilliseconds))
                    {
                        timedOut = true;
                        try { process.Kill(); }
                        catch { }
                        process.WaitForExit(2000);
                    }
                    if (!timedOut || process.HasExited)
                    {
                        process.WaitForExit();
                        try { exitCode = process.ExitCode; }
                        catch { exitCode = -1; }
                    }
                    outputThread.Join(2000);
                    errorThread.Join(2000);
                }
            }
            catch (Exception ex)
            {
                standardError.Append("Could not start command: " + ex.Message);
            }

            StringBuilder json = new StringBuilder();
            json.Append("{\"command\":");
            json.Append(Json.Quote(command));
            json.Append(",\"working_directory\":");
            json.Append(Json.Quote(workingDirectory));
            json.Append(",\"exit_code\":");
            json.Append(exitCode.ToString());
            json.Append(",\"timed_out\":");
            json.Append(timedOut ? "true" : "false");
            json.Append(",\"stdout\":");
            json.Append(Json.Quote(standardOutput.Text));
            json.Append(",\"stderr\":");
            json.Append(Json.Quote(standardError.Text));
            json.Append(",\"output_truncated\":");
            json.Append(standardOutput.Truncated || standardError.Truncated ?
                "true" : "false");
            json.Append('}');
            return json.ToString();
        }

        private string ResolveWorkingDirectory(string requested)
        {
            if (requested == null || requested.Trim().Length == 0)
                return defaultWorkingDirectory;
            requested = requested.Trim();
            if (Path.IsPathRooted(requested)) return Path.GetFullPath(requested);
            return Path.GetFullPath(Path.Combine(defaultWorkingDirectory, requested));
        }

        private static string Error(string message)
        {
            return "{\"error\":" + Json.Quote(message) + "}";
        }

        private sealed class ReaderWorker
        {
            private readonly TextReader reader;
            private readonly BoundedText output;

            public ReaderWorker(TextReader source, BoundedText target)
            {
                reader = source;
                output = target;
            }

            public void Read()
            {
                char[] buffer = new char[1024];
                try
                {
                    int count;
                    while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                        output.Append(buffer, count);
                }
                catch
                {
                    // A killed process can close a redirected stream mid-read.
                }
            }
        }

        private sealed class BoundedText
        {
            private readonly int maximum;
            private readonly StringBuilder value = new StringBuilder();
            private bool truncated;

            public BoundedText(int maximumCharacters)
            {
                maximum = maximumCharacters;
            }

            public void Append(char[] buffer, int count)
            {
                lock (value)
                {
                    int remaining = maximum - value.Length;
                    if (remaining > 0)
                        value.Append(buffer, 0, Math.Min(remaining, count));
                    if (count > remaining) truncated = true;
                }
            }

            public void Append(string text)
            {
                char[] characters = text.ToCharArray();
                Append(characters, characters.Length);
            }

            public string Text
            {
                get { lock (value) { return value.ToString(); } }
            }

            public bool Truncated
            {
                get { lock (value) { return truncated; } }
            }
        }
    }
}
