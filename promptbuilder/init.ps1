param (
    [string]$SolutionName = "PromptBuilder"
)

$ErrorActionPreference = "Stop"

Write-Host "📦 Creating folders..."
$projects = @(
    "src/Prompt.Core",
    "src/Prompt.Builder",
    "src/Prompt.Flow",
    "tests/Prompt.Tests.Core",
    "tests/Prompt.Tests.Builder",
    "tests/Prompt.Tests.Flow"
)
$projects | ForEach-Object { New-Item -ItemType Directory -Force -Path $_ | Out-Null }

Write-Host "🔧 Creating solution..."
dotnet new sln -n $SolutionName

Write-Host "🚀 Creating projects..."
dotnet new classlib -n Prompt.Core -o src/Prompt.Core
dotnet new classlib -lang "F#" -n Prompt.Builder -o src/Prompt.Builder
dotnet new classlib -n Prompt.Flow -o src/Prompt.Flow
dotnet new xunit -n Prompt.Tests.Core -o tests/Prompt.Tests.Core
dotnet new classlib -lang "F#" -n Prompt.Tests.Builder -o tests/Prompt.Tests.Builder
dotnet new xunit -n Prompt.Tests.Flow -o tests/Prompt.Tests.Flow

Write-Host "🔗 Adding references..."
dotnet add tests/Prompt.Tests.Core/Prompt.Tests.Core.csproj reference src/Prompt.Core/Prompt.Core.csproj
dotnet add src/Prompt.Builder/Prompt.Builder.fsproj reference src/Prompt.Core/Prompt.Core.csproj
dotnet add tests/Prompt.Tests.Builder/Prompt.Tests.Builder.fsproj reference src/Prompt.Builder/Prompt.Builder.fsproj
dotnet add tests/Prompt.Tests.Builder/Prompt.Tests.Builder.fsproj reference src/Prompt.Core/Prompt.Core.csproj
dotnet add src/Prompt.Flow/Prompt.Flow.csproj reference src/Prompt.Core/Prompt.Core.csproj
dotnet add tests/Prompt.Tests.Flow/Prompt.Tests.Flow.csproj reference src/Prompt.Flow/Prompt.Flow.csproj
dotnet add tests/Prompt.Tests.Flow/Prompt.Tests.Flow.csproj reference src/Prompt.Core/Prompt.Core.csproj

Write-Host "📌 Adding all projects to solution..."
Get-ChildItem -Path . -Recurse -Include *.csproj,*.fsproj -File |
    ForEach-Object { dotnet sln add $_.FullName }

Write-Host "📄 Writing starter source files..."

Set-Content -Path src/Prompt.Core/PromptRequest.cs -Value @"
namespace Prompt.Core;

public record PromptRequest(string PromptText, float Temperature = 0.7f, string? Metadata = null);
"@

Set-Content -Path src/Prompt.Core/IPromptRunner.cs -Value @"
using System.Threading.Tasks;

namespace Prompt.Core;

public interface IPromptRunner
{
    Task<string> RunAsync(PromptRequest request);
}
"@

Set-Content -Path src/Prompt.Core/Json.cs -Value @"
using System.Text.Json;

namespace Prompt.Core;

public static class Json
{
    public static T? TryDeserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json); }
        catch { return default; }
    }
}
"@

Set-Content -Path src/Prompt.Core/OpenAIClient.cs -Value @"
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Prompt.Core;

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
                  .GetString() ?? string.Empty;
    }
}
"@

Set-Content -Path src/Prompt.Builder/PromptBuilder.fs -Value @"
namespace Prompt.Builder

open System.Net.Http
open Prompt.Core

type PromptBuilder() =
    member _.Bind(prompt: string, next: string -> Async<'T option>) =
        async {
            let client = OpenAIClient(HttpClient(), ""<your-key>"")
            let! raw = client.RunAsync(PromptRequest(prompt)) |> Async.AwaitTask
            return! next raw
        }

    member _.Return(x) = async { return Some x }
    member _.ReturnFrom(x) = x

let prompt = PromptBuilder()
"@

Set-Content -Path src/Prompt.Builder/Builder.fs -Value @"
namespace Prompt.Builder

open Prompt.Core

module Json =
    let tryDeserialize<'T> (json: string) : 'T option =
        Json.TryDeserialize<'T>(json)
"@

Set-Content -Path src/Prompt.Flow/PromptFlow.cs -Value @"
using System;
using System.Threading.Tasks;
using Prompt.Core;

namespace Prompt.Flow;

public class PromptFlow<T>
{
    private string _prompt = """";
    private Func<string, T?> _parser = _ => default!;
    private IPromptRunner _runner;
    private int _retryCount = 0;

    public PromptFlow(IPromptRunner runner) => _runner = runner;

    public PromptFlow<T> WithPrompt(string prompt)
    {
        _prompt = prompt;
        return this;
    }

    public PromptFlow<T> Expecting(Func<string, T?> parser)
    {
        _parser = parser;
        return this;
    }

    public PromptFlow<T> WithRetry(int count)
    {
        _retryCount = count;
        return this;
    }

    public async Task<T?> RunAsync()
    {
        for (int i = 0; i <= _retryCount; i++)
        {
            var result = await _runner.RunAsync(new PromptRequest(_prompt));
            var parsed = _parser(result);
            if (parsed != null)
                return parsed;
        }
        return default;
    }
}
"@

Write-Host "✅ PromptBuilder fully initialized with source code!"
