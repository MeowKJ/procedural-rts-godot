using Godot;

namespace ProceduralRts.Core;

public sealed record CompletedProductionItem(
    int ProducerId,
    ProductionKind Kind,
    string DesignId,
    FactionId FactionId,
    Vector2? SpawnPosition = null,
    float Facing = 0
);
