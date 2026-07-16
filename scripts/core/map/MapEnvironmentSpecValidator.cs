using System.Globalization;

namespace ProceduralRts.Core;

public enum MapEnvironmentObjectKind
{
    World,
    Terrain,
    StaticObstacle,
    Resource,
}

public sealed record MapPlacementConflictTarget(
    MapEnvironmentObjectKind Kind,
    string Id,
    string Geometry)
{
    public string KindKey => Kind switch
    {
        MapEnvironmentObjectKind.World => "world",
        MapEnvironmentObjectKind.Terrain => "terrain",
        MapEnvironmentObjectKind.StaticObstacle => "static_obstacle",
        MapEnvironmentObjectKind.Resource => "resource",
        _ => "unknown",
    };

    public override string ToString()
    {
        return $"object={KindKey} id={Id} geometry={Geometry}";
    }
}

public sealed record MapEnvironmentValidationConflict(
    string MapId,
    MapPlacementConflictTarget Target,
    string Detail)
{
    public override string ToString()
    {
        return $"map={MapId} conflict={Target.KindKey} {Target} detail={Detail}";
    }
}

public static class MapEnvironmentSpecValidator
{
    public static IReadOnlyList<MapEnvironmentValidationConflict> Validate(MapSpec map)
    {
        var conflicts = new List<MapEnvironmentValidationConflict>();
        var validWorld = IsFinite(map.WorldSize.Width)
            && IsFinite(map.WorldSize.Height)
            && map.WorldSize.Width > 0
            && map.WorldSize.Height > 0;
        if (!validWorld)
        {
            conflicts.Add(new MapEnvironmentValidationConflict(
                map.Id,
                new MapPlacementConflictTarget(
                    MapEnvironmentObjectKind.World,
                    map.Id,
                    MapPlacementEvidence.World(map.WorldSize)),
                "invalid_size"));
        }

        for (var index = 0; index < map.TerrainCells.Count; index++)
        {
            var terrain = map.TerrainCells[index];
            var detail = RectDetail(terrain.Bounds, map.WorldSize, validWorld);
            if (detail is null && (!IsFinite(terrain.MovementCost) || terrain.MovementCost <= 0))
            {
                detail = "invalid_movement_cost";
            }

            if (detail is not null)
            {
                conflicts.Add(new MapEnvironmentValidationConflict(
                    map.Id,
                    new MapPlacementConflictTarget(
                        MapEnvironmentObjectKind.Terrain,
                        terrain.Id,
                        MapPlacementEvidence.Rect(terrain.Bounds.ToPlacementRect())),
                    detail));
            }
        }

        for (var index = 0; index < map.Obstacles.Count; index++)
        {
            var obstacle = map.Obstacles[index];
            var detail = RectDetail(obstacle.Bounds, map.WorldSize, validWorld);
            if (detail is not null)
            {
                conflicts.Add(new MapEnvironmentValidationConflict(
                    map.Id,
                    new MapPlacementConflictTarget(
                        MapEnvironmentObjectKind.StaticObstacle,
                        obstacle.Id,
                        MapPlacementEvidence.Rect(obstacle.Bounds.ToPlacementRect())),
                    detail));
            }
        }

        for (var index = 0; index < map.Resources.Count; index++)
        {
            var resource = map.Resources[index];
            var finite = IsFinite(resource.Position.X)
                && IsFinite(resource.Position.Y)
                && IsFinite(resource.Radius);
            var detail = !finite || resource.Radius <= 0
                ? "invalid_circle"
                : validWorld && !CircleInside(resource, map.WorldSize)
                    ? "outside"
                    : null;
            if (detail is not null)
            {
                conflicts.Add(new MapEnvironmentValidationConflict(
                    map.Id,
                    new MapPlacementConflictTarget(
                        MapEnvironmentObjectKind.Resource,
                        resource.Id,
                        MapPlacementEvidence.Circle(resource.Position, resource.Radius)),
                    detail));
            }
        }

        return conflicts.AsReadOnly();
    }

    private static string? RectDetail(MapRect rect, MapSize world, bool validWorld)
    {
        if (!IsFinite(rect.X)
            || !IsFinite(rect.Y)
            || !IsFinite(rect.Width)
            || !IsFinite(rect.Height)
            || rect.Width <= 0
            || rect.Height <= 0)
        {
            return "invalid_rect";
        }

        return validWorld
            && (rect.X < 0 || rect.Y < 0 || rect.X + rect.Width > world.Width || rect.Y + rect.Height > world.Height)
                ? "outside"
                : null;
    }

    private static bool CircleInside(MapResourceNodeSpec resource, MapSize world)
    {
        return resource.Position.X - resource.Radius >= 0
            && resource.Position.Y - resource.Radius >= 0
            && resource.Position.X + resource.Radius <= world.Width
            && resource.Position.Y + resource.Radius <= world.Height;
    }

    private static bool IsFinite(float value)
    {
        return float.IsFinite(value);
    }
}

internal static class MapPlacementEvidence
{
    public static string World(MapSize size)
    {
        return FormattableString.Invariant($"size(width={size.Width:R},height={size.Height:R})");
    }

    public static string Rect(PlacementRect rect)
    {
        return FormattableString.Invariant(
            $"rect(x={rect.X:R},y={rect.Y:R},width={rect.Width:R},height={rect.Height:R})");
    }

    public static string Circle(MapPoint center, float radius)
    {
        return FormattableString.Invariant(
            $"circle(x={center.X:R},y={center.Y:R},radius={radius:R})");
    }

    public static string Point(float x, float y, TerrainLayer layer, string relation)
    {
        return FormattableString.Invariant(
            $"point(x={x:R},y={y:R},layer={layer.ToString().ToLowerInvariant()},relation={relation})");
    }
}
