namespace ProceduralRts.Core;

public readonly record struct ProductionCompletionCandidate(
    int BuildingId,
    UnitBattlefieldBuildingSnapshot Snapshot,
    UnitProductionQueueItem Item);
