using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class Trigger : Node2D
{
    [Export] public string Id { get; set; } = "trigger.area";
    [Export] public Vector2 Size { get; set; } = new(128, 128);
    [Export] public string EventKey { get; set; } = MapAuthoringKeyCatalog.DefaultEventKey;
}
