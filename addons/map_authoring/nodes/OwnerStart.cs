using Godot;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class OwnerStart : Node2D
{
    [Export] public int OwnerId { get; set; } = 1;
    [Export] public string FactionId { get; set; } = "dog";
    [Export] public int StartingCredits { get; set; } = 2400;
}
