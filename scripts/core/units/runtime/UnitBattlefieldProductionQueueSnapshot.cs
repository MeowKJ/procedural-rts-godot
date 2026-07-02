namespace ProceduralRts.Core;

public readonly record struct UnitBattlefieldProductionQueueSnapshot(
    int BuildingId,
    UnitBattlefieldBuildingSnapshot Snapshot,
    UnitProductionQueueItem Item);
