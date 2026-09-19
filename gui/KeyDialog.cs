using System;
using System.Drawing;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class KeyDialog : Form
    {
        private readonly TextBox keyBox;
        public string ApiKey;

        public KeyDialog()
        {
            Text = "OpenRouter API key";
            ClientSize = new Size(470, 125);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            Label label = new Label();
            label.Text = "Enter your OpenRouter API key. It will be stored locally.";
            label.Location = new Point(12, 12);
            label.AutoSize = true;
            Controls.Add(label);

            keyBox = new TextBox();
            keyBox.Location = new Point(12, 38);
            keyBox.Size = new Size(446, 20);
            keyBox.PasswordChar = '*';
            Controls.Add(keyBox);

            Button ok = new Button();
            ok.Text = "Save";
            ok.Location = new Point(302, 78);
            ok.Size = new Size(75, 28);
            ok.Click += new EventHandler(SaveClicked);
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.Location = new Point(383, 78);
            cancel.Size = new Size(75, 28);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void SaveClicked(object sender, EventArgs e)
        {
            if (keyBox.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "Enter an API key first.", "Harness98",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ApiKey = keyBox.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
