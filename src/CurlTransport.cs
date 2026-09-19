using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Harness98
{
    public interface IHttpTransport
    {
        HttpResult Get(string url, string apiKey);
        HttpResult PostJson(string url, string json, string apiKey);
    }

    public sealed class CurlTransport : IHttpTransport
    {
        private readonly string baseDirectory;
        private readonly string curlPath;
        private readonly string certificatePath;
        private readonly string requestPath;
        private readonly string responsePath;

        public CurlTransport(string baseDirectory)
        {
            this.baseDirectory = baseDirectory;
            curlPath = FindDependency(baseDirectory, "curl.exe");
            certificatePath = FindDependency(baseDirectory, "cacert.pem");
            requestPath = Path.Combine(baseDirectory, "REQ.JSN");
            responsePath = Path.Combine(baseDirectory, "RESP.JSN");
        }

        public void ValidateDependencies()
        {
            if (!File.Exists(curlPath))
            {
                throw new FileNotFoundException("Could not find curl.exe.", curlPath);
            }
            if (!File.Exists(certificatePath))
            {
                throw new FileNotFoundException("Could not find cacert.pem.",
                    certificatePath);
            }
        }

        public HttpResult Get(string url, string apiKey)
        {
            return Send(url, "GET", null, apiKey);
        }

        public HttpResult PostJson(string url, string json, string apiKey)
        {
            return Send(url, "POST", json, apiKey);
        }

        private HttpResult Send(string url, string method, string body, string apiKey)
        {
            DeleteIfPresent(responsePath);
            DeleteIfPresent(requestPath);

            try
            {
                if (body != null)
                {
                    WriteUtf8(requestPath, body);
                }

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = curlPath;
                start.Arguments = "--config -";
                start.WorkingDirectory = baseDirectory;
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.RedirectStandardInput = true;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;

                string standardOutput;
                string standardError;
                int exitCode;

                using (Process process = Process.Start(start))
                {
                    process.StandardInput.Write(BuildConfig(url, method, body != null,
                        apiKey));
                    process.StandardInput.Close();
                    standardOutput = process.StandardOutput.ReadToEnd();
                    standardError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }

                if (exitCode != 0)
                {
                    string detail = standardError.Trim();
                    if (detail.Length == 0)
                    {
                        detail = "curl exited with code " + exitCode.ToString() + ".";
                    }
                    throw new ApplicationException(detail);
                }

                int statusCode;
                if (!Int32.TryParse(standardOutput.Trim(), out statusCode))
                {
                    throw new ApplicationException("curl did not return an HTTP status code. " +
                        standardOutput.Trim());
                }

                HttpResult result = new HttpResult();
                result.StatusCode = statusCode;
                result.Body = File.Exists(responsePath) ? ReadUtf8(responsePath) : "";
                return result;
            }
            finally
            {
                DeleteIfPresent(requestPath);
                DeleteIfPresent(responsePath);
            }
        }

        private string BuildConfig(string url, string method, bool hasBody,
            string apiKey)
        {
            StringBuilder config = new StringBuilder();
            AppendOption(config, "url", url);
            AppendOption(config, "request", method);
            AppendOption(config, "header", "Authorization: Bearer " + apiKey);
            AppendOption(config, "header", "Content-Type: application/json");
            AppendOption(config, "header", "X-OpenRouter-Title: Harness98");
            AppendOption(config, "cacert", certificatePath);
            AppendOption(config, "output", responsePath);
            AppendOption(config, "write-out", "%{http_code}");
            config.AppendLine("silent");
            config.AppendLine("show-error");
            config.AppendLine("tlsv1.2");
            config.AppendLine("connect-timeout = 30");
            config.AppendLine("max-time = 300");

            if (hasBody)
            {
                AppendOption(config, "data-binary", "@" + requestPath);
            }

            return config.ToString();
        }

        private static void AppendOption(StringBuilder config, string name,
            string value)
        {
            config.Append(name);
            config.Append(" = \"");
            config.Append(EscapeConfig(value));
            config.AppendLine("\"");
        }

        private static string EscapeConfig(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string FindDependency(string baseDirectory, string fileName)
        {
            string besideProgram = Path.Combine(baseDirectory, fileName);
            if (File.Exists(besideProgram))
            {
                return besideProgram;
            }

            return Path.Combine(Path.Combine(baseDirectory, "curl-probe"), fileName);
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

        private static void DeleteIfPresent(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Temporary response cleanup should not hide the real result.
            }
        }
    }
}
