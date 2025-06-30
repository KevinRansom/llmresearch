using System;

public class PromptFailure<T> : Exception
{
    public string Prompt { get; }             // The original user-supplied prompt
    public string Instruction { get; }        // Synthesized system instruction (e.g., from PromptInstruction.For<T>())
    public string? RawResponse { get; }
    public T? Result { get; }
    public Type ExpectedType => typeof(T);

    public PromptFailure(
        string prompt,
        string instruction,
        string? rawResponse = null,
        T? result = default,
        Exception? inner = null)
        : base($"Prompt deserialization failed for prompt: \"{prompt}\"", inner)
    {
        Prompt = prompt;
        Instruction = instruction;
        RawResponse = rawResponse;
        Result = result;
    }

    public string Format(bool includeStack = false)
    {
        var lines = new[]
        {
            "❌ Prompt deserialization failed.",
            $"  - Prompt      : {Prompt}",
            $"  - Instruction : {Instruction}",
            $"  - Expected    : {typeof(T)}",
            $"  - Response    : {(string.IsNullOrWhiteSpace(RawResponse) ? "<none>" : RawResponse)}",
            $"  - Result      : {(Result?.ToString() ?? "<null>")}",
            $"  - Inner Error : {(InnerException != null ? (includeStack ? InnerException.ToString() : InnerException.Message) : "<none>")}"
        };

        return string.Join(Environment.NewLine, lines);
    }

    public override string ToString() => Format();
}
