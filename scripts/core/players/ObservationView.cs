namespace ProceduralRts.Core;

public readonly record struct ObservedEntity(
    EntityId Id,
    string SpecId,
    EntityKind Kind,
    OwnerId OwnerId,
    float PositionX,
    float PositionY,
    float Facing,
    float HealthFraction,
    bool IsOwnedByViewer);

public readonly record struct ObservedPlayerState(
    PlayerSlotId SlotId,
    OwnerId OwnerId,
    int Credits,
    bool IsDefeated)
{
    public bool IsKnown => SlotId.Value > 0 && OwnerId.IsValid;
}

public readonly record struct ObservedCommandAffordance(
    PlayerCommandKind Kind,
    string SpecId,
    bool IsAvailable,
    string UnavailableReason);

public readonly record struct ObservationView(
    PlayerSlotId ViewerSlotId,
    OwnerId ViewerOwnerId,
    int Tick,
    ObservedPlayerState Self,
    IReadOnlyList<ObservedPlayerState> KnownPlayers,
    IReadOnlyList<ObservedEntity> VisibleEntities,
    IReadOnlyList<ObservedCommandAffordance> CommandAffordances)
{
    public bool IsValid => ViewerSlotId.Value > 0 && ViewerOwnerId.IsValid && Tick >= 0;

    public static ObservationView Empty(PlayerSlotId viewerSlotId, OwnerId viewerOwnerId, int tick)
    {
        return new ObservationView(
            viewerSlotId,
            viewerOwnerId,
            tick,
            new ObservedPlayerState(viewerSlotId, viewerOwnerId, Credits: 0, IsDefeated: false),
            Array.Empty<ObservedPlayerState>(),
            Array.Empty<ObservedEntity>(),
            Array.Empty<ObservedCommandAffordance>());
    }
}
