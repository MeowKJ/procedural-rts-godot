using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class GridLayer : Node2D
{
    private const int LargeArcSegments = 48;
    private const int MediumArcSegments = 36;
    private const int SmallArcSegments = 28;
    private IReadOnlyList<TerrainFloorTileLayout>? _floorTileLayout;
    private Vector2 _cachedLayoutWorldSize = new(-1, -1);
    private Rect2? _visibleWorldRect;

    public Vector2 WorldSize { get; set; } = new(3600, 2400);
    public Func<WorldVisualThemeState>? VisualThemeProvider { get; init; }
    public Rect2? VisibleWorldRect
    {
        get => _visibleWorldRect;
        set
        {
            if (_visibleWorldRect == value)
            {
                return;
            }

            _visibleWorldRect = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var theme = VisualThemeProvider?.Invoke();
        var palette = theme is null
            ? WorldThemeMath.Palette(WorldVisualTheme.NightRadar)
            : WorldThemeMath.Palette(theme);
        var profile = theme is null
            ? WorldThemeMath.Profile(WorldVisualTheme.NightRadar)
            : WorldThemeMath.Profile(theme);
        var visibleRect = VisibleDrawRect();
        DrawRect(visibleRect, palette.Background);
        DrawWorldBoundary(palette, visibleRect);
    }

    private Rect2 VisibleDrawRect()
    {
        var worldRect = new Rect2(Vector2.Zero, WorldSize);
        return (VisibleWorldRect ?? worldRect).Intersection(worldRect).Grow(96);
    }

    private void DrawFloorPanels(WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        foreach (var layout in FloorTileLayout())
        {
            if (!visibleRect.Intersects(layout.Rect))
            {
                continue;
            }

            var (fill, edge) = TerrainFloorMath.PaletteFor(layout.Kind, layout.Noise, palette);
            var tile = new TerrainFloorTile(layout.Rect, layout.Kind, layout.Noise, fill, edge);
            if (tile.Kind is TerrainFloorKind.Ground)
            {
                DrawGroundWear(tile, palette, profile);
                continue;
            }

            DrawRect(tile.Rect, tile.Fill, true);
            var edgeWidth = tile.Kind == TerrainFloorKind.NavigationLane
                ? 1.05f + profile.PlanningClarity * 0.45f
                : 0.68f + profile.TerrainReadability * 0.28f;
            DrawRect(tile.Rect, ScaleAlpha(tile.Edge, 0.86f + profile.TerrainReadability * 0.24f), false, edgeWidth);
            DrawTileMotif(tile, palette, profile);
        }
    }

    private IReadOnlyList<TerrainFloorTileLayout> FloorTileLayout()
    {
        if (_floorTileLayout is not null && _cachedLayoutWorldSize == WorldSize)
        {
            return _floorTileLayout;
        }

        _cachedLayoutWorldSize = WorldSize;
        _floorTileLayout = TerrainFloorMath.CreateTileLayout(WorldSize);
        return _floorTileLayout;
    }

    private void DrawGroundWear(TerrainFloorTile tile, WorldThemePalette palette, WorldThemeTacticalProfile profile)
    {
        if (tile.Noise < 0.48f)
        {
            return;
        }

        var alpha = (tile.Noise - 0.48f) * 0.055f * (0.7f + profile.TerrainReadability * 0.3f);
        var center = tile.Rect.GetCenter();
        var half = tile.Rect.Size * 0.22f;
        var offset = new Vector2((tile.Noise - 0.5f) * 22f, (0.5f - tile.Noise) * 16f);
        DrawLine(center - new Vector2(half.X, -offset.Y), center + new Vector2(half.X * 0.72f, offset.Y), ScaleAlpha(palette.StrataLine, alpha), 0.75f, true);
        if (tile.Noise > 0.82f)
        {
            DrawLine(center + offset - new Vector2(0, half.Y * 0.55f), center + offset + new Vector2(0, half.Y * 0.52f), ScaleAlpha(palette.Boundary, alpha * 0.7f), 0.65f, true);
        }
    }

    private void DrawCommandZoneWashes(WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        DrawCommandZone(new Vector2(WorldSize.X * 0.16f, WorldSize.Y * 0.30f), 470, palette, profile, visibleRect);
        DrawCommandZone(new Vector2(WorldSize.X * 0.76f, WorldSize.Y * 0.54f), 540, palette, profile, visibleRect);
    }

    private void DrawCommandZone(Vector2 center, float radius, WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        if (!CircleIntersectsVisible(center, radius, visibleRect))
        {
            return;
        }

        var fill = ScaleAlpha(palette.CommandFill, 0.18f + profile.RebuildingFocus * 0.08f);
        var edge = ScaleAlpha(palette.CommandEdge, 0.22f + profile.RepairFocus * 0.12f);
        DrawSoftCommandField(center, radius, fill, edge, profile);
    }

    private void DrawSoftCommandField(Vector2 center, float radius, Color fill, Color edge, WorldThemeTacticalProfile profile)
    {
        for (var ring = 5; ring >= 0; ring--)
        {
            var t = ring / 5f;
            var ringRadius = radius * Mathf.Lerp(1.0f, 0.42f, 1 - t);
            var alpha = fill.A * Mathf.Pow(1 - t * 0.72f, 1.7f);
            DrawCircle(center, ringRadius, new Color(fill, alpha));
        }

        DrawCommandLobe(center + new Vector2(radius * 0.12f, -radius * 0.06f), radius * 0.64f, fill, 0.52f);
        DrawCommandLobe(center + new Vector2(-radius * 0.18f, radius * 0.12f), radius * 0.48f, fill, 0.34f);

        var clarity = 0.74f + profile.PlanningClarity * 0.22f;
        DrawArc(center, radius * 0.92f, -0.22f, Mathf.Pi * 1.08f, LargeArcSegments, ScaleAlpha(edge, 0.58f * clarity), 1.25f, true);
        DrawArc(center + new Vector2(radius * 0.07f, -radius * 0.05f), radius * 0.58f, Mathf.Pi * 0.18f, Mathf.Pi * 1.42f, MediumArcSegments, ScaleAlpha(edge, 0.34f * clarity), 0.9f, true);
        DrawArc(center + new Vector2(-radius * 0.18f, radius * 0.12f), radius * 0.34f, Mathf.Pi * 1.12f, Mathf.Pi * 1.92f, SmallArcSegments, ScaleAlpha(edge, 0.22f * clarity), 0.75f, true);
    }

    private void DrawCommandLobe(Vector2 center, float radius, Color fill, float alphaScale)
    {
        for (var ring = 3; ring >= 0; ring--)
        {
            var t = ring / 3f;
            var ringRadius = radius * Mathf.Lerp(1.0f, 0.36f, 1 - t);
            var alpha = fill.A * alphaScale * Mathf.Pow(1 - t * 0.76f, 1.55f);
            DrawCircle(center, ringRadius, new Color(fill, alpha));
        }
    }

    private void DrawTileMotif(TerrainFloorTile tile, WorldThemePalette palette, WorldThemeTacticalProfile profile)
    {
        var center = tile.Rect.GetCenter();
        var planningAlpha = 0.78f + profile.PlanningClarity * 0.34f;
        switch (tile.Kind)
        {
            case TerrainFloorKind.Water:
                DrawArc(center, tile.Rect.Size.X * 0.24f, 0.2f, Mathf.Pi - 0.2f, 24, ScaleAlpha(palette.WaterEdge, 0.92f + profile.ResourceReadability * 0.18f), 1.1f, true);
                DrawArc(center + new Vector2(tile.Rect.Size.X * 0.15f, tile.Rect.Size.Y * 0.2f), tile.Rect.Size.X * 0.18f, 0.1f, Mathf.Pi - 0.1f, 20, ScaleAlpha(palette.Boundary, 0.28f), 0.8f, true);
                break;
            case TerrainFloorKind.Coast:
                DrawLine(tile.Rect.Position + new Vector2(12, tile.Rect.Size.Y - 18), tile.Rect.End - new Vector2(12, 28), ScaleAlpha(palette.CoastEdge, 1.05f + profile.ResourceReadability * 0.22f), 1.2f, true);
                break;
            case TerrainFloorKind.NavigationLane:
                DrawLine(new Vector2(tile.Rect.Position.X + 12, center.Y), new Vector2(tile.Rect.End.X - 12, center.Y), ScaleAlpha(palette.NavigationLine, planningAlpha), 1.05f + profile.PlanningClarity * 0.34f, true);
                DrawLine(center + new Vector2(0, -9), center + new Vector2(0, 9), ScaleAlpha(palette.Boundary, 0.24f + profile.TerrainReadability * 0.12f), 0.9f, true);
                break;
        }
    }

    private void DrawNavigationHints(WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        var lane = ScaleAlpha(palette.NavigationLine, 0.88f + profile.PlanningClarity * 0.32f);
        var dim = ScaleAlpha(palette.Boundary, 0.26f + profile.TerrainReadability * 0.16f);
        DrawVisibleLine(new Vector2(0, WorldSize.Y * 0.82f), new Vector2(WorldSize.X, WorldSize.Y * 0.82f - WorldSize.X * 0.23f), lane, 3.4f + profile.PlanningClarity * 1.2f, visibleRect);
        DrawVisibleLine(new Vector2(0, WorldSize.Y * 0.57f), new Vector2(WorldSize.X, WorldSize.Y * 0.57f), dim, 1.8f + profile.TerrainReadability * 0.7f, visibleRect);
        DrawVisibleLine(new Vector2(WorldSize.X * 0.48f, 0), new Vector2(WorldSize.X * 0.48f, WorldSize.Y), dim, 1.5f + profile.TerrainReadability * 0.55f, visibleRect);
    }

    private void DrawWaterHighlights(WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        var lagoon = new Vector2(WorldSize.X * 0.84f, WorldSize.Y * 0.16f);
        if (!CircleIntersectsVisible(lagoon, WorldSize.X * 0.20f, visibleRect))
        {
            return;
        }

        DrawArc(lagoon, WorldSize.X * 0.20f, 0, Mathf.Tau, LargeArcSegments, ScaleAlpha(palette.WaterEdge, 0.62f + profile.ResourceReadability * 0.20f), 2.0f + profile.ResourceReadability * 0.35f, true);
        DrawArc(lagoon, WorldSize.X * 0.155f, 0, Mathf.Tau, LargeArcSegments, ScaleAlpha(palette.CoastEdge, 0.54f + profile.ResourceReadability * 0.18f), 1.25f + profile.TerrainReadability * 0.24f, true);
    }

    private void DrawDirectionalStrata(int stride, WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        var startY = (int)MathF.Floor((visibleRect.Position.Y - WorldSize.X * 0.16f - stride) / stride) * stride;
        var endY = visibleRect.End.Y + stride;
        for (var y = startY; y < endY; y += stride)
        {
            DrawVisibleLine(new Vector2(0, y), new Vector2(WorldSize.X, y + WorldSize.X * 0.16f), ScaleAlpha(palette.StrataLine, 0.84f + profile.TerrainReadability * 0.22f), 0.9f + profile.TerrainReadability * 0.16f, visibleRect);
            DrawVisibleLine(new Vector2(0, y + 34), new Vector2(WorldSize.X, y + 34 + WorldSize.X * 0.16f), ScaleAlpha(palette.Boundary, 0.09f + profile.SignalNoise * 0.08f), 0.8f, visibleRect);
        }
    }

    private void DrawOldCitySurveyMarks(WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        var color = ScaleAlpha(palette.CommandEdge, 0.28f + profile.PlanningClarity * 0.10f);
        var points = new[]
        {
            new Vector2(WorldSize.X * 0.12f, WorldSize.Y * 0.16f),
            new Vector2(WorldSize.X * 0.27f, WorldSize.Y * 0.42f),
            new Vector2(WorldSize.X * 0.44f, WorldSize.Y * 0.28f),
            new Vector2(WorldSize.X * 0.58f, WorldSize.Y * 0.55f),
            new Vector2(WorldSize.X * 0.72f, WorldSize.Y * 0.36f),
            new Vector2(WorldSize.X * 0.83f, WorldSize.Y * 0.72f),
            new Vector2(WorldSize.X * 0.20f, WorldSize.Y * 0.78f),
        };

        foreach (var point in points)
        {
            if (!CircleIntersectsVisible(point, 40, visibleRect))
            {
                continue;
            }

            DrawLine(point + new Vector2(-18, 0), point + new Vector2(18, 0), color, 1.0f, true);
            DrawLine(point + new Vector2(0, -18), point + new Vector2(0, 18), color, 1.0f, true);
            DrawArc(point, 34, 0.18f, Mathf.Pi * 1.18f, 36, ScaleAlpha(palette.Boundary, 0.13f + profile.SignalNoise * 0.05f), 0.75f, true);
        }
    }

    private void DrawIrregularDistrictTraces(WorldThemePalette palette, WorldThemeTacticalProfile profile, Rect2 visibleRect)
    {
        var line = ScaleAlpha(palette.Boundary, 0.08f + profile.TerrainReadability * 0.035f);
        DrawTrace(line, 0.9f, visibleRect, [
            new Vector2(WorldSize.X * 0.06f, WorldSize.Y * 0.22f),
            new Vector2(WorldSize.X * 0.18f, WorldSize.Y * 0.20f),
            new Vector2(WorldSize.X * 0.30f, WorldSize.Y * 0.27f),
            new Vector2(WorldSize.X * 0.40f, WorldSize.Y * 0.25f),
        ]);
        DrawTrace(line, 0.85f, visibleRect, [
            new Vector2(WorldSize.X * 0.10f, WorldSize.Y * 0.66f),
            new Vector2(WorldSize.X * 0.24f, WorldSize.Y * 0.62f),
            new Vector2(WorldSize.X * 0.39f, WorldSize.Y * 0.67f),
            new Vector2(WorldSize.X * 0.53f, WorldSize.Y * 0.63f),
        ]);
        DrawTrace(line, 0.75f, visibleRect, [
            new Vector2(WorldSize.X * 0.62f, WorldSize.Y * 0.18f),
            new Vector2(WorldSize.X * 0.69f, WorldSize.Y * 0.31f),
            new Vector2(WorldSize.X * 0.75f, WorldSize.Y * 0.46f),
            new Vector2(WorldSize.X * 0.88f, WorldSize.Y * 0.50f),
        ]);
        DrawTrace(ScaleAlpha(palette.StrataLine, 0.10f), 0.7f, visibleRect, [
            new Vector2(WorldSize.X * 0.16f, WorldSize.Y * 0.08f),
            new Vector2(WorldSize.X * 0.31f, WorldSize.Y * 0.15f),
            new Vector2(WorldSize.X * 0.48f, WorldSize.Y * 0.13f),
            new Vector2(WorldSize.X * 0.61f, WorldSize.Y * 0.21f),
        ]);
    }

    private void DrawTrace(Color color, float width, Rect2 visibleRect, Vector2[] points)
    {
        for (var index = 0; index < points.Length - 1; index++)
        {
            DrawVisibleLine(points[index], points[index + 1], color, width, visibleRect);
        }
    }

    private void DrawVisibleLine(Vector2 from, Vector2 to, Color color, float width, Rect2 visibleRect)
    {
        if (!LineIntersectsVisible(from, to, visibleRect))
        {
            return;
        }

        DrawLine(from, to, color, width, true);
    }

    private void DrawWorldBoundary(WorldThemePalette palette, Rect2 visibleRect)
    {
        var worldRect = new Rect2(Vector2.Zero, WorldSize);
        if (visibleRect.Position.Y <= worldRect.Position.Y)
        {
            DrawLine(worldRect.Position, new Vector2(worldRect.End.X, worldRect.Position.Y), palette.Boundary, 4, true);
        }

        if (visibleRect.End.Y >= worldRect.End.Y)
        {
            DrawLine(new Vector2(worldRect.Position.X, worldRect.End.Y), worldRect.End, palette.Boundary, 4, true);
        }

        if (visibleRect.Position.X <= worldRect.Position.X)
        {
            DrawLine(worldRect.Position, new Vector2(worldRect.Position.X, worldRect.End.Y), palette.Boundary, 4, true);
        }

        if (visibleRect.End.X >= worldRect.End.X)
        {
            DrawLine(new Vector2(worldRect.End.X, worldRect.Position.Y), worldRect.End, palette.Boundary, 4, true);
        }
    }

    private static bool CircleIntersectsVisible(Vector2 center, float radius, Rect2 visibleRect)
    {
        return visibleRect.Intersects(new Rect2(center - Vector2.One * radius, Vector2.One * radius * 2f));
    }

    private static bool LineIntersectsVisible(Vector2 from, Vector2 to, Rect2 visibleRect)
    {
        var min = new Vector2(Mathf.Min(from.X, to.X), Mathf.Min(from.Y, to.Y));
        var max = new Vector2(Mathf.Max(from.X, to.X), Mathf.Max(from.Y, to.Y));
        return visibleRect.Intersects(new Rect2(min, max - min));
    }

    private static Color ScaleAlpha(Color color, float scale)
    {
        return new Color(color, Mathf.Clamp(color.A * scale, 0, 1));
    }

}
