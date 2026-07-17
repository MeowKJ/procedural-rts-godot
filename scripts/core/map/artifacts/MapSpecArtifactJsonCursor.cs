using System.Text.Json;

namespace ProceduralRts.Core;

sealed class MapSpecArtifactJsonCursor
{
    private readonly Dictionary<string, JsonElement> _values;

    public MapSpecArtifactJsonCursor(JsonElement element, params string[] expected)
    {
        RequireKind(element, JsonValueKind.Object, "object");
        var properties = element.EnumerateObject().ToArray();
        if (properties.Length != expected.Length
            || !properties.Select(property => property.Name).SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new MapSpecArtifactException($"Expected object fields [{string.Join(",", expected)}] in canonical order.");
        }

        _values = properties.ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
    }

    public JsonElement Element(string name)
    {
        return _values[name];
    }

    public string String(string name)
    {
        var element = Element(name);
        RequireKind(element, JsonValueKind.String, name);
        return element.GetString() ?? throw new MapSpecArtifactException($"Field '{name}' must not be null.");
    }

    public string? NullableString(string name)
    {
        var element = Element(name);
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireKind(element, JsonValueKind.String, name);
        return element.GetString() ?? throw new MapSpecArtifactException($"Field '{name}' must not be null.");
    }

    public int Int32(string name)
    {
        var element = Element(name);
        RequireKind(element, JsonValueKind.Number, name);
        if (!element.TryGetInt32(out var value))
        {
            throw new MapSpecArtifactException($"Field '{name}' must be an Int32.");
        }

        return value;
    }

    public int? NullableInt32(string name)
    {
        return Element(name).ValueKind == JsonValueKind.Null ? null : Int32(name);
    }

    public float Single(string name)
    {
        var element = Element(name);
        RequireKind(element, JsonValueKind.Number, name);
        if (!element.TryGetSingle(out var value) || !float.IsFinite(value))
        {
            throw new MapSpecArtifactException($"Field '{name}' must be a finite Single.");
        }

        return value;
    }

    public float? NullableSingle(string name)
    {
        return Element(name).ValueKind == JsonValueKind.Null ? null : Single(name);
    }

    public bool Boolean(string name)
    {
        var element = Element(name);
        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new MapSpecArtifactException($"Field '{name}' must be a boolean.");
        }

        return element.GetBoolean();
    }

    public static JsonElement[] Array(JsonElement element, string name)
    {
        RequireKind(element, JsonValueKind.Array, name);
        return element.EnumerateArray().ToArray();
    }

    private static void RequireKind(JsonElement element, JsonValueKind expected, string name)
    {
        if (element.ValueKind != expected)
        {
            throw new MapSpecArtifactException($"Field '{name}' must be {expected}.");
        }
    }
}
