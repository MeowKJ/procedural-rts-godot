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

    public static SkirmishMapLayout ToSkirmishMapLayout(this MapSpec spec)
    {
        return new SkirmishMapLayout(
            spec.WorldSize.ToVector2(),
            spec.StartFor(new OwnerId(1)).Position.ToVector2(),
            spec.StartFor(new OwnerId(2)).Position.ToVector2(),
            spec.Resources
                .Select(resource => new SkirmishResourceNode(
                    resource.Position.ToVector2(),
                    resource.Radius,
                    resource.Amount,
                    resource.Accent.ToColor()))
                .ToArray(),
            spec.Obstacles
                .Select(obstacle => obstacle.ToPlacementObstacle())
                .ToArray());
    }
}
