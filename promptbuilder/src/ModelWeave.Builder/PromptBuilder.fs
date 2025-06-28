namespace ModelWeave.Builders

open System.Threading.Tasks
open ModelWeave.Core

type PromptBuilder(runner: IPromptRunner) =

    member _.Bind
        (instruction: Prompt<'T>, continuation: 'T option -> Task<'R>) : Task<'R> =

        task {
            let jsonPrompt = PromptRequest $"Return the following as JSON: {instruction}"
            let! raw = runner.RunAsync(jsonPrompt)

            let result =
                try
                    match PromptRegistry.Deserialize<'T>(raw) with
                    | null -> None
                    | value -> Some value
                with _ -> None

            return! continuation result
        }

    member this.Bind (instruction: string, continuation: string option -> Task<'R>) : Task<'R> =
        this.Bind(Prompt.withSignature<string> instruction, continuation)

    member _.Return(x: 'T) : Task<'T> =
        Task.FromResult x
