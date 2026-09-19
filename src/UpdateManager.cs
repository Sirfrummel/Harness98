using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Harness98
{
    public sealed class UpdateManager
    {
        public const string CurrentVersion = "3.0.0";
        public const string DefaultServer =
            @"\\192.168.50.170\retro\to-transfer\harness98-updates\stable";

        private readonly string baseDirectory;

        public UpdateManager(string applicationDirectory)
        {
            baseDirectory = applicationDirectory;
        }

        public string CheckAndStage()
        {
            string server = LoadServer();
            string manifestPath = Path.Combine(server, "MANIFEST.INI");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("No update manifest was found at " +
                    manifestPath);
            }

            UpdateManifest manifest = UpdateManifest.Load(manifestPath);
            Version available = new Version(manifest.Version);
            Version current = new Version(CurrentVersion);
            if (available.CompareTo(current) <= 0)
            {
                return "Harness98 " + CurrentVersion + " is up to date.";
            }

            string stage = Path.Combine(baseDirectory, "UPDATE-STAGE");
            RecreateDirectory(stage);
            for (int i = 0; i < manifest.Files.Count; i++)
            {
                UpdateFile file = (UpdateFile)manifest.Files[i];
                string source = Path.Combine(server, file.Name);
                string destination = Path.Combine(stage, file.Name);
                if (!File.Exists(source))
                {
                    throw new FileNotFoundException("Update file is missing: " + source);
                }
                File.Copy(source, destination, true);
                string actual = UpdateManifest.HashFile(destination);
                if (String.Compare(actual, file.Sha256, true) != 0)
                {
                    throw new ApplicationException("Hash check failed for " + file.Name + ".");
                }
            }

            File.Copy(manifestPath, Path.Combine(stage, "MANIFEST.INI"), true);
            WriteAscii(Path.Combine(stage, "READY.TAG"), manifest.Version);
            return "New update " + manifest.Version +
                " found and verified. Restart Harness98 to apply it.";
        }

        public string LoadServer()
        {
            string config = Path.Combine(baseDirectory, "HARNESS98.CFG");
            if (!File.Exists(config))
            {
                WriteAscii(config, "UPDATE_SERVER=" + DefaultServer + "\r\n");
                return DefaultServer;
            }

            string[] lines = File.ReadAllLines(config, Encoding.ASCII);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                int equals = line.IndexOf('=');
                if (equals <= 0) continue;
                string key = line.Substring(0, equals).Trim();
                if (String.Compare(key, "UPDATE_SERVER", true) == 0)
                {
                    string value = line.Substring(equals + 1).Trim();
                    if (value.Length == 0)
                    {
                        throw new FormatException("UPDATE_SERVER is empty in HARNESS98.CFG.");
                    }
                    return value;
                }
            }
            throw new FormatException("HARNESS98.CFG has no UPDATE_SERVER setting.");
        }

        private static void RecreateDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        private static void WriteAscii(string path, string value)
        {
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.ASCII))
            {
                writer.Write(value);
            }
        }
    }
}
