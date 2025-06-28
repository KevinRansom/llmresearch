using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Prompt.Core;

namespace Prompt.Providers;

public class OllamaClient : IPromptRunner
{
    private readonly HttpClient _http;
    private readonly string _model;

    public static OllamaClient llama_31_8b = new OllamaClient(new HttpClient(), "llama3.1:8b");
    public static OllamaClient gemma3_12b = new OllamaClient(new HttpClient(), "gemma3:12b");

    public OllamaClient(HttpClient http, string model = "llama3.1:8b")
    {
        _http = http;
        _model = model;
    }

    public async Task<string> RunAsync(PromptRequest request)
    {
        var payload = new
        {
            model = _model,
            prompt = request.PromptText,
            options = new { temperature = request.Temperature },
            stream = false
        };

        var res = await _http.PostAsJsonAsync("http://localhost:11434/api/generate", payload);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("response").GetString() ?? "";
    }
}
