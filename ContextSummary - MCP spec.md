# MCP Module Spec: MemoryTool

Below is an updated specification for a Model Control Protocol (MCP) module that exposes session memory as a tool. You can drop this into Gemma’s `.mcp/modules` folder and use GitHub Copilot to flesh out the implementation.

---

## 1. Module Manifest (`memory-tool.module.json`)

```json
{
  "id": "com.modelweave.memory",
  "version": "1.0.0",
  "name": "MemoryTool Module",
  "entrypoint": "ModelWeave.Memory.MemoryModule, ModelWeave.Memory",
  "commands": [
    {
      "name": "recall_memory",
      "description": "Retrieves past interactions or summaries relevant to the query. Use when the user asks about previous context or continuity.",
      "parameters": {
        "type": "object",
        "properties": {
          "query": { "type": "string" }
        },
        "required": ["query"]
      },
      "response": {
        "type": "object",
        "properties": {
          "summary": { "type": "string" },
          "confidence": { "type": "number" },
          "source_refs": {
            "type": "array",
            "items": { "type": "string" }
          }
        },
        "required": ["summary", "confidence"]
      }
    }
  ]
}
```

---

## 2. C# Module Interface (`MemoryModule.cs`)

```csharp
using System;
using System.Threading.Tasks;
using ModelControl.Protocol;   // hypothetical MCP namespace

namespace ModelWeave.Memory
{
    public class MemoryModule : IMcpModule
    {
        public ModuleManifest Manifest => ModuleManifest.Load("memory-tool.module.json");

        public async Task<CommandResult> InvokeAsync(CommandInvocation invocation)
        {
            if (invocation.Name != "recall_memory")
                throw new InvalidOperationException($"Unsupported command: {invocation.Name}");

            var query = invocation.Arguments.GetString("query");
            var (summary, confidence, refs) = await MemoryStore.FetchAsync(query);

            return new CommandResult
            {
                Output = new
                {
                    summary,
                    confidence,
                    source_refs = refs
                }
            };
        }
    }
}
```

---

## 3. Folder Structure

```
.mcp/
?? modules/
   ?? com.modelweave.memory/
      ?? memory-tool.module.json
      ?? ModelWeave.Memory.dll
```

---

## 4. Host Integration Steps

1.  **Module Discovery**  
    On startup, Gemma scans `.mcp/modules/*/memory-tool.module.json`.

2.  **Tool Registration**  
    It reads the `commands` array and adds each to the LLM’s tool list in the system prompt.

3.  **Invocation Handling**  
    When the LLM emits a `tool_call` for `recall_memory`, Gemma routes it to `MemoryModule.InvokeAsync()`.

4.  **Context Injection**  
    Gemma appends the `summary` result as an assistant message, then resumes the conversation.

---

You now have a correct MCP (Model Control Protocol) spec. Open these files in your .NET project, fire up GitHub Copilot, and let it generate the supporting classes (`MemoryStore`, JSON (de)serialization, error handling) to complete the module.
