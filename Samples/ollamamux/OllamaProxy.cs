namespace OllamaMux
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    class OllamaProxy
    {
        private const int OllamaDefaultPort = 11434;
        private const int OllamaExecutionPort = 11435;
        private const string AckGuid = "{2FE6FFE6-C2AC-4DC0-BFAD-2371B47AAE71}";

        public async Task<bool> IsProxyAlreadyRunningAsync()
        {
            var (ollamaMuxHost, _) = GetHosts();

            try
            {
                using var client = new HttpClient();
                var probePayload = new StringContent(
                    $"{{\"model\": \"llama2\", \"isOllamaProxy\": \"{AckGuid}\"}}",
                    Encoding.UTF8,
                    "application/json"
                );

                var uri = new Uri(ollamaMuxHost);
                var response = await client.PostAsync($"{uri.Scheme}://{uri.Host}:{uri.Port}/api/generate", probePayload);
                var body = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode && body.Contains("\"acknowledged\":true");
            }
            catch
            {
                return false;
            }
        }

        private Task StartAsync(string ollamaMuxHost, string ollamaExecutionHost)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"{ollamaMuxHost}");
            listener.Start();

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context, ollamaExecutionHost));
                }
            });

            Console.WriteLine($"[?] Proxy listening on {ollamaMuxHost}");
            Console.WriteLine($"[✓] Forwarding to Ollama at {ollamaExecutionHost}");
            return Task.CompletedTask;
        }

        public string StartProxy()
        {
            var (ollamaMuxHost, ollamaExecutionHost) = GetHosts();
            _ = StartAsync(ollamaMuxHost, ollamaExecutionHost);
            return ollamaExecutionHost;
        }

        private async Task HandleRequestAsync(HttpListenerContext context, string targetHost)
        {
            var request = context.Request;
            var response = context.Response;

            Console.WriteLine($"[→] Incoming {request.HttpMethod} {request.Url?.PathAndQuery}");
            Console.WriteLine($"     Headers: {string.Join(", ", request.Headers.AllKeys.Select(k => $"{k}: {request.Headers[k]}"))}");

            if (request.HttpMethod == "OPTIONS")
            {
                Console.WriteLine("[↻] Handling CORS preflight");
                response.StatusCode = 204;
                response.AddHeader("Access-Control-Allow-Origin", request.Headers["Origin"] ?? "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, User-Agent, Accept");
                response.AddHeader("Access-Control-Max-Age", "86400");
                response.OutputStream.Close();
                return;
            }

            try
            {
                var (isProbe, bufferedBody) = await IsProbePayloadAsync(request.InputStream);
                if (request.Url?.AbsolutePath == "/api/generate" &&
                    request.HttpMethod == "POST" &&
                    isProbe)
                {
                    Console.WriteLine("[✓] Probe detected, sending acknowledgment");
                    response.StatusCode = 200;
                    var ack = Encoding.UTF8.GetBytes("{\"acknowledged\":true}");
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(ack, 0, ack.Length);
                    response.OutputStream.Close();
                    return;
                }

                using var client = new HttpClient();


                var baseUri = new Uri(targetHost.TrimEnd('/'));
                var relativeUri = request.Url?.PathAndQuery?.TrimStart('/');
                var fullUri = new Uri(baseUri, relativeUri ?? string.Empty);

                var forwardRequest = new HttpRequestMessage(
                    new HttpMethod(request.HttpMethod),
                    fullUri
                )
                {
                    Content = new ByteArrayContent(bufferedBody)
                };

                if (!string.IsNullOrEmpty(request.ContentType))
                {
                    forwardRequest.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
                }

                Console.WriteLine($"[→] Forwarding to {forwardRequest.RequestUri}");

                var upstreamResponse = await client.SendAsync(forwardRequest, HttpCompletionOption.ResponseHeadersRead);
                response.StatusCode = (int)upstreamResponse.StatusCode;

                var originalContentType = upstreamResponse.Content.Headers.ContentType?.ToString();
                var mediaType = upstreamResponse.Content.Headers.ContentType?.MediaType;
                response.ContentType = mediaType ?? "application/json";

                Console.WriteLine($"[←] Upstream responded with {response.StatusCode} ({mediaType})");

                if (originalContentType != response.ContentType)
                {
                    Console.WriteLine($"[!] Sanitized Content-Type. Original: '{originalContentType}', Sent: '{response.ContentType}'");
                }

                using var stream = await upstreamResponse.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(response.OutputStream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Proxy error: {ex.Message}");
                response.StatusCode = 500;
                var errorBytes = Encoding.UTF8.GetBytes($"{{\"error\":\"{ex.Message}\"}}");
                await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
            }
            finally
            {
                response.AddHeader("Access-Control-Allow-Origin", request.Headers["Origin"] ?? "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, User-Agent, Accept");
                response.AddHeader("Access-Control-Max-Age", "86400");

                response.OutputStream.Close();
            }
        }

        private async Task<(bool isProbe, byte[] bufferedBody)> IsProbePayloadAsync(Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bufferedBody = ms.ToArray();

            var content = Encoding.UTF8.GetString(bufferedBody);
            var isProbe = content.Contains($"\"isOllamaProxy\": \"{AckGuid}\"");
            return (isProbe, bufferedBody);
        }

        private static (string ollamaMuxHost, string ollamaExecutionHost) GetHosts()
        {
            var host = Environment.GetEnvironmentVariable("OLLAMA_HOST");
            string ollamaMuxHost = $"http://127.0.0.1:{OllamaDefaultPort}/";
            string ollamaExecutionHost = $"http://127.0.0.1:{OllamaExecutionPort}/";

            if (!string.IsNullOrWhiteSpace(host))
            {
                try
                {
                    var uri = new Uri(host, UriKind.RelativeOrAbsolute);
                    if (!uri.IsAbsoluteUri)
                        uri = new Uri($"http://{host}");

                    if (!IPAddress.TryParse(uri.Host, out var ip) || !IPAddress.IsLoopback(ip))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[!] WARNING: Proxy is binding to external IP '{uri.Host}'.");
                        Console.WriteLine($"    You may need to configure your firewall to allow inbound traffic on port {uri.Port}.");
                        Console.WriteLine($"    This could expose your Ollama instance to the network. Proceed with caution.");
                        Console.ResetColor();
                    }
                    ollamaMuxHost = uri.ToString();
                }
                catch
                {
                    Console.Error.WriteLine($"[!] Invalid OLLAMA_HOST: '{host}'. Falling back to default.");
                }
            }

            return (ollamaMuxHost, ollamaExecutionHost);
        }
    }
}
