// File: PromptInstruction.cs
using System;

namespace ModelWeave.Core
{
    public static class PromptInstruction
    {
        public static string For<T>(string userPrompt)
            => For(typeof(T), userPrompt);

        public static string For(Type type, string userPrompt)
        {
            var core = Normalize(userPrompt);

            if (type == typeof(string))
                return $"Answer the following question with a plain string. No quotes or explanations. {core}";

            if (type == typeof(bool))
                return $"Respond only with true or false. No extra text. {core}";

            if (IsNumeric(type))
                return $"Return a single numeric value. No units, no formatting. {core}";

            return $"Return a JSON-encoded value of type {type.Name}. {core}";
        }

        private static string Normalize(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "Question: <blank>";
            return prompt.Trim().EndsWith("?")
                ? $"Question: {prompt.Trim()}"
                : $"Question: {prompt.Trim()}?";
        }

        private static bool IsNumeric(Type t)
        {
            return t == typeof(byte) || t == typeof(sbyte) ||
                   t == typeof(short) || t == typeof(ushort) ||
                   t == typeof(int) || t == typeof(uint) ||
                   t == typeof(long) || t == typeof(ulong) ||
                   t == typeof(float) || t == typeof(double) ||
                   t == typeof(decimal);
        }
    }
}
