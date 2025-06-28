```markdown
# ModelWeave

**ModelWeave** is a lightweight .NET framework for orchestrating type-safe interactions with language models using structured prompts and typed bindings.

- ? Compose fluent prompt workflows in **C#** or use **F# computation expressions**  
- ?? Define return types using records, unions, or schema projections  
- ?? Swap providers: Ollama, OpenAI, or custom runners with minimal overhead  
- ?? Built for testing and iteration—ideal for automation, research, or app-layer orchestration

---

## ?? Getting Started

### Install via NuGet

```bash
dotnet add package ModelWeave
```

> Compatible with .NET 7+ | F# 7+ | C# 11+

---

## ?? F# Example (Computation Expression)

```fsharp
open ModelWeave.Providers
open ModelWeave.Builder

let prompt = PromptBuilder(OllamaClient.llama_31_8b)

let workflow = 
    prompt {
        let! raw = "List 3 famous F# libraries as JSON"
        // Optionally: return! Json.tryDeserialize<Library list> raw
        return raw
    }

workflow 
|> Async.AwaitTask 
|> Async.RunSynchronously 
|> printfn "Model replied: %s"
```

---

## ?? C# Example (Fluent API)

```csharp
using ModelWeave;
using ModelWeave.Providers;

var client = OllamaClient.llama_31_8b;
var prompt = PromptBuilder.Create(client);

var result = await prompt
    .WithInstruction("List 3 famous C# libraries as JSON")
    //.Expect<Library[]>()
    .RunAsync();

Console.WriteLine(result ?? "Prompt failed.");
```

> The fluent API supports method chaining, typed deserialization, and simple fallback behavior.

---

## ?? Project Structure

- `ModelWeave.Builder` – Builders for C# and F# orchestration styles  
- `ModelWeave.Providers` – Drop-in client runners (Ollama, OpenAI, etc.)  
- `ModelWeave.Schema` *(optional)* – Type binding and shape enforcement

---

## ?? Documentation

- API Reference (coming soon)  
- Guides for prompt shaping and typed bindings  
- Provider integration tutorials (Ollama, REST, OpenAI, etc.)

---

## ?? License

MIT License © 2025 Kevin

---

```
         ??????   M O D E L W E A V E   ??????  
   Orchestrate structured prompting from .NET  
     Bridging the fuzzy and the formal—effectively.
```
```
