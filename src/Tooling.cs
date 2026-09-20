using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Harness98
{
    public sealed class ToolRegistry
    {
        private readonly CommandTool commandTool;
        private readonly ReadFileTool readFileTool;
        private readonly WriteFileTool writeFileTool;
        private readonly EditFileTool editFileTool;
        private readonly string temporaryDirectory;

        public ToolRegistry(string defaultWorkingDirectory)
            : this(defaultWorkingDirectory, null)
        {
        }

        public ToolRegistry(string defaultWorkingDirectory,
            ICommandRunControl commandControl)
        {
            commandTool = new CommandTool(defaultWorkingDirectory,
                commandControl);
            FileToolServices files = new FileToolServices(defaultWorkingDirectory);
            readFileTool = new ReadFileTool(files);
            writeFileTool = new WriteFileTool(files);
            editFileTool = new EditFileTool(files);
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "HARNESS98");
            Directory.CreateDirectory(temporaryDirectory);
        }

        public string DefinitionsJson
        {
            get
            {
                return "[" + commandTool.DefinitionJson + "," +
                    readFileTool.DefinitionJson + "," +
                    writeFileTool.DefinitionJson + "," +
                    editFileTool.DefinitionJson + "]";
            }
        }

        public string TemporaryDirectory
        {
            get { return temporaryDirectory; }
        }

        public string Execute(ToolCall call)
        {
            if (String.Compare(call.Name, "run_command", true) == 0)
                return commandTool.Execute(call.Arguments);
            if (String.Compare(call.Name, "read_file", true) == 0)
                return readFileTool.Execute(call.Arguments);
            if (String.Compare(call.Name, "write_file", true) == 0)
                return writeFileTool.Execute(call.Arguments);
            if (String.Compare(call.Name, "edit_file", true) == 0)
                return editFileTool.Execute(call.Arguments);
            return "{\"error\":" + Json.Quote("Unknown tool: " + call.Name) + "}";
        }
    }

    public sealed class CommandTool
    {
        private const int TimeoutMilliseconds = 30000;
        private const int MaximumStandardOutputCharacters = 8192;
        private const int MaximumStandardErrorCharacters = 2048;
        private readonly string defaultWorkingDirectory;
        private readonly ICommandRunControl runControl;

        public CommandTool(string workingDirectory)
            : this(workingDirectory, null)
        {
        }

        public CommandTool(string workingDirectory, ICommandRunControl control)
        {
            defaultWorkingDirectory = Path.GetFullPath(workingDirectory);
            runControl = control;
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
            bool cancelled = runControl != null && runControl.CancelCommand;
            try
            {
                if (cancelled) throw new CommandCancelledException();
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

                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(
                        TimeoutMilliseconds);
                    while (!process.WaitForExit(100))
                    {
                        if (runControl != null && runControl.CancelCommand)
                        {
                            cancelled = true;
                            TerminateProcessTree(process);
                            break;
                        }
                        if (DateTime.UtcNow >= deadline)
                        {
                            timedOut = true;
                            TerminateProcessTree(process);
                            break;
                        }
                    }
                    if (timedOut || cancelled) process.WaitForExit(2000);
                    try
                    {
                        if (process.HasExited) exitCode = process.ExitCode;
                    }
                    catch { exitCode = -1; }
                    outputThread.Join(2000);
                    errorThread.Join(2000);
                }
            }
            catch (CommandCancelledException)
            {
                // The UI cancelled before the process was started.
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
            json.Append(",\"cancelled\":");
            json.Append(cancelled ? "true" : "false");
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

        private static void TerminateProcessTree(Process process)
        {
            try { ProcessTreeTerminator.TerminateDescendants(process.Id); }
            catch { }
            try { process.Kill(); }
            catch { }
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

        private sealed class CommandCancelledException : Exception
        {
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

    internal static class ProcessTreeTerminator
    {
        private const uint SnapshotProcessFlag = 0x00000002;
        private static readonly IntPtr InvalidHandle = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct ProcessEntry32
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint Threads;
            public uint ParentProcessId;
            public int BasePriority;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExecutableFile;
        }

        private sealed class ProcessRelation
        {
            public int ProcessId;
            public int ParentProcessId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags,
            uint processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool Process32First(IntPtr snapshot,
            ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool Process32Next(IntPtr snapshot,
            ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        public static void TerminateDescendants(int rootProcessId)
        {
            ArrayList processes = SnapshotProcesses();
            Hashtable visited = new Hashtable();
            TerminateChildren(rootProcessId, processes, visited);
        }

        private static ArrayList SnapshotProcesses()
        {
            ArrayList processes = new ArrayList();
            IntPtr snapshot = CreateToolhelp32Snapshot(SnapshotProcessFlag, 0);
            if (snapshot == InvalidHandle) return processes;
            try
            {
                ProcessEntry32 entry = new ProcessEntry32();
                entry.Size = (uint)Marshal.SizeOf(typeof(ProcessEntry32));
                if (!Process32First(snapshot, ref entry)) return processes;
                do
                {
                    ProcessRelation relation = new ProcessRelation();
                    relation.ProcessId = (int)entry.ProcessId;
                    relation.ParentProcessId = (int)entry.ParentProcessId;
                    processes.Add(relation);
                    entry.Size = (uint)Marshal.SizeOf(typeof(ProcessEntry32));
                }
                while (Process32Next(snapshot, ref entry));
            }
            finally
            {
                CloseHandle(snapshot);
            }
            return processes;
        }

        private static void TerminateChildren(int parentProcessId,
            ArrayList processes, Hashtable visited)
        {
            if (visited.ContainsKey(parentProcessId)) return;
            visited[parentProcessId] = true;
            for (int i = 0; i < processes.Count; i++)
            {
                ProcessRelation relation = (ProcessRelation)processes[i];
                if (relation.ParentProcessId != parentProcessId) continue;
                TerminateChildren(relation.ProcessId, processes, visited);
                try
                {
                    Process child = Process.GetProcessById(relation.ProcessId);
                    child.Kill();
                    child.Dispose();
                }
                catch { }
            }
        }
    }
}
