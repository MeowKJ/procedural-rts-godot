namespace ProceduralRts.Core;

public static partial class MapBuildingPlacementValidator
{
    private static void AppendEnvironmentConflicts(
        MapSpec map,
        MapRuntimeEnvironment environment,
        BuildSpec spec,
        MapBuildingSeedSpec building,
        PlacementRect rect,
        IReadOnlyList<PlacementRect> reservations,
        int gridX,
        int gridY,
        List<MapBuildingPlacementConflict> conflicts)
    {
        if (TryFirstTerrainConflict(map, environment, spec.PlacementDomain, rect, "hard", out var terrainTarget))
        {
            conflicts.Add(EnvironmentConflict(
                map.Id,
                MapBuildingPlacementConflictKind.Terrain,
                building,
                gridX,
                gridY,
                terrainTarget));
        }
        else
        {
            for (var reservationIndex = 0; reservationIndex < reservations.Count; reservationIndex++)
            {
                if (!TryFirstTerrainConflict(
                        map,
                        environment,
                        spec.PlacementDomain,
                        reservations[reservationIndex],
                        $"reservation[{reservationIndex}]",
                        out terrainTarget))
                {
                    continue;
                }

                conflicts.Add(EnvironmentConflict(
                    map.Id,
                    MapBuildingPlacementConflictKind.Terrain,
                    building,
                    gridX,
                    gridY,
                    terrainTarget));
                break;
            }
        }

        var obstacleClearance = spec.PlacementClearanceCells * PlacementMath.GridSize;
        for (var obstacleIndex = 0; obstacleIndex < environment.StaticObstacles.Count; obstacleIndex++)
        {
            var obstacle = environment.StaticObstacles[obstacleIndex];
            var relation = ViolatingRelation(rect, reservations, obstacle.Bounds, obstacleClearance);
            if (relation is null)
            {
                continue;
            }

            conflicts.Add(EnvironmentConflict(
                map.Id,
                MapBuildingPlacementConflictKind.StaticObstacle,
                building,
                gridX,
                gridY,
                new MapPlacementConflictTarget(
                    MapEnvironmentObjectKind.StaticObstacle,
                    obstacle.Id,
                    $"{MapPlacementEvidence.Rect(obstacle.Bounds)} relation={relation}")));
            break;
        }

        var resourceClearance = MapPlacementRules.ResourceClearance(spec);
        for (var resourceIndex = 0; resourceIndex < map.Resources.Count; resourceIndex++)
        {
            var resource = map.Resources[resourceIndex];
            var resourceObstacle = new PlacementResourceObstacle(
                resource.Position.X,
                resource.Position.Y,
                resource.Radius);
            var relation = ViolatingRelation(rect, reservations, resourceObstacle, resourceClearance);
            if (relation is null)
            {
                continue;
            }

            conflicts.Add(EnvironmentConflict(
                map.Id,
                MapBuildingPlacementConflictKind.Resource,
                building,
                gridX,
                gridY,
                new MapPlacementConflictTarget(
                    MapEnvironmentObjectKind.Resource,
                    resource.Id,
                    $"{MapPlacementEvidence.Circle(resource.Position, resource.Radius)} relation={relation}")));
            break;
        }
    }

    private static MapBuildingPlacementConflict EnvironmentConflict(
        string mapId,
        MapBuildingPlacementConflictKind kind,
        MapBuildingSeedSpec building,
        int gridX,
        int gridY,
        MapPlacementConflictTarget target)
    {
        return new MapBuildingPlacementConflict(mapId, kind, building, gridX, gridY, Target: target);
    }

    private static string? ViolatingRelation(
        PlacementRect hard,
        IReadOnlyList<PlacementRect> reservations,
        PlacementRect obstacle,
        float clearance)
    {
        if (PlacementMath.ViolatesClearance(hard, obstacle, clearance))
        {
            return "hard";
        }

        for (var index = 0; index < reservations.Count; index++)
        {
            if (PlacementMath.ViolatesClearance(reservations[index], obstacle, clearance))
            {
                return $"reservation[{index}]";
            }
        }

        return null;
    }

    private static string? ViolatingRelation(
        PlacementRect hard,
        IReadOnlyList<PlacementRect> reservations,
        PlacementResourceObstacle resource,
        float clearance)
    {
        if (PlacementMath.ViolatesClearance(hard, resource, clearance))
        {
            return "hard";
        }

        for (var index = 0; index < reservations.Count; index++)
        {
            if (PlacementMath.ViolatesClearance(reservations[index], resource, clearance))
            {
                return $"reservation[{index}]";
            }
        }

        return null;
    }

    private static bool TryFirstTerrainConflict(
        MapSpec map,
        MapRuntimeEnvironment environment,
        MovementDomain placementDomain,
        PlacementRect rect,
        string relation,
        out MapPlacementConflictTarget target)
    {
        var allowed = TerrainPassability.AllowedLayers(placementDomain);
        if (TryTerrainPoint(map, environment, rect.X, rect.Y, allowed, relation, out target)
            || TryTerrainPoint(map, environment, rect.EndX, rect.Y, allowed, relation, out target)
            || TryTerrainPoint(map, environment, rect.X, rect.EndY, allowed, relation, out target)
            || TryTerrainPoint(map, environment, rect.EndX, rect.EndY, allowed, relation, out target)
            || TryTerrainPoint(
                map,
                environment,
                rect.X + rect.Width * 0.5f,
                rect.Y + rect.Height * 0.5f,
                allowed,
                relation,
                out target))
        {
            return true;
        }

        var xSteps = Math.Max(0, (int)MathF.Ceiling(rect.Width / PlacementMath.TerrainSampleStep) - 1);
        var ySteps = Math.Max(0, (int)MathF.Ceiling(rect.Height / PlacementMath.TerrainSampleStep) - 1);
        for (var xStep = 1; xStep <= xSteps; xStep++)
        {
            var x = rect.X + rect.Width * xStep / (xSteps + 1);
            if (TryTerrainPoint(map, environment, x, rect.Y, allowed, relation, out target)
                || TryTerrainPoint(map, environment, x, rect.EndY, allowed, relation, out target))
            {
                return true;
            }
        }

        for (var yStep = 1; yStep <= ySteps; yStep++)
        {
            var y = rect.Y + rect.Height * yStep / (ySteps + 1);
            if (TryTerrainPoint(map, environment, rect.X, y, allowed, relation, out target)
                || TryTerrainPoint(map, environment, rect.EndX, y, allowed, relation, out target))
            {
                return true;
            }
        }

        target = null!;
        return false;
    }

    private static bool TryTerrainPoint(
        MapSpec map,
        MapRuntimeEnvironment environment,
        float x,
        float y,
        TerrainLayer allowed,
        string relation,
        out MapPlacementConflictTarget target)
    {
        var sample = environment.SampleTerrain(x, y, map.WorldSize.Width, map.WorldSize.Height);
        if ((sample.Layer & allowed) != 0)
        {
            target = null!;
            return false;
        }

        target = new MapPlacementConflictTarget(
            MapEnvironmentObjectKind.Terrain,
            sample.SourceId,
            MapPlacementEvidence.Point(x, y, sample.Layer, relation));
        return true;
    }
}
