using Godot;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class Unit : Node2D
{
    [Export] public string DesignId { get; set; } = "dog.infantry";
    [Export] public int OwnerId { get; set; } = 1;
}
