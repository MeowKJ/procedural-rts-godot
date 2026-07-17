using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Nodes;

[Tool]
public partial class Narrative : Node2D
{
    [Export] public string Id { get; set; } = "narrative.node";
    [Export] public string TextKey { get; set; } = MapAuthoringKeyCatalog.DefaultNarrativeKey;
    [Export] public string TriggerId { get; set; } = "";
}
