namespace ModelWeave.Tests.Builder

open Xunit
open ModelWeave.Tests.Assertions
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
                return response
            }

        // Run the workflow and print the result
        workflow
        |> expect (fun _name -> ())

    /// Prompt Returns single String
    [<Fact>]
    let ``Prompt Returns single String`` () =
        let prompt = PromptBuilder(OllamaClient.llama_31_8b)            // Use the static instance of OllamaClient for llama_31_8b

        let workflow =
            prompt {
                let! response = ask<string> "Which Egyptian King is buried in the Great Pyramid at Giza"
                return response
            }

        // Run the workflow and print the result
        workflow
        |> expect (fun name -> Assert.StartsWith("Kh", name))

    [<Fact>]
    /// Prompt Returns Tuple
    let ``Prompt Returns Tuple`` () =
        let prompt = PromptBuilder(OllamaClient.llama_31_8b)            // Use the static instance of OllamaClient for llama_31_8b

        let workflow =
            prompt {
                let! response = ask<(string * int * bool)> "Which Egyptian King built the Great Pyramid, when was it completed, and is it open to tourists?"
                return response
            }

        // Run the workflow and print the result
        workflow
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> function
           | Some result -> printfn "Prompt result: %A" result
           | None -> failwith "Prompt failed."

