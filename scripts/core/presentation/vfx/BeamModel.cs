using Godot;

namespace ProceduralRts.Core;

public sealed class BeamModel
{
    public required int Id { get; init; }
    public required CombatSourceKind SourceKind { get; init; }
    public required int SourceId { get; init; }
    public required int TargetId { get; init; }
    public required CombatTargetKind TargetKind { get; init; }
    public required Vector2 Start { get; init; }
    public required Vector2 End { get; init; }
    public required float Duration { get; init; }
    public required float Age { get; set; }
    public required float Width { get; init; }
    public required Color Accent { get; init; }
}
