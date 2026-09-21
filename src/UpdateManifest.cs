using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Harness98
{
    public sealed class UpdateFile
    {
        public string Name;
        public string Sha256;
    }

    public sealed class UpdateManifest
    {
        public string Version;
        public readonly ArrayList Files = new ArrayList();

        public static UpdateManifest Load(string path)
        {
            UpdateManifest manifest = new UpdateManifest();
            string[] lines = File.ReadAllLines(path, Encoding.ASCII);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                int equals = line.IndexOf('=');
                if (equals <= 0) throw new FormatException("Invalid manifest line " +
                    (i + 1).ToString(CultureInfo.InvariantCulture) + ".");
                string key = line.Substring(0, equals).Trim().ToUpper();
                string value = line.Substring(equals + 1).Trim();
                if (key == "VERSION")
                {
                    manifest.Version = value;
                }
                else if (key == "FILE")
                {
                    int divider = value.IndexOf('|');
                    if (divider <= 0 || divider == value.Length - 1)
                    {
                        throw new FormatException("Invalid FILE entry.");
                    }
                    UpdateFile file = new UpdateFile();
                    file.Name = value.Substring(0, divider).Trim();
                    file.Sha256 = value.Substring(divider + 1).Trim().ToLower();
                    ValidateFile(file);
                    manifest.Files.Add(file);
                }
                else
                {
                    throw new FormatException("Unknown manifest key: " + key);
                }
            }

            if (manifest.Version == null || manifest.Version.Length == 0)
            {
                throw new FormatException("Manifest VERSION is missing.");
            }
            new Version(manifest.Version);
            if (manifest.Files.Count == 0)
            {
                throw new FormatException("Manifest has no files.");
            }
            return manifest;
        }

        public static string HashFile(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 algorithm = new SHA256Managed())
            {
                byte[] hash = algorithm.ComputeHash(stream);
                StringBuilder text = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    text.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return text.ToString();
            }
        }

        private static void ValidateFile(UpdateFile file)
        {
            if (file.Name.Length == 0 || file.Name == "." || file.Name == ".." ||
                Path.GetFileName(file.Name) != file.Name)
            {
                throw new FormatException("Update filenames must be simple filenames.");
            }
            string upper = file.Name.ToUpper();
            if (upper == "HARNESS98.KEY" || upper == "OPENROUT.KEY" ||
                upper == "HARNESS98.CFG" ||
                upper == "HARNESS98.INSTRUCTIONS.TXT" ||
                upper == "MANIFEST.INI" ||
                upper == "READY.TAG" || upper == "HARNESS98-UPDATER.EXE")
            {
                throw new FormatException("Protected file cannot be updated: " +
                    file.Name + ".");
            }
            if (file.Sha256.Length != 64)
            {
                throw new FormatException("Invalid SHA-256 hash for " + file.Name + ".");
            }
            for (int i = 0; i < file.Sha256.Length; i++)
            {
                char value = file.Sha256[i];
                if (!Char.IsDigit(value) && (value < 'a' || value > 'f'))
                {
                    throw new FormatException("Invalid SHA-256 hash for " + file.Name + ".");
                }
            }
        }
    }
}
