using Godot;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class Obstacle : Node2D
{
    [Export] public string Id { get; set; } = "obstacle.block";
    [Export] public Vector2 Size { get; set; } = new(128, 128);
}
