using System;
using System.IO;
using System.Text;

namespace Harness98
{
    public sealed class AppConfiguration
    {
        private readonly string path;

        public string UpdateServer = UpdateManager.DefaultServer;
        public bool AutoTitleConversations;
        public string TitleModelId = "";

        public AppConfiguration(string baseDirectory)
        {
            path = Path.Combine(baseDirectory, "HARNESS98.CFG");
        }

        public void Load()
        {
            if (!File.Exists(path))
            {
                Save();
                return;
            }

            string[] lines = File.ReadAllLines(path, Encoding.ASCII);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                int equals = line.IndexOf('=');
                if (equals <= 0) continue;
                string key = line.Substring(0, equals).Trim().ToUpper();
                string value = line.Substring(equals + 1).Trim();
                if (key == "UPDATE_SERVER") UpdateServer = value;
                else if (key == "AUTO_TITLE")
                    AutoTitleConversations = String.Compare(value, "true", true) == 0;
                else if (key == "TITLE_MODEL") TitleModelId = value;
            }
            if (UpdateServer.Length == 0) UpdateServer = UpdateManager.DefaultServer;
        }

        public void Save()
        {
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.ASCII))
            {
                writer.WriteLine("UPDATE_SERVER=" + UpdateServer);
                writer.WriteLine("AUTO_TITLE=" +
                    (AutoTitleConversations ? "true" : "false"));
                writer.WriteLine("TITLE_MODEL=" + TitleModelId);
            }
        }
    }
}
