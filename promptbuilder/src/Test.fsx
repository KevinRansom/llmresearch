#load "Core.fs"
#load "Builder.fs"

open PromptCore
open PromptBuilder

type Airport = {
    Name: string
    Location: string
}

// Test deserialization of mocked JSON output
let testDeserialization () =
    let json = """[{"Name":"Seattle-Tacoma International Airport","Location":"Seattle, WA"}]"""
    match Json.tryDeserialize<Airport list> json with
    | Some airports -> printfn "Deserialized airports: %A" airports
    | None -> printfn "Failed to deserialize JSON."

// Test composed workflows
let testPromptWorkflow () =
    let workflow =
        prompt {
            let! json = "List airports near Duvall, WA as JSON"
            return! Json.tryDeserialize<Airport list> json
        }
    printfn "Workflow result: %A" workflow

// Test fallback behavior on malformed LLM output
let testFallbackBehavior () =
    let json = """[{"Name":"Invalid JSON"}""" // Malformed JSON
    match Json.tryDeserialize<Airport list> json with
    | Some airports -> printfn "Deserialized airports: %A" airports
    | None -> printfn "Fallback triggered: Failed to deserialize JSON."

// Run tests
testDeserialization()
testPromptWorkflow()
testFallbackBehavior()