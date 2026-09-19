#if LIBCURL_DLL
using System;
using System.IO;
using System.Text;
using SeasideResearch.LibCurlNet;

namespace Harness98
{
    public sealed class LibCurlTransport : IHttpTransport, IDisposable
    {
        private readonly string certificatePath;
        private bool initialized;
        private bool disposed;

        public LibCurlTransport(string baseDirectory)
        {
            certificatePath = Path.Combine(baseDirectory, "CACERT.PEM");
        }

        public void ValidateDependencies()
        {
            if (!File.Exists(certificatePath))
            {
                throw new FileNotFoundException("Could not find CACERT.PEM.",
                    certificatePath);
            }

            RequireBesideProgram("LibCurlShim.dll");
            RequireBesideProgram("libcurl.dll");
            RequireBesideProgram("libeay32.dll");
            RequireBesideProgram("ssleay32.dll");

            string localRuntime = Path.Combine(
                Path.GetDirectoryName(certificatePath), "MSVCR80.DLL");
            string systemRuntime = Path.Combine(Environment.SystemDirectory,
                "MSVCR80.DLL");
            if (!File.Exists(localRuntime) && !File.Exists(systemRuntime))
            {
                throw new FileNotFoundException("MSVCR80.DLL is required by the " +
                    "LibCurl/OpenSSL DLL set. Install the Visual C++ 2005 runtime " +
                    "or place MSVCR80.DLL beside ORCHAT2.EXE.");
            }

            CURLcode result = Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_DEFAULT);
            if (result != CURLcode.CURLE_OK)
            {
                throw new ApplicationException("LibCurl initialization failed: " +
                    result.ToString());
            }

            initialized = true;
        }

        private void RequireBesideProgram(string fileName)
        {
            string path = Path.Combine(Path.GetDirectoryName(certificatePath), fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Required DLL is missing: " + fileName,
                    path);
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

        public void Dispose()
        {
            if (!disposed && initialized)
            {
                Curl.GlobalCleanup();
            }
            disposed = true;
        }

        private HttpResult Send(string url, string method, string body, string apiKey)
        {
            if (!initialized || disposed)
            {
                throw new InvalidOperationException("LibCurl is not initialized.");
            }

            Easy easy = new Easy();
            Slist headers = new Slist();
            MemoryStream received = new MemoryStream();
            Easy.WriteFunction write = delegate(byte[] buffer, int size, int count,
                object extraData)
            {
                int length = size * count;
                received.Write(buffer, 0, length);
                return length;
            };

            try
            {
                headers.Append("Authorization: Bearer " + apiKey);
                headers.Append("Content-Type: application/json");
                headers.Append("X-OpenRouter-Title: Harness98");
                headers.Append("Expect:");

                Set(easy, CURLoption.CURLOPT_URL, url);
                Set(easy, CURLoption.CURLOPT_CAINFO, certificatePath);
                Set(easy, CURLoption.CURLOPT_HTTPHEADER, headers);
                Set(easy, CURLoption.CURLOPT_WRITEFUNCTION, write);
                Set(easy, CURLoption.CURLOPT_CONNECTTIMEOUT, 30);
                Set(easy, CURLoption.CURLOPT_TIMEOUT, 300);
                Set(easy, CURLoption.CURLOPT_FOLLOWLOCATION, 1L);
                Set(easy, CURLoption.CURLOPT_SSL_VERIFYPEER, true);
                Set(easy, CURLoption.CURLOPT_SSL_VERIFYHOST, 2L);
                Set(easy, CURLoption.CURLOPT_USERAGENT, "Harness98/2.0");

                if (method == "POST")
                {
                    Set(easy, CURLoption.CURLOPT_POST, true);
                    Set(easy, CURLoption.CURLOPT_POSTFIELDS, body);
                    Set(easy, CURLoption.CURLOPT_POSTFIELDSIZE, body.Length);
                }
                else
                {
                    Set(easy, CURLoption.CURLOPT_HTTPGET, true);
                }

                CURLcode result = easy.Perform();
                if (result != CURLcode.CURLE_OK)
                {
                    throw new ApplicationException("LibCurl request failed: " +
                        easy.StrError(result) + " (" + result.ToString() + ")");
                }

                int statusCode = 0;
                CURLcode infoResult = easy.GetInfo(CURLINFO.CURLINFO_RESPONSE_CODE,
                    ref statusCode);
                if (infoResult != CURLcode.CURLE_OK)
                {
                    throw new ApplicationException("Could not read the HTTP status: " +
                        easy.StrError(infoResult));
                }

                HttpResult response = new HttpResult();
                response.StatusCode = statusCode;
                response.Body = Encoding.UTF8.GetString(received.ToArray());
                return response;
            }
            finally
            {
                headers.FreeAll();
                easy.Cleanup();
                received.Close();
            }
        }

        private static void Set(Easy easy, CURLoption option, object value)
        {
            CURLcode result = easy.SetOpt(option, value);
            if (result != CURLcode.CURLE_OK)
            {
                throw new ApplicationException("Could not set " + option.ToString() +
                    ": " + easy.StrError(result));
            }
        }
    }
}
#endif
