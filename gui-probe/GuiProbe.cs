using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Harness98GuiProbe
{
    public sealed class GuiProbe
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ProbeForm());
        }
    }

    public sealed class ProbeForm : Form
    {
        private readonly ComboBox modelBox;
        private readonly RichTextBox transcript;
        private readonly TextBox prompt;
        private readonly Button sendButton;
        private readonly StatusBar status;
        private readonly BackgroundWorker worker;

        public ProbeForm()
        {
            Text = "Harness98 3.1.0 GUI Probe";
            ClientSize = new Size(620, 440);
            MinimumSize = new Size(480, 360);
            StartPosition = FormStartPosition.CenterScreen;

            Label modelLabel = new Label();
            modelLabel.Text = "Model:";
            modelLabel.Location = new Point(8, 12);
            modelLabel.AutoSize = true;
            Controls.Add(modelLabel);

            modelBox = new ComboBox();
            modelBox.Location = new Point(58, 8);
            modelBox.Size = new Size(554, 21);
            modelBox.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            modelBox.DropDownStyle = ComboBoxStyle.DropDown;
            modelBox.Items.Add("OpenRouter Auto");
            modelBox.Items.Add("A small model");
            modelBox.Items.Add("A larger model");
            modelBox.SelectedIndex = 0;
            Controls.Add(modelBox);

            transcript = new RichTextBox();
            transcript.Location = new Point(8, 38);
            transcript.Size = new Size(604, 286);
            transcript.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            transcript.ReadOnly = true;
            transcript.HideSelection = false;
            transcript.DetectUrls = false;
            transcript.Text = "Harness98 GUI compatibility probe\r\n\r\n" +
                "This area should scroll, allow text selection, and retain " +
                "the conversation. Type below and press Send or Ctrl+Enter.";
            Controls.Add(transcript);

            prompt = new TextBox();
            prompt.Location = new Point(8, 332);
            prompt.Size = new Size(508, 74);
            prompt.Anchor = AnchorStyles.Bottom | AnchorStyles.Left |
                AnchorStyles.Right;
            prompt.Multiline = true;
            prompt.ScrollBars = ScrollBars.Vertical;
            prompt.AcceptsReturn = true;
            prompt.KeyDown += new KeyEventHandler(PromptKeyDown);
            Controls.Add(prompt);

            sendButton = new Button();
            sendButton.Text = "Send";
            sendButton.Location = new Point(524, 332);
            sendButton.Size = new Size(88, 30);
            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sendButton.Click += new EventHandler(SendClicked);
            Controls.Add(sendButton);

            Button clearButton = new Button();
            clearButton.Text = "Clear";
            clearButton.Location = new Point(524, 370);
            clearButton.Size = new Size(88, 30);
            clearButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clearButton.Click += new EventHandler(ClearClicked);
            Controls.Add(clearButton);

            status = new StatusBar();
            status.Text = "Ready";
            status.ShowPanels = false;
            Controls.Add(status);

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(WorkerDoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                WorkerCompleted);

            AcceptButton = sendButton;
            prompt.Focus();
        }

        private void PromptKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BeginSend();
            }
        }

        private void SendClicked(object sender, EventArgs e)
        {
            BeginSend();
        }

        private void BeginSend()
        {
            string text = prompt.Text.Trim();
            if (text.Length == 0 || worker.IsBusy) return;

            AppendLine("\r\n\r\nYou> " + text);
            prompt.Clear();
            prompt.Enabled = false;
            sendButton.Enabled = false;
            modelBox.Enabled = false;
            status.Text = "Waiting for simulated model response...";
            worker.RunWorkerAsync(text);
        }

        private static void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1500);
            e.Result = "The background worker received: " + (string)e.Argument;
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                AppendLine("\r\n\r\nAssistant> " + (string)e.Result);
                status.Text = "Ready";
            }
            else
            {
                AppendLine("\r\n\r\nERROR: " + e.Error.Message);
                status.Text = "Error";
            }
            prompt.Enabled = true;
            sendButton.Enabled = true;
            modelBox.Enabled = true;
            prompt.Focus();
        }

        private void ClearClicked(object sender, EventArgs e)
        {
            transcript.Clear();
            status.Text = "Transcript cleared";
        }

        private void AppendLine(string text)
        {
            transcript.AppendText(text);
            transcript.SelectionStart = transcript.TextLength;
            transcript.ScrollToCaret();
        }
    }
}
