namespace ProceduralRts.Core;

public enum MapBuildingPlacementConflictKind
{
    Rotation,
    Unsnapped,
    Outside,
    Overlap,
    Clearance,
    Reserved,
}

public sealed record MapBuildingPlacementConflict(
    string MapId,
    MapBuildingPlacementConflictKind Conflict,
    MapBuildingSeedSpec Building,
    int GridX,
    int GridY,
    MapBuildingSeedSpec? Other = null)
{
    public override string ToString()
    {
        var subject = Identity(Building, GridX, GridY);
        if (Other is null)
        {
            return $"map={MapId} {subject} conflict={Conflict.ToString().ToLowerInvariant()}";
        }

        var otherGrid = MapBuildingPlacementValidator.GridCoordinate(Other);
        return $"map={MapId} {subject} conflict={Conflict.ToString().ToLowerInvariant()} other=[{Identity(Other, otherGrid.X, otherGrid.Y)}]";
    }

    private static string Identity(MapBuildingSeedSpec building, int gridX, int gridY)
    {
        return $"owner={building.OwnerId.Value} faction={building.Faction} kind={building.Kind} grid=({gridX},{gridY})";
    }
}

public static class MapBuildingPlacementValidator
{
    public static IReadOnlyList<MapBuildingPlacementConflict> Validate(MapSpec map)
    {
        var conflicts = new List<MapBuildingPlacementConflict>();
        var placements = new List<Placement>();

        foreach (var building in map.Buildings)
        {
            var spec = BuildSpecCatalog.For(building.Kind);
            var isCardinal = PlacementMath.TryNormalizeCardinalFacing(building.Facing, out var cardinalFacing);
            var footprint = spec.FootprintCells.Rotated(cardinalFacing);
            var snappedX = PlacementMath.SnapAnchor(building.Position.X, footprint.WidthCells);
            var snappedY = PlacementMath.SnapAnchor(building.Position.Y, footprint.HeightCells);
            var rect = PlacementMath.RectFromCenter(
                building.Position.X,
                building.Position.Y,
                footprint.WorldSize.X,
                footprint.WorldSize.Y);
            var grid = GridCoordinate(snappedX, snappedY, footprint);
            var reservations = new PlacementRect[spec.PlacementReservations.Count];
            for (var reservationIndex = 0; reservationIndex < reservations.Length; reservationIndex++)
            {
                reservations[reservationIndex] = PlacementReservationMath.WorldRect(
                    spec,
                    spec.PlacementReservations[reservationIndex],
                    building.Position.ToVector2(),
                    cardinalFacing);
            }

            if (!isCardinal)
            {
                conflicts.Add(new MapBuildingPlacementConflict(
                    map.Id,
                    MapBuildingPlacementConflictKind.Rotation,
                    building,
                    grid.X,
                    grid.Y));
            }

            if (!NearlyEqual(building.Position.X, snappedX) || !NearlyEqual(building.Position.Y, snappedY))
            {
                conflicts.Add(new MapBuildingPlacementConflict(
                    map.Id,
                    MapBuildingPlacementConflictKind.Unsnapped,
                    building,
                    grid.X,
                    grid.Y));
            }

            if (IsOutside(rect, map.WorldSize)
                || reservations.Any(reservation => IsOutside(reservation, map.WorldSize)))
            {
                conflicts.Add(new MapBuildingPlacementConflict(
                    map.Id,
                    MapBuildingPlacementConflictKind.Outside,
                    building,
                    grid.X,
                    grid.Y));
            }

            placements.Add(new Placement(
                building,
                rect,
                reservations,
                spec.PlacementClearanceCells,
                grid.X,
                grid.Y));
        }

        for (var firstIndex = 0; firstIndex < placements.Count; firstIndex++)
        {
            var first = placements[firstIndex];
            for (var secondIndex = firstIndex + 1; secondIndex < placements.Count; secondIndex++)
            {
                var second = placements[secondIndex];
                if (PlacementMath.Intersects(first.Rect, second.Rect))
                {
                    conflicts.Add(new MapBuildingPlacementConflict(
                        map.Id,
                        MapBuildingPlacementConflictKind.Overlap,
                        first.Building,
                        first.GridX,
                        first.GridY,
                        second.Building));
                    continue;
                }

                var pairClearance = Math.Max(first.ClearanceCells, second.ClearanceCells)
                    * PlacementMath.GridSize;
                if (PlacementMath.ViolatesClearance(first.Rect, second.Rect, pairClearance))
                {
                    conflicts.Add(new MapBuildingPlacementConflict(
                        map.Id,
                        MapBuildingPlacementConflictKind.Clearance,
                        first.Building,
                        first.GridX,
                        first.GridY,
                        second.Building));
                    continue;
                }

                if (ReservationsConflict(first, second, pairClearance))
                {
                    conflicts.Add(new MapBuildingPlacementConflict(
                        map.Id,
                        MapBuildingPlacementConflictKind.Reserved,
                        first.Building,
                        first.GridX,
                        first.GridY,
                        second.Building));
                }
            }
        }

        return conflicts.AsReadOnly();
    }

    public static void EnsureValid(MapSpec map)
    {
        var conflicts = Validate(map);
        if (conflicts.Count > 0)
        {
            throw new MapBuildingPlacementValidationException(map.Id, conflicts);
        }
    }

    public static (int X, int Y) GridCoordinate(MapBuildingSeedSpec building)
    {
        var spec = BuildSpecCatalog.For(building.Kind);
        PlacementMath.TryNormalizeCardinalFacing(building.Facing, out var cardinalFacing);
        var footprint = spec.FootprintCells.Rotated(cardinalFacing);
        var snappedX = PlacementMath.SnapAnchor(building.Position.X, footprint.WidthCells);
        var snappedY = PlacementMath.SnapAnchor(building.Position.Y, footprint.HeightCells);
        return GridCoordinate(snappedX, snappedY, footprint);
    }

    private static (int X, int Y) GridCoordinate(
        float snappedX,
        float snappedY,
        PlacementGridFootprint footprint)
    {
        var originX = snappedX - footprint.WorldSize.X * 0.5f;
        var originY = snappedY - footprint.WorldSize.Y * 0.5f;
        return (
            (int)MathF.Round(originX / PlacementMath.GridSize),
            (int)MathF.Round(originY / PlacementMath.GridSize));
    }

    private static bool ReservationsConflict(Placement first, Placement second, float pairClearance)
    {
        for (var firstIndex = 0; firstIndex < first.Reservations.Count; firstIndex++)
        {
            var firstReservation = first.Reservations[firstIndex];
            if (PlacementMath.ViolatesClearance(firstReservation, second.Rect, pairClearance))
            {
                return true;
            }

            for (var secondIndex = 0; secondIndex < second.Reservations.Count; secondIndex++)
            {
                if (PlacementMath.ViolatesClearance(
                        firstReservation,
                        second.Reservations[secondIndex],
                        pairClearance))
                {
                    return true;
                }
            }
        }

        for (var secondIndex = 0; secondIndex < second.Reservations.Count; secondIndex++)
        {
            if (PlacementMath.ViolatesClearance(first.Rect, second.Reservations[secondIndex], pairClearance))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOutside(PlacementRect rect, MapSize worldSize)
    {
        return rect.X < 0
            || rect.Y < 0
            || rect.EndX > worldSize.Width
            || rect.EndY > worldSize.Height;
    }

    private static bool NearlyEqual(float first, float second)
    {
        return MathF.Abs(first - second) <= 0.001f;
    }

    private sealed record Placement(
        MapBuildingSeedSpec Building,
        PlacementRect Rect,
        IReadOnlyList<PlacementRect> Reservations,
        int ClearanceCells,
        int GridX,
        int GridY);
}
