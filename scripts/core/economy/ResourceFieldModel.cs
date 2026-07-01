using Godot;

namespace ProceduralRts.Core;

public sealed class ResourceFieldModel
{
    public required int Id { get; init; }
    public required Vector2 Position { get; init; }
    public required float Radius { get; init; }
    public required int MaxAmount { get; init; }
    public required int Amount { get; set; }
    public required Color Accent { get; init; }
    public float Pulse { get; set; }
}
