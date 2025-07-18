$ErrorActionPreference = "Stop"

Write-Host "?  Creating Prompt.Providers..."
$projectPath = "src/Prompt.Providers"
New-Item -ItemType Directory -Force -Path $projectPath | Out-Null

Write-Host "?  Creating project..."
dotnet new classlib -n Prompt.Providers -o $projectPath

Write-Host "?  Adding reference to Prompt.Core..."
dotnet add $projectPath/Prompt.Providers.csproj reference src/Prompt.Core/Prompt.Core.csproj

Write-Host "? Adding Prompt.Providers to solution..."
dotnet sln add $projectPath/Prompt.Providers.csproj

Write-Host "?  Writing providers..."

Set-Content -Path "$projectPath/OpenAIClient.cs" -Value @"
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Prompt.Core;

namespace Prompt.Providers;

public class OpenAIClient : IPromptRunner
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _apiKey;

    public OpenAIClient(HttpClient http, string apiKey, string model = ""gpt-3.5-turbo"")
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
            messages = new[] { new { role = ""user"", content = request.PromptText } },
            temperature = request.Temperature
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, ""https://api.openai.com/v1/chat/completions"");
        req.Headers.Add(""Authorization"", $""Bearer {_apiKey}"");
        req.Content = JsonContent.Create(payload);

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
                  .GetProperty(""choices"")[0]
                  .GetProperty(""message"")
                  .GetProperty(""content"")
                  .GetString() ?? """";
    }
}
"@

Set-Content -Path "$projectPath/OllamaClient.cs" -Value @"
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

    public OllamaClient(HttpClient http, string model = ""llama3"")
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

        var res = await _http.PostAsJsonAsync(""http://localhost:11434/api/generate"", payload);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty(""response"").GetString() ?? """";
    }
}
"@

Set-Content -Path "$projectPath/LMStudioClient.cs" -Value @"
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

    public LMStudioClient(HttpClient http, string model = ""local-model"")
    {
        _http = http;
        _model = model;
    }

    public async Task<string> RunAsync(PromptRequest request)
    {
        var payload = new
        {
            model = _model,
            messages = new[] { new { role = ""user"", content = request.PromptText } },
            temperature = request.Temperature
        };

        var res = await _http.PostAsJsonAsync(""http://localhost:1234/v1/chat/completions"", payload);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
                  .GetProperty(""choices"")[0]
                  .GetProperty(""message"")
                  .GetProperty(""content"")
                  .GetString() ?? """";
    }
}
"@

Set-Content -Path "$projectPath/PromptRunnerFactory.cs" -Value @"
using System;
using System.Net.Http;
using Prompt.Core;

namespace Prompt.Providers;

public static class PromptRunnerFactory
{
    public static IPromptRunner Create(string provider, HttpClient http, string model, string apiKey = """")
    {
        return provider.ToLowerInvariant() switch
        {
            ""openai"" => new OpenAIClient(http, apiKey, model),
            ""ollama"" => new OllamaClient(http, model),
            ""lmstudio"" => new LMStudioClient(http, model),
            _ => throw new NotSupportedException($""Unknown provider: {provider}"")
        };
    }
}
"@

Write-Host "? Prompt.Providers added with OpenAI, Ollama, LMStudio runners and factory."
