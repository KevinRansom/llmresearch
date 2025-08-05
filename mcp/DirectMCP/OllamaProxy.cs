using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class OllamaProxyServer
{
    const int ProxyPort = 11435;                // Your proxy listens here
    const int OllamaDefaultPort = 11434;

    static public async Task StartOllamaProxy()
    {
        int ollamaPort = FindOllamaPort() ?? OllamaDefaultPort;
        string ollamaHost = $"http://127.0.0.1:{ollamaPort}";
        Environment.SetEnvironmentVariable("OLLAMA_HOST", ollamaHost);
        Console.WriteLine($"[✓] Proxy forwarding to Ollama at {ollamaHost}");

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{ProxyPort}/");
        listener.Start();
        Console.WriteLine($"[✓] Proxy listening on http://localhost:{ProxyPort}/");

        while (true)
        {
            Console.WriteLine(".");
            var context = await listener.GetContextAsync();
            Console.WriteLine("+");
            _ = Task.Run(() => HandleRequest(context, ollamaHost));
            Console.WriteLine("-");
        }
    }

    static async Task HandleRequest(HttpListenerContext context, string ollamaHost)
    {
        try
        {
            var request = context.Request;
            var body = new StreamReader(request.InputStream).ReadToEnd();
            var targetUrl = $"{ollamaHost}{request.Url?.PathAndQuery}";

            using var client = new HttpClient();
            var content = new StringContent(body, Encoding.UTF8, request.ContentType ?? "application/json");
            var response = await client.PostAsync(targetUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(responseBody));
            context.Response.Close();

            Console.WriteLine($"[→] Forwarded to Ollama: {request.Url?.PathAndQuery}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Proxy error: {ex.Message}");
            context.Response.StatusCode = 500;
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"{{\"error\":\"{ex.Message}\"}}"));
            context.Response.Close();
        }
    }

    static int? FindOllamaPort()
    {
        var ipProps = IPGlobalProperties.GetIPGlobalProperties();
        foreach (var ep in ipProps.GetActiveTcpListeners())
        {
            if (ep.Address.ToString() == "127.0.0.1" && ep.Port >= 11434 && ep.Port <= 11444)
                return ep.Port;
        }
        return null;
    }
}
