using System;
using System.Collections;

namespace Win98Ai
{
    public sealed class ModelInfo
    {
        public string Id;
        public string Name;
        public long ContextLength;
        public string PromptPrice;
        public string CompletionPrice;
    }

    public sealed class ChatMessage
    {
        public string Role;
        public string Content;

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public sealed class Conversation
    {
        private readonly ArrayList messages = new ArrayList();

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
    }

    public sealed class HttpResult
    {
        public int StatusCode;
        public string Body;
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
}
