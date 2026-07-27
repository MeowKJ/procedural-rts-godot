using Godot;

namespace ProceduralRts.Core;

public static class FactionCatalog
{
    public static readonly IReadOnlyDictionary<FactionId, FactionDefinition> Definitions =
        new Dictionary<FactionId, FactionDefinition>
        {
            [FactionId.Dog] = new(
                FactionId.Dog,
                "faction.dog.name",
                "DOG",
                IconGlyph.StanceHold,
                new Color("#3f8068"),
                new Color("#c47719")),
            [FactionId.Cat] = new(
                FactionId.Cat,
                "faction.cat.name",
                "CAT",
                IconGlyph.StanceAggressive,
                new Color("#50439c"),
                new Color("#a83255")),
        };

    public static FactionDefinition For(FactionId factionId)
    {
        return Definitions[factionId];
    }

    public static UnitFactionId UnitFactionFor(FactionId factionId)
    {
        return factionId switch
        {
            FactionId.Dog => UnitFactionId.Dog,
            FactionId.Cat => UnitFactionId.Cat,
            FactionId.Corruption => UnitFactionId.Corruption,
            _ => throw new ArgumentOutOfRangeException(nameof(factionId), factionId, null),
        };
    }
}
