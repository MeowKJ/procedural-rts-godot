using Godot;

namespace ProceduralRts.MapAuthoring.Editor;

public enum MapOverlayPrimitiveKind
{
    Grid, World, HardFootprint, Clearance, ProductionEgress, RefineryDock, InvalidBuildingFallback, ResourceRadius,
    Obstacle, Terrain, Trigger, OwnerFacing, Unit, Objective, Narrative,
}

public sealed record MapOverlayPrimitive(
    MapOverlayPrimitiveKind Kind,
    Rect2 Rect,
    Vector2 Start,
    Vector2 End,
    float Radius,
    NodePath Source,
    bool Selected,
    bool Error);

public sealed record MapAuthoringOverlayPlan(
    IReadOnlyList<MapOverlayPrimitive> Primitives)
{
    public static MapAuthoringOverlayPlan Empty { get; } = new([]);
}
