namespace ProceduralRts.Core;

public sealed record EntityAuthoringMetadata(
    UnitFactionId? UnitFaction = null,
    string? BuildingSpecId = null,
    int TechTier = 0,
    IReadOnlySet<string>? RosterTags = null);
