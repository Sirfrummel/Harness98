using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
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
        private readonly TextBox toolLimitBox;
        private readonly CheckBox costWarning;
        private readonly Label costAmountLabel;
        private readonly TextBox costAmountBox;
        private ModelInfo titleModel;

        public SettingsDialog(HarnessCore harnessCore)
        {
            core = harnessCore;
            models = core.Models;
            Text = "Harness98 Settings";
            ClientSize = new Size(540, 350);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            TabControl tabs = new TabControl();
            tabs.Location = new Point(8, 8);
            tabs.Size = new Size(524, 300);
            Controls.Add(tabs);

            TabPage general = new TabPage("General");
            tabs.TabPages.Add(general);

            Label keyLabel = MakeLabel("OpenRouter API key:", 12, 15);
            general.Controls.Add(keyLabel);
            keyBox = new TextBox();
            keyBox.Location = new Point(12, 35);
            keyBox.Size = new Size(492, 20);
            keyBox.PasswordChar = '*';
            keyBox.Text = core.ApiKey;
            general.Controls.Add(keyBox);

            Label serverLabel = MakeLabel("Update server:", 12, 70);
            general.Controls.Add(serverLabel);
            serverBox = new TextBox();
            serverBox.Location = new Point(12, 90);
            serverBox.Size = new Size(492, 20);
            serverBox.Text = core.Configuration.UpdateServer;
            general.Controls.Add(serverBox);

            GroupBox titles = new GroupBox();
            titles.Text = "Conversation titles";
            titles.Location = new Point(12, 125);
            titles.Size = new Size(492, 120);
            general.Controls.Add(titles);

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
            titleModelBox.Size = new Size(268, 20);
            titleModelBox.ReadOnly = true;
            titles.Controls.Add(titleModelBox);

            Button chooseTitleModel = new Button();
            chooseTitleModel.Text = "Choose...";
            chooseTitleModel.Location = new Point(386, 49);
            chooseTitleModel.Size = new Size(88, 26);
            chooseTitleModel.Click += new EventHandler(ChooseTitleModel);
            titles.Controls.Add(chooseTitleModel);

            Label note = MakeLabel("An automatic title uses one additional model " +
                "request after the first reply.", 12, 84);
            note.Size = new Size(462, 28);
            titles.Controls.Add(note);

            TabPage limits = new TabPage("Limits");
            tabs.TabPages.Add(limits);

            Label toolLabel = MakeLabel("Tool calls per run:", 16, 20);
            limits.Controls.Add(toolLabel);
            toolLimitBox = new TextBox();
            toolLimitBox.Location = new Point(145, 17);
            toolLimitBox.Size = new Size(70, 20);
            toolLimitBox.Text = core.Configuration.ToolCallLimit > 0 ?
                core.Configuration.ToolCallLimit.ToString() : "";
            limits.Controls.Add(toolLimitBox);
            Label toolNote = MakeLabel("Leave blank to use the default of 10.",
                16, 48);
            limits.Controls.Add(toolNote);

            GroupBox costs = new GroupBox();
            costs.Text = "Session costs";
            costs.Location = new Point(16, 82);
            costs.Size = new Size(472, 125);
            limits.Controls.Add(costs);

            costWarning = new CheckBox();
            costWarning.Text = "Warn when this session exceeds a cost amount";
            costWarning.Location = new Point(14, 24);
            costWarning.AutoSize = true;
            costWarning.Checked = core.Configuration.CostWarningEnabled;
            costWarning.CheckedChanged += new EventHandler(CostWarningChanged);
            costs.Controls.Add(costWarning);

            costAmountLabel = MakeLabel("Warn after:  $", 14, 59);
            costs.Controls.Add(costAmountLabel);
            costAmountBox = new TextBox();
            costAmountBox.Location = new Point(105, 56);
            costAmountBox.Size = new Size(85, 20);
            costAmountBox.Text = core.Configuration.CostWarningAmount.ToString(
                "0.######", CultureInfo.InvariantCulture);
            costs.Controls.Add(costAmountBox);
            Label costNote = MakeLabel(
                "Continue accepts the warning for the rest of this app session.",
                14, 88);
            costs.Controls.Add(costNote);

            titleModel = core.FindModel(core.Configuration.TitleModelId);
            UpdateTitleModelText();
            AutoTitleChanged(null, EventArgs.Empty);
            CostWarningChanged(null, EventArgs.Empty);

            Button save = new Button();
            save.Text = "Save";
            save.Location = new Point(372, 315);
            save.Size = new Size(75, 28);
            save.Click += new EventHandler(SaveClicked);
            Controls.Add(save);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.Location = new Point(453, 315);
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

        private void CostWarningChanged(object sender, EventArgs e)
        {
            costAmountLabel.Enabled = costWarning.Checked;
            costAmountBox.Enabled = costWarning.Checked;
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
                int toolLimit = 0;
                if (toolLimitBox.Text.Trim().Length > 0 &&
                    (!Int32.TryParse(toolLimitBox.Text.Trim(), out toolLimit) ||
                    toolLimit < 1 || toolLimit > 100))
                    throw new ApplicationException(
                        "Tool calls per run must be blank or a number from 1 to 100.");

                double costAmount = core.Configuration.CostWarningAmount;
                if (costWarning.Checked || costAmountBox.Text.Trim().Length > 0)
                {
                    if (!Double.TryParse(costAmountBox.Text.Trim(),
                        NumberStyles.Float, CultureInfo.InvariantCulture,
                        out costAmount) || costAmount <= 0)
                        throw new ApplicationException(
                            "The session cost warning must be a positive dollar amount.");
                }

                core.Credentials.SaveKey(keyBox.Text);
                core.ApiKey = keyBox.Text.Trim();
                if (serverBox.Text.Trim().Length == 0)
                    throw new ApplicationException("The update server cannot be empty.");
                core.Configuration.UpdateServer = serverBox.Text.Trim();
                core.Configuration.AutoTitleConversations = autoTitle.Checked;
                core.Configuration.TitleModelId = titleModel == null ? "" :
                    titleModel.Id;
                core.Configuration.ToolCallLimit = toolLimit;
                core.Configuration.CostWarningEnabled = costWarning.Checked;
                core.Configuration.CostWarningAmount = costAmount;
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
