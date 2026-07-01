using Godot;

namespace ProceduralRts.Core;

public static class TerrainFloorMath
{
    public const int DefaultTileSize = 160;

    public static IReadOnlyList<TerrainFloorTile> CreateTiles(Vector2 worldSize, int tileSize = DefaultTileSize)
    {
        return CreateTiles(worldSize, WorldThemeMath.Palette(WorldVisualTheme.NightRadar), tileSize);
    }

    public static IReadOnlyList<TerrainFloorTile> CreateTiles(Vector2 worldSize, WorldThemePalette palette, int tileSize = DefaultTileSize)
    {
        return CreateTileLayout(worldSize, tileSize)
            .Select(tile =>
            {
                var (fill, edge) = PaletteFor(tile.Kind, tile.Noise, palette);
                return new TerrainFloorTile(tile.Rect, tile.Kind, tile.Noise, fill, edge);
            })
            .ToList();
    }

    public static IReadOnlyList<TerrainFloorTileLayout> CreateTileLayout(Vector2 worldSize, int tileSize = DefaultTileSize)
    {
        var tiles = new List<TerrainFloorTileLayout>();
        for (var y = 0; y < worldSize.Y; y += tileSize)
        {
            for (var x = 0; x < worldSize.X; x += tileSize)
            {
                var rect = new Rect2(
                    x + 3,
                    y + 3,
                    MathF.Min(tileSize - 6, worldSize.X - x - 6),
                    MathF.Min(tileSize - 6, worldSize.Y - y - 6));
                if (rect.Size.X <= 0 || rect.Size.Y <= 0)
                {
                    continue;
                }

                var center = rect.GetCenter();
                var noise = PanelNoise(x / tileSize, y / tileSize);
                var kind = KindAt(center, worldSize);
                tiles.Add(new TerrainFloorTileLayout(rect, kind, noise));
            }
        }

        return tiles;
    }

    public static (Color Fill, Color Edge) PaletteFor(TerrainFloorKind kind, float noise, WorldThemePalette palette)
    {
        return Palette(kind, noise, palette);
    }

    public static TerrainFloorKind KindAt(Vector2 point, Vector2 worldSize)
    {
        var water = WaterField(point, worldSize);
        if (water > 0.58f)
        {
            return TerrainFloorKind.Water;
        }

        if (water > 0.43f)
        {
            return TerrainFloorKind.Coast;
        }

        if (IsNavigationLane(point, worldSize))
        {
            return TerrainFloorKind.NavigationLane;
        }

        return TerrainFloorKind.Ground;
    }

    public static bool IsNavigationLane(Vector2 point, Vector2 worldSize)
    {
        var diagonal = MathF.Abs(point.Y - (worldSize.Y * 0.82f - point.X * 0.23f));
        var horizontal = MathF.Abs(point.Y - worldSize.Y * 0.57f);
        var vertical = MathF.Abs(point.X - worldSize.X * 0.48f);
        return diagonal < 42 || horizontal < 28 || vertical < 24;
    }

    private static float WaterField(Vector2 point, Vector2 worldSize)
    {
        var lagoon = new Vector2(worldSize.X * 0.84f, worldSize.Y * 0.16f);
        var normalized = new Vector2(
            (point.X - lagoon.X) / (worldSize.X * 0.22f),
            (point.Y - lagoon.Y) / (worldSize.Y * 0.18f));
        var basin = 1 - MathF.Min(1, normalized.Length());
        var riverCenter = worldSize.Y * 0.92f - point.X * 0.18f;
        var river = 1 - MathF.Min(1, MathF.Abs(point.Y - riverCenter) / 92f);
        return MathF.Max(basin, river * 0.72f);
    }

    private static (Color Fill, Color Edge) Palette(TerrainFloorKind kind, float noise, WorldThemePalette palette)
    {
        var noiseAlpha = noise * 0.05f;
        return kind switch
        {
            TerrainFloorKind.Water => (
                WithAlphaOffset(palette.WaterFill, noiseAlpha),
                WithAlphaOffset(palette.WaterEdge, noiseAlpha * 0.6f)),
            TerrainFloorKind.Coast => (
                WithAlphaOffset(palette.CoastFill, noiseAlpha),
                WithAlphaOffset(palette.CoastEdge, noiseAlpha * 0.6f)),
            TerrainFloorKind.NavigationLane => (
                WithAlphaOffset(palette.NavigationFill, noiseAlpha),
                WithAlphaOffset(palette.NavigationEdge, noiseAlpha * 0.8f)),
            TerrainFloorKind.CommandPlate => (
                WithAlphaOffset(palette.CommandFill, noiseAlpha),
                WithAlphaOffset(palette.CommandEdge, noiseAlpha * 0.7f)),
            _ => (
                WithAlphaOffset(palette.GroundFill, noiseAlpha),
                WithAlphaOffset(palette.GroundEdge, noiseAlpha * 0.5f)),
        };
    }

    private static Color WithAlphaOffset(Color color, float alphaOffset)
    {
        return new Color(color, Mathf.Clamp(color.A + alphaOffset, 0, 1));
    }

    private static float PanelNoise(int x, int y)
    {
        var value = unchecked((uint)(x * 73856093 ^ y * 19349663));
        value ^= value >> 13;
        value *= 1274126177;
        return (value & 1023) / 1023f;
    }
}
