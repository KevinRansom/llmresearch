namespace ModelWeave.Tests.Builder

open Xunit
open ModelWeave.Providers
open ModelWeave.Builders

/// Smoketests for F# surface area
module SmokeTests =

    let executePrompt prompt =
        prompt
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Fact>]
    /// Smoke-test
    let ``Smoke-test`` () =
        let prompt = PromptBuilder(OllamaClient.llama_31_8b)            // Use the static instance of OllamaClient for llama_31_8b

        let workflow =
            prompt {
                let! response = "List the major London Airports by name, provide their postal addresses and main switchboard telephone number"
                // Here you could add deserialization, e.g. return! Json.tryDeserialize<Library list> response
                return response
            }

        // Run the workflow and print the result
        workflow
        |> executePrompt
        |> function
           | Some result -> printfn "Prompt result: %s" result
           | None -> printfn "Prompt failed."

    [<Fact>]
    /// Prompt Returns single String
    let ``Prompt Returns single String`` () =
        let prompt = PromptBuilder(OllamaClient.llama_31_8b)            // Use the static instance of OllamaClient for llama_31_8b

        let workflow =
            prompt {
                let! response = withSignature<string> "Which Egyptian King is buried in the Great Pyramid at Giza"
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

