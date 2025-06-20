Certainly! Here's the updated, copy-paste-friendly version of the **PromptBuilder proposal** for your convenience:

---

## 📦 Proposal: PromptBuilder — Structured AI Prompting for .NET Developers

### Overview

**PromptBuilder** is a dual-language .NET library that enables developers to declaratively incorporate large language model (LLM) reasoning into their applications. It empowers both F# and C# developers to tap into LLMs for facts, structured data, and procedural logic—without having to hand-roll prompt orchestration or glue code.

PromptBuilder is especially suited to problems developers face every day:

> “How do I get structured data or context-aware insights into my application—without scraping sites or wiring up obscure APIs?”

Now, asking something like “Airports within 50 miles of Duvall, WA” can be done directly in your programming language—with type safety, traceability, and retry logic built in.

---

### 🎯 Goals

- Build a shared, testable .NET **core library** for executing and parsing prompts  
- Offer an **idiomatic F# PromptBuilder** using computation expressions  
- Provide a **C# fluent API** that follows conventional async and chaining patterns  
- Support **agent-oriented composition** on top of prompts  
- Enable developers to embed *AI as a world-aware, type-safe function call*

---

### 🧱 Architecture

```
PromptBuilder/
├── PromptCore       # Shared C# logic for prompt execution
│   ├── PromptRequest, PromptResponse
│   ├── Json parsers, retry helpers
│   └── IPromptRunner interface
├── PromptBuilder.FSharp
│   └── prompt { ... } computation expression
│   └── F# Option-based deserialization
├── PromptBuilder.CSharp
│   └── Fluent PromptFlow API
└── PromptAgents     # Lightweight agent primitives (optional layer)
    └── Agent<'Input, 'State, 'Output>
```

---

### ✈️ F# Example

```fsharp
type Airport = { name: string; iata: string; location: string }

prompt {
    let! raw = """
        List airports within 50 miles of Duvall, WA.
        Return JSON: [{ "name": "...", "iata": "...", "location": "..." }]
    """
    return! Json.tryDeserialize<Airport list> raw
}
|> Option.iter (fun airports ->
    for a in airports do printfn "%s (%s)" a.name a.iata
)
```

---

### 🧰 C# Example

```csharp
var result = await PromptFlow
    .WithPrompt("""
        List airports within 50 miles of Duvall, WA.
        Return as JSON: [{ "name": "...", "iata": "...", "location": "..." }]
    """)
    .Expecting<List<Airport>>()
    .WithRetry(1)
    .RunAsync();
```

---

### 🤖 Agents on Top

```fsharp
let airportAgent : Agent<string, unit, Airport list option> =
    fun (query, _) -> prompt {
        let! raw = $"Find airports near {query}. Return JSON."
        return! Json.tryDeserialize<Airport list> raw
    }
```

```csharp
var agent = new PromptAgent<string, List<Airport>>()
    .WithPrompt(query => $"Airports near {query} as JSON")
    .Expecting<List<Airport>>();
```

---

### 🌍 Why This Matters

Developers often struggle to access structured world knowledge programmatically. PromptBuilder makes AI-grounded, semantically flexible knowledge **available as a typed input** in everyday code—without scraping, API hunting, or brittle integrations. It’s about empowering familiar workflows, not reinventing them.

---

### ✅ Use Cases

- Test generation and schema synthesis  
- Structured data retrieval (e.g., airports, embassies, regions)  
- Code analysis and explanation tools  
- CI pipelines that tag, summarize, or lint PRs  
- Local agents that perform structured, self-contained tasks

---

### 🛣️ Roadmap

| Phase | Deliverables |
|-------|--------------|
| v0.1  | Core prompt engine, JSON parsing, retry logic |
| v0.2  | F# computation expression with builder |
| v0.3  | C# fluent API (`PromptFlow`) |
| v0.4  | Agent abstraction + chaining |
| v1.0  | NuGet packaging, playgrounds, documentation |

---

Let me know if you’d like a matching README scaffold or a working stub for `PromptRunner.cs` and `PromptBuilder.fs`. I’d love to help you spin up the repo.