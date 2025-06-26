namespace Prompt.Builder

open Prompt.Core

module Json =
    let tryDeserialize<'T> (json: string) : 'T=
        Json.TryDeserialize<'T>(json)
