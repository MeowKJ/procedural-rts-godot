using Godot;

namespace ProceduralRts.MapAuthoring;

sealed class FixtureMapMetadata
{
    private readonly Node _node;

    public FixtureMapMetadata(Node node)
    {
        _node = node;
    }

    public bool Has(string name)
    {
        return _node.HasMeta(name);
    }

    public string RequiredString(string name)
    {
        var value = Required(name);
        RequireType(name, value, Variant.Type.String);
        return (string)value;
    }

    public string String(string name, string fallback)
    {
        return Has(name) ? RequiredString(name) : fallback;
    }

    public string? OptionalString(string name)
    {
        if (!Has(name))
        {
            return null;
        }

        var value = RequiredString(name);
        return value.Length == 0 ? null : value;
    }

    public int RequiredInt32(string name)
    {
        var value = Required(name);
        RequireType(name, value, Variant.Type.Int);
        var number = value.AsInt64();
        if (number is < int.MinValue or > int.MaxValue)
        {
            throw Failure(name, "Int32");
        }

        return (int)number;
    }

    public int Int32(string name, int fallback)
    {
        return Has(name) ? RequiredInt32(name) : fallback;
    }

    public int? OptionalInt32(string name)
    {
        return Has(name) ? RequiredInt32(name) : null;
    }

    public float Single(string name, float fallback = 0f)
    {
        if (!Has(name))
        {
            return fallback;
        }

        var value = Required(name);
        if (value.VariantType is not (Variant.Type.Int or Variant.Type.Float))
        {
            throw Failure(name, "finite number");
        }

        var number = value.AsDouble();
        if (!double.IsFinite(number) || number is < -float.MaxValue or > float.MaxValue)
        {
            throw Failure(name, "finite Single");
        }

        return (float)number;
    }

    public float? OptionalSingle(string name)
    {
        return Has(name) ? Single(name) : null;
    }

    public bool Boolean(string name, bool fallback = false)
    {
        if (!Has(name))
        {
            return fallback;
        }

        var value = Required(name);
        RequireType(name, value, Variant.Type.Bool);
        return value.AsBool();
    }

    private Variant Required(string name)
    {
        if (!Has(name))
        {
            throw new InvalidOperationException($"Node '{_node.Name}' is missing metadata '{name}'.");
        }

        return _node.GetMeta(name);
    }

    private void RequireType(string name, Variant value, Variant.Type expected)
    {
        if (value.VariantType != expected)
        {
            throw Failure(name, expected.ToString());
        }
    }

    private InvalidOperationException Failure(string name, string expected)
    {
        return new InvalidOperationException($"Node '{_node.Name}' metadata '{name}' must be {expected}.");
    }
}
