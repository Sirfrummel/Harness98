using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace Harness98
{
    public sealed class ConversationStore
    {
        private const int FormatVersion = 1;
        private readonly string directory;

        public ConversationStore(string baseDirectory)
        {
            directory = Path.Combine(baseDirectory, "Conversations");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public Conversation Create(string modelId)
        {
            Conversation conversation = new Conversation();
            conversation.Id = NextId();
            conversation.Title = "";
            conversation.ModelId = modelId;
            conversation.CreatedUtc = DateTime.UtcNow;
            conversation.UpdatedUtc = conversation.CreatedUtc;
            return conversation;
        }

        public void Save(Conversation conversation)
        {
            if (conversation == null)
            {
                throw new ArgumentNullException("conversation");
            }
            if (conversation.Count == 0) return;
            if (!IsValidId(conversation.Id))
            {
                throw new ApplicationException("Invalid conversation ID.");
            }

            conversation.UpdatedUtc = DateTime.UtcNow;
            if (conversation.Title == null || conversation.Title.Length == 0 ||
                conversation.Title == "(empty conversation)")
            {
                conversation.Title = MakeTitle(conversation.Messages);
            }

            string path = PathFor(conversation.Id, ".JSN");
            string temporary = PathFor(conversation.Id, ".TMP");
            string backup = PathFor(conversation.Id, ".BAK");
            WriteUtf8(temporary, Serialize(conversation));

            try
            {
                if (File.Exists(path))
                {
                    File.Copy(path, backup, true);
                }
                File.Copy(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        public Conversation Load(string id)
        {
            string normalized = NormalizeId(id);
            string path = PathFor(normalized, ".JSN");
            try
            {
                return Deserialize(ReadUtf8(path));
            }
            catch
            {
                string backup = PathFor(normalized, ".BAK");
                if (File.Exists(backup))
                {
                    return Deserialize(ReadUtf8(backup));
                }
                throw;
            }
        }

        public ArrayList List()
        {
            ArrayList conversations = new ArrayList();
            string[] files = Directory.GetFiles(directory, "C*.JSN");
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    Conversation conversation = Deserialize(ReadUtf8(files[i]));
                    if (conversation.Count > 0) conversations.Add(conversation);
                }
                catch
                {
                    // A damaged chat should not hide the rest of the saved list.
                }
            }

            conversations.Sort(new ConversationUpdatedComparer());
            return conversations;
        }

        public Conversation MostRecent()
        {
            ArrayList conversations = List();
            return conversations.Count == 0 ? null : (Conversation)conversations[0];
        }

        private string NextId()
        {
            int maximum = 0;
            string[] files = Directory.GetFiles(directory, "C*.JSN");
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                if (!IsValidId(name)) continue;
                int number;
                if (Int32.TryParse(name.Substring(1), out number) && number > maximum)
                {
                    maximum = number;
                }
            }

            if (maximum >= 999999)
            {
                throw new ApplicationException("The conversation ID limit was reached.");
            }
            return "C" + (maximum + 1).ToString("D6");
        }

        private static string Serialize(Conversation conversation)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"format\":");
            json.Append(FormatVersion.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"id\":");
            json.Append(Json.Quote(conversation.Id));
            json.Append(",\"title\":");
            json.Append(Json.Quote(conversation.Title));
            json.Append(",\"model\":");
            json.Append(Json.Quote(conversation.ModelId));
            json.Append(",\"created_utc\":");
            json.Append(Json.Quote(FormatDate(conversation.CreatedUtc)));
            json.Append(",\"updated_utc\":");
            json.Append(Json.Quote(FormatDate(conversation.UpdatedUtc)));
            json.Append(",\"messages\":[");

            for (int i = 0; i < conversation.Messages.Count; i++)
            {
                if (i > 0) json.Append(',');
                ChatMessage message = (ChatMessage)conversation.Messages[i];
                json.Append("{\"role\":");
                json.Append(Json.Quote(message.Role));
                json.Append(",\"content\":");
                json.Append(Json.Quote(message.Content));
                json.Append('}');
            }

            json.Append("]}");
            return json.ToString();
        }

        private static Conversation Deserialize(string text)
        {
            Hashtable root = Json.AsObject(Json.Parse(text));
            if (root == null)
            {
                throw new FormatException("Conversation file is not a JSON object.");
            }

            Conversation conversation = new Conversation();
            conversation.Id = NormalizeId(Json.GetString(root, "id"));
            conversation.Title = Json.GetString(root, "title");
            conversation.ModelId = Json.GetString(root, "model");
            conversation.CreatedUtc = ParseDate(Json.GetString(root, "created_utc"));
            conversation.UpdatedUtc = ParseDate(Json.GetString(root, "updated_utc"));
            ArrayList messages = Json.AsArray(root["messages"]);
            if (messages == null)
            {
                throw new FormatException("Conversation messages are missing.");
            }

            for (int i = 0; i < messages.Count; i++)
            {
                Hashtable item = Json.AsObject(messages[i]);
                string role = Json.GetString(item, "role");
                string content = Json.GetString(item, "content");
                if (role == null || content == null)
                {
                    throw new FormatException("A conversation message is invalid.");
                }
                conversation.Add(role, content);
            }

            return conversation;
        }

        private static string MakeTitle(IList messages)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                ChatMessage message = (ChatMessage)messages[i];
                if (message.Role != "user") continue;
                string title = message.Content.Replace('\r', ' ').Replace('\n', ' ').Trim();
                while (title.IndexOf("  ") >= 0) title = title.Replace("  ", " ");
                if (title.Length > 60) title = title.Substring(0, 57) + "...";
                return title;
            }
            return "(empty conversation)";
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ",
                CultureInfo.InvariantCulture);
        }

        private static DateTime ParseDate(string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss.fffZ",
                CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal |
                DateTimeStyles.AssumeUniversal);
        }

        private static string NormalizeId(string id)
        {
            string normalized = id == null ? "" : id.Trim().ToUpper();
            if (!IsValidId(normalized))
            {
                throw new ApplicationException("Conversation ID must look like C000001.");
            }
            return normalized;
        }

        private static bool IsValidId(string id)
        {
            if (id == null || id.Length != 7 || id[0] != 'C') return false;
            for (int i = 1; i < id.Length; i++)
            {
                if (!Char.IsDigit(id[i])) return false;
            }
            return true;
        }

        private string PathFor(string id, string extension)
        {
            return Path.Combine(directory, id + extension);
        }

        private static void WriteUtf8(string path, string value)
        {
            using (StreamWriter writer = new StreamWriter(path, false,
                new UTF8Encoding(false)))
            {
                writer.Write(value);
            }
        }

        private static string ReadUtf8(string path)
        {
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
