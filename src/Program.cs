using System;
using System.Collections;

namespace Harness98
{
    public sealed class Program
    {
        public static int Main(string[] args)
        {
#if HARNESS98_V3
            Console.WriteLine("Harness98 " + VersionInfo.Current + " CLI");
#elif LIBCURL_DLL
            Console.WriteLine("Harness98 v2 DLL benchmark");
#else
            Console.WriteLine("Harness98 v1");
#endif
            Console.WriteLine("=========================");
            Console.WriteLine();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            HarnessCore core = null;
            try
            {
                core = new HarnessCore(baseDirectory);
                Settings settings = core.Credentials;
                string apiKey = settings.LoadOrCreateKey();
                core.Connect(apiKey);
                Console.WriteLine("Transport: LibCurl.NET DLL");

                ArrayList models = LoadModels(core, settings, ref apiKey);
                if (models == null)
                {
                    return 1;
                }

                ConversationStore store = core.Conversations;
                ChatSession session = SelectInitialSession(models, store);
                if (session == null)
                {
                    return 0;
                }

                RunChat(core, settings, models, ref apiKey, session);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("FATAL ERROR: " + ex.Message);
                return 1;
            }
            finally
            {
                if (core != null) core.Dispose();
            }
        }

        private static ArrayList LoadModels(HarnessCore core,
            Settings settings, ref string apiKey)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("Loading the OpenRouter model list...");
                    ArrayList models = core.LoadModels();
                    Console.WriteLine("Loaded " + models.Count.ToString() + " models.");
                    return models;
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Could not load models: " + ex.Message);
                    Console.Write("R to retry, K to replace the key, or Q to quit: ");
                    string input = Console.ReadLine();
                    if (input == null || String.Compare(input.Trim(), "q", true) == 0)
                    {
                        return null;
                    }
                    if (String.Compare(input.Trim(), "k", true) == 0)
                    {
                        apiKey = settings.ReplaceKey();
                        core.ApiKey = apiKey;
                    }
                }
            }
        }

        private static ChatSession SelectInitialSession(ArrayList models,
            ConversationStore store)
        {
            ArrayList saved = store.List();
            if (saved.Count == 0)
            {
                return CreateNewSession(models, store);
            }

            Console.WriteLine();
            Console.WriteLine("Found " + saved.Count.ToString() +
                " saved conversation(s).");
            while (true)
            {
                Console.Write("Enter /continue, /resume, /resume ID, /new, or /exit: ");
                string input = Console.ReadLine();
                if (input == null || String.Compare(input.Trim(), "/exit", true) == 0)
                {
                    return null;
                }
                input = input.Trim();
                if (String.Compare(input, "/new", true) == 0)
                {
                    return CreateNewSession(models, store);
                }
                if (String.Compare(input, "/continue", true) == 0)
                {
                    return CreateResumedSession(store.MostRecent(), models, store);
                }
                if (String.Compare(input, "/resume", true) == 0)
                {
                    Conversation selected = ConsoleUi.SelectConversation(saved);
                    if (selected != null)
                    {
                        return CreateResumedSession(selected, models, store);
                    }
                    continue;
                }
                if (input.ToLower().StartsWith("/resume "))
                {
                    try
                    {
                        return CreateResumedSession(store.Load(input.Substring(8)),
                            models, store);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not resume: " + ex.Message);
                    }
                    continue;
                }
                Console.WriteLine("Unknown choice.");
            }
        }

        private static ChatSession CreateNewSession(ArrayList models,
            ConversationStore store)
        {
            ModelInfo model = ConsoleUi.SelectModel(models);
            if (model == null) return null;
            Conversation conversation = store.Create(model.Id);
            return new ChatSession(model, conversation);
        }

        private static ChatSession CreateResumedSession(Conversation conversation,
            ArrayList models, ConversationStore store)
        {
            if (conversation == null) return null;
            ModelInfo model = FindModel(models, conversation.ModelId);
            if (model == null)
            {
                Console.WriteLine("The saved model is no longer in OpenRouter's list:");
                Console.WriteLine(conversation.ModelId);
                Console.WriteLine("Choose a replacement model.");
                model = ConsoleUi.SelectModel(models);
                if (model == null) return null;
                conversation.ModelId = model.Id;
                TrySave(store, conversation);
            }
            return new ChatSession(model, conversation);
        }

        private static ModelInfo FindModel(ArrayList models, string id)
        {
            for (int i = 0; i < models.Count; i++)
            {
                ModelInfo model = (ModelInfo)models[i];
                if (String.Compare(model.Id, id, true) == 0) return model;
            }
            return null;
        }

        private static void RunChat(HarnessCore core, Settings settings,
            ArrayList models, ref string apiKey, ChatSession session)
        {
            ConversationStore store = core.Conversations;
            Console.WriteLine();
            ShowActiveSession(session);
            ConsoleUi.ShowChatHelp();

            while (true)
            {
                Console.WriteLine();
                Console.Write("You> ");
                string input = Console.ReadLine();
                if (input == null) return;
                input = input.Trim();
                if (input.Length == 0) continue;

                if (String.Compare(input, "/exit", true) == 0) return;
                if (String.Compare(input, "/help", true) == 0)
                {
                    ConsoleUi.ShowChatHelp();
                    continue;
                }
                if (String.Compare(input, "/new", true) == 0)
                {
                    ChatSession selected = CreateNewSession(models, store);
                    if (selected != null)
                    {
                        session = selected;
                        ShowActiveSession(session);
                    }
                    continue;
                }
                if (String.Compare(input, "/clear", true) == 0)
                {
                    session = new ChatSession(session.Model,
                        store.Create(session.Model.Id));
                    Console.WriteLine("Previous conversation preserved.");
                    ShowActiveSession(session);
                    continue;
                }
                if (String.Compare(input, "/continue", true) == 0)
                {
                    ChatSession selected = CreateResumedSession(store.MostRecent(),
                        models, store);
                    if (selected == null)
                    {
                        Console.WriteLine("No saved conversations were found.");
                    }
                    else
                    {
                        session = selected;
                        ShowActiveSession(session);
                    }
                    continue;
                }
                if (String.Compare(input, "/resume", true) == 0)
                {
                    Conversation conversation = ConsoleUi.SelectConversation(
                        store.List());
                    ChatSession selected = CreateResumedSession(conversation,
                        models, store);
                    if (selected != null)
                    {
                        session = selected;
                        ShowActiveSession(session);
                    }
                    continue;
                }
                if (input.ToLower().StartsWith("/resume "))
                {
                    try
                    {
                        ChatSession selected = CreateResumedSession(
                            store.Load(input.Substring(8)),
                            models, store);
                        if (selected != null)
                        {
                            session = selected;
                            ShowActiveSession(session);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not resume: " + ex.Message);
                    }
                    continue;
                }
                if (String.Compare(input, "/model", true) == 0)
                {
                    ModelInfo selected = ConsoleUi.SelectModel(models);
                    if (selected != null)
                    {
                        session.Model = selected;
                        session.Conversation.ModelId = selected.Id;
                        TrySave(store, session.Conversation);
                        Console.WriteLine("Selected model: " + selected.Name);
                        Console.WriteLine("Conversation history retained (" +
                            session.Conversation.Count.ToString() + " messages).");
                    }
                    continue;
                }
                if (String.Compare(input, "/key", true) == 0)
                {
                    apiKey = settings.ReplaceKey();
                    core.ApiKey = apiKey;
                    Console.WriteLine("The new key will be used for the next request.");
                    continue;
                }
                if (String.Compare(input, "/update", true) == 0)
                {
                    try
                    {
                        Console.WriteLine("Checking for updates...");
                        Console.WriteLine(core.Updates.CheckAndStage());
                        if (core.Updates.HasStagedUpdate)
                            Console.WriteLine("Exit and launch HARNESS98.EXE to apply it.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("UPDATE CHECK FAILED: " + ex.Message);
                    }
                    continue;
                }
                if (input[0] == '/')
                {
                    Console.WriteLine("Unknown command. Type /help for the list.");
                    continue;
                }

                try
                {
                    Console.WriteLine();
                    Console.WriteLine("Waiting for " + session.Model.Name + "...");
                    ChatResult result = core.SendMessage(session.Conversation,
                        session.Model, input, new ConsoleAgentProgressSink());
                    Console.WriteLine();
                    Console.WriteLine("Assistant> " + result.Answer);
                    if (result.GeneratedTitle != null)
                        Console.WriteLine("Title> " + result.GeneratedTitle);
                    if (result.Warning != null)
                        Console.WriteLine("WARNING: " + result.Warning);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("REQUEST FAILED: " + ex.Message);
                    Console.WriteLine("Your message was not added to the history.");
                }
            }
        }

        private static void ShowActiveSession(ChatSession session)
        {
            Console.WriteLine();
            Console.WriteLine("Conversation: " + session.Conversation.Id + " - " +
                session.Conversation.Title);
            Console.WriteLine("Model: " + session.Model.Name);
            Console.WriteLine(session.Model.Id);
            Console.WriteLine("History: " + session.Conversation.Count.ToString() +
                " messages");
        }

        private static void TrySave(ConversationStore store,
            Conversation conversation)
        {
            if (conversation.Count == 0) return;
            try
            {
                store.Save(conversation);
            }
            catch (Exception ex)
            {
                Console.WriteLine("WARNING: Could not save conversation: " + ex.Message);
            }
        }

        private sealed class ChatSession
        {
            public ModelInfo Model;
            public Conversation Conversation;

            public ChatSession(ModelInfo model, Conversation conversation)
            {
                Model = model;
                Conversation = conversation;
            }
        }

        private sealed class ConsoleAgentProgressSink : IAgentProgressSink
        {
            public void Report(AgentProgress progress)
            {
                if (progress.Type == AgentProgressType.ToolStarted)
                {
                    Console.WriteLine();
                    Console.WriteLine("Command: " + ReadCommand(
                        progress.ToolCall.Arguments));
                }
                else if (progress.Type == AgentProgressType.ToolCompleted)
                {
                    Console.WriteLine(FormatToolResult(progress.ToolResult));
                    Console.WriteLine("Returning command output to the model...");
                }
            }

            private static string ReadCommand(string arguments)
            {
                try
                {
                    Hashtable values = Json.AsObject(Json.Parse(arguments));
                    string command = Json.GetString(values, "command");
                    return command == null ? arguments : command;
                }
                catch
                {
                    return arguments;
                }
            }

            private static string FormatToolResult(string resultText)
            {
                try
                {
                    Hashtable result = Json.AsObject(Json.Parse(resultText));
                    string error = Json.GetString(result, "error");
                    if (error != null) return "Tool error: " + error;
                    string output = Json.GetString(result, "stdout");
                    string errors = Json.GetString(result, "stderr");
                    string text = output == null ? "" : output.TrimEnd();
                    if (errors != null && errors.Length > 0)
                        text += "\r\nError output:\r\n" + errors.TrimEnd();
                    return text + "\r\nExit code: " +
                        Json.GetInt64(result, "exit_code").ToString();
                }
                catch
                {
                    return resultText;
                }
            }
        }
    }
}
