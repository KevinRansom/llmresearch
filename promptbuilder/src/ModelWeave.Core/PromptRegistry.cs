using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ModelWeave.Core
{
    public static class PromptRegistry
    {
        private static readonly ConcurrentDictionary<Type, object> _deserializers = new();

        public static void Register<T>(IDeserializer<T> deserializer)
        {
            _deserializers[typeof(T)] = deserializer;
        }

        public static T? Deserialize<T>(string input)
        {
            if (TryGetDeserializer<T>(out var deserializer) && deserializer != null)
            {
                return deserializer.Deserialize(input);
            }

            // Fallback: try System.Text.Json
            try
            {
                return JsonSerializer.Deserialize<T>(input);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize input to type {typeof(T).FullName}. No registered deserializer and fallback failed.",
                    ex);
            }
        }

        public static bool HasDeserializer<T>() =>
            _deserializers.ContainsKey(typeof(T));

        public static bool TryGetDeserializer<T>(out IDeserializer<T>? deserializer)
        {
            if (_deserializers.TryGetValue(typeof(T), out var obj) && obj is IDeserializer<T> d)
            {
                deserializer = d;
                return true;
            }

            deserializer = null;
            return false;
        }
    }
}
