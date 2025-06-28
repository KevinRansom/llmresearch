using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModelWeave.Core;

public class OpenAIClient : IPromptRunner
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _apiKey;

    public OpenAIClient(HttpClient http, string apiKey, string model = "gpt-3.5-turbo")
    {
        _http = http;
        _model = model;
        _apiKey = apiKey;
    }

    public async Task<string> RunAsync(PromptRequest request)
    {
        var payload = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = request.PromptText } },
            temperature = request.Temperature
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = JsonContent.Create(payload);

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString() ?? string.Empty;
    }
}
