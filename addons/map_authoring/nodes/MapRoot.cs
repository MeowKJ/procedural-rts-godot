using Godot;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class MapRoot : Node2D
{
    [Export] public string Id { get; set; } = "map.new";
    [Export] public int Seed { get; set; }
    [Export] public Vector2 WorldSize { get; set; } = new(3600, 2400);
    [Export(PropertyHint.File, "*.mapspec.json")]
    public string ArtifactPath { get; set; } = "res://assets/maps/map-new.mapspec.json";
}
