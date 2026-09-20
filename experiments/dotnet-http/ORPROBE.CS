using System;
using System.IO;
using System.Net;

public class OpenRouterProbe
{
    private const string HostName = "openrouter.ai";
    private const string ModelsUrl = "https://openrouter.ai/api/v1/models";

    public static int Main(string[] args)
    {
        Console.WriteLine("OpenRouter Windows 98 connectivity probe");
        Console.WriteLine("CLR version: " + Environment.Version);
        Console.WriteLine("Target: " + ModelsUrl);
        Console.WriteLine();

        if (!TestDns())
        {
            Console.WriteLine();
            Console.WriteLine("RESULT: DNS FAILED");
            return 2;
        }

        Console.WriteLine();
        return TestHttps();
    }

    private static bool TestDns()
    {
        Console.WriteLine("[1/2] Resolving " + HostName + "...");

        try
        {
            IPHostEntry entry = Dns.GetHostEntry(HostName);
            IPAddress[] addresses = entry.AddressList;

            if (addresses == null || addresses.Length == 0)
            {
                Console.WriteLine("DNS returned no addresses.");
                return false;
            }

            for (int i = 0; i < addresses.Length; i++)
            {
                Console.WriteLine("  " + addresses[i].ToString());
            }

            Console.WriteLine("DNS succeeded.");
            return true;
        }
        catch (Exception ex)
        {
            PrintException(ex);
            return false;
        }
    }

    private static int TestHttps()
    {
        Console.WriteLine("[2/2] Requesting the public OpenRouter model list...");
        Console.WriteLine("Configured security protocols: " +
            ServicePointManager.SecurityProtocol.ToString());

        try
        {
            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create(ModelsUrl);

            request.Method = "GET";
            request.Accept = "application/json";
            request.UserAgent = "Win98-OpenRouter-Probe/0.1";
            request.KeepAlive = false;
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;

            using (HttpWebResponse response =
                (HttpWebResponse)request.GetResponse())
            {
                Console.WriteLine("HTTP status: " +
                    ((int)response.StatusCode).ToString() + " " +
                    response.StatusDescription);
                Console.WriteLine("HTTP protocol: " +
                    response.ProtocolVersion.ToString());
                Console.WriteLine("Content type: " + response.ContentType);

                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    char[] buffer = new char[512];
                    int count = reader.Read(buffer, 0, buffer.Length);

                    Console.WriteLine();
                    Console.WriteLine("Response preview:");
                    if (count > 0)
                    {
                        Console.WriteLine(new String(buffer, 0, count));
                    }
                    else
                    {
                        Console.WriteLine("(empty response)");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("RESULT: DIRECT HTTPS WORKS");
            return 0;
        }
        catch (WebException ex)
        {
            Console.WriteLine("HTTPS request failed.");
            Console.WriteLine("WebException status: " + ex.Status.ToString());

            HttpWebResponse response = ex.Response as HttpWebResponse;
            if (response != null)
            {
                Console.WriteLine("HTTP status: " +
                    ((int)response.StatusCode).ToString() + " " +
                    response.StatusDescription);
                response.Close();
            }

            PrintException(ex);
            Console.WriteLine();
            Console.WriteLine("RESULT: DIRECT HTTPS FAILED");
            return 3;
        }
        catch (Exception ex)
        {
            PrintException(ex);
            Console.WriteLine();
            Console.WriteLine("RESULT: DIRECT HTTPS FAILED");
            return 4;
        }
    }

    private static void PrintException(Exception ex)
    {
        int depth = 0;
        Exception current = ex;

        while (current != null)
        {
            Console.WriteLine("Exception " + depth.ToString() + ": " +
                current.GetType().FullName);
            Console.WriteLine("  " + current.Message);
            current = current.InnerException;
            depth++;
        }
    }
}
