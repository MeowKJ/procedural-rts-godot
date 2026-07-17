using Godot;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class Resource : Node2D
{
    [Export] public string Id { get; set; } = "resource.field";
    [Export] public float Radius { get; set; } = 120;
    [Export] public int Amount { get; set; } = 1000;
    [Export] public Color Accent { get; set; } = new("#8fffe1");
}
