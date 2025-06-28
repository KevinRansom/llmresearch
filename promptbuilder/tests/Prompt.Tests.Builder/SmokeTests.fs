namespace Prompt.Tests.Builder

open Xunit
open Prompt.Providers
open Prompt.Builder

/// Smoketests for F# surface area
module SmokeTests =


    [<Fact>]
    /// Example prompt using the PromptBuilder and OllamaClient.llama_31_8b
    let ``Sample test - always passes`` () =
        // Use the static instance of OllamaClient for llama_31_8b
        let runner = OllamaClient.llama_31_8b
        let prompt = PromptBuilder(runner)

        let workflow =
            prompt {
                let! response = "List 3 famous F# libraries as JSON"
                // Here you could add deserialization, e.g. return! Json.tryDeserialize<Library list> response
                return response
            }

        // Run the workflow and print the result
        workflow
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> function
            | Some result -> printfn "Prompt result: %s" result
            | None -> printfn "Prompt failed."
