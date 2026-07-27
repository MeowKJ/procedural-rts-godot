using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class Building : Node2D
{
    [Export] public string BuildingId { get; set; } = BuildingDesignIds.Headquarters;
    [Export] public int OwnerId { get; set; } = 1;
    [Export] public string FactionId { get; set; } = "dog";
    [Export] public bool OverrideHp { get; set; }
    [Export] public float Hp { get; set; } = 1000;
    [Export] public float BuildProgress { get; set; } = 1;
    [Export] public bool HasRuntimeId { get; set; }
    [Export] public int RuntimeId { get; set; }
}
