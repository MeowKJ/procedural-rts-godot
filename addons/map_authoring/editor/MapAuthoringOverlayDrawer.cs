using Godot;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Editor;

public static class MapAuthoringOverlayDrawer
{
    public static void Draw(Control overlay, MapRoot root, MapAuthoringOverlayPlan plan)
    {
        var viewport = EditorInterface.Singleton.GetEditorViewport2D();
        if (viewport is null) return;
        var transform = viewport.GlobalCanvasTransform * root.GlobalTransform;
        var scale = MathF.Max(0.001f, transform.X.Length());
        foreach (var primitive in plan.Primitives)
        {
            var color = ColorFor(primitive);
            var width = primitive.Selected ? 3.5f : primitive.Error ? 2.5f : WidthFor(primitive.Kind);
            if (primitive.Kind is MapOverlayPrimitiveKind.ResourceRadius
                or MapOverlayPrimitiveKind.Unit
                or MapOverlayPrimitiveKind.Objective
                or MapOverlayPrimitiveKind.Narrative
                or MapOverlayPrimitiveKind.InvalidBuildingFallback)
            {
                overlay.DrawArc(transform * primitive.Start, primitive.Radius * scale, 0, Mathf.Tau, 40, color, width, true);
                continue;
            }
            if (primitive.Kind is MapOverlayPrimitiveKind.Grid or MapOverlayPrimitiveKind.OwnerFacing)
            {
                overlay.DrawLine(transform * primitive.Start, transform * primitive.End, color, width, true);
                continue;
            }
            DrawRectLines(overlay, transform, primitive.Rect, color, width);
        }
    }

    private static void DrawRectLines(Control overlay, Transform2D transform, Rect2 rect, Color color, float width)
    {
        var points = new[]
        {
            transform * rect.Position,
            transform * new Vector2(rect.End.X, rect.Position.Y),
            transform * rect.End,
            transform * new Vector2(rect.Position.X, rect.End.Y),
        };
        for (var index = 0; index < points.Length; index++)
            overlay.DrawLine(points[index], points[(index + 1) % points.Length], color, width, true);
    }

    private static float WidthFor(MapOverlayPrimitiveKind kind)
        => kind == MapOverlayPrimitiveKind.Grid ? 1 : kind == MapOverlayPrimitiveKind.World ? 3 : 1.5f;

    private static Color ColorFor(MapOverlayPrimitive value)
    {
        if (value.Selected) return new Color("#fff176");
        if (value.Error) return new Color("#ff5d75");
        return value.Kind switch
        {
            MapOverlayPrimitiveKind.Grid => new Color(0.34f, 0.48f, 0.55f, 0.18f),
            MapOverlayPrimitiveKind.World => new Color("#b0bec5"),
            MapOverlayPrimitiveKind.HardFootprint => new Color("#66bb6a"),
            MapOverlayPrimitiveKind.Clearance => new Color(1, 0.76f, 0.22f, 0.72f),
            MapOverlayPrimitiveKind.ProductionEgress => new Color("#ab47bc"),
            MapOverlayPrimitiveKind.RefineryDock => new Color("#ff8a65"),
            MapOverlayPrimitiveKind.InvalidBuildingFallback => new Color("#ff1744"),
            MapOverlayPrimitiveKind.ResourceRadius => new Color("#26c6da"),
            MapOverlayPrimitiveKind.Obstacle => new Color("#78909c"),
            MapOverlayPrimitiveKind.Terrain => new Color("#8bc34a"),
            MapOverlayPrimitiveKind.Trigger => new Color("#ff7043"),
            MapOverlayPrimitiveKind.OwnerFacing => new Color("#42a5f5"),
            MapOverlayPrimitiveKind.Unit => new Color("#29b6f6"),
            MapOverlayPrimitiveKind.Objective => new Color("#ffee58"),
            _ => new Color("#ec407a"),
        };
    }
}
