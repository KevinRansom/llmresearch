using System.Text.Json;

namespace ModelWeave.Core;

public static class Json
{
    public static T? TryDeserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json); }
        catch { return default; }
    }
}
