using Godot;

namespace ProceduralRts.Core;

public static class UnitVisualRenderer
{
    private const int UnitArcSegments = 28;
    private const bool AntiAliasedUnitStroke = false;

    [ThreadStatic]
    private static Dictionary<int, Vector2[]>? _closedPolylineBuffers;

    public static void DrawUnitArtRecipe(
        CanvasItem canvas,
        UnitArtRecipe recipe,
        UnitRenderPalette palette,
        Vector2 center,
        float scale,
        float bodyFacing = 0,
        IReadOnlyDictionary<string, float>? mountFacings = null)
    {
        var compiled = UnitBodyRenderRecipeCache.For(recipe);
        DrawArtLayers(canvas, compiled.BodyLayers, palette, center, scale, bodyFacing);

        foreach (var group in compiled.MountGroups)
        {
            var facing = mountFacings is not null && mountFacings.TryGetValue(group.MountId, out var mountFacing)
                ? mountFacing
                : bodyFacing;
            DrawArtLayers(canvas, group.Layers, palette, center, scale, facing);
        }

        DrawArtLayers(canvas, compiled.RuntimePulseLayers, palette, center, scale, bodyFacing);
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    public static void DrawUnitArtRecipe(
        CanvasItem canvas,
        UnitArtRecipe recipe,
        EntityRenderPalette palette,
        Vector2 center,
        float scale,
        float bodyFacing = 0,
        IReadOnlyDictionary<string, float>? mountFacings = null,
        EnvironmentTone? environmentTone = null)
    {
        var compiled = UnitBodyRenderRecipeCache.For(recipe);
        DrawArtLayers(canvas, compiled.BodyLayers, palette, center, scale, bodyFacing, environmentTone);

        foreach (var group in compiled.MountGroups)
        {
            var facing = mountFacings is not null && mountFacings.TryGetValue(group.MountId, out var mountFacing)
                ? mountFacing
                : bodyFacing;
            DrawArtLayers(canvas, group.Layers, palette, center, scale, facing, environmentTone);
        }

        DrawArtLayers(canvas, compiled.RuntimePulseLayers, palette, center, scale, bodyFacing, environmentTone);
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private static void DrawArtLayers(CanvasItem canvas, IReadOnlyList<ArtLayer> layers, UnitRenderPalette palette, Vector2 center, float scale, float facing)
    {
        canvas.DrawSetTransform(center, facing, new Vector2(scale, scale));
        foreach (var layer in layers)
        {
            DrawShapeLayer(canvas, layer.Shape, palette.Resolve(layer.ColorRole));
        }
    }

    private static void DrawArtLayers(CanvasItem canvas, IReadOnlyList<UnitBodyRenderLayer> layers, UnitRenderPalette palette, Vector2 center, float scale, float facing)
    {
        canvas.DrawSetTransform(center, facing, new Vector2(scale, scale));
        foreach (var layer in layers)
        {
            DrawShapeLayer(canvas, layer, palette.Resolve(layer.ColorRole));
        }
    }

    private static void DrawArtLayers(
        CanvasItem canvas,
        IReadOnlyList<ArtLayer> layers,
        EntityRenderPalette palette,
        Vector2 center,
        float scale,
        float facing,
        EnvironmentTone? environmentTone)
    {
        canvas.DrawSetTransform(center, facing, new Vector2(scale, scale));
        foreach (var layer in layers)
        {
            DrawShapeLayer(canvas, layer.Shape, palette.Resolve(layer.ColorRole, environmentTone, layer.EnvironmentResponse));
        }
    }

    private static void DrawArtLayers(
        CanvasItem canvas,
        IReadOnlyList<UnitBodyRenderLayer> layers,
        EntityRenderPalette palette,
        Vector2 center,
        float scale,
        float facing,
        EnvironmentTone? environmentTone)
    {
        canvas.DrawSetTransform(center, facing, new Vector2(scale, scale));
        foreach (var layer in layers)
        {
            DrawShapeLayer(canvas, layer, palette.Resolve(layer.ColorRole, environmentTone, layer.EnvironmentResponse));
        }
    }

    private static void DrawShapeLayer(CanvasItem canvas, UnitShapeLayer layer, Color color)
    {
        switch (layer.Kind)
        {
            case UnitShapeKind.Polygon when layer.Filled:
                canvas.DrawColoredPolygon(layer.Points, color);
                break;
            case UnitShapeKind.Polygon:
                DrawClosedPolyline(canvas, layer.Points, color, layer.Width);
                break;
            case UnitShapeKind.Line:
                canvas.DrawLine(layer.From, layer.To, color, layer.Width, AntiAliasedUnitStroke);
                break;
            case UnitShapeKind.Circle:
                DrawCircleLayer(canvas, layer, color);
                break;
            case UnitShapeKind.Arc:
                canvas.DrawArc(layer.From, layer.Radius, 0, Mathf.Tau, UnitArcSegments, color, layer.Width, AntiAliasedUnitStroke);
                break;
        }
    }

    private static void DrawShapeLayer(CanvasItem canvas, UnitBodyRenderLayer layer, Color color)
    {
        var shape = layer.Shape;
        switch (shape.Kind)
        {
            case UnitShapeKind.Polygon when shape.Filled:
                canvas.DrawColoredPolygon(shape.Points, color);
                break;
            case UnitShapeKind.Polygon:
                canvas.DrawPolyline(layer.ClosedPoints ?? shape.Points, color, shape.Width, AntiAliasedUnitStroke);
                break;
            case UnitShapeKind.Line:
                canvas.DrawLine(shape.From, shape.To, color, shape.Width, AntiAliasedUnitStroke);
                break;
            case UnitShapeKind.Circle:
                DrawCircleLayer(canvas, shape, color);
                break;
            case UnitShapeKind.Arc:
                canvas.DrawArc(shape.From, shape.Radius, 0, Mathf.Tau, UnitArcSegments, color, shape.Width, AntiAliasedUnitStroke);
                break;
        }
    }

    private static void DrawClosedPolyline(CanvasItem canvas, IReadOnlyList<Vector2> points, Color color, float width)
    {
        if (points.Count == 0)
        {
            return;
        }

        var closedCount = points.Count + 1;
        var buffers = _closedPolylineBuffers ??= [];
        if (!buffers.TryGetValue(closedCount, out var buffer))
        {
            buffer = new Vector2[closedCount];
            buffers[closedCount] = buffer;
        }

        for (var i = 0; i < points.Count; i++)
        {
            buffer[i] = points[i];
        }

        buffer[points.Count] = points[0];
        canvas.DrawPolyline(buffer, color, width, AntiAliasedUnitStroke);
    }

    private static void DrawCircleLayer(CanvasItem canvas, UnitShapeLayer layer, Color color)
    {
        if (layer.Filled)
        {
            canvas.DrawCircle(layer.From, layer.Radius, color);
            return;
        }

        canvas.DrawCircle(layer.From, layer.Radius, color, false, layer.Width, AntiAliasedUnitStroke);
    }

}
