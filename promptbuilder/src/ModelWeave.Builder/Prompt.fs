namespace ModelWeave.Builders

open ModelWeave.Core

[<AutoOpen>]
module Prompt =
    let withSignature<'T> (instruction: string) : Prompt<'T> =
        Prompt.WithSignature<'T>(instruction)


