namespace ProceduralRts.Core;

public sealed record ProductionQueueSnapshot(
    int Id,
    ProductionKind Kind,
    string DesignId,
    FactionId FactionId,
    float ProgressRatio,
    int Cost,
    int Refund,
    bool CanCancel
);
