using System;
using System.IO;
using System.Text;

namespace Harness98
{
    public sealed class ExtraInstructionsStore
    {
        public const string FileName = "HARNESS98.INSTRUCTIONS.TXT";
        private readonly string path;

        public ExtraInstructionsStore(string baseDirectory)
        {
            path = Path.Combine(baseDirectory, FileName);
        }

        public string Load()
        {
            if (!File.Exists(path)) return "";
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public void Save(string instructions)
        {
            string value = instructions == null ? "" : instructions;
            if (value.Trim().Length == 0)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }
            File.WriteAllText(path, value, new UTF8Encoding(false));
        }
    }
}
