using System;
using System.Collections;

namespace Harness98
{
    public static class ConsoleUi
    {
        private const int PageSize = 12;

        public static ModelInfo SelectModel(IList models)
        {
            string filter = "";
            int page = 0;

            while (true)
            {
                ArrayList matches = FilterModels(models, filter);
                int pageCount = matches.Count == 0 ? 1 :
                    (matches.Count + PageSize - 1) / PageSize;
                if (page >= pageCount) page = pageCount - 1;
                if (page < 0) page = 0;

                Console.WriteLine();
                Console.WriteLine("Choose a model");
                Console.WriteLine("--------------");
                Console.WriteLine("Filter: " + (filter.Length == 0 ? "(all models)" : filter));
                Console.WriteLine("Matches: " + matches.Count.ToString() +
                    "  Page " + (page + 1).ToString() + " of " + pageCount.ToString());
                Console.WriteLine();

                int start = page * PageSize;
                int shown = Math.Min(PageSize, matches.Count - start);
                for (int i = 0; i < shown; i++)
                {
                    ModelInfo model = (ModelInfo)matches[start + i];
                    Console.WriteLine((i + 1).ToString() + ") " + model.Name);
                    Console.WriteLine("   " + model.Id);
                }

                if (matches.Count == 0)
                {
                    Console.WriteLine("No models match that filter.");
                }

                Console.WriteLine();
                Console.WriteLine("Enter a number to select, text to filter, N/P for pages,");
                Console.Write("or Q to quit: ");
                string input = Console.ReadLine();
                if (input == null) return null;
                input = input.Trim();

                if (String.Compare(input, "q", true) == 0) return null;
                if (String.Compare(input, "n", true) == 0)
                {
                    if (page + 1 < pageCount) page++;
                    continue;
                }
                if (String.Compare(input, "p", true) == 0)
                {
                    if (page > 0) page--;
                    continue;
                }

                int selection;
                if (Int32.TryParse(input, out selection) && selection >= 1 &&
                    selection <= shown)
                {
                    return (ModelInfo)matches[start + selection - 1];
                }

                filter = input;
                page = 0;
            }
        }

        public static void ShowChatHelp()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  /new         Start a new conversation");
            Console.WriteLine("  /continue    Resume the most recently updated conversation");
            Console.WriteLine("  /resume      Choose from the saved conversation list");
            Console.WriteLine("  /resume ID   Resume a conversation such as C000001");
            Console.WriteLine("  /clear       Preserve this chat and start an empty one");
            Console.WriteLine("  /model       Choose a different model");
            Console.WriteLine("  /key         Replace the saved OpenRouter key");
            Console.WriteLine("  /help        Show this command list");
            Console.WriteLine("  /exit        Exit the program");
        }

        public static Conversation SelectConversation(ArrayList conversations)
        {
            if (conversations == null || conversations.Count == 0)
            {
                Console.WriteLine("No saved conversations were found.");
                return null;
            }

            int page = 0;
            int pageCount = (conversations.Count + PageSize - 1) / PageSize;
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Saved conversations");
                Console.WriteLine("-------------------");
                Console.WriteLine("Page " + (page + 1).ToString() + " of " +
                    pageCount.ToString());
                Console.WriteLine();

                int start = page * PageSize;
                int shown = Math.Min(PageSize, conversations.Count - start);
                for (int i = 0; i < shown; i++)
                {
                    Conversation conversation =
                        (Conversation)conversations[start + i];
                    Console.WriteLine((i + 1).ToString() + ") " + conversation.Id +
                        "  " + conversation.UpdatedUtc.ToLocalTime().ToString(
                            "yyyy-MM-dd HH:mm"));
                    Console.WriteLine("   " + conversation.Title);
                    Console.WriteLine("   " + conversation.ModelId + "  (" +
                        conversation.Count.ToString() + " messages)");
                }

                Console.WriteLine();
                Console.Write("Enter a number, N/P for pages, or Q to cancel: ");
                string input = Console.ReadLine();
                if (input == null || String.Compare(input.Trim(), "q", true) == 0)
                {
                    return null;
                }
                input = input.Trim();
                if (String.Compare(input, "n", true) == 0)
                {
                    if (page + 1 < pageCount) page++;
                    continue;
                }
                if (String.Compare(input, "p", true) == 0)
                {
                    if (page > 0) page--;
                    continue;
                }

                int selection;
                if (Int32.TryParse(input, out selection) && selection >= 1 &&
                    selection <= shown)
                {
                    return (Conversation)conversations[start + selection - 1];
                }
            }
        }

        private static ArrayList FilterModels(IList models, string filter)
        {
            ArrayList matches = new ArrayList();
            string needle = filter.ToLower();
            for (int i = 0; i < models.Count; i++)
            {
                ModelInfo model = (ModelInfo)models[i];
                if (needle.Length == 0 || model.Name.ToLower().IndexOf(needle) >= 0 ||
                    model.Id.ToLower().IndexOf(needle) >= 0)
                {
                    matches.Add(model);
                }
            }

            return matches;
        }
    }
}
