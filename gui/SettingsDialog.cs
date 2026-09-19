using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class SettingsDialog : Form
    {
        private readonly HarnessCore core;
        private readonly IList models;
        private readonly TextBox keyBox;
        private readonly TextBox serverBox;
        private readonly CheckBox autoTitle;
        private readonly TextBox titleModelBox;
        private ModelInfo titleModel;

        public SettingsDialog(HarnessCore harnessCore)
        {
            core = harnessCore;
            models = core.Models;
            Text = "Harness98 Settings";
            ClientSize = new Size(540, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            Label keyLabel = MakeLabel("OpenRouter API key:", 12, 15);
            Controls.Add(keyLabel);
            keyBox = new TextBox();
            keyBox.Location = new Point(12, 35);
            keyBox.Size = new Size(516, 20);
            keyBox.PasswordChar = '*';
            keyBox.Text = core.ApiKey;
            Controls.Add(keyBox);

            Label serverLabel = MakeLabel("Update server:", 12, 70);
            Controls.Add(serverLabel);
            serverBox = new TextBox();
            serverBox.Location = new Point(12, 90);
            serverBox.Size = new Size(516, 20);
            serverBox.Text = core.Configuration.UpdateServer;
            Controls.Add(serverBox);

            GroupBox titles = new GroupBox();
            titles.Text = "Conversation titles";
            titles.Location = new Point(12, 125);
            titles.Size = new Size(516, 120);
            Controls.Add(titles);

            autoTitle = new CheckBox();
            autoTitle.Text = "Automatically title new conversations";
            autoTitle.Location = new Point(12, 23);
            autoTitle.AutoSize = true;
            autoTitle.Checked = core.Configuration.AutoTitleConversations;
            autoTitle.CheckedChanged += new EventHandler(AutoTitleChanged);
            titles.Controls.Add(autoTitle);

            Label titleModelLabel = MakeLabel("Use this model:", 12, 55);
            titles.Controls.Add(titleModelLabel);
            titleModelBox = new TextBox();
            titleModelBox.Location = new Point(110, 52);
            titleModelBox.Size = new Size(292, 20);
            titleModelBox.ReadOnly = true;
            titles.Controls.Add(titleModelBox);

            Button chooseTitleModel = new Button();
            chooseTitleModel.Text = "Choose...";
            chooseTitleModel.Location = new Point(410, 49);
            chooseTitleModel.Size = new Size(88, 26);
            chooseTitleModel.Click += new EventHandler(ChooseTitleModel);
            titles.Controls.Add(chooseTitleModel);

            Label note = MakeLabel("An automatic title uses one additional model request " +
                "after the first reply.", 12, 84);
            note.Size = new Size(486, 28);
            titles.Controls.Add(note);

            titleModel = core.FindModel(core.Configuration.TitleModelId);
            UpdateTitleModelText();
            AutoTitleChanged(null, EventArgs.Empty);

            Button save = new Button();
            save.Text = "Save";
            save.Location = new Point(372, 260);
            save.Size = new Size(75, 28);
            save.Click += new EventHandler(SaveClicked);
            Controls.Add(save);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.Location = new Point(453, 260);
            cancel.Size = new Size(75, 28);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.AutoSize = true;
            return label;
        }

        private void AutoTitleChanged(object sender, EventArgs e)
        {
            titleModelBox.Enabled = autoTitle.Checked;
        }

        private void ChooseTitleModel(object sender, EventArgs e)
        {
            ModelPickerDialog picker = new ModelPickerDialog(models, titleModel);
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                titleModel = picker.SelectedModel;
                UpdateTitleModelText();
            }
            picker.Dispose();
        }

        private void UpdateTitleModelText()
        {
            titleModelBox.Text = titleModel == null ?
                "Use the conversation model" : titleModel.Name;
        }

        private void SaveClicked(object sender, EventArgs e)
        {
            try
            {
                core.Credentials.SaveKey(keyBox.Text);
                core.ApiKey = keyBox.Text.Trim();
                if (serverBox.Text.Trim().Length == 0)
                    throw new ApplicationException("The update server cannot be empty.");
                core.Configuration.UpdateServer = serverBox.Text.Trim();
                core.Configuration.AutoTitleConversations = autoTitle.Checked;
                core.Configuration.TitleModelId = titleModel == null ? "" :
                    titleModel.Id;
                core.Configuration.Save();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not save settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
