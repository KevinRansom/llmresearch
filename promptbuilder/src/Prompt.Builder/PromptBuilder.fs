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
