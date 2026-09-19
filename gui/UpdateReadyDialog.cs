using System;
using System.Drawing;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class UpdateReadyDialog : Form
    {
        public UpdateReadyDialog(string message)
        {
            Text = "Harness98 update";
            ClientSize = new Size(390, 135);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            Label label = new Label();
            label.Text = message + "\r\n\r\nRestart now to apply it?";
            label.Location = new Point(14, 14);
            label.Size = new Size(362, 72);
            Controls.Add(label);

            Button restart = new Button();
            restart.Text = "Restart now";
            restart.Location = new Point(196, 96);
            restart.Size = new Size(88, 27);
            restart.DialogResult = DialogResult.Yes;
            Controls.Add(restart);

            Button later = new Button();
            later.Text = "Later";
            later.Location = new Point(292, 96);
            later.Size = new Size(84, 27);
            later.DialogResult = DialogResult.Cancel;
            Controls.Add(later);

            AcceptButton = restart;
            CancelButton = later;
        }
    }
}
