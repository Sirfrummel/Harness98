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
                model.SupportsTools = ArrayContains(
                    Json.AsArray(item["supported_parameters"]), "tools");

                Hashtable pricing = Json.AsObject(item["pricing"]);
                model.PromptPrice = Json.GetString(pricing, "prompt");
                model.CompletionPrice = Json.GetString(pricing, "completion");
                models.Add(model);
            }

            return models;
        }

        public string SendChat(string apiKey, string modelId, IList messages)
        {
            return SendChatWithUsage(apiKey, modelId, messages).Answer;
        }

        public ChatCompletion SendChatWithUsage(string apiKey, string modelId,
            IList messages)
        {
            return SendChatWithUsage(apiKey, modelId, messages, null);
        }

        public ChatCompletion SendChatWithUsage(string apiKey, string modelId,
            IList messages, string toolsJson)
        {
            return SendChatWithUsage(apiKey, modelId, messages, toolsJson, false);
        }

        public ChatCompletion SendChatWithUsage(string apiKey, string modelId,
            IList messages, string toolsJson, bool disableToolCalls)
        {
            string request = BuildChatRequest(modelId, messages, toolsJson,
                disableToolCalls);
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

            ChatCompletion result = new ChatCompletion();
            string content = ExtractText(message["content"]);
            ArrayList toolCalls = Json.AsArray(message["tool_calls"]);
            if (toolCalls != null)
            {
                for (int i = 0; i < toolCalls.Count; i++)
                {
                    Hashtable item = Json.AsObject(toolCalls[i]);
                    Hashtable function = item == null ? null :
                        Json.AsObject(item["function"]);
                    string id = Json.GetString(item, "id");
                    string name = Json.GetString(function, "name");
                    string arguments = Json.GetString(function, "arguments");
                    if (id == null || name == null) continue;
                    ToolCall call = new ToolCall();
                    call.Id = id;
                    call.Name = name;
                    call.Arguments = arguments == null ? "{}" : arguments;
                    result.AddToolCall(call);
                }
            }
            if ((content == null || content.Length == 0) &&
                result.ToolCalls.Count == 0)
                content = "(The model returned no text.)";
            result.Answer = content == null ? "" : content;
            Hashtable usage = Json.AsObject(root["usage"]);
            result.PromptTokens = Json.GetInt64(usage, "prompt_tokens");
            result.CompletionTokens = Json.GetInt64(usage, "completion_tokens");
            result.TotalTokens = Json.GetInt64(usage, "total_tokens");
            result.Cost = Json.GetDouble(usage, "cost");
            result.HasCost = usage != null && usage.ContainsKey("cost");
            return result;
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

        private static string BuildChatRequest(string modelId, IList messages,
            string toolsJson, bool disableToolCalls)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"model\":");
            json.Append(Json.Quote(modelId));
            json.Append(",\"stream\":false,\"usage\":{\"include\":true},");
            if (toolsJson != null && toolsJson.Length > 0)
            {
                json.Append("\"tools\":");
                json.Append(toolsJson);
                json.Append(",\"parallel_tool_calls\":false,");
                if (disableToolCalls)
                    json.Append("\"tool_choice\":\"none\",");
            }
            json.Append("\"messages\":[");

            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) json.Append(',');
                ChatMessage message = (ChatMessage)messages[i];
                AppendMessage(json, message);
            }

            json.Append("]}");
            return json.ToString();
        }

        private static void AppendMessage(StringBuilder json, ChatMessage message)
        {
            json.Append("{\"role\":");
            json.Append(Json.Quote(message.Role));
            json.Append(",\"content\":");
            json.Append(Json.Quote(message.Content == null ? "" : message.Content));
            if (message.ToolCallId != null)
            {
                json.Append(",\"tool_call_id\":");
                json.Append(Json.Quote(message.ToolCallId));
            }
            if (message.ToolCalls.Count > 0)
            {
                json.Append(",\"tool_calls\":[");
                for (int i = 0; i < message.ToolCalls.Count; i++)
                {
                    if (i > 0) json.Append(',');
                    ToolCall call = (ToolCall)message.ToolCalls[i];
                    json.Append("{\"id\":");
                    json.Append(Json.Quote(call.Id));
                    json.Append(",\"type\":\"function\",\"function\":{");
                    json.Append("\"name\":");
                    json.Append(Json.Quote(call.Name));
                    json.Append(",\"arguments\":");
                    json.Append(Json.Quote(call.Arguments));
                    json.Append("}}");
                }
                json.Append(']');
            }
            json.Append('}');
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
