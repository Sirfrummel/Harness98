using System;
using System.Collections;

namespace Harness98
{
    public sealed class HarnessCore : IDisposable
    {
        private readonly string baseDirectory;
        private IHttpTransport transport;
        private OpenRouterClient client;

        public readonly Settings Credentials;
        public readonly AppConfiguration Configuration;
        public readonly ConversationStore Conversations;
        public readonly UpdateManager Updates;
        public string ApiKey;
        public ArrayList Models;

        public HarnessCore(string applicationDirectory)
        {
            baseDirectory = applicationDirectory;
            Credentials = new Settings(baseDirectory);
            Configuration = new AppConfiguration(baseDirectory);
            Configuration.Load();
            Conversations = new ConversationStore(baseDirectory);
            Updates = new UpdateManager(baseDirectory);
        }

        public void Connect(string apiKey)
        {
            if (transport != null) throw new InvalidOperationException(
                "HarnessCore is already connected.");
            ApiKey = apiKey;
#if LIBCURL_DLL
            LibCurlTransport dllTransport = new LibCurlTransport(baseDirectory);
            dllTransport.ValidateDependencies();
            transport = dllTransport;
#else
            CurlTransport processTransport = new CurlTransport(baseDirectory);
            processTransport.ValidateDependencies();
            transport = processTransport;
#endif
            client = new OpenRouterClient(transport);
        }

        public ArrayList LoadModels()
        {
            RequireConnected();
            Models = client.GetModels(ApiKey);
            return Models;
        }

        public ModelInfo FindModel(string id)
        {
            if (Models == null) return null;
            for (int i = 0; i < Models.Count; i++)
            {
                ModelInfo model = (ModelInfo)Models[i];
                if (String.Compare(model.Id, id, true) == 0) return model;
            }
            return null;
        }

        public Conversation NewConversation(string modelId)
        {
            Conversation conversation = Conversations.Create(modelId);
            return conversation;
        }

        public ChatResult SendMessage(Conversation conversation, ModelInfo model,
            string text)
        {
            return SendMessage(conversation, model, text, null);
        }

        public ChatResult SendMessage(Conversation conversation, ModelInfo model,
            string text, IAgentProgressSink progress)
        {
            RequireConnected();
            if (conversation == null || model == null)
                throw new ArgumentNullException("conversation");
            bool firstTurn = conversation.Count == 0;
            int originalCount = conversation.Count;
            conversation.Add("user", text);
            ChatResult result;
            try
            {
                AgentRunner runner = new AgentRunner(client, ApiKey, baseDirectory,
                    progress);
                result = runner.Run(model, conversation);
            }
            catch
            {
                if (conversation.Count <= originalCount + 1)
                {
                    conversation.Truncate(originalCount);
                }
                else
                {
                    conversation.ModelId = model.Id;
                    try { Conversations.Save(conversation); }
                    catch { }
                }
                throw;
            }

            conversation.Add("assistant", result.Answer);
            conversation.ModelId = model.Id;

            try
            {
                Conversations.Save(conversation);
            }
            catch (Exception ex)
            {
                result.Warning = "Conversation could not be saved: " + ex.Message;
            }
            if (firstTurn && Configuration.AutoTitleConversations)
            {
                try
                {
                    result.GeneratedTitle = GenerateTitle(conversation, model,
                        result, progress);
                }
                catch
                {
                    // A title is optional; never discard a successful chat turn.
                }
            }
            return result;
        }

        private string GenerateTitle(Conversation conversation, ModelInfo chatModel,
            ChatResult result, IAgentProgressSink progress)
        {
            ModelInfo titleModel = FindModel(Configuration.TitleModelId);
            if (titleModel == null) titleModel = chatModel;
            ArrayList messages = new ArrayList();
            messages.Add(new ChatMessage("system", "Create a concise plain-text " +
                "title for this conversation. Return only the title, no quotes or " +
                "punctuation wrapper. Maximum 60 characters."));
            ChatMessage first = (ChatMessage)conversation.Messages[0];
            messages.Add(new ChatMessage("user", first.Content));
            ChatCompletion completion = client.SendChatWithUsage(ApiKey,
                titleModel.Id, messages);
            AgentRunner.ApplyCostFallback(completion, titleModel);
            AddUsage(result, completion);
            ReportUsage(progress, completion);
            string title = completion.Answer.Trim();
            title = title.Replace('\r', ' ').Replace('\n', ' ').Trim(' ', '"');
            if (title.Length > 60) title = title.Substring(0, 57) + "...";
            if (title.Length > 0)
            {
                conversation.Title = title;
                Conversations.Save(conversation);
                return title;
            }
            return null;
        }

        private static void AddUsage(ChatResult total, ChatCompletion usage)
        {
            total.PromptTokens += usage.PromptTokens;
            total.CompletionTokens += usage.CompletionTokens;
            total.TotalTokens += usage.TotalTokens;
            total.Cost += usage.Cost;
        }

        private static void ReportUsage(IAgentProgressSink progress,
            ChatCompletion usage)
        {
            if (progress == null) return;
            AgentProgress update = new AgentProgress();
            update.Type = AgentProgressType.UsageReceived;
            update.PromptTokens = usage.PromptTokens;
            update.CompletionTokens = usage.CompletionTokens;
            update.TotalTokens = usage.TotalTokens;
            update.Cost = usage.Cost;
            progress.Report(update);
        }

        public void Dispose()
        {
            IDisposable disposable = transport as IDisposable;
            if (disposable != null) disposable.Dispose();
            transport = null;
            client = null;
        }

        private void RequireConnected()
        {
            if (client == null) throw new InvalidOperationException(
                "HarnessCore is not connected.");
        }
    }
}
