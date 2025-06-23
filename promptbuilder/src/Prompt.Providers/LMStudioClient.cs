using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Prompt.Core;

namespace Prompt.Providers;

public class LMStudioClient : IPromptRunner
{
    private readonly HttpClient _http;
    private readonly string _model;

    public LMStudioClient(HttpClient http, string model = "local-model")
    {
        _http = http;
        _model = model;
    }

    public async Task<string> RunAsync(PromptRequest request)
    {
        var payload = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = request.PromptText } },
            temperature = request.Temperature
        };

        var res = await _http.PostAsJsonAsync("http://localhost:1234/v1/chat/completions", payload);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString() ?? "";
    }
}
