namespace ProceduralRts.Core;

public enum SandboxFactionAvailability
{
    Playable,
    LockedPlaceholder,
}

public readonly record struct SandboxDeveloperOwnerOption(
    OwnerId OwnerId,
    PlayerSlotId PlayerSlotId,
    string Key,
    string Label);

public readonly record struct SandboxDeveloperFactionOption(
    UnitFactionId Faction,
    string Key,
    string Label,
    SandboxFactionAvailability Availability,
    string? LockedReasonKey = null)
{
    public bool CanSpawn => Availability == SandboxFactionAvailability.Playable;
}

public readonly record struct SandboxDeveloperTeamOption(
    int TeamId,
    string Key,
    string Label);

public readonly record struct SandboxDeveloperRelationOption(
    PlayerRelation Relation,
    string Key,
    string Label);

public readonly record struct SandboxDeveloperEnvironmentOption(
    SandboxAtmospherePreset Preset,
    string Key,
    string Label);

public readonly record struct SandboxDeveloperTimeScaleOption(
    float Scale,
    string Key,
    string Label);

public readonly record struct SandboxDeveloperContext(
    OwnerId OwnerId,
    UnitFactionId Faction,
    int TeamId,
    PlayerRelation Relation,
    SandboxAtmospherePreset Environment,
    float TimeScale,
    SandboxDebugOverlayState DebugOverlay)
{
    public static SandboxDeveloperContext Default { get; } = new(
        OwnerId.FromPlayerSlot(PlayerSlotId.One),
        UnitFactionId.Dog,
        1,
        PlayerRelation.Hostile,
        SandboxAtmospherePreset.Daytime,
        SandboxTimeScaleMath.DefaultScale,
        SandboxDebugOverlayState.Empty);

    public SandboxDeveloperFactionOption FactionOption => SandboxDeveloperContextOptions.FactionOption(Faction);

    public bool CanSpawnCurrentFaction => FactionOption.CanSpawn;

    public SandboxDeveloperContext Apply(SandboxDeveloperContextRequest request)
    {
        var nextOverlay = request.DebugOverlay ?? DebugOverlay;

        if (request.DebugOverlayPreset is { } preset)
        {
            nextOverlay = nextOverlay.ApplyPreset(preset);
        }

        if (request.DebugOverlaySetFlag is { } setFlag && request.DebugOverlayEnabled is { } enabled)
        {
            nextOverlay = nextOverlay.Set(setFlag, enabled);
        }

        if (request.DebugOverlayToggle is { } toggleFlag)
        {
            nextOverlay = nextOverlay.Toggle(toggleFlag);
        }

        return new SandboxDeveloperContext(
            SandboxDeveloperContextOptions.NormalizeOwner(request.OwnerId ?? OwnerId),
            SandboxDeveloperContextOptions.NormalizeFaction(request.Faction ?? Faction),
            SandboxDeveloperContextOptions.NormalizeTeam(request.TeamId ?? TeamId),
            SandboxDeveloperContextOptions.NormalizeRelation(request.Relation ?? Relation),
            SandboxDeveloperContextOptions.NormalizeEnvironment(request.Environment ?? Environment),
            SandboxDeveloperContextOptions.NormalizeTimeScale(request.TimeScale ?? TimeScale),
            nextOverlay);
    }

    public string FormatStatus()
    {
        var owner = SandboxDeveloperContextOptions.OwnerOption(OwnerId);
        var faction = SandboxDeveloperContextOptions.FactionOption(Faction);
        var environment = SandboxDeveloperContextOptions.EnvironmentOption(Environment);
        return $"Sandbox context: {owner.Key}, {faction.Key}, team-{TeamId}, {Relation.ToString().ToLowerInvariant()}, {environment.Key}, {SandboxTimeScaleMath.Format(TimeScale)}, {DebugOverlay.FormatStatus()}";
    }
}

public readonly record struct SandboxDeveloperContextRequest(
    OwnerId? OwnerId = null,
    UnitFactionId? Faction = null,
    int? TeamId = null,
    PlayerRelation? Relation = null,
    SandboxAtmospherePreset? Environment = null,
    float? TimeScale = null,
    SandboxDebugOverlayState? DebugOverlay = null,
    SandboxDebugOverlayPreset? DebugOverlayPreset = null,
    SandboxDebugOverlayFlag? DebugOverlayToggle = null,
    SandboxDebugOverlayFlag? DebugOverlaySetFlag = null,
    bool? DebugOverlayEnabled = null);

public static partial class SandboxDeveloperContextOptions
{
    public static readonly IReadOnlyList<SandboxDeveloperOwnerOption> Owners =
    [
        new(OwnerId.FromPlayerSlot(PlayerSlotId.One), PlayerSlotId.One, "owner-1", "Owner 1"),
        new(OwnerId.FromPlayerSlot(PlayerSlotId.Two), PlayerSlotId.Two, "owner-2", "Owner 2"),
        new(OwnerId.FromPlayerSlot(PlayerSlotId.Three), PlayerSlotId.Three, "owner-3", "Owner 3"),
        new(OwnerId.FromPlayerSlot(PlayerSlotId.Four), PlayerSlotId.Four, "owner-4", "Owner 4"),
    ];

    public static readonly IReadOnlyList<SandboxDeveloperFactionOption> Factions =
    [
        new(UnitFactionId.Dog, "dog", "Dog", SandboxFactionAvailability.Playable),
        new(UnitFactionId.Cat, "cat", "Cat", SandboxFactionAvailability.Playable),
        new(UnitFactionId.Corruption, "corruption", "Corruption Locked", SandboxFactionAvailability.LockedPlaceholder, "faction.corruption.locked"),
    ];

    public static readonly IReadOnlyList<SandboxDeveloperTeamOption> Teams =
    [
        new(1, "team-1", "Team 1"),
        new(2, "team-2", "Team 2"),
        new(3, "team-3", "Team 3"),
        new(4, "team-4", "Team 4"),
    ];

    public static readonly IReadOnlyList<SandboxDeveloperRelationOption> Relations =
    [
        new(PlayerRelation.Self, "self", "Self"),
        new(PlayerRelation.Allied, "allied", "Allied"),
        new(PlayerRelation.Neutral, "neutral", "Neutral"),
        new(PlayerRelation.Hostile, "hostile", "Hostile"),
    ];

    public static readonly IReadOnlyList<SandboxDeveloperEnvironmentOption> Environments =
    [
        new(SandboxAtmospherePreset.Daytime, "daytime", "Daytime"),
        new(SandboxAtmospherePreset.Dusk, "dusk", "Dusk"),
        new(SandboxAtmospherePreset.Night, "night", "Night"),
        new(SandboxAtmospherePreset.SignalRestoration, "signal-restoration", "Signal restoration"),
        new(SandboxAtmospherePreset.Corruption, "corruption", "Corruption"),
    ];

    public static IReadOnlyList<SandboxDeveloperTimeScaleOption> TimeScales { get; } =
        SandboxTimeScaleMath.Presets
            .Select(scale => new SandboxDeveloperTimeScaleOption(scale, TimeScaleKey(scale), SandboxTimeScaleMath.Format(scale)))
            .ToArray();

    public static SandboxDeveloperOwnerOption OwnerOption(OwnerId ownerId)
    {
        return Owners.FirstOrDefault(option => option.OwnerId == ownerId) is { Key: not null } option
            ? option
            : throw new ArgumentOutOfRangeException(nameof(ownerId), ownerId, "Sandbox owner must be one of the deterministic developer slots.");
    }

    public static SandboxDeveloperFactionOption FactionOption(UnitFactionId faction)
    {
        return Factions.FirstOrDefault(option => option.Faction == faction) is { Key: not null } option
            ? option
            : throw new ArgumentOutOfRangeException(nameof(faction), faction, "Sandbox faction is not registered.");
    }

    public static SandboxDeveloperTeamOption TeamOption(int teamId)
    {
        return Teams.FirstOrDefault(option => option.TeamId == teamId) is { Key: not null } option
            ? option
            : throw new ArgumentOutOfRangeException(nameof(teamId), teamId, "Sandbox team must be one of the deterministic developer teams.");
    }

    public static SandboxDeveloperRelationOption RelationOption(PlayerRelation relation)
    {
        return Relations.FirstOrDefault(option => option.Relation == relation) is { Key: not null } option
            ? option
            : throw new ArgumentOutOfRangeException(nameof(relation), relation, "Sandbox relation is not registered.");
    }

    public static SandboxDeveloperEnvironmentOption EnvironmentOption(SandboxAtmospherePreset preset)
    {
        return Environments.FirstOrDefault(option => option.Preset == preset) is { Key: not null } option
            ? option
            : throw new ArgumentOutOfRangeException(nameof(preset), preset, "Sandbox environment preset is not registered.");
    }

    public static SandboxDeveloperTimeScaleOption TimeScaleOption(float scale)
    {
        var normalized = NormalizeTimeScale(scale);
        return TimeScales.First(option => MathF.Abs(option.Scale - normalized) < 0.0001f);
    }

    public static OwnerId NormalizeOwner(OwnerId ownerId)
    {
        return OwnerOption(ownerId).OwnerId;
    }

    public static UnitFactionId NormalizeFaction(UnitFactionId faction)
    {
        return FactionOption(faction).Faction;
    }

    public static int NormalizeTeam(int teamId)
    {
        return TeamOption(teamId).TeamId;
    }

    public static PlayerRelation NormalizeRelation(PlayerRelation relation)
    {
        return RelationOption(relation).Relation;
    }

    public static SandboxAtmospherePreset NormalizeEnvironment(SandboxAtmospherePreset preset)
    {
        return EnvironmentOption(preset).Preset;
    }

    public static float NormalizeTimeScale(float scale)
    {
        var best = SandboxTimeScaleMath.Presets[0];
        var bestDistance = float.MaxValue;
        foreach (var preset in SandboxTimeScaleMath.Presets)
        {
            var distance = MathF.Abs(preset - scale);
            if (distance < bestDistance)
            {
                best = preset;
                bestDistance = distance;
            }
        }

        return best;
    }

}
