using Godot;

namespace ProceduralRts.Core;

public enum RepairOrderStallReason
{
    None,
    InsufficientCredits,
}

public readonly record struct RepairOrderProjection(
    EntityId Repairer,
    EntityId Target,
    Vector2 RepairerPosition,
    Vector2 TargetPosition,
    RepairOrderStallReason StallReason)
{
    public bool IsStalled => StallReason != RepairOrderStallReason.None;
}
