using System;
using System.Globalization;
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
        public int ToolCallLimit;
        public bool CostWarningEnabled;
        public double CostWarningAmount = 1.0;

        public int EffectiveToolCallLimit
        {
            get { return ToolCallLimit > 0 ? ToolCallLimit : 10; }
        }

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
                else if (key == "TOOL_CALL_LIMIT")
                {
                    int limit;
                    if (Int32.TryParse(value, out limit) && limit > 0)
                        ToolCallLimit = limit;
                }
                else if (key == "COST_WARNING_ENABLED")
                    CostWarningEnabled = String.Compare(value, "true", true) == 0;
                else if (key == "COST_WARNING_AMOUNT")
                {
                    double amount;
                    if (Double.TryParse(value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out amount) && amount > 0)
                        CostWarningAmount = amount;
                }
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
                writer.WriteLine("TOOL_CALL_LIMIT=" +
                    (ToolCallLimit > 0 ? ToolCallLimit.ToString() : ""));
                writer.WriteLine("COST_WARNING_ENABLED=" +
                    (CostWarningEnabled ? "true" : "false"));
                writer.WriteLine("COST_WARNING_AMOUNT=" +
                    CostWarningAmount.ToString("0.######",
                    CultureInfo.InvariantCulture));
            }
        }
    }
}
