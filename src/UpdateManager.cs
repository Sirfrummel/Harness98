using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Harness98
{
    public sealed class UpdateManager
    {
        public const string CurrentVersion = "3.1.2";
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
            AppConfiguration configuration = new AppConfiguration(baseDirectory);
            configuration.Load();
            return configuration.UpdateServer;
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
