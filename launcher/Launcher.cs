using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Harness98.Launcher
{
    public sealed class LauncherProgram
    {
        [STAThread]
        public static int Main()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                string marker = Path.Combine(root, "UPDATE-STAGE\\READY.TAG");
                if (File.Exists(marker))
                {
                    string updater = Path.Combine(root, "HARNESS98-UPDATER.EXE");
                    if (!File.Exists(updater))
                        throw new FileNotFoundException("The updater is missing.", updater);
                    Process updateProcess = Process.Start(updater);
                    updateProcess.WaitForExit();
                    if (updateProcess.ExitCode != 0)
                        throw new ApplicationException("The staged update could not be applied.");
                }

                string gui = Path.Combine(root, "H98GUI.EXE");
                if (!File.Exists(gui))
                    throw new FileNotFoundException("The Harness98 GUI is missing.", gui);
                Process.Start(gui);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Harness98 launcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
