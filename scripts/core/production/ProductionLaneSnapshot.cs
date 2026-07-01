using Godot;

namespace ProceduralRts.Core;

public sealed record ProductionLaneSnapshot(
    int ProducerId,
    string ProducerKind,
    FactionId FactionId,
    string ProducerLabel,
    bool Powered,
    bool Completed,
    Vector2? RallyPoint,
    IReadOnlyList<ProductionQueueSnapshot> Queue
);
