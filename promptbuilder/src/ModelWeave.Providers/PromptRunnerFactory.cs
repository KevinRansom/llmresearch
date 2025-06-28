using System;
using System.Net.Http;
using ModelWeave.Core;

namespace ModelWeave.Providers;

public static class PromptRunnerFactory
{
    public static IPromptRunner Create(string provider, HttpClient http, string model, string apiKey = "")
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" => new OpenAIClient(http, apiKey, model),
            "ollama" => new OllamaClient(http, model),
            "lmstudio" => new LMStudioClient(http, model),
            _ => throw new NotSupportedException($"Unknown provider: {provider}")
        };
    }
}
