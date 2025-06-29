using System;

namespace ModelWeave.Core
{
    public class PromptFailure<T> : Exception
    {
        public string Instruction { get; }
        public string? RawResponse { get; }
        public Type ExpectedType => typeof(T);

        public PromptFailure(string instruction, string? rawResponse = null, Exception? inner = null)
            : base($"Prompt deserialization failed for instruction: \"{instruction}\"", inner)
        {
            Instruction = instruction;
            RawResponse = rawResponse;
        }

        public override string ToString()
        {
            return Format(includeStack: false);
        }

        public string Format(bool includeStack = false)
        {
            var lines = new[]
            {
                "❌ Prompt deserialization failed.",
                $"  - Instruction : {Instruction}",
                $"  - Expected    : {typeof(T)}",
                $"  - Response    : {(string.IsNullOrWhiteSpace(RawResponse) ? "<none>" : RawResponse)}",
                $"  - Inner Error : {(InnerException != null ? (includeStack ? InnerException.ToString() : InnerException.Message) : "<none>")}"
            };

            return string.Join(Environment.NewLine, lines);
        }
    }
}
