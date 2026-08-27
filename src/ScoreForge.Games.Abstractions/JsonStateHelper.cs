using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dadstart.Labs.ScoreForge.Games.Abstractions;

public static class JsonStateHelper
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public static JsonElement SerializeToElement<T>(T value) =>
        JsonSerializer.SerializeToElement(value, Options);

    public static T Deserialize<T>(JsonElement element) =>
        JsonSerializer.Deserialize<T>(element, Options)
        ?? throw new GameEngineException($"Unable to deserialize {typeof(T).Name}.");

    public static T? TryGetProperty<T>(JsonElement options, string name, T? defaultValue = default)
    {
        if (options.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return defaultValue;

        if (!options.TryGetProperty(name, out var property) &&
            !options.TryGetProperty(char.ToUpperInvariant(name[0]) + name[1..], out property))
            return defaultValue;

        return property.Deserialize<T>(Options);
    }
}
