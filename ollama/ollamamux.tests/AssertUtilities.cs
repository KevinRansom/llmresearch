using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace OllamaMux.Testing
{
    static class AssertUtilities
    {
        public static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    static class Assert
    {
        /// <summary>
        /// Re-wraps xUnit's standard Assert methods and provides a hook for custom assertions.
        /// </summary>
        public class RaisedEvent<T>
        {
            public object? Sender { get; }
            public T Arguments { get; }
            public RaisedEvent(object? sender, T args) => (Sender, Arguments) = (sender, args);
        }

        public static void False([DoesNotReturnIf(true)] bool condition) =>
            Xunit.Assert.False(condition);
        public static void False([DoesNotReturnIf(true)] bool? condition) =>
            Xunit.Assert.False(condition);
        public static void False([DoesNotReturnIf(true)] bool condition, string? userMessage) =>
            Xunit.Assert.False(condition, userMessage);
        public static void False([DoesNotReturnIf(true)] bool? condition, string? userMessage) =>
            Xunit.Assert.False(condition, userMessage);

        public static void True([DoesNotReturnIf(false)] bool condition) =>
            Xunit.Assert.True(condition);
        public static void True([DoesNotReturnIf(false)] bool? condition) =>
            Xunit.Assert.True(condition);
        public static void True([DoesNotReturnIf(false)] bool condition, string? userMessage) =>
            Xunit.Assert.True(condition, userMessage);
        public static void True([DoesNotReturnIf(false)] bool? condition, string? userMessage) =>
            Xunit.Assert.True(condition, userMessage);

        public static void All<T>(IEnumerable<T> collection, Action<T> action) =>
            Xunit.Assert.All(collection, action);
        public static void All<T>(IEnumerable<T> collection, Action<T, int> action) =>
            Xunit.Assert.All(collection, action);

        public static void Collection<T>(IEnumerable<T> collection, params Action<T>[] elementInspectors) =>
            Xunit.Assert.Collection(collection, elementInspectors);

        public static void Contains<T>(T expected, IEnumerable<T> collection) =>
            Xunit.Assert.Contains(expected, collection);
        public static void Contains<T>(T expected, IEnumerable<T> collection, IEqualityComparer<T> comparer) =>
            Xunit.Assert.Contains(expected, collection, comparer);
        public static void Contains<T>(IEnumerable<T> collection, Predicate<T> filter) =>
            Xunit.Assert.Contains(collection, filter);

        public static void Distinct<T>(IEnumerable<T> collection) =>
            Xunit.Assert.Distinct(collection);
        public static void Distinct<T>(IEnumerable<T> collection, IEqualityComparer<T> comparer) =>
            Xunit.Assert.Distinct(collection, comparer);

        public static void DoesNotContain<T>(T expected, IEnumerable<T> collection) =>
            Xunit.Assert.DoesNotContain(expected, collection);
        public static void DoesNotContain<T>(T expected, IEnumerable<T> collection, IEqualityComparer<T> comparer) =>
            Xunit.Assert.DoesNotContain(expected, collection, comparer);
        public static void DoesNotContain<T>(IEnumerable<T> collection, Predicate<T> filter) =>
            Xunit.Assert.DoesNotContain(collection, filter);

        public static void Empty(IEnumerable collection) =>
            Xunit.Assert.Empty(collection);

        public static void Equal<T>(IEnumerable<T>? expected, IEnumerable<T>? actual) =>
            Xunit.Assert.Equal(expected, actual);
        public static void Equal<T>(IEnumerable<T>? expected, IEnumerable<T>? actual, IEqualityComparer<T> comparer) =>
            Xunit.Assert.Equal(expected, actual, comparer);
        public static void Equal<T>(IEnumerable<T>? expected, IEnumerable<T>? actual, Func<T, T, bool> comparer) =>
            Xunit.Assert.Equal(expected, actual, comparer);

        public static void NotEmpty(IEnumerable collection) =>
            Xunit.Assert.NotEmpty(collection);

        public static void NotEqual<T>(IEnumerable<T>? expected, IEnumerable<T>? actual) =>
            Xunit.Assert.NotEqual(expected, actual);
        public static void NotEqual<T>(IEnumerable<T>? expected, IEnumerable<T>? actual, IEqualityComparer<T> comparer) =>
            Xunit.Assert.NotEqual(expected, actual, comparer);
        public static void NotEqual<T>(IEnumerable<T>? expected, IEnumerable<T>? actual, Func<T, T, bool> comparer) =>
            Xunit.Assert.NotEqual(expected, actual, comparer);

        public static object? Single(IEnumerable collection) =>
            Xunit.Assert.Single(collection);
        public static void Single(IEnumerable collection, object? expected) =>
            Xunit.Assert.Single(collection, expected);
        public static T Single<T>(IEnumerable<T> collection) =>
            Xunit.Assert.Single(collection);
        public static T Single<T>(IEnumerable<T> collection, Predicate<T> predicate) =>
            Xunit.Assert.Single(collection, predicate);

        public static TValue Contains<TKey, TValue>(TKey expected, IDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.Contains(expected, collection);
        public static TValue Contains<TKey, TValue>(TKey expected, IReadOnlyDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.Contains(expected, collection);
        public static TValue Contains<TKey, TValue>(TKey expected, ConcurrentDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.Contains(expected, collection);
        public static TValue Contains<TKey, TValue>(TKey expected, Dictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.Contains(expected, collection);
        public static TValue Contains<TKey, TValue>(TKey expected, ReadOnlyDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.Contains(expected, collection);

        public static void DoesNotContain<TKey, TValue>(TKey expected, IDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.DoesNotContain(expected, collection);
        public static void DoesNotContain<TKey, TValue>(TKey expected, IReadOnlyDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.DoesNotContain(expected, collection);
        public static void DoesNotContain<TKey, TValue>(TKey expected, ConcurrentDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.DoesNotContain(expected, collection);
        public static void DoesNotContain<TKey, TValue>(TKey expected, Dictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.DoesNotContain(expected, collection);
        public static void DoesNotContain<TKey, TValue>(TKey expected, ReadOnlyDictionary<TKey, TValue> collection) where TKey : notnull =>
            Xunit.Assert.DoesNotContain(expected, collection);

        public static void Contains(
            string expected,
            IEnumerable<string> collection,
            StringComparison comparison) =>
                Xunit.Assert.Contains(expected, collection, StringComparer.FromComparison(comparison));
        public static void Contains(string expectedSubstring, string actualString) =>
            Xunit.Assert.Contains(expectedSubstring, actualString);
        public static void Contains(string expectedSubstring, string actualString, StringComparison comparisonType) =>
            Xunit.Assert.Contains(expectedSubstring, actualString, comparisonType);

        public static void ListOutputValid(string output)
        {
            Assert.False(string.IsNullOrWhiteSpace(output), "Output was empty");

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length > 1, "Expected header and at least one model row");

            var header = lines[0].Trim();
            var expectedHeaders = new[] { "NAME", "ID", "SIZE", "MODIFIED" };

            foreach (var expected in expectedHeaders)
            {
                Assert.Contains(expected, header, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Assert.True(columns.Length >= 4, $"Row malformed: '{line}'");
            }
        }
    }
}
