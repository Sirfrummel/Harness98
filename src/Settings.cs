using System;
using System.IO;
using System.Text;

namespace Win98Ai
{
    public sealed class Settings
    {
        private readonly string keyPath;

        public Settings(string baseDirectory)
        {
            keyPath = Path.Combine(baseDirectory, "OPENROUT.KEY");
        }

        public string LoadOrCreateKey()
        {
            if (File.Exists(keyPath))
            {
                string saved = ReadKeyFile();
                if (saved.Length > 0)
                {
                    Console.WriteLine("Loaded the saved OpenRouter key.");
                    return saved;
                }
            }

            return PromptAndSaveKey();
        }

        public string ReplaceKey()
        {
            Console.WriteLine("The existing saved key will be replaced.");
            return PromptAndSaveKey();
        }

        private string PromptAndSaveKey()
        {
            Console.WriteLine();
            Console.WriteLine("Enter your OpenRouter API key.");
            Console.WriteLine("Input is hidden. The key will be stored as plain text in:");
            Console.WriteLine(keyPath);
            Console.Write("Key: ");

            string key = ReadHiddenLine().Trim();
            Console.WriteLine();
            if (key.Length == 0)
            {
                throw new ApplicationException("No API key was entered.");
            }

            using (StreamWriter writer = new StreamWriter(keyPath, false,
                new UTF8Encoding(false)))
            {
                writer.WriteLine(key);
            }

            Console.WriteLine("Key saved locally.");
            return key;
        }

        private string ReadKeyFile()
        {
            using (StreamReader reader = new StreamReader(keyPath, Encoding.UTF8, true))
            {
                return reader.ReadToEnd().Trim();
            }
        }

        private static string ReadHiddenLine()
        {
            StringBuilder value = new StringBuilder();
            try
            {
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.KeyChar == '\r' || key.KeyChar == '\n')
                    {
                        break;
                    }
                    if (key.KeyChar == '\b')
                    {
                        if (value.Length > 0)
                        {
                            value.Length--;
                        }
                        continue;
                    }
                    if (!Char.IsControl(key.KeyChar))
                    {
                        value.Append(key.KeyChar);
                    }
                }
                return value.ToString();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine();
                Console.WriteLine("Hidden input is unavailable; input will be visible.");
                return Console.ReadLine();
            }
        }
    }
}
