using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class Objective : Node2D
{
    [Export] public string Id { get; set; } = "objective.node";
    [Export] public string ObjectiveKey { get; set; } = MapAuthoringKeyCatalog.DefaultObjectiveKey;
    [Export] public bool Primary { get; set; } = true;
}
