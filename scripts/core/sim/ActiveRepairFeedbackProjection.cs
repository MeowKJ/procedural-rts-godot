using Godot;

namespace ProceduralRts.Core;

public readonly record struct ActiveRepairFeedbackProjection(
    EntityId RepairerId,
    EntityId TargetId,
    Vector2 RepairerPosition,
    Vector2 TargetPosition,
    float TargetRadius,
    float WorkRate,
    float ProgressCarry,
    Color Accent)
{
    public float SegmentLength => RepairerPosition.DistanceTo(TargetPosition);
}
