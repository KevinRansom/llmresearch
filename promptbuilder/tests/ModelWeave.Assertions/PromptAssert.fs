namespace ModelWeave.Tests.Assertions
    open ModelWeave.Core
    open System.Threading.Tasks
    open Xunit

    [<AutoOpen>]
    module PromptAssert =

        let success<'T> (workflow: Task<Option<'T>>) : 'T =
            try
                match workflow |> Async.AwaitTask |> Async.RunSynchronously with
                | Some value -> value
                | None -> failwith $"Prompt failed to return a result of type {typeof<'T>}."
            with
            | :? PromptFailure<'T> as pf ->
                Assert.True(false, pf.Format())
                Unchecked.defaultof<'T>
            | ex ->
                Assert.True(false, $"Unhandled exception: {ex.Message}")
                Unchecked.defaultof<'T>

        /// Runs a workflow and applies a validator to the result.
        /// Fails with rich context if the prompt fails or validation throws.
        let expect<'T> (validate: 'T -> unit) (workflow: Task<Option<'T>>) : unit =
            try
                let value = success workflow
                validate value
            with
            | :? PromptFailure<'T> as pf ->
                Assert.True(false, pf.Format())
            | ex ->
                Assert.True(false, $"Validation or prompt failed: {ex.Message}")
