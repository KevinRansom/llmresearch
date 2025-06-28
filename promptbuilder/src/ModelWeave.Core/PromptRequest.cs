namespace ModelWeave.Core;

public record PromptRequest(string PromptText, float Temperature = 0.7f, string? Metadata = null);
