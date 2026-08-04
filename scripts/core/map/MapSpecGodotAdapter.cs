using Godot;

namespace ProceduralRts.Core;

public static class MapSpecGodotAdapter
{
    public static Vector2 ToVector2(this MapPoint point)
    {
        return new Vector2(point.X, point.Y);
    }

    public static MapPoint ToMapPoint(this Vector2 point)
    {
        return new MapPoint(point.X, point.Y);
    }

    public static MapSize ToMapSize(this Vector2 size)
    {
        return new MapSize(size.X, size.Y);
    }

    public static Vector2 ToVector2(this MapSize size)
    {
        return new Vector2(size.Width, size.Height);
    }

    public static Color ToColor(this MapColor color)
    {
        return new Color(color.Hex);
    }

    public static PlacementObstacle ToPlacementObstacle(this MapObstacleSpec obstacle)
    {
        return new PlacementObstacle(
            obstacle.Bounds.X,
            obstacle.Bounds.Y,
            obstacle.Bounds.Width,
            obstacle.Bounds.Height);
    }

}
