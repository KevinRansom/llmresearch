using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

public class OllamaProxy
{
    private const int ProxyPort = 11435;
    private const int OllamaDefaultPort = 11434;
    private const string AckGuid = "{2FE6FFE6-C2AC-4DC0-BFAD-2371B47AAE71}";

    public async Task<bool> IsProxyAlreadyRunningAsync()
    {
        try
        {
            using var client = new HttpClient();
            var probePayload = new StringContent(
                $"{{\"model\": \"llama2\", \"isOllamaProxy\": \"{AckGuid}\"}}",
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"http://127.0.0.1:{ProxyPort}/api/generate", probePayload);
            var body = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode && body.Contains("\"acknowledged\":true");
        }
        catch
        {
            return false;
        }
    }

    public async Task StartAsync()
    {
        string ollamaHost = GetHost();
        Environment.SetEnvironmentVariable("OLLAMA_HOST", ollamaHost);
        Console.WriteLine($"[?] Proxy forwarding to Ollama at {ollamaHost}");

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{ProxyPort}/");
        listener.Start();
        Console.WriteLine($"[?] Proxy listening on http://127.0.0.1:{ProxyPort}/");

        while (true)
        {
            var context = await listener.GetContextAsync();
            _ = Task.Run(() => HandleRequestAsync(context, ollamaHost));
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, string targetHost)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var (isProbe, bufferedBody) = await IsProbePayloadAsync(request.InputStream);
            if (request.Url?.AbsolutePath == "/api/generate" &&
                request.HttpMethod == "POST" &&
                isProbe)
            {
                response.StatusCode = 200;
                var ack = Encoding.UTF8.GetBytes("{\"acknowledged\":true}");
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(ack, 0, ack.Length);
                response.OutputStream.Close();
                return;
            }

            using var client = new HttpClient();
            var forwardRequest = new HttpRequestMessage(
                new HttpMethod(request.HttpMethod),
                $"{targetHost}{request.Url?.PathAndQuery}"
            )
            {
                Content = new ByteArrayContent(bufferedBody)
            };

            if (!string.IsNullOrEmpty(request.ContentType))
            {
                forwardRequest.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
            }

            var upstreamResponse = await client.SendAsync(forwardRequest, HttpCompletionOption.ResponseHeadersRead);
            response.StatusCode = (int)upstreamResponse.StatusCode;

            var originalContentType = upstreamResponse.Content.Headers.ContentType?.ToString();
            var mediaType = upstreamResponse.Content.Headers.ContentType?.MediaType;
            response.ContentType = mediaType ?? "application/json";

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

    private static string GetHost()
    {
        var host = Environment.GetEnvironmentVariable("OLLAMA_HOST");

        if (!string.IsNullOrWhiteSpace(host))
        {
            try
            {
                var uri = new Uri(host, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri)
                    uri = new Uri($"http://{host}");

                return uri.ToString(); // Return parsed host if valid
            }
            catch
            {
                Console.Error.WriteLine($"[!] Invalid OLLAMA_HOST: '{host}'. Falling back to proxy.");
            }
        }

        return $"http://127.0.0.1:{ProxyPort}/";
    }
}
