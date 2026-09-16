using System;
using System.Collections;

namespace Win98Ai
{
    public sealed class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Windows 98 AI Harness v1");
            Console.WriteLine("=========================");
            Console.WriteLine();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                CurlTransport transport = new CurlTransport(baseDirectory);
                transport.ValidateDependencies();

                Settings settings = new Settings(baseDirectory);
                string apiKey = settings.LoadOrCreateKey();
                OpenRouterClient client = new OpenRouterClient(transport);

                ArrayList models = LoadModels(client, settings, ref apiKey);
                if (models == null)
                {
                    return 1;
                }

                ModelInfo model = ConsoleUi.SelectModel(models);
                if (model == null)
                {
                    return 0;
                }

                RunChat(client, settings, models, ref apiKey, ref model);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("FATAL ERROR: " + ex.Message);
                return 1;
            }
        }

        private static ArrayList LoadModels(OpenRouterClient client,
            Settings settings, ref string apiKey)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("Loading the OpenRouter model list...");
                    ArrayList models = client.GetModels(apiKey);
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
                    }
                }
            }
        }

        private static void RunChat(OpenRouterClient client, Settings settings,
            ArrayList models, ref string apiKey, ref ModelInfo model)
        {
            Conversation conversation = new Conversation();
            Console.WriteLine();
            Console.WriteLine("Selected model: " + model.Name);
            Console.WriteLine(model.Id);
            Console.WriteLine();
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
                if (String.Compare(input, "/clear", true) == 0)
                {
                    conversation.Clear();
                    Console.WriteLine("Conversation history cleared.");
                    continue;
                }
                if (String.Compare(input, "/model", true) == 0)
                {
                    ModelInfo selected = ConsoleUi.SelectModel(models);
                    if (selected != null)
                    {
                        model = selected;
                        Console.WriteLine("Selected model: " + model.Name);
                        Console.WriteLine("Conversation history retained (" +
                            conversation.Count.ToString() + " messages).");
                    }
                    continue;
                }
                if (String.Compare(input, "/key", true) == 0)
                {
                    apiKey = settings.ReplaceKey();
                    Console.WriteLine("The new key will be used for the next request.");
                    continue;
                }

                conversation.Add("user", input);
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("Waiting for " + model.Name + "...");
                    string answer = client.SendChat(apiKey, model.Id,
                        conversation.Messages);
                    conversation.Add("assistant", answer);
                    Console.WriteLine();
                    Console.WriteLine("Assistant> " + answer);
                }
                catch (Exception ex)
                {
                    conversation.RemoveLast();
                    Console.WriteLine();
                    Console.WriteLine("REQUEST FAILED: " + ex.Message);
                    Console.WriteLine("Your message was not added to the history.");
                }
            }
        }
    }
}
