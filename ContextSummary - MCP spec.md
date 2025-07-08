# MCP Module Spec: MemoryTool

This specification defines a Model Control Protocol (MCP) module that exposes session memory as an invocable tool. Designed for compatibility with Gemma’s `.mcp/modules` discovery mechanism, it supports structured recall operations that return context summaries and traceability metadata.

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
          "confidence": { "type": ["number", "null"] },
          "source_refs": {
            "type": "array",
            "items": { "type": "string" }
          }
        },
        "required": ["summary"]
      }
    }
  ],
  "schema_version": "1.0"
}
```

---

## 2. C# Module Interface (`MemoryModule.cs`)

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using ModelControl.Protocol;

namespace ModelWeave.Memory
{
    public class MemoryModule : IMcpModule
    {
        public ModuleManifest Manifest => ModuleManifest.Load("memory-tool.module.json");

        public async Task<CommandResult> InvokeAsync(CommandInvocation invocation, CancellationToken cancellationToken = default)
        {
            if (invocation.Name != "recall_memory")
                throw new InvalidOperationException($"Unsupported command: {invocation.Name}");

            var query = invocation.Arguments.GetString("query");
            var (summary, confidence, refs) = await MemoryStore.FetchAsync(query, cancellationToken);

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
      ?? docs/
      ?   ?? usage.md
      ?? samples/
          ?? test_invocation.json
```

---

## 4. Host Integration Steps

1. **Module Discovery**  
   On startup, the LLM scans all `.mcp/modules/*/memory-tool.module.json` files to enumerate available MCP modules.

2. **Tool Registration**  
   It parses the `commands` block and incorporates each tool into the system prompt for tool-calling LLMs.

3. **Invocation Handling**  
   When the model emits a `tool_call` for `recall_memory`, Gemma dispatches it to `MemoryModule.InvokeAsync()`.

4. **Context Injection**  
   On successful completion, the LLM appends the `summary` to the assistant message stream and continues the session. If the call fails, fallback behavior (no-op, retry, or omission) must be handled gracefully by the host runtime.

Drafted by Kevin, annotated by Gemma, assembled by Copilot (Dood by nature, Copilot by name).
