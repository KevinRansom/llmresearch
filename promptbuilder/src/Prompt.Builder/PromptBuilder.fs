namespace Prompt.Builder

    open System.Threading.Tasks
    open Prompt.Core

    type PromptBuilder(runner: IPromptRunner) =
        member _.Bind(prompt: string, next: string -> Task<'T>) =
            task {
                let! raw = runner.RunAsync(PromptRequest(prompt))
                return! next raw
            }

        member _.Return(x) = Task.FromResult(Some x)
        member _.ReturnFrom(x: Task<'T>) = x
