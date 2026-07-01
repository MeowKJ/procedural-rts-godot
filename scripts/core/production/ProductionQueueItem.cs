namespace ProceduralRts.Core;

public sealed class ProductionQueueItem
{
    public required int Id { get; init; }
    public required ProductionKind Kind { get; init; }
    public required string DesignId { get; init; }
    public required FactionId FactionId { get; init; }
    public float Progress { get; set; }
}
