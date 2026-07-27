using Godot;

namespace ProceduralRts.Core;

public sealed class ProjectileModel
{
    public required int Id { get; init; }
    public required CombatSourceKind SourceKind { get; init; }
    public required int SourceId { get; init; }
    public required int TargetId { get; init; }
    public required CombatTargetKind TargetKind { get; init; }
    public required string AmmoId { get; init; }
    public required ProjectileBehavior Behavior { get; init; }
    public required HitRule HitRule { get; init; }
    public required Vector2 Position { get; set; }
    public required Vector2 Velocity { get; set; }
    public Vector2? ImpactPosition { get; init; }
    public required float Speed { get; init; }
    public required float Damage { get; init; }
    public required float HitRadiusMultiplier { get; init; }
    public required float TrackingStrength { get; init; }
    public required float TrailWidth { get; init; }
    public required float CoreWidth { get; init; }
    public required float HeadRadius { get; init; }
    public required Color Accent { get; init; }
}
