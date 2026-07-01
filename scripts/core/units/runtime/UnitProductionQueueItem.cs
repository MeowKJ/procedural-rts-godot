namespace ProceduralRts.Core;

public sealed class UnitProductionQueueItem
{
    public required int Id { get; init; }
    public required ProductionKind Kind { get; init; }
    public required string DesignId { get; init; }
    public required UnitFactionId Faction { get; init; }
    public float Progress { get; set; }
}
