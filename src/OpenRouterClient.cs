using System;
using System.Collections;
using System.Text;

namespace Harness98
{
    public sealed class OpenRouterClient
    {
        private const string ModelsUrl = "https://openrouter.ai/api/v1/models";
        private const string ChatUrl = "https://openrouter.ai/api/v1/chat/completions";
        private readonly IHttpTransport transport;

        public OpenRouterClient(IHttpTransport transport)
        {
            this.transport = transport;
        }

        public ArrayList GetModels(string apiKey)
        {
            HttpResult response = transport.Get(ModelsUrl, apiKey);
            Hashtable root = ParseResponse(response);
            ArrayList data = Json.AsArray(root["data"]);
            if (data == null)
            {
                throw new ApplicationException("The model response did not contain a data list.");
            }

            ArrayList models = new ArrayList();
            for (int i = 0; i < data.Count; i++)
            {
                Hashtable item = Json.AsObject(data[i]);
                if (item == null)
                {
                    continue;
                }

                string id = Json.GetString(item, "id");
                string name = Json.GetString(item, "name");
                if (id == null || id.Length == 0)
                {
                    continue;
                }

                ModelInfo model = new ModelInfo();
                model.Id = id;
                model.Name = name == null || name.Length == 0 ? id : name;
                model.ContextLength = Json.GetInt64(item, "context_length");
                model.Description = Json.GetString(item, "description");

                Hashtable architecture = Json.AsObject(item["architecture"]);
                model.AcceptsImages = ArrayContains(
                    Json.AsArray(architecture == null ? null :
                    architecture["input_modalities"]), "image");
                model.GeneratesImages = ArrayContains(
                    Json.AsArray(architecture == null ? null :
                    architecture["output_modalities"]), "image");

                Hashtable pricing = Json.AsObject(item["pricing"]);
                model.PromptPrice = Json.GetString(pricing, "prompt");
                model.CompletionPrice = Json.GetString(pricing, "completion");
                models.Add(model);
            }

            models.Sort(new ModelNameComparer());
            return models;
        }

        public string SendChat(string apiKey, string modelId, IList messages)
        {
            string request = BuildChatRequest(modelId, messages);
            HttpResult response = transport.PostJson(ChatUrl, request, apiKey);
            Hashtable root = ParseResponse(response);
            ArrayList choices = Json.AsArray(root["choices"]);
            if (choices == null || choices.Count == 0)
            {
                throw new ApplicationException("OpenRouter returned no response choices.");
            }

            Hashtable choice = Json.AsObject(choices[0]);
            Hashtable message = choice == null ? null : Json.AsObject(choice["message"]);
            if (message == null)
            {
                throw new ApplicationException("The response choice did not contain a message.");
            }

            string content = ExtractText(message["content"]);
            if (content == null || content.Length == 0)
            {
                return "(The model returned no text.)";
            }

            return content;
        }

        private static bool ArrayContains(ArrayList values, string expected)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i] as string;
                if (String.Compare(value, expected, true) == 0) return true;
            }
            return false;
        }

        private static string BuildChatRequest(string modelId, IList messages)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"model\":");
            json.Append(Json.Quote(modelId));
            json.Append(",\"stream\":false,\"messages\":[");

            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) json.Append(',');
                ChatMessage message = (ChatMessage)messages[i];
                json.Append("{\"role\":");
                json.Append(Json.Quote(message.Role));
                json.Append(",\"content\":");
                json.Append(Json.Quote(message.Content));
                json.Append('}');
            }

            json.Append("]}");
            return json.ToString();
        }

        private static Hashtable ParseResponse(HttpResult response)
        {
            Hashtable root;
            try
            {
                root = Json.AsObject(Json.Parse(response.Body));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("OpenRouter returned unreadable JSON (HTTP " +
                    response.StatusCode.ToString() + "): " + ex.Message);
            }

            if (root == null)
            {
                throw new ApplicationException("OpenRouter returned an unexpected JSON value.");
            }

            if (response.StatusCode < 200 || response.StatusCode >= 300)
            {
                throw new ApplicationException("OpenRouter returned HTTP " +
                    response.StatusCode.ToString() + ": " + ReadError(root));
            }

            return root;
        }

        private static string ReadError(Hashtable root)
        {
            object errorValue = root["error"];
            string errorText = errorValue as string;
            if (errorText != null)
            {
                return errorText;
            }

            Hashtable error = Json.AsObject(errorValue);
            string message = Json.GetString(error, "message");
            return message == null ? "Unknown API error." : message;
        }

        private static string ExtractText(object content)
        {
            string text = content as string;
            if (text != null)
            {
                return text;
            }

            ArrayList parts = Json.AsArray(content);
            if (parts == null)
            {
                return null;
            }

            StringBuilder combined = new StringBuilder();
            for (int i = 0; i < parts.Count; i++)
            {
                Hashtable part = Json.AsObject(parts[i]);
                string partText = Json.GetString(part, "text");
                if (partText != null)
                {
                    combined.Append(partText);
                }
            }

            return combined.ToString();
        }
    }
}
