namespace ProceduralRts.Core;

internal static class SkirmishStartingEnvironmentAdapter
{
    private const int MaxResourceMoveSteps = 32;

    public static MapSpec Apply(MapSpec map)
    {
        var adapted = map with
        {
            TerrainCells = AppendGroundOverrides(map),
            Resources = MoveResourcePairs(map),
        };
        MapBuildingPlacementValidator.EnsureValid(adapted);
        return adapted;
    }

    private static IReadOnlyList<MapTerrainCellSpec> AppendGroundOverrides(MapSpec map)
    {
        var terrain = new List<MapTerrainCellSpec>(map.TerrainCells.Count + map.Buildings.Count * 2);
        terrain.AddRange(map.TerrainCells);
        for (var buildingIndex = 0; buildingIndex < map.Buildings.Count; buildingIndex++)
        {
            var building = map.Buildings[buildingIndex];
            var spec = BuildSpecCatalog.For(building.Kind);
            PlacementMath.TryNormalizeCardinalFacing(building.Facing, out var facing);
            var footprint = spec.FootprintCells.Rotated(facing).WorldSize;
            var hard = PlacementMath.RectFromCenter(
                building.Position.X,
                building.Position.Y,
                footprint.X,
                footprint.Y);
            terrain.Add(GroundCell($"generated.start.{buildingIndex}.hard", hard));

            for (var reservationIndex = 0;
                 reservationIndex < spec.PlacementReservations.Count;
                 reservationIndex++)
            {
                var reservation = PlacementReservationMath.WorldRect(
                    spec,
                    spec.PlacementReservations[reservationIndex],
                    building.Position.ToVector2(),
                    facing);
                terrain.Add(GroundCell(
                    $"generated.start.{buildingIndex}.reservation.{reservationIndex}",
                    reservation));
            }
        }

        return terrain.AsReadOnly();
    }

    private static IReadOnlyList<MapResourceNodeSpec> MoveResourcePairs(MapSpec map)
    {
        if (map.Resources.Count % 2 != 0)
        {
            throw new InvalidOperationException("Generated skirmish resources must be emitted as mirrored pairs.");
        }

        var resources = new MapResourceNodeSpec[map.Resources.Count];
        for (var pairIndex = 0; pairIndex < map.Resources.Count; pairIndex += 2)
        {
            var first = map.Resources[pairIndex];
            var second = map.Resources[pairIndex + 1];
            if (!IsMirror(first.Position, second.Position, map.WorldSize))
            {
                throw new InvalidOperationException(
                    $"Generated resource pair '{first.Id}'/'{second.Id}' must remain mirrored.");
            }

            var stepX = Math.Sign(map.WorldSize.Width * 0.5f - first.Position.X) * PlacementMath.GridSize;
            var stepY = Math.Sign(map.WorldSize.Height * 0.5f - first.Position.Y) * PlacementMath.GridSize;
            var found = false;
            for (var step = 0; step <= MaxResourceMoveSteps; step++)
            {
                var firstPosition = new MapPoint(
                    first.Position.X + stepX * step,
                    first.Position.Y + stepY * step);
                var secondPosition = SkirmishMapSpecGenerator.Mirror(firstPosition, map.WorldSize);
                var firstCandidate = first with { Position = firstPosition };
                var secondCandidate = second with { Position = secondPosition };
                if (!IsValidResource(firstCandidate, map)
                    || !IsValidResource(secondCandidate, map))
                {
                    continue;
                }

                resources[pairIndex] = firstCandidate;
                resources[pairIndex + 1] = secondCandidate;
                found = true;
                break;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"Generated resource pair '{first.Id}'/'{second.Id}' could not reach valid starting-building clearance within {MaxResourceMoveSteps} grid steps.");
            }
        }

        return Array.AsReadOnly(resources);
    }

    private static bool IsValidResource(MapResourceNodeSpec resource, MapSpec map)
    {
        if (resource.Position.X - resource.Radius < 0
            || resource.Position.Y - resource.Radius < 0
            || resource.Position.X + resource.Radius > map.WorldSize.Width
            || resource.Position.Y + resource.Radius > map.WorldSize.Height)
        {
            return false;
        }

        var obstacle = new PlacementResourceObstacle(
            resource.Position.X,
            resource.Position.Y,
            resource.Radius);
        for (var buildingIndex = 0; buildingIndex < map.Buildings.Count; buildingIndex++)
        {
            var building = map.Buildings[buildingIndex];
            var spec = BuildSpecCatalog.For(building.Kind);
            PlacementMath.TryNormalizeCardinalFacing(building.Facing, out var facing);
            var footprint = spec.FootprintCells.Rotated(facing).WorldSize;
            var hard = PlacementMath.RectFromCenter(
                building.Position.X,
                building.Position.Y,
                footprint.X,
                footprint.Y);
            var clearance = MapPlacementRules.ResourceClearance(spec);
            if (PlacementMath.ViolatesClearance(hard, obstacle, clearance))
            {
                return false;
            }

            for (var reservationIndex = 0;
                 reservationIndex < spec.PlacementReservations.Count;
                 reservationIndex++)
            {
                var reservation = PlacementReservationMath.WorldRect(
                    spec,
                    spec.PlacementReservations[reservationIndex],
                    building.Position.ToVector2(),
                    facing);
                if (PlacementMath.ViolatesClearance(reservation, obstacle, clearance))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static MapTerrainCellSpec GroundCell(string id, PlacementRect rect)
    {
        return new MapTerrainCellSpec(
            id,
            new MapRect(rect.X, rect.Y, rect.Width, rect.Height),
            "generated.start.ground");
    }

    private static bool IsMirror(MapPoint first, MapPoint second, MapSize world)
    {
        var mirrored = SkirmishMapSpecGenerator.Mirror(first, world);
        return MathF.Abs(mirrored.X - second.X) <= 0.001f
            && MathF.Abs(mirrored.Y - second.Y) <= 0.001f;
    }
}
