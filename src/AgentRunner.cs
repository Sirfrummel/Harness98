using System;
using System.Collections;

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
                AddUsage(total, completion);

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

            throw new ApplicationException("The model reached the maximum of " +
                MaximumIterations.ToString() + " tool-call rounds.");
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
