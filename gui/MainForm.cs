using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class MainForm : Form
    {
        private readonly HarnessCore core;
        private readonly ComboBox conversations;
        private readonly TextBox modelName;
        private readonly RichTextBox transcript;
        private readonly TextBox prompt;
        private readonly Button sendButton;
        private readonly Button newButton;
        private readonly Button modelButton;
        private readonly StatusBar status;
        private readonly BackgroundWorker startupWorker;
        private readonly BackgroundWorker chatWorker;
        private readonly BackgroundWorker updateWorker;
        private bool changingConversation;
        private Conversation activeConversation;
        private ModelInfo activeModel;

        public MainForm()
        {
            Text = "Harness98 3.1.0";
            ClientSize = new Size(720, 520);
            MinimumSize = new Size(560, 400);
            StartPosition = FormStartPosition.CenterScreen;
            core = new HarnessCore(AppDomain.CurrentDomain.BaseDirectory);

            MainMenu menu = new MainMenu();
            MenuItem conversationMenu = new MenuItem("&Conversation");
            conversationMenu.MenuItems.Add(new MenuItem("&New", NewConversation));
            conversationMenu.MenuItems.Add(new MenuItem("Choose &model...", ChooseModel));
            conversationMenu.MenuItems.Add("-");
            conversationMenu.MenuItems.Add(new MenuItem("E&xit", ExitClicked));
            menu.MenuItems.Add(conversationMenu);
            MenuItem toolsMenu = new MenuItem("&Tools");
            toolsMenu.MenuItems.Add(new MenuItem("&Settings...", ShowSettings));
            toolsMenu.MenuItems.Add(new MenuItem("Check for &updates", CheckUpdates));
            menu.MenuItems.Add(toolsMenu);
            MenuItem helpMenu = new MenuItem("&Help");
            helpMenu.MenuItems.Add(new MenuItem("&About", ShowAbout));
            menu.MenuItems.Add(helpMenu);
            Menu = menu;

            Label conversationLabel = MakeLabel("Conversation:", 8, 13);
            Controls.Add(conversationLabel);
            conversations = new ComboBox();
            conversations.Location = new Point(92, 9);
            conversations.Size = new Size(300, 21);
            conversations.DropDownStyle = ComboBoxStyle.DropDownList;
            conversations.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            conversations.SelectedIndexChanged += new EventHandler(
                ConversationChanged);
            Controls.Add(conversations);

            newButton = new Button();
            newButton.Text = "New...";
            newButton.Location = new Point(400, 7);
            newButton.Size = new Size(72, 25);
            newButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            newButton.Click += new EventHandler(NewConversation);
            Controls.Add(newButton);

            Label modelLabel = MakeLabel("Model:", 8, 45);
            Controls.Add(modelLabel);
            modelName = new TextBox();
            modelName.Location = new Point(58, 41);
            modelName.Size = new Size(558, 20);
            modelName.ReadOnly = true;
            modelName.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            Controls.Add(modelName);

            modelButton = new Button();
            modelButton.Text = "Choose...";
            modelButton.Location = new Point(624, 39);
            modelButton.Size = new Size(88, 25);
            modelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            modelButton.Click += new EventHandler(ChooseModel);
            Controls.Add(modelButton);

            transcript = new RichTextBox();
            transcript.Location = new Point(8, 72);
            transcript.Size = new Size(704, 326);
            transcript.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            transcript.ReadOnly = true;
            transcript.HideSelection = false;
            transcript.DetectUrls = false;
            Controls.Add(transcript);

            prompt = new TextBox();
            prompt.Location = new Point(8, 406);
            prompt.Size = new Size(608, 74);
            prompt.Anchor = AnchorStyles.Bottom | AnchorStyles.Left |
                AnchorStyles.Right;
            prompt.Multiline = true;
            prompt.AcceptsReturn = true;
            prompt.ScrollBars = ScrollBars.Vertical;
            prompt.KeyDown += new KeyEventHandler(PromptKeyDown);
            Controls.Add(prompt);

            sendButton = new Button();
            sendButton.Text = "Send";
            sendButton.Location = new Point(624, 406);
            sendButton.Size = new Size(88, 30);
            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sendButton.Click += new EventHandler(SendClicked);
            Controls.Add(sendButton);

            Label sendHint = MakeLabel("Ctrl+Enter", 637, 445);
            sendHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Controls.Add(sendHint);

            status = new StatusBar();
            status.Text = "Starting Harness98...";
            Controls.Add(status);

            startupWorker = new BackgroundWorker();
            startupWorker.DoWork += new DoWorkEventHandler(StartupDoWork);
            startupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                StartupCompleted);
            chatWorker = new BackgroundWorker();
            chatWorker.DoWork += new DoWorkEventHandler(ChatDoWork);
            chatWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                ChatCompleted);
            updateWorker = new BackgroundWorker();
            updateWorker.DoWork += new DoWorkEventHandler(UpdateDoWork);
            updateWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                UpdateCompleted);

            SetInteractive(false);
            Shown += new EventHandler(FormShown);
            FormClosing += new FormClosingEventHandler(FormIsClosing);
            FormClosed += new FormClosedEventHandler(FormWasClosed);
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.AutoSize = true;
            return label;
        }

        private void FormShown(object sender, EventArgs e)
        {
            string key = core.Credentials.LoadKey();
            if (key == null)
            {
                KeyDialog dialog = new KeyDialog();
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    dialog.Dispose();
                    Close();
                    return;
                }
                key = dialog.ApiKey;
                dialog.Dispose();
                core.Credentials.SaveKey(key);
            }
            try
            {
                core.Connect(key);
                status.Text = "Loading OpenRouter models...";
                startupWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ShowFatal(ex);
            }
        }

        private void StartupDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = core.LoadModels();
        }

        private void StartupCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ShowFatal(e.Error);
                return;
            }
            RefreshConversationList();
            Conversation recent = core.Conversations.MostRecent();
            if (recent != null)
            {
                OpenConversation(recent);
            }
            else if (!CreateNewConversation())
            {
                Close();
                return;
            }
            SetInteractive(true);
            status.Text = "Ready - " + core.Models.Count.ToString() +
                " models loaded";
            prompt.Focus();
        }

        private void NewConversation(object sender, EventArgs e)
        {
            if (IsBusy()) return;
            CreateNewConversation();
        }

        private bool CreateNewConversation()
        {
            ModelPickerDialog picker = new ModelPickerDialog(core.Models, activeModel);
            if (picker.ShowDialog(this) != DialogResult.OK)
            {
                picker.Dispose();
                return false;
            }
            activeModel = picker.SelectedModel;
            picker.Dispose();
            activeConversation = core.NewConversation(activeModel.Id);
            RefreshConversationList();
            SelectConversationInList(activeConversation.Id);
            RenderConversation();
            return true;
        }

        private void ConversationChanged(object sender, EventArgs e)
        {
            if (changingConversation || conversations.SelectedItem == null) return;
            ConversationListItem item =
                (ConversationListItem)conversations.SelectedItem;
            try
            {
                OpenConversation(core.Conversations.Load(item.Id));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not open conversation",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenConversation(Conversation conversation)
        {
            ModelInfo model = core.FindModel(conversation.ModelId);
            if (model == null)
            {
                MessageBox.Show(this, "The saved model is no longer available. " +
                    "Please choose a replacement.", "Harness98",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ModelPickerDialog picker = new ModelPickerDialog(core.Models, null);
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    picker.Dispose();
                    return;
                }
                model = picker.SelectedModel;
                picker.Dispose();
                conversation.ModelId = model.Id;
                core.Conversations.Save(conversation);
            }
            activeConversation = conversation;
            activeModel = model;
            SelectConversationInList(conversation.Id);
            RenderConversation();
        }

        private void RefreshConversationList()
        {
            string selectedId = activeConversation == null ? null :
                activeConversation.Id;
            changingConversation = true;
            conversations.Items.Clear();
            ArrayList saved = core.Conversations.List();
            for (int i = 0; i < saved.Count; i++)
                conversations.Items.Add(new ConversationListItem(
                    (Conversation)saved[i]));
            changingConversation = false;
            if (selectedId != null) SelectConversationInList(selectedId);
        }

        private void SelectConversationInList(string id)
        {
            changingConversation = true;
            for (int i = 0; i < conversations.Items.Count; i++)
            {
                ConversationListItem item =
                    (ConversationListItem)conversations.Items[i];
                if (item.Id == id)
                {
                    conversations.SelectedIndex = i;
                    break;
                }
            }
            changingConversation = false;
        }

        private void ChooseModel(object sender, EventArgs e)
        {
            if (core.Models == null || IsBusy()) return;
            ModelPickerDialog picker = new ModelPickerDialog(core.Models, activeModel);
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                activeModel = picker.SelectedModel;
                if (activeConversation != null)
                {
                    activeConversation.ModelId = activeModel.Id;
                    core.Conversations.Save(activeConversation);
                }
                RenderConversation();
            }
            picker.Dispose();
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
            if (text.Length == 0 || chatWorker.IsBusy || activeConversation == null)
                return;
            ChatWork work = new ChatWork();
            work.Conversation = activeConversation;
            work.Model = activeModel;
            work.Text = text;
            AppendTranscript("\r\n\r\nYou> " + text);
            prompt.Clear();
            SetInteractive(false);
            status.Text = "Waiting for " + activeModel.Name + "...";
            chatWorker.RunWorkerAsync(work);
        }

        private void ChatDoWork(object sender, DoWorkEventArgs e)
        {
            ChatWork work = (ChatWork)e.Argument;
            work.Result = core.SendMessage(work.Conversation, work.Model, work.Text);
            e.Result = work;
        }

        private void ChatCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetInteractive(true);
            if (e.Error != null)
            {
                status.Text = "Request failed";
                MessageBox.Show(this, e.Error.Message, "OpenRouter request failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                RenderConversation();
                return;
            }
            ChatWork work = (ChatWork)e.Result;
            activeConversation = work.Conversation;
            RenderConversation();
            RefreshConversationList();
            status.Text = work.Result.Warning == null ? "Ready" : work.Result.Warning;
            prompt.Focus();
        }

        private void RenderConversation()
        {
            transcript.Clear();
            if (activeConversation == null) return;
            for (int i = 0; i < activeConversation.Messages.Count; i++)
            {
                ChatMessage message =
                    (ChatMessage)activeConversation.Messages[i];
                if (i > 0) transcript.AppendText("\r\n\r\n");
                string role = message.Role == "user" ? "You" :
                    (message.Role == "assistant" ? "Assistant" : message.Role);
                transcript.AppendText(role + "> " + message.Content);
            }
            transcript.SelectionStart = transcript.TextLength;
            transcript.ScrollToCaret();
            modelName.Text = activeModel == null ? "" :
                activeModel.Name + "  (" + activeModel.Id + ")";
            Text = "Harness98 3.1.0 - " + activeConversation.Title;
        }

        private void AppendTranscript(string text)
        {
            transcript.AppendText(text);
            transcript.SelectionStart = transcript.TextLength;
            transcript.ScrollToCaret();
        }

        private void ShowSettings(object sender, EventArgs e)
        {
            if (core.Models == null || IsBusy()) return;
            SettingsDialog dialog = new SettingsDialog(core);
            dialog.ShowDialog(this);
            dialog.Dispose();
        }

        private void CheckUpdates(object sender, EventArgs e)
        {
            if (IsBusy()) return;
            SetInteractive(false);
            status.Text = "Checking for updates...";
            updateWorker.RunWorkerAsync();
        }

        private void UpdateDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = core.Updates.CheckAndStage();
        }

        private void UpdateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetInteractive(true);
            status.Text = "Ready";
            string message = e.Error == null ? (string)e.Result : e.Error.Message;
            MessageBox.Show(this, message, e.Error == null ? "Harness98 update" :
                "Update check failed", MessageBoxButtons.OK, e.Error == null ?
                MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private void SetInteractive(bool enabled)
        {
            conversations.Enabled = enabled;
            newButton.Enabled = enabled;
            modelButton.Enabled = enabled;
            prompt.Enabled = enabled;
            sendButton.Enabled = enabled;
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Harness98 3.1.0\r\nWindows 98 AI harness\r\n" +
                "GUI and CLI share the same core and conversation files.",
                "About Harness98", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitClicked(object sender, EventArgs e)
        {
            Close();
        }

        private void ShowFatal(Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Harness98 could not start",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }

        private void FormWasClosed(object sender, FormClosedEventArgs e)
        {
            core.Dispose();
        }

        private void FormIsClosing(object sender, FormClosingEventArgs e)
        {
            if (!IsBusy()) return;
            e.Cancel = true;
            MessageBox.Show(this, "Please wait for the current operation to finish.",
                "Harness98", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool IsBusy()
        {
            return startupWorker.IsBusy || chatWorker.IsBusy || updateWorker.IsBusy;
        }

        private sealed class ChatWork
        {
            public Conversation Conversation;
            public ModelInfo Model;
            public string Text;
            public ChatResult Result;
        }

        private sealed class ConversationListItem
        {
            public readonly string Id;
            private readonly string text;

            public ConversationListItem(Conversation conversation)
            {
                Id = conversation.Id;
                text = conversation.Id + " - " + conversation.Title;
            }

            public override string ToString()
            {
                return text;
            }
        }
    }
}
