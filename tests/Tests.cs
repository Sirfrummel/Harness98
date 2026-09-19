using System;
using System.Collections;
using System.IO;
using Harness98;

public sealed class Tests
{
    private static int failures;

    public static int Main(string[] args)
    {
        Run("JSON strings", TestJsonStrings);
        Run("Model parsing and sorting", TestModels);
        Run("Chat history serialization", TestChat);
        Run("Content-part response", TestContentParts);
        Run("API error message", TestApiError);
        Run("Saved conversations", TestSavedConversations);

        Console.WriteLine();
        if (failures == 0)
        {
            Console.WriteLine("All tests passed.");
            return 0;
        }

        Console.WriteLine(failures.ToString() + " test(s) failed.");
        return 1;
    }

    private static void TestJsonStrings()
    {
        string original = "quote \" slash \\ newline\n tab\t snowman \u2603";
        Hashtable parsed = Json.AsObject(Json.Parse("{\"value\":" +
            Json.Quote(original) + "}"));
        AssertEqual(original, Json.GetString(parsed, "value"));
    }

    private static void TestModels()
    {
        FakeTransport transport = new FakeTransport();
        transport.GetResponse = Ok("{\"data\":[" +
            "{\"id\":\"z/model\",\"name\":\"Zulu\",\"context_length\":8192," +
            "\"pricing\":{\"prompt\":\"0.1\",\"completion\":\"0.2\"}}," +
            "{\"id\":\"a/model\",\"name\":\"Alpha\",\"context_length\":4096}]}" );
        OpenRouterClient client = new OpenRouterClient(transport);
        ArrayList models = client.GetModels("test-key");
        AssertEqual("2", models.Count.ToString());
        AssertEqual("a/model", ((ModelInfo)models[0]).Id);
        AssertEqual("z/model", ((ModelInfo)models[1]).Id);
    }

    private static void TestChat()
    {
        FakeTransport transport = new FakeTransport();
        transport.PostResponse = Ok("{\"choices\":[{\"message\":{" +
            "\"role\":\"assistant\",\"content\":\"second answer\"}}]}" );
        OpenRouterClient client = new OpenRouterClient(transport);
        ArrayList messages = new ArrayList();
        messages.Add(new ChatMessage("user", "first \"question\"\nline two"));
        messages.Add(new ChatMessage("assistant", "first answer"));
        messages.Add(new ChatMessage("user", "second question"));

        string answer = client.SendChat("secret", "test/model", messages);
        AssertEqual("second answer", answer);

        Hashtable request = Json.AsObject(Json.Parse(transport.LastPostBody));
        AssertEqual("test/model", Json.GetString(request, "model"));
        ArrayList sentMessages = Json.AsArray(request["messages"]);
        AssertEqual("3", sentMessages.Count.ToString());
        AssertEqual("first \"question\"\nline two",
            Json.GetString(Json.AsObject(sentMessages[0]), "content"));
    }

    private static void TestContentParts()
    {
        FakeTransport transport = new FakeTransport();
        transport.PostResponse = Ok("{\"choices\":[{\"message\":{" +
            "\"content\":[{\"type\":\"text\",\"text\":\"one\"}," +
            "{\"type\":\"text\",\"text\":\" two\"}]}}]}" );
        OpenRouterClient client = new OpenRouterClient(transport);
        string answer = client.SendChat("key", "model", new ArrayList());
        AssertEqual("one two", answer);
    }

    private static void TestApiError()
    {
        FakeTransport transport = new FakeTransport();
        HttpResult response = new HttpResult();
        response.StatusCode = 401;
        response.Body = "{\"error\":{\"message\":\"bad key\"}}";
        transport.GetResponse = response;
        OpenRouterClient client = new OpenRouterClient(transport);

        try
        {
            client.GetModels("key");
            throw new Exception("Expected an API exception.");
        }
        catch (ApplicationException ex)
        {
            if (ex.Message.IndexOf("bad key") < 0)
            {
                throw;
            }
        }
    }

    private static void TestSavedConversations()
    {
        string root = Path.Combine(Path.GetTempPath(), "Harness98Tests-" +
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            ConversationStore store = new ConversationStore(root);
            Conversation first = store.Create("provider/first-model");
            first.Add("user", "A saved question with a snowman \u2603");
            first.Add("assistant", "A saved answer");
            store.Save(first);

            Conversation loaded = store.Load(first.Id.ToLower());
            AssertEqual("C000001", loaded.Id);
            AssertEqual("provider/first-model", loaded.ModelId);
            AssertEqual("2", loaded.Count.ToString());
            AssertEqual("A saved question with a snowman \u2603",
                ((ChatMessage)loaded.Messages[0]).Content);
            AssertEqual("A saved question with a snowman \u2603", loaded.Title);

            Conversation second = store.Create("provider/second-model");
            store.Save(second);
            AssertEqual("C000002", second.Id);
            AssertEqual("2", store.List().Count.ToString());
            AssertEqual("C000002", store.MostRecent().Id);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static HttpResult Ok(string body)
    {
        HttpResult result = new HttpResult();
        result.StatusCode = 200;
        result.Body = body;
        return result;
    }

    private static void Run(string name, TestMethod test)
    {
        try
        {
            test();
            Console.WriteLine("PASS: " + name);
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine("FAIL: " + name + " - " + ex.Message);
        }
    }

    private static void AssertEqual(string expected, string actual)
    {
        if (expected != actual)
        {
            throw new Exception("Expected [" + expected + "] but got [" + actual + "].");
        }
    }

    private delegate void TestMethod();

    private sealed class FakeTransport : IHttpTransport
    {
        public HttpResult GetResponse;
        public HttpResult PostResponse;
        public string LastPostBody;

        public HttpResult Get(string url, string apiKey)
        {
            return GetResponse;
        }

        public HttpResult PostJson(string url, string json, string apiKey)
        {
            LastPostBody = json;
            return PostResponse;
        }
    }
}
