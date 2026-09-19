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
            Console.WriteLine("  /help   Show this command list");
            Console.WriteLine("  /clear  Clear the conversation history");
            Console.WriteLine("  /model  Choose a different model");
            Console.WriteLine("  /key    Replace the saved OpenRouter key");
            Console.WriteLine("  /exit   Exit the program");
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
