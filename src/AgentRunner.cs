using System;
using System.Collections;
using System.Globalization;

namespace Harness98
{
    public sealed class AgentRunner
    {
        private const int MaximumIterations = 10;
        private readonly OpenRouterClient client;
        private readonly string apiKey;
        private readonly string applicationDirectory;
        private readonly ToolRegistry tools;
        private readonly IAgentProgressSink progress;

        public AgentRunner(OpenRouterClient openRouter, string key,
            string workingDirectory, IAgentProgressSink progressSink)
        {
            client = openRouter;
            apiKey = key;
            applicationDirectory = workingDirectory;
            tools = new ToolRegistry(workingDirectory);
            progress = progressSink;
        }

        public ChatResult Run(ModelInfo model, Conversation conversation)
        {
            ChatResult total = new ChatResult();
            bool toolsEnabled = model.SupportsTools;
            for (int iteration = 0; iteration < MaximumIterations; iteration++)
            {
                Report(AgentProgressType.ModelRequestStarted, iteration + 1,
                    null, null);
                ArrayList messages = BuildMessages(conversation, toolsEnabled);
                ChatCompletion completion = client.SendChatWithUsage(apiKey,
                    model.Id, messages, toolsEnabled ? tools.DefinitionsJson : null);
                ApplyCostFallback(completion, model);
                AddUsage(total, completion);
                ReportUsage(completion, iteration + 1);

                if (completion.ToolCalls.Count == 0)
                {
                    total.Answer = completion.Answer;
                    return total;
                }

                ChatMessage assistant = new ChatMessage("assistant",
                    completion.Answer);
                for (int i = 0; i < completion.ToolCalls.Count; i++)
                    assistant.AddToolCall((ToolCall)completion.ToolCalls[i]);
                conversation.Add(assistant);

                for (int i = 0; i < completion.ToolCalls.Count; i++)
                {
                    ToolCall call = (ToolCall)completion.ToolCalls[i];
                    Report(AgentProgressType.ToolStarted, iteration + 1,
                        call, null);
                    string toolOutput = tools.Execute(call);
                    Report(AgentProgressType.ToolCompleted, iteration + 1,
                        call, toolOutput);
                    ChatMessage result = new ChatMessage("tool", toolOutput);
                    result.ToolCallId = call.Id;
                    result.ToolName = call.Name;
                    conversation.Add(result);
                }
            }

            ArrayList finalMessages = BuildMessages(conversation, toolsEnabled);
            finalMessages.Add(new ChatMessage("system",
                "The maximum of " + MaximumIterations.ToString() +
                " tool-call rounds has been reached. Do not request any more " +
                "tools. Respond to the user now using the information already " +
                "collected, and briefly mention any work that remains."));
            Report(AgentProgressType.ModelRequestStarted,
                MaximumIterations + 1, null, null);
            ChatCompletion finalCompletion = client.SendChatWithUsage(apiKey,
                model.Id, finalMessages, toolsEnabled ? tools.DefinitionsJson : null,
                toolsEnabled);
            ApplyCostFallback(finalCompletion, model);
            AddUsage(total, finalCompletion);
            ReportUsage(finalCompletion, MaximumIterations + 1);
            total.Answer = finalCompletion.Answer.Length == 0 ?
                "The tool-call limit was reached before the model produced a " +
                "final response." : finalCompletion.Answer;
            return total;
        }

        private void Report(AgentProgressType type, int iteration,
            ToolCall call, string result)
        {
            if (progress == null) return;
            AgentProgress update = new AgentProgress();
            update.Type = type;
            update.Iteration = iteration;
            update.ToolCall = call;
            update.ToolResult = result;
            progress.Report(update);
        }

        private void ReportUsage(ChatCompletion completion, int iteration)
        {
            if (progress == null) return;
            AgentProgress update = new AgentProgress();
            update.Type = AgentProgressType.UsageReceived;
            update.Iteration = iteration;
            update.PromptTokens = completion.PromptTokens;
            update.CompletionTokens = completion.CompletionTokens;
            update.TotalTokens = completion.TotalTokens;
            update.Cost = completion.Cost;
            progress.Report(update);
        }

        public static void ApplyCostFallback(ChatCompletion completion,
            ModelInfo model)
        {
            if (completion.HasCost) return;
            double promptPrice;
            double completionPrice;
            if (!Double.TryParse(model.PromptPrice, NumberStyles.Float,
                CultureInfo.InvariantCulture, out promptPrice)) promptPrice = 0;
            if (!Double.TryParse(model.CompletionPrice, NumberStyles.Float,
                CultureInfo.InvariantCulture, out completionPrice))
                completionPrice = 0;
            completion.Cost = completion.PromptTokens * promptPrice +
                completion.CompletionTokens * completionPrice;
        }

        private ArrayList BuildMessages(Conversation conversation,
            bool toolsEnabled)
        {
            ArrayList messages = new ArrayList();
            if (toolsEnabled)
            {
                messages.Add(new ChatMessage("system",
                    "You are running in Harness98 on a Windows 98-era computer. " +
                    "You can use run_command to inspect files, run programs, and " +
                    "make changes. Commands are executed through COMMAND.COM. " +
                    "Prefer Windows 98-compatible commands and inspect command " +
                    "results before deciding the next step. The default working " +
                    "directory is " + applicationDirectory + "."));
            }
            for (int i = 0; i < conversation.Messages.Count; i++)
                messages.Add(conversation.Messages[i]);
            return messages;
        }

        private static void AddUsage(ChatResult total, ChatCompletion usage)
        {
            total.PromptTokens += usage.PromptTokens;
            total.CompletionTokens += usage.CompletionTokens;
            total.TotalTokens += usage.TotalTokens;
            total.Cost += usage.Cost;
        }
    }
}
