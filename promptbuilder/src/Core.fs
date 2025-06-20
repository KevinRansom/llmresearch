module PromptCore

open System
open System.Net.Http
open System.Text.Json

// Represents a prompt request with metadata
type PromptRequest = {
    PromptText: string
    Temperature: float
    Metadata: Map<string, string>
}

// Interface for running prompts
type IPromptRunner =
    abstract member RunAsync: PromptRequest -> Async<string>

// Default OpenAI implementation using HttpClient
type OpenAIRunner(apiKey: string) =
    let client = new HttpClient()
    interface IPromptRunner with
        member _.RunAsync(request: PromptRequest) =
            async {
                let content = new StringContent(request.PromptText)
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}")
                let! response = client.PostAsync("https://api.openai.com/v1/completions", content) |> Async.AwaitTask
                return! response.Content.ReadAsStringAsync() |> Async.AwaitTask
            }

// JSON deserialization helper
module Json =
    let tryDeserialize<'T> (json: string) : 'T option =
        try
            let options = JsonSerializerOptions()
            options.PropertyNameCaseInsensitive <- true
            Some (JsonSerializer.Deserialize<'T>(json, options))
        with _ -> None

// Retry logic with optional logging
let retryWithLogging (operation: unit -> Async<'T>) (maxRetries: int) (log: string -> unit) : Async<'T> =
    async {
        let rec attempt count =
            async {
                try
                    return! operation()
                with ex ->
                    log $"Attempt {count} failed: {ex.Message}"
                    if count < maxRetries then
                        return! attempt (count + 1)
                    else
                        raise ex
            }
        return! attempt 1
    }