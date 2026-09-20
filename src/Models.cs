using System;
using System.Collections;

namespace Harness98
{
    public sealed class ModelInfo
    {
        public string Id;
        public string Name;
        public long ContextLength;
        public string PromptPrice;
        public string CompletionPrice;
        public string Description;
        public bool AcceptsImages;
        public bool GeneratesImages;
        public bool SupportsTools;

        public override string ToString()
        {
            return Name;
        }
    }

    public sealed class ChatMessage
    {
        private readonly ArrayList toolCalls = new ArrayList();

        public string Role;
        public string Content;
        public string ToolCallId;
        public string ToolName;

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public IList ToolCalls
        {
            get { return toolCalls; }
        }

        public void AddToolCall(ToolCall call)
        {
            toolCalls.Add(call);
        }
    }

    public sealed class ToolCall
    {
        public string Id;
        public string Name;
        public string Arguments;
    }

    public enum AgentProgressType
    {
        ModelRequestStarted,
        UsageReceived,
        ToolStarted,
        ToolCompleted,
        ToolInterrupted
    }

    public sealed class AgentProgress
    {
        public AgentProgressType Type;
        public int Iteration;
        public ToolCall ToolCall;
        public string ToolResult;
        public long PromptTokens;
        public long CompletionTokens;
        public long TotalTokens;
        public double Cost;
        public bool Auxiliary;
    }

    public interface IAgentProgressSink
    {
        void Report(AgentProgress progress);
    }

    public interface IAgentRunControl
    {
        bool ContinueRun { get; }
    }

    public interface ICommandRunControl
    {
        bool CancelCommand { get; }
    }

    public sealed class Conversation
    {
        private readonly ArrayList messages = new ArrayList();

        public string Id;
        public string Title;
        public string ModelId;
        public DateTime CreatedUtc;
        public DateTime UpdatedUtc;

        public IList Messages
        {
            get { return messages; }
        }

        public int Count
        {
            get { return messages.Count; }
        }

        public void Add(string role, string content)
        {
            messages.Add(new ChatMessage(role, content));
        }

        public void Add(ChatMessage message)
        {
            messages.Add(message);
        }

        public void RemoveLast()
        {
            if (messages.Count > 0)
            {
                messages.RemoveAt(messages.Count - 1);
            }
        }

        public void Clear()
        {
            messages.Clear();
        }

        public void Truncate(int count)
        {
            while (messages.Count > count)
            {
                messages.RemoveAt(messages.Count - 1);
            }
        }
    }

    public sealed class HttpResult
    {
        public int StatusCode;
        public string Body;
    }

    public sealed class ChatResult
    {
        public string Answer;
        public string GeneratedTitle;
        public string Warning;
        public long PromptTokens;
        public long CompletionTokens;
        public long TotalTokens;
        public double Cost;
    }

    public sealed class ChatCompletion
    {
        private readonly ArrayList toolCalls = new ArrayList();

        public string Answer;
        public long PromptTokens;
        public long CompletionTokens;
        public long TotalTokens;
        public double Cost;
        public bool HasCost;

        public IList ToolCalls
        {
            get { return toolCalls; }
        }

        public void AddToolCall(ToolCall call)
        {
            toolCalls.Add(call);
        }
    }

    public sealed class ModelNameComparer : IComparer
    {
        public int Compare(object left, object right)
        {
            ModelInfo a = (ModelInfo)left;
            ModelInfo b = (ModelInfo)right;
            int byName = String.Compare(a.Name, b.Name, true);
            if (byName != 0)
            {
                return byName;
            }

            return String.Compare(a.Id, b.Id, true);
        }
    }

    public sealed class ConversationUpdatedComparer : IComparer
    {
        public int Compare(object left, object right)
        {
            Conversation a = (Conversation)left;
            Conversation b = (Conversation)right;
            int byDate = DateTime.Compare(b.UpdatedUtc, a.UpdatedUtc);
            if (byDate != 0)
            {
                return byDate;
            }

            return String.Compare(a.Id, b.Id, true);
        }
    }
}
