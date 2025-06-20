(*
GOAL:
Build a library to simplify structured, type-safe prompting of large language models (LLMs) from F#.

STRUCTURE:
Split into:
1. PromptCore: shared logic for prompt execution, response parsing, and retry.
2. PromptBuilder: an F# computation expression wrapping PromptCore to create prompt blocks like:
    prompt {
        let! json = "List airports near Duvall, WA as JSON"
        return! Json.tryDeserialize<Airport list> json
    }

PromptCore Requirements:
- PromptRequest type with prompt text, temperature, and metadata
- IPromptRunner interface with RunAsync
- Default OpenAI implementation using HttpClient
- Json.tryDeserialize<'T> helper using System.Text.Json
- Retry and optional logging hooks

PromptBuilder Requirements:
- Define PromptBuilder computation expression
- Support return, return!, let!, and match!
- Support pipeline to attach deserializer, e.g. |> Json.tryDeserialize<T>
- Return option type, not exceptions
- Keep side effects pure except inside prompt {}

TESTS:
- Provide sample test cases for:
    - Deserialization of mocked JSON output
    - PromptBuilder composed workflows
    - Safe fallback behavior on malformed LLM output

NEXT STEP:
Scaffold folder structure with:
- Core.fs for PromptCore logic
- Builder.fs for PromptBuilder logic
- Test.fsx with test cases for each
*)
