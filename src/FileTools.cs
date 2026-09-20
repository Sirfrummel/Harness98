using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Harness98
{
    public sealed class FileToolServices
    {
        public const int MaximumWriteCharacters = 65536;
        public const int MaximumEditCharacters = 1048576;
        private readonly string defaultWorkingDirectory;

        public FileToolServices(string workingDirectory)
        {
            defaultWorkingDirectory = Path.GetFullPath(workingDirectory);
        }

        public string ResolvePath(string requested)
        {
            if (requested == null || requested.Trim().Length == 0)
                throw new ApplicationException("The path argument is required.");
            requested = requested.Trim();
            if (Path.IsPathRooted(requested)) return Path.GetFullPath(requested);
            return Path.GetFullPath(Path.Combine(defaultWorkingDirectory, requested));
        }

        public bool IsBinary(string path)
        {
            byte[] buffer = new byte[8192];
            int count;
            using (FileStream stream = new FileStream(path, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite))
            {
                count = stream.Read(buffer, 0, buffer.Length);
            }
            if (count >= 2 && ((buffer[0] == 0xff && buffer[1] == 0xfe) ||
                (buffer[0] == 0xfe && buffer[1] == 0xff))) return false;

            int controls = 0;
            for (int i = 0; i < count; i++)
            {
                byte value = buffer[i];
                if (value == 0) return true;
                if (value < 32 && value != 9 && value != 10 && value != 12 &&
                    value != 13) controls++;
            }
            return controls > 4 && controls * 20 > count;
        }

        public Encoding DetectEncoding(string path)
        {
            byte[] marker = new byte[3];
            int count;
            using (FileStream stream = new FileStream(path, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite))
            {
                count = stream.Read(marker, 0, marker.Length);
            }
            if (count >= 3 && marker[0] == 0xef && marker[1] == 0xbb &&
                marker[2] == 0xbf) return new UTF8Encoding(true);
            if (count >= 2 && marker[0] == 0xff && marker[1] == 0xfe)
                return Encoding.Unicode;
            if (count >= 2 && marker[0] == 0xfe && marker[1] == 0xff)
                return Encoding.BigEndianUnicode;
            return Encoding.Default;
        }

        public string ReadAllText(string path, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(path, encoding, true))
                return reader.ReadToEnd();
        }

        public void WriteAllText(string path, string content, Encoding encoding)
        {
            string directory = Path.GetDirectoryName(path);
            if (directory == null || !Directory.Exists(directory))
                throw new DirectoryNotFoundException(
                    "The destination directory does not exist: " + directory);
            string temporary = Path.Combine(directory, ".H98-" +
                DateTime.UtcNow.Ticks.ToString("x") + ".TMP");
            try
            {
                using (StreamWriter writer = new StreamWriter(temporary, false,
                    encoding)) writer.Write(content);
                if (File.Exists(path)) File.Copy(temporary, path, true);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public static string NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n').
                Replace("\n", "\r\n");
        }

        public static string Error(string message)
        {
            return "{\"error\":" + Json.Quote(message) + "}";
        }

        public static int CountLines(string text)
        {
            if (text.Length == 0) return 0;
            int count = 1;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') count++;
            return count;
        }
    }

    public sealed class ReadFileTool
    {
        private const int DefaultLines = 200;
        private const int MaximumLines = 1000;
        private const int MaximumCharacters = 32768;
        private readonly FileToolServices files;

        public ReadFileTool(FileToolServices services)
        {
            files = services;
        }

        public string DefinitionJson
        {
            get
            {
                return "{\"type\":\"function\",\"function\":{" +
                    "\"name\":\"read_file\",\"description\":" +
                    Json.Quote("Read a bounded range from a text file. Binary " +
                    "files are refused. Returns at most 1000 lines and 32768 " +
                    "characters; use start_line to continue a truncated read.") +
                    ",\"parameters\":{\"type\":\"object\",\"properties\":{" +
                    "\"path\":{\"type\":\"string\",\"description\":" +
                    Json.Quote("Absolute or application-relative file path.") + "}," +
                    "\"start_line\":{\"type\":\"integer\",\"description\":" +
                    Json.Quote("Optional one-based first line; defaults to 1.") + "}," +
                    "\"max_lines\":{\"type\":\"integer\",\"description\":" +
                    Json.Quote("Optional line count; defaults to 200, maximum 1000.") +
                    "}},\"required\":[\"path\"]}}}";
            }
        }

        public string Execute(string argumentsJson)
        {
            try
            {
                Hashtable arguments = Json.AsObject(Json.Parse(argumentsJson));
                if (arguments == null)
                    return FileToolServices.Error(
                        "Tool arguments must be a JSON object.");
                string path = files.ResolvePath(Json.GetString(arguments, "path"));
                if (!File.Exists(path))
                    return FileToolServices.Error("File does not exist: " + path);
                if (files.IsBinary(path))
                    return FileToolServices.Error(
                        "The requested file appears to be binary: " + path);
                long requestedStart = Json.GetInt64(arguments, "start_line");
                long requestedMaximum = Json.GetInt64(arguments, "max_lines");
                int startLine = requestedStart == 0 ? 1 : (int)requestedStart;
                int maximumLines = requestedMaximum == 0 ? DefaultLines :
                    (int)requestedMaximum;
                if (startLine < 1)
                    return FileToolServices.Error("start_line must be at least 1.");
                if (maximumLines < 1 || maximumLines > MaximumLines)
                    return FileToolServices.Error(
                        "max_lines must be from 1 to 1000.");

                Encoding encoding = files.DetectEncoding(path);
                StringBuilder content = new StringBuilder();
                int lineNumber = 0;
                int returned = 0;
                bool truncated = false;
                bool lineTruncated = false;
                using (StreamReader reader = new StreamReader(path, encoding, true))
                {
                    string line;
                    while (lineNumber < startLine - 1 &&
                        (line = reader.ReadLine()) != null) lineNumber++;
                    while (returned < maximumLines &&
                        (line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        int separator = returned == 0 ? 0 : 2;
                        int remaining = MaximumCharacters - content.Length - separator;
                        if (remaining <= 0)
                        {
                            truncated = true;
                            break;
                        }
                        if (separator > 0) content.Append("\r\n");
                        if (line.Length > remaining)
                        {
                            content.Append(line.Substring(0, remaining));
                            lineTruncated = true;
                            truncated = true;
                            returned++;
                            break;
                        }
                        content.Append(line);
                        returned++;
                    }
                    if (!truncated && reader.Peek() >= 0) truncated = true;
                }

                int endLine = returned == 0 ? 0 : startLine + returned - 1;
                StringBuilder json = new StringBuilder();
                json.Append("{\"path\":").Append(Json.Quote(path));
                json.Append(",\"start_line\":").Append(startLine.ToString());
                json.Append(",\"end_line\":").Append(endLine.ToString());
                json.Append(",\"content\":").Append(Json.Quote(content.ToString()));
                json.Append(",\"truncated\":").Append(truncated ? "true" : "false");
                json.Append(",\"line_truncated\":").Append(
                    lineTruncated ? "true" : "false");
                if (truncated && !lineTruncated)
                    json.Append(",\"next_start_line\":").Append(
                        (endLine + 1).ToString());
                json.Append('}');
                return json.ToString();
            }
            catch (Exception ex)
            {
                return FileToolServices.Error("Could not read file: " + ex.Message);
            }
        }
    }

    public sealed class WriteFileTool
    {
        private readonly FileToolServices files;

        public WriteFileTool(FileToolServices services)
        {
            files = services;
        }

        public string DefinitionJson
        {
            get
            {
                return "{\"type\":\"function\",\"function\":{" +
                    "\"name\":\"write_file\",\"description\":" +
                    Json.Quote("Create or completely replace a text file with " +
                    "at most 65536 characters. Existing binary files are refused.") +
                    ",\"parameters\":{\"type\":\"object\",\"properties\":{" +
                    "\"path\":{\"type\":\"string\"}," +
                    "\"content\":{\"type\":\"string\"}}," +
                    "\"required\":[\"path\",\"content\"]}}}";
            }
        }

        public string Execute(string argumentsJson)
        {
            try
            {
                Hashtable arguments = Json.AsObject(Json.Parse(argumentsJson));
                if (arguments == null)
                    return FileToolServices.Error(
                        "Tool arguments must be a JSON object.");
                string path = files.ResolvePath(Json.GetString(arguments, "path"));
                string content = Json.GetString(arguments, "content");
                if (content == null)
                    return FileToolServices.Error("The content argument is required.");
                content = FileToolServices.NormalizeNewlines(content);
                if (content.Length > FileToolServices.MaximumWriteCharacters)
                    return FileToolServices.Error(
                        "Content exceeds the 65536-character write limit.");
                Encoding encoding = Encoding.Default;
                if (File.Exists(path))
                {
                    if (files.IsBinary(path))
                        return FileToolServices.Error(
                            "The destination appears to be binary: " + path);
                    encoding = files.DetectEncoding(path);
                }
                files.WriteAllText(path, content, encoding);
                return "{\"path\":" + Json.Quote(path) +
                    ",\"characters_written\":" + content.Length.ToString() +
                    ",\"lines_written\":" +
                    FileToolServices.CountLines(content).ToString() +
                    ",\"message\":" + Json.Quote("Wrote " + path) + "}";
            }
            catch (Exception ex)
            {
                return FileToolServices.Error("Could not write file: " + ex.Message);
            }
        }
    }

    public sealed class EditFileTool
    {
        private readonly FileToolServices files;

        public EditFileTool(FileToolServices services)
        {
            files = services;
        }

        public string DefinitionJson
        {
            get
            {
                return "{\"type\":\"function\",\"function\":{" +
                    "\"name\":\"edit_file\",\"description\":" +
                    Json.Quote("Replace exactly one occurrence of old_text in a " +
                    "text file. The edit fails if the match is absent or ambiguous.") +
                    ",\"parameters\":{\"type\":\"object\",\"properties\":{" +
                    "\"path\":{\"type\":\"string\"}," +
                    "\"old_text\":{\"type\":\"string\"}," +
                    "\"new_text\":{\"type\":\"string\"}}," +
                    "\"required\":[\"path\",\"old_text\",\"new_text\"]}}}";
            }
        }

        public string Execute(string argumentsJson)
        {
            try
            {
                Hashtable arguments = Json.AsObject(Json.Parse(argumentsJson));
                if (arguments == null)
                    return FileToolServices.Error(
                        "Tool arguments must be a JSON object.");
                string path = files.ResolvePath(Json.GetString(arguments, "path"));
                string oldText = Json.GetString(arguments, "old_text");
                string newText = Json.GetString(arguments, "new_text");
                if (oldText == null || oldText.Length == 0)
                    return FileToolServices.Error(
                        "old_text must contain the exact text to replace.");
                if (newText == null)
                    return FileToolServices.Error("The new_text argument is required.");
                if (!File.Exists(path))
                    return FileToolServices.Error("File does not exist: " + path);
                if (new FileInfo(path).Length > 2097152)
                    return FileToolServices.Error(
                        "The file is too large for an exact edit.");
                if (files.IsBinary(path))
                    return FileToolServices.Error(
                        "The requested file appears to be binary: " + path);

                Encoding encoding = files.DetectEncoding(path);
                string content = files.ReadAllText(path, encoding);
                int first = content.IndexOf(oldText, StringComparison.Ordinal);
                if (first < 0)
                    return FileToolServices.Error("old_text was not found in " + path);
                int second = content.IndexOf(oldText, first + oldText.Length,
                    StringComparison.Ordinal);
                if (second >= 0)
                    return FileToolServices.Error(
                        "old_text matched more than once; provide a larger unique block.");
                string updated = content.Substring(0, first) + newText +
                    content.Substring(first + oldText.Length);
                if (updated.Length > FileToolServices.MaximumEditCharacters)
                    return FileToolServices.Error(
                        "The edited file would exceed the 1048576-character limit.");
                files.WriteAllText(path, updated, encoding);
                return "{\"path\":" + Json.Quote(path) +
                    ",\"replacements\":1,\"characters_written\":" +
                    updated.Length.ToString() + ",\"message\":" +
                    Json.Quote("Edited " + path) + "}";
            }
            catch (Exception ex)
            {
                return FileToolServices.Error("Could not edit file: " + ex.Message);
            }
        }
    }
}
