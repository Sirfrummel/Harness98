using System;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class GuiProgram
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
