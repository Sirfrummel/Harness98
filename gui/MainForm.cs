using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class MainForm : Form
    {
        private const int WmVScroll = 0x0115;
        private const int SbBottom = 7;
        private static readonly Color UserColor = Color.FromArgb(30, 60, 125);
        private static readonly Color ToolRequestColor = Color.FromArgb(105, 55, 125);
        private static readonly Color ToolResultColor = Color.FromArgb(25, 100, 105);
        private static readonly Color ErrorColor = Color.FromArgb(160, 35, 35);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message,
            IntPtr parameter, IntPtr data);

        private readonly HarnessCore core;
        private readonly ListBox conversations;
        private readonly SplitContainer mainSplit;
        private readonly TextBox modelName;
        private readonly RichTextBox transcript;
        private readonly TextBox prompt;
        private readonly Button sendButton;
        private readonly Button newButton;
        private readonly Button modelButton;
        private readonly Button hideChatsButton;
        private readonly Button showChatsButton;
        private readonly Panel costToolbar;
        private readonly Label costLabel;
        private readonly MenuItem costToolbarMenu;
        private readonly StatusBar status;
        private readonly BackgroundWorker startupWorker;
        private readonly BackgroundWorker chatWorker;
        private readonly BackgroundWorker updateWorker;
        private bool changingConversation;
        private Conversation activeConversation;
        private ModelInfo activeModel;
        private long sessionPromptTokens;
        private long sessionCompletionTokens;
        private long sessionTotalTokens;
        private double sessionCost;
        private bool costWarningAcknowledged;

        public MainForm()
        {
            Text = "Harness98 " + VersionInfo.Current;
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
            MenuItem viewMenu = new MenuItem("&View");
            viewMenu.MenuItems.Add(new MenuItem("Toggle &conversations",
                ToggleConversations));
            costToolbarMenu = new MenuItem("&Cost Toolbar", ToggleCostToolbar);
            viewMenu.MenuItems.Add(costToolbarMenu);
            menu.MenuItems.Add(viewMenu);
            MenuItem helpMenu = new MenuItem("&Help");
            helpMenu.MenuItems.Add(new MenuItem("&About", ShowAbout));
            menu.MenuItems.Add(helpMenu);
            Menu = menu;

            status = new StatusBar();
            status.Text = "Starting Harness98...";
            Controls.Add(status);

            mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            Controls.Add(mainSplit);
            status.BringToFront();

            mainSplit.Panel1MinSize = 160;
            mainSplit.Panel2MinSize = 350;
            mainSplit.SplitterDistance = 210;

            Label conversationLabel = MakeLabel("Conversations", 8, 13);
            conversationLabel.Font = new Font(conversationLabel.Font, FontStyle.Bold);
            mainSplit.Panel1.Controls.Add(conversationLabel);

            conversations = new ListBox();
            conversations.Location = new Point(8, 42);
            conversations.Size = new Size(194, 420);
            conversations.IntegralHeight = false;
            conversations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            conversations.SelectedIndexChanged += new EventHandler(
                ConversationChanged);
            mainSplit.Panel1.Controls.Add(conversations);

            newButton = new Button();
            newButton.Text = "New";
            newButton.Location = new Point(98, 7);
            newButton.Size = new Size(50, 27);
            newButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            newButton.Click += new EventHandler(NewConversation);
            mainSplit.Panel1.Controls.Add(newButton);

            hideChatsButton = new Button();
            hideChatsButton.Text = "<<";
            hideChatsButton.Location = new Point(154, 7);
            hideChatsButton.Size = new Size(48, 27);
            hideChatsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            hideChatsButton.Click += new EventHandler(ToggleConversations);
            mainSplit.Panel1.Controls.Add(hideChatsButton);

            showChatsButton = new Button();
            showChatsButton.Text = "Chats >>";
            showChatsButton.Location = new Point(8, 7);
            showChatsButton.Size = new Size(72, 27);
            showChatsButton.Click += new EventHandler(ToggleConversations);
            mainSplit.Panel2.Controls.Add(showChatsButton);
            UpdateConversationToggleButtons();

            Label modelLabel = MakeLabel("Model:", 88, 13);
            mainSplit.Panel2.Controls.Add(modelLabel);
            modelName = new TextBox();
            modelName.Location = new Point(138, 9);
            modelName.Size = new Size(257, 20);
            modelName.ReadOnly = true;
            modelName.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            mainSplit.Panel2.Controls.Add(modelName);

            modelButton = new Button();
            modelButton.Text = "Choose...";
            modelButton.Location = new Point(403, 7);
            modelButton.Size = new Size(88, 25);
            modelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            modelButton.Click += new EventHandler(ChooseModel);
            mainSplit.Panel2.Controls.Add(modelButton);

            costToolbar = new Panel();
            costToolbar.Location = new Point(8, 39);
            costToolbar.Size = new Size(483, 25);
            costToolbar.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            costToolbar.BorderStyle = BorderStyle.FixedSingle;
            costToolbar.Visible = false;
            mainSplit.Panel2.Controls.Add(costToolbar);

            costLabel = new Label();
            costLabel.Location = new Point(7, 5);
            costLabel.AutoSize = true;
            costToolbar.Controls.Add(costLabel);
            UpdateCostToolbar();

            transcript = new RichTextBox();
            transcript.Location = new Point(8, 40);
            transcript.Size = new Size(483, 382);
            transcript.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            transcript.ReadOnly = true;
            transcript.HideSelection = false;
            transcript.DetectUrls = false;
            mainSplit.Panel2.Controls.Add(transcript);

            prompt = new TextBox();
            prompt.Location = new Point(8, 430);
            prompt.Size = new Size(387, 54);
            prompt.Anchor = AnchorStyles.Bottom | AnchorStyles.Left |
                AnchorStyles.Right;
            prompt.Multiline = true;
            prompt.AcceptsReturn = true;
            prompt.ScrollBars = ScrollBars.Vertical;
            prompt.KeyDown += new KeyEventHandler(PromptKeyDown);
            mainSplit.Panel2.Controls.Add(prompt);

            sendButton = new Button();
            sendButton.Text = "Send";
            sendButton.Location = new Point(403, 430);
            sendButton.Size = new Size(88, 30);
            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sendButton.Click += new EventHandler(SendClicked);
            mainSplit.Panel2.Controls.Add(sendButton);

            Label sendHint = MakeLabel("Shift+Enter: new line", 385, 466);
            sendHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            mainSplit.Panel2.Controls.Add(sendHint);

            startupWorker = new BackgroundWorker();
            startupWorker.DoWork += new DoWorkEventHandler(StartupDoWork);
            startupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                StartupCompleted);
            chatWorker = new BackgroundWorker();
            chatWorker.WorkerReportsProgress = true;
            chatWorker.DoWork += new DoWorkEventHandler(ChatDoWork);
            chatWorker.ProgressChanged += new ProgressChangedEventHandler(
                ChatProgressChanged);
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
            status.Text = "Ready";
            prompt.Focus();
        }

        private void NewConversation(object sender, EventArgs e)
        {
            if (IsBusy()) return;
            CreateNewConversation();
        }

        private bool CreateNewConversation()
        {
            if (activeModel == null)
            {
                ModelPickerDialog picker = new ModelPickerDialog(core.Models, null);
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    picker.Dispose();
                    return false;
                }
                activeModel = picker.SelectedModel;
                picker.Dispose();
            }
            activeConversation = core.NewConversation(activeModel.Id);
            RefreshConversationList();
            RenderConversation();
            prompt.Focus();
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
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.Handled = true;
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
            if (core.Configuration.CostWarningEnabled &&
                !costWarningAcknowledged &&
                sessionCost >= core.Configuration.CostWarningAmount &&
                !ShowCostWarning(sessionCost)) return;
            ChatWork work = new ChatWork();
            work.Conversation = activeConversation;
            work.Model = activeModel;
            work.Text = text;
            work.SessionCostBeforeRun = sessionCost;
            work.CostWarningEnabled = core.Configuration.CostWarningEnabled &&
                !costWarningAcknowledged;
            work.CostWarningAmount = core.Configuration.CostWarningAmount;
            AppendPendingUser(text);
            prompt.Clear();
            SetInteractive(false);
            status.Text = "Waiting for " + activeModel.Name + "...";
            chatWorker.RunWorkerAsync(work);
        }

        private void ChatDoWork(object sender, DoWorkEventArgs e)
        {
            ChatWork work = (ChatWork)e.Argument;
            BackgroundWorker worker = (BackgroundWorker)sender;
            work.Result = core.SendMessage(work.Conversation, work.Model, work.Text,
                new BackgroundAgentProgressSink(this, worker, work));
            e.Result = work;
        }

        private void ChatProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            AgentProgress progress = (AgentProgress)e.UserState;
            if (progress.Type == AgentProgressType.UsageReceived)
            {
                sessionPromptTokens += progress.PromptTokens;
                sessionCompletionTokens += progress.CompletionTokens;
                sessionTotalTokens += progress.TotalTokens;
                sessionCost += progress.Cost;
                UpdateCostToolbar();
                return;
            }
            if (progress.Type == AgentProgressType.ModelRequestStarted)
            {
                status.Text = "Waiting for " + activeModel.Name + " (round " +
                    progress.Iteration.ToString() + ")...";
                return;
            }
            if (progress.Type == AgentProgressType.ToolStarted)
            {
                string command = DescribeToolCall(progress.ToolCall);
                status.Text = String.Compare(progress.ToolCall.Name,
                    "run_command", true) == 0 ? "Running command..." :
                    "Running " + progress.ToolCall.Name + "...";
                AppendLiveCommand(command);
                return;
            }
            if (progress.Type == AgentProgressType.ToolCompleted)
            {
                ChatMessage message = new ChatMessage("tool", progress.ToolResult);
                message.ToolName = progress.ToolCall.Name;
                status.Text = "Returning tool result to the model...";
                AppendLiveToolResult(message);
            }
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
                RefreshConversationList();
                return;
            }
            ChatWork work = (ChatWork)e.Result;
            activeConversation = work.Conversation;
            sessionPromptTokens += Math.Max(0, work.Result.PromptTokens -
                work.ReportedPromptTokens);
            sessionCompletionTokens += Math.Max(0, work.Result.CompletionTokens -
                work.ReportedCompletionTokens);
            sessionTotalTokens += Math.Max(0, work.Result.TotalTokens -
                work.ReportedTotalTokens);
            sessionCost += Math.Max(0, work.Result.Cost - work.ReportedCost);
            UpdateCostToolbar();
            RenderConversation();
            RefreshConversationList();
            status.Text = work.Result.Warning == null ? "Ready" : work.Result.Warning;
            prompt.Focus();
        }

        private void RenderConversation()
        {
            transcript.Clear();
            if (activeConversation == null) return;
            StringBuilder rtf = new StringBuilder();
            rtf.Append("{\\rtf1\\ansi\\deff0");
            rtf.Append("{\\fonttbl{\\f0\\fnil MS Sans Serif;}}");
            rtf.Append("{\\colortbl;\\red30\\green60\\blue125;");
            rtf.Append("\\red55\\green105\\blue60;");
            rtf.Append("\\red105\\green55\\blue125;");
            rtf.Append("\\red25\\green100\\blue105;");
            rtf.Append("\\red160\\green35\\blue35;}");
            rtf.Append("\\viewkind4\\uc1\\f0\\fs18 ");
            for (int i = 0; i < activeConversation.Messages.Count; i++)
            {
                ChatMessage message =
                    (ChatMessage)activeConversation.Messages[i];
                AppendRtfMessage(rtf, message);
            }
            rtf.Append('}');
            transcript.Rtf = rtf.ToString();
            ScrollTranscriptToEnd();
            modelName.Text = activeModel == null ? "" :
                activeModel.Name + "  (" + activeModel.Id + ")";
            string title = activeConversation.Title.Length == 0 ?
                "(new conversation)" : activeConversation.Title;
            Text = "Harness98 " + VersionInfo.Current + " - " + title;
        }

        private static void AppendRtfMessage(StringBuilder rtf,
            ChatMessage message)
        {
            if (message.Role == "user")
            {
                rtf.Append("\\pard\\li0\\ri180\\sb60\\sa160\\cf1\\b >\\b0  ");
                rtf.Append(RtfEncode(message.Content));
                rtf.Append("\\cf0\\par ");
                return;
            }

            if (message.Role == "tool")
            {
                rtf.Append("\\pard\\li360\\ri110\\sb0\\sa140\\cf");
                rtf.Append(ToolResultFailed(message) ? "5 " : "4 ");
                rtf.Append(RtfEncode(CompactToolResult(message)));
                rtf.Append("\\cf0\\par ");
                return;
            }

            if (message.ToolCalls.Count > 0)
            {
                if (message.Content != null && message.Content.Length > 0)
                {
                    AppendRtfAssistant(rtf, message.Content);
                }
                for (int i = 0; i < message.ToolCalls.Count; i++)
                {
                    ToolCall call = (ToolCall)message.ToolCalls[i];
                    rtf.Append("\\pard\\li110\\ri110\\sb80\\sa20\\cf3 *");
                    rtf.Append("\\b Ran\\b0  ");
                    rtf.Append(RtfEncode(DescribeToolCall(call)));
                    rtf.Append("\\cf0\\par ");
                }
                return;
            }

            AppendRtfAssistant(rtf, message.Content);
        }

        private static void AppendRtfAssistant(StringBuilder rtf, string text)
        {
            rtf.Append("\\pard\\li0\\ri0\\sb80\\sa180");
            rtf.Append("\\brdrt\\brdrs\\brdrw10\\brdrcf2");
            rtf.Append("\\brdrl\\brdrs\\brdrw10\\brdrcf2");
            rtf.Append("\\brdrb\\brdrs\\brdrw10\\brdrcf2");
            rtf.Append("\\brdrr\\brdrs\\brdrw10\\brdrcf2");
            rtf.Append("\\cf2\\b :\\b0\\cf0  ");
            rtf.Append(RtfEncode(text));
            rtf.Append("\\par ");
        }

        private static string DescribeToolCall(ToolCall call)
        {
            try
            {
                Hashtable arguments = Json.AsObject(Json.Parse(call.Arguments));
                string command = Json.GetString(arguments, "command");
                if (command != null) return command;
                string path = Json.GetString(arguments, "path");
                if (path != null) return call.Name + " " + path;
                return call.Name + " " + call.Arguments;
            }
            catch
            {
                return call.Name + " " + call.Arguments;
            }
        }

        private static string CompactToolResult(ChatMessage message)
        {
            try
            {
                Hashtable result = Json.AsObject(Json.Parse(message.Content));
                if (result == null) return message.Content;
                string error = Json.GetString(result, "error");
                if (error != null) return CompactLines("Error: " + error);
                if (String.Compare(message.ToolName, "read_file", true) == 0)
                {
                    string content = Json.GetString(result, "content");
                    if (content == null) content = "(no text returned)";
                    if (result["truncated"] is bool && (bool)result["truncated"])
                    {
                        long next = Json.GetInt64(result, "next_start_line");
                        content += next > 0 ? "\r\n[More content starts at line " +
                            next.ToString() + "]" :
                            "\r\n[One line exceeded the display/read limit]";
                    }
                    return CompactLines(content);
                }
                string toolMessage = Json.GetString(result, "message");
                if (toolMessage != null) return CompactLines(toolMessage);
                StringBuilder text = new StringBuilder();
                string output = Json.GetString(result, "stdout");
                string errors = Json.GetString(result, "stderr");
                if (output != null && output.Length > 0)
                    text.Append(output.TrimEnd());
                if (errors != null && errors.Length > 0)
                {
                    if (text.Length > 0) text.Append("\r\n");
                    text.Append("[stderr] ").Append(errors.TrimEnd());
                }
                long exitCode = Json.GetInt64(result, "exit_code");
                if (exitCode != 0)
                {
                    if (text.Length > 0) text.Append("\r\n");
                    text.Append("Exit code: ").Append(exitCode.ToString());
                }
                if (result["timed_out"] is bool && (bool)result["timed_out"])
                    text.Append(text.Length > 0 ? "\r\n[Timed out]" : "[Timed out]");
                if (result["output_truncated"] is bool &&
                    (bool)result["output_truncated"])
                    text.Append(text.Length > 0 ? "\r\n[Output was truncated]" :
                        "[Output was truncated]");
                if (text.Length == 0) text.Append("(no output)");
                return CompactLines(text.ToString());
            }
            catch
            {
                return CompactLines(message.Content);
            }
        }

        private static string CompactLines(string text)
        {
            if (text == null || text.Length == 0) return "(no output)";
            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split(new char[] { '\n' });
            if (lines.Length <= 4) return String.Join("\r\n", lines);
            return lines[0] + "\r\n" + lines[1] + "\r\n... + " +
                (lines.Length - 4).ToString() + " lines\r\n" +
                lines[lines.Length - 2] + "\r\n" + lines[lines.Length - 1];
        }

        private static bool ToolResultFailed(ChatMessage message)
        {
            try
            {
                Hashtable result = Json.AsObject(Json.Parse(message.Content));
                if (result == null) return false;
                if (Json.GetString(result, "error") != null) return true;
                return Json.GetInt64(result, "exit_code") != 0;
            }
            catch
            {
                return false;
            }
        }

        private void AppendLiveCommand(string command)
        {
            transcript.SelectionStart = transcript.TextLength;
            transcript.SelectionColor = ToolRequestColor;
            transcript.AppendText("\r\n\r\n*");
            transcript.SelectionFont = new Font(transcript.Font, FontStyle.Bold);
            transcript.AppendText("Ran");
            transcript.SelectionFont = transcript.Font;
            transcript.AppendText(" " + command);
            transcript.SelectionColor = transcript.ForeColor;
            ScrollTranscriptToEnd();
        }

        private void AppendLiveToolResult(ChatMessage message)
        {
            transcript.SelectionStart = transcript.TextLength;
            transcript.SelectionIndent = 24;
            transcript.SelectionColor = ToolResultFailed(message) ?
                ErrorColor : ToolResultColor;
            transcript.AppendText("\r\n" + CompactToolResult(message));
            transcript.SelectionIndent = 0;
            transcript.SelectionColor = transcript.ForeColor;
            ScrollTranscriptToEnd();
        }

        private void AppendPendingUser(string text)
        {
            transcript.SelectionStart = transcript.TextLength;
            transcript.SelectionColor = UserColor;
            transcript.SelectionFont = new Font(transcript.Font, FontStyle.Bold);
            transcript.AppendText("\r\n> ");
            transcript.SelectionFont = transcript.Font;
            transcript.SelectionColor = UserColor;
            transcript.AppendText(text);
            transcript.SelectionColor = transcript.ForeColor;
            ScrollTranscriptToEnd();
        }

        private void ScrollTranscriptToEnd()
        {
            transcript.SelectionStart = transcript.TextLength;
            transcript.SelectionLength = 0;
            transcript.ScrollToCaret();
            if (transcript.IsHandleCreated)
                SendMessage(transcript.Handle, WmVScroll,
                    new IntPtr(SbBottom), IntPtr.Zero);
        }

        private static string RtfEncode(string text)
        {
            StringBuilder encoded = new StringBuilder();
            if (text == null) return "";
            for (int i = 0; i < text.Length; i++)
            {
                char value = text[i];
                if (value == '\r') continue;
                if (value == '\n') encoded.Append("\\line ");
                else if (value == '\\' || value == '{' || value == '}')
                    encoded.Append('\\').Append(value);
                else if (value > 127)
                    encoded.Append("\\u").Append(((short)value).ToString()).Append('?');
                else encoded.Append(value);
            }
            return encoded.ToString();
        }

        private void ToggleConversations(object sender, EventArgs e)
        {
            mainSplit.Panel1Collapsed = !mainSplit.Panel1Collapsed;
            UpdateConversationToggleButtons();
        }

        private void UpdateConversationToggleButtons()
        {
            hideChatsButton.Visible = !mainSplit.Panel1Collapsed;
            showChatsButton.Visible = mainSplit.Panel1Collapsed;
        }

        private void ToggleCostToolbar(object sender, EventArgs e)
        {
            int transcriptBottom = transcript.Bottom;
            costToolbar.Visible = !costToolbar.Visible;
            costToolbarMenu.Checked = costToolbar.Visible;
            transcript.Top = costToolbar.Visible ? 70 : 40;
            transcript.Height = Math.Max(40, transcriptBottom - transcript.Top);
        }

        private void UpdateCostToolbar()
        {
            costLabel.Text = "Session cost: $" +
                sessionCost.ToString("0.000000", CultureInfo.InvariantCulture) +
                "   Input: " + sessionPromptTokens.ToString() +
                "   Output: " + sessionCompletionTokens.ToString() +
                "   Total: " + sessionTotalTokens.ToString() + " tokens";
        }

        private void ShowSettings(object sender, EventArgs e)
        {
            if (core.Models == null || IsBusy()) return;
            SettingsDialog dialog = new SettingsDialog(core);
            if (dialog.ShowDialog(this) == DialogResult.OK)
                costWarningAcknowledged = false;
            dialog.Dispose();
        }

        private bool ShowCostWarning(double currentCost)
        {
            CostLimitDialog dialog = new CostLimitDialog(
                core.Configuration.CostWarningAmount, currentCost);
            bool continueRun = dialog.ShowDialog(this) == DialogResult.Yes;
            dialog.Dispose();
            if (continueRun) costWarningAcknowledged = true;
            return continueRun;
        }

        private delegate bool CostWarningCallback(double currentCost);

        private bool ShowCostWarningFromWorker(double currentCost)
        {
            if (InvokeRequired)
                return (bool)Invoke(new CostWarningCallback(
                    ShowCostWarningFromWorker), new object[] { currentCost });
            return ShowCostWarning(currentCost);
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
            if (e.Error != null)
            {
                MessageBox.Show(this, e.Error.Message, "Update check failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string message = (string)e.Result;
            if (!core.Updates.HasStagedUpdate)
            {
                MessageBox.Show(this, message, "Harness98 update",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UpdateReadyDialog dialog = new UpdateReadyDialog(message);
            DialogResult choice = dialog.ShowDialog(this);
            dialog.Dispose();
            if (choice == DialogResult.Yes) RestartForUpdate();
        }

        private void RestartForUpdate()
        {
            try
            {
                string applicationDirectory =
                    AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                string installedHelper = Path.Combine(applicationDirectory,
                    "H98RESTART.EXE");
                if (!File.Exists(installedHelper))
                    throw new FileNotFoundException(
                        "The Harness98 restart helper is missing.", installedHelper);
                string temporaryHelper = Path.Combine(Path.GetTempPath(),
                    "H98RST" + DateTime.UtcNow.Ticks.ToString("x") + ".EXE");
                File.Copy(installedHelper, temporaryHelper, true);

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = temporaryHelper;
                start.Arguments = "\"" + applicationDirectory + "\" " +
                    Process.GetCurrentProcess().Id.ToString();
                start.WorkingDirectory = applicationDirectory;
                start.UseShellExecute = true;
                Process.Start(start);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not restart Harness98",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            MessageBox.Show(this, "Harness98 " + VersionInfo.Current +
                "\r\nWindows 98 AI harness\r\n" +
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
            public long ReportedPromptTokens;
            public long ReportedCompletionTokens;
            public long ReportedTotalTokens;
            public double ReportedCost;
            public double SessionCostBeforeRun;
            public double CostWarningAmount;
            public bool CostWarningEnabled;
            public bool CostWarningHandled;
            public bool StopRequested;
        }

        private sealed class BackgroundAgentProgressSink : IAgentProgressSink,
            IAgentRunControl
        {
            private readonly MainForm owner;
            private readonly BackgroundWorker worker;
            private readonly ChatWork work;

            public BackgroundAgentProgressSink(MainForm mainForm,
                BackgroundWorker backgroundWorker, ChatWork chatWork)
            {
                owner = mainForm;
                worker = backgroundWorker;
                work = chatWork;
            }

            public void Report(AgentProgress progress)
            {
                if (progress.Type == AgentProgressType.UsageReceived)
                {
                    work.ReportedPromptTokens += progress.PromptTokens;
                    work.ReportedCompletionTokens += progress.CompletionTokens;
                    work.ReportedTotalTokens += progress.TotalTokens;
                    work.ReportedCost += progress.Cost;
                }
                worker.ReportProgress(0, progress);
                if (progress.Type == AgentProgressType.UsageReceived &&
                    work.CostWarningEnabled && !work.CostWarningHandled &&
                    work.SessionCostBeforeRun + work.ReportedCost >=
                    work.CostWarningAmount)
                {
                    work.CostWarningHandled = true;
                    if (!owner.ShowCostWarningFromWorker(
                        work.SessionCostBeforeRun + work.ReportedCost))
                        work.StopRequested = true;
                }
            }

            public bool ContinueRun
            {
                get { return !work.StopRequested; }
            }
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
