using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Harness98.Restarter
{
    public sealed class RestarterProgram
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length != 2)
                    throw new ApplicationException("Restart arguments are missing.");
                string applicationDirectory = args[0];
                int processId = Int32.Parse(args[1]);
                try
                {
                    Process runningGui = Process.GetProcessById(processId);
                    runningGui.WaitForExit();
                }
                catch (ArgumentException)
                {
                    // The GUI already exited before the helper began waiting.
                }

                string launcher = Path.Combine(applicationDirectory,
                    "HARNESS98.EXE");
                if (!File.Exists(launcher))
                    throw new FileNotFoundException("The Harness98 launcher is missing.",
                        launcher);
                Process.Start(launcher);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Harness98 restart",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
