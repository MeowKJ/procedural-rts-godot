using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class TerrainRegion : Node2D
{
    [Export] public string Id { get; set; } = "terrain.region";
    [Export] public Vector2 Size { get; set; } = new(128, 128);
    [Export] public string TerrainId { get; set; } = MapAuthoringKeyCatalog.DefaultTerrainId;
    [Export] public float MovementCost { get; set; } = 1;
    [Export] public bool BlocksLand { get; set; }
}
