namespace ProceduralRts.Core;

public enum MapAuthoringKeyKind
{
    Terrain,
    Event,
    Objective,
    Narrative,
}

public static class MapAuthoringKeyCatalog
{
    public const string DefaultTerrainId = "ground";
    public const string DefaultEventKey = "chapter0.gate_contact";
    public const string DefaultObjectiveKey = "objective.restore_signal";
    public const string DefaultNarrativeKey = "narrative.safe_mark";

    public static IReadOnlyList<string> TerrainIds { get; } = Array.AsReadOnly(new[]
    {
        "base-ground",
        "ground",
        "soft-road",
        "water",
    });

    public static IReadOnlyList<string> EventKeys { get; } = Array.AsReadOnly(new[]
    {
        DefaultEventKey,
    });

    public static IReadOnlyList<string> ObjectiveKeys { get; } = Array.AsReadOnly(new[]
    {
        DefaultObjectiveKey,
    });

    public static IReadOnlyList<string> NarrativeKeys { get; } = Array.AsReadOnly(new[]
    {
        DefaultNarrativeKey,
    });

    public static IReadOnlyList<string> Options(MapAuthoringKeyKind kind)
    {
        return kind switch
        {
            MapAuthoringKeyKind.Terrain => TerrainIds,
            MapAuthoringKeyKind.Event => EventKeys,
            MapAuthoringKeyKind.Objective => ObjectiveKeys,
            MapAuthoringKeyKind.Narrative => NarrativeKeys,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static string Require(MapAuthoringKeyKind kind, string value)
    {
        if (!Options(kind).Contains(value, StringComparer.Ordinal))
        {
            throw new MapAuthoringCatalogException(kind.ToString().ToLowerInvariant(), value);
        }

        return value;
    }
}
