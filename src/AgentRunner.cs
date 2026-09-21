using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Harness98
{
    public sealed class AgentRunner
    {
        private readonly OpenRouterClient client;
        private readonly string apiKey;
        private readonly string applicationDirectory;
        private readonly ToolRegistry tools;
        private readonly IAgentProgressSink progress;
        private readonly string extraInstructions;

        public AgentRunner(OpenRouterClient openRouter, string key,
            string workingDirectory, IAgentProgressSink progressSink)
            : this(openRouter, key, workingDirectory, progressSink, "")
        {
        }

        public AgentRunner(OpenRouterClient openRouter, string key,
            string workingDirectory, IAgentProgressSink progressSink,
            string additionalInstructions)
        {
            client = openRouter;
            apiKey = key;
            applicationDirectory = workingDirectory;
            tools = new ToolRegistry(workingDirectory,
                progressSink as ICommandRunControl);
            progress = progressSink;
            extraInstructions = additionalInstructions == null ? "" :
                additionalInstructions.Trim();
        }

        public ChatResult Run(ModelInfo model, Conversation conversation)
        {
            ChatResult total = new ChatResult();
            bool toolsEnabled = model.SupportsTools;
            int iteration = 0;
            while (true)
            {
                iteration++;
                Report(AgentProgressType.ModelRequestStarted, iteration,
                    null, null);
                ArrayList messages = BuildMessages(conversation, toolsEnabled);
                ChatCompletion completion = client.SendChatWithUsage(apiKey,
                    model.Id, messages, toolsEnabled ? tools.DefinitionsJson : null);
                ApplyCostFallback(completion, model);
                AddUsage(total, completion);
                ReportUsage(completion, iteration);

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

                bool runInterrupted = false;
                for (int i = 0; i < completion.ToolCalls.Count; i++)
                {
                    ToolCall call = (ToolCall)completion.ToolCalls[i];
                    if (!ShouldContinue())
                    {
                        string interruptedOutput = InterruptedToolResult();
                        Report(AgentProgressType.ToolInterrupted, iteration,
                            call, interruptedOutput);
                        AddToolResult(conversation, call, interruptedOutput);
                        runInterrupted = true;
                        continue;
                    }
                    Report(AgentProgressType.ToolStarted, iteration,
                        call, null);
                    string toolOutput = tools.Execute(call);
                    Report(AgentProgressType.ToolCompleted, iteration,
                        call, toolOutput);
                    AddToolResult(conversation, call, toolOutput);
                    if (ToolWasCancelled(toolOutput) || !ShouldContinue())
                        runInterrupted = true;
                }

                if (runInterrupted)
                {
                    return RequestFinalResponse(model, conversation, total,
                        toolsEnabled, iteration + 1,
                        "The user interrupted this agent run. Some requested " +
                        "tools may not have executed. Do not request any more " +
                        "tools in this response. Briefly explain what completed, " +
                        "what was interrupted, and what work remains.",
                        "The agent run was interrupted by the user.");
                }
            }
        }

        private static void AddToolResult(Conversation conversation,
            ToolCall call, string output)
        {
            ChatMessage result = new ChatMessage("tool", output);
            result.ToolCallId = call.Id;
            result.ToolName = call.Name;
            conversation.Add(result);
        }

        private static string InterruptedToolResult()
        {
            return "{\"error\":" + Json.Quote(
                "Tool call interrupted by the user before execution.") +
                ",\"interrupted\":true}";
        }

        private ChatResult RequestFinalResponse(ModelInfo model,
            Conversation conversation, ChatResult total, bool toolsEnabled,
            int iteration, string instruction, string emptyAnswer)
        {
            ArrayList finalMessages = BuildMessages(conversation, toolsEnabled);
            finalMessages.Add(new ChatMessage("system", instruction));
            Report(AgentProgressType.ModelRequestStarted, iteration, null, null);
            ChatCompletion finalCompletion = client.SendChatWithUsage(apiKey,
                model.Id, finalMessages, toolsEnabled ? tools.DefinitionsJson : null,
                toolsEnabled);
            ApplyCostFallback(finalCompletion, model);
            AddUsage(total, finalCompletion);
            ReportUsage(finalCompletion, iteration);
            total.Answer = finalCompletion.Answer.Length == 0 ?
                emptyAnswer : finalCompletion.Answer;
            return total;
        }

        private static bool ToolWasCancelled(string output)
        {
            try
            {
                Hashtable result = Json.AsObject(Json.Parse(output));
                return result != null && result["cancelled"] is bool &&
                    (bool)result["cancelled"];
            }
            catch
            {
                return false;
            }
        }

        private bool ShouldContinue()
        {
            IAgentRunControl control = progress as IAgentRunControl;
            return control == null || control.ContinueRun;
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
            StringBuilder systemPrompt = new StringBuilder();
            if (toolsEnabled)
            {
                systemPrompt.Append(
                    "You are running in Harness98 on a Windows 98-era computer. " +
                    "Use read_file, write_file, and edit_file for text files, and " +
                    "run_command to run programs or native commands. Commands use " +
                    "Windows 98 COMMAND.COM, not CMD.EXE. Never use &, &&, ||, " +
                    "parenthesized command groups, or caret escaping. Use separate " +
                    "tool calls when you need to inspect intermediate results. For " +
                    "predetermined multi-step work, write a .BAT file with one " +
                    "command per line and run it. The application and default " +
                    "working directory is " + applicationDirectory + ". For " +
                    "temporary scripts or scratch files, prefer " +
                    tools.TemporaryDirectory + ". Inspect results before deciding " +
                    "the next step.");
            }
            if (extraInstructions.Length > 0)
            {
                if (systemPrompt.Length > 0) systemPrompt.Append("\n\n");
                systemPrompt.Append("Additional instructions configured by the " +
                    "user for this Harness98 installation follow. Apply them to " +
                    "this conversation.");
                if (toolsEnabled)
                    systemPrompt.Append(" They supplement the built-in tool and " +
                        "platform rules above.");
                systemPrompt.Append("\n\n");
                systemPrompt.Append(extraInstructions);
            }
            if (systemPrompt.Length > 0)
                messages.Add(new ChatMessage("system", systemPrompt.ToString()));
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
