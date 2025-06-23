using System.Text.Json;

namespace Prompt.Core;

public static class Json
{
    public static T? TryDeserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json); }
        catch { return default; }
    }
}
