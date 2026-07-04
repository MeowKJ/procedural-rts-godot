using Godot;

namespace ProceduralRts.Core;

public static class DamageElementCatalog
{
    private static readonly SortedDictionary<string, DamageElementDefinition> DefinitionsById = new(StringComparer.Ordinal)
    {
        [DamageElementIds.Kinetic] = new(DamageElementIds.Kinetic, "Kinetic", new Color("#d7d2c4")),
        [DamageElementIds.Explosive] = new(DamageElementIds.Explosive, "Explosive", new Color("#ffb35c")),
        [DamageElementIds.Thermal] = new(DamageElementIds.Thermal, "Thermal", new Color("#ff7763")),
        [DamageElementIds.Energy] = new(DamageElementIds.Energy, "Energy", new Color("#8fffe1")),
        [DamageElementIds.Moonshadow] = new(DamageElementIds.Moonshadow, "Moonshadow", new Color("#9f9cff")),
        [DamageElementIds.Entropy] = new(DamageElementIds.Entropy, "Entropy", new Color("#b46a8f")),
        [DamageElementIds.Resonance] = new(DamageElementIds.Resonance, "Resonance", new Color("#f2d16b")),
    };

    public static IReadOnlyDictionary<string, DamageElementDefinition> Definitions => DefinitionsById;

    public static DamageElementDefinition For(string id)
    {
        return DefinitionsById.TryGetValue(id, out var definition)
            ? definition
            : throw new InvalidOperationException($"Unknown damage element '{id}'.");
    }
}
