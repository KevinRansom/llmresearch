namespace ModelWeave.Builders

open ModelWeave.Core

[<AutoOpen>]
module Prompt =
    let withSignature<'T> (instruction: string) : Prompt<'T> =
        Prompt.WithSignature<'T>(instruction)

    /// Ergonomic alias for building a structured prompt with expected return type.
    /// Intended for use in application code: 
    ///     let response: Prompt<string * int * bool> = ask "..."
    let ask<'T> (instruction: string) : Prompt<'T> =
        Prompt.WithSignature<'T> instruction

