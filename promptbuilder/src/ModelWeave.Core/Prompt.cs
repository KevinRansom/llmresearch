using System;

namespace ModelWeave.Core
{
    public static class Prompt
    {
        /// <summary>
        /// Creates a strongly typed prompt with instruction derived from the expected return type.
        /// </summary>
        public static Prompt<T> WithSignature<T>(string promptText)
            => new Prompt<T>(promptText);

        /// <summary>
        /// Alias for WithSignature<T>, intended for expressive usage.
        /// </summary>
        public static Prompt<T> Ask<T>(string promptText)
            => new Prompt<T>(promptText);

        /// <summary>
        /// For advanced scenarios where custom instruction shaping is required.
        /// </summary>
        public static Prompt<T> WithInstruction<T>(string promptText, string instruction)
            => new Prompt<T>(promptText, instruction);
    }

    public readonly struct Prompt<T>
    {
        /// <summary>
        /// The original developer-authored prompt (before any shaping).
        /// </summary>
        public string PromptText { get; }

        /// <summary>
        /// The shaped instruction used for submission to the model.
        /// </summary>
        public string Instruction { get; }

        /// <summary>
        /// The expected return type.
        /// </summary>
        public Type ExpectedType => typeof(T);

        /// <summary>
        /// Constructs a typed prompt and automatically generates the instruction.
        /// </summary>
        public Prompt(string promptText)
        {
            PromptText = promptText ?? throw new ArgumentNullException(nameof(promptText));
            Instruction = PromptInstruction.For<T>(promptText);
        }

        /// <summary>
        /// Constructs a typed prompt with an explicitly provided instruction.
        /// </summary>
        public Prompt(string promptText, string instruction)
        {
            PromptText = promptText ?? throw new ArgumentNullException(nameof(promptText));
            Instruction = instruction ?? throw new ArgumentNullException(nameof(instruction));
        }

        public override string ToString() => $"[Prompt<{typeof(T).Name}>] {PromptText}";
    }
}
