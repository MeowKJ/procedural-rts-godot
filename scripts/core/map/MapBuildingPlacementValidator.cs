namespace ProceduralRts.Core;

public enum MapBuildingPlacementConflictKind
{
    Unsnapped,
    Rotation,
    Overlap,
    Clearance,
    Outside,
}

public sealed record MapBuildingPlacementConflict(
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
            return $"{subject} conflict={Conflict.ToString().ToLowerInvariant()}";
        }

        var otherGrid = MapBuildingPlacementValidator.GridCoordinate(Other);
        return $"{subject} conflict={Conflict.ToString().ToLowerInvariant()} other=[{Identity(Other, otherGrid.X, otherGrid.Y)}]";
    }

    private static string Identity(MapBuildingSeedSpec building, int gridX, int gridY)
    {
        return $"owner={building.OwnerId.Value} faction={building.Faction} kind={building.Kind} grid=({gridX},{gridY})";
    }
}

public static class MapBuildingPlacementValidator
{
    public const float DefaultClearance = PlacementMath.GridSize;

    public static IReadOnlyList<MapBuildingPlacementConflict> Validate(
        MapSpec map,
        float clearance = DefaultClearance)
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

            if (!isCardinal)
            {
                conflicts.Add(new MapBuildingPlacementConflict(
                    MapBuildingPlacementConflictKind.Rotation,
                    building,
                    grid.X,
                    grid.Y));
            }

            if (!NearlyEqual(building.Position.X, snappedX) || !NearlyEqual(building.Position.Y, snappedY))
            {
                conflicts.Add(new MapBuildingPlacementConflict(
                    MapBuildingPlacementConflictKind.Unsnapped,
                    building,
                    grid.X,
                    grid.Y));
            }

            if (rect.X < 0 || rect.Y < 0 || rect.EndX > map.WorldSize.Width || rect.EndY > map.WorldSize.Height)
            {
                conflicts.Add(new MapBuildingPlacementConflict(
                    MapBuildingPlacementConflictKind.Outside,
                    building,
                    grid.X,
                    grid.Y));
            }

            placements.Add(new Placement(building, rect, grid.X, grid.Y));
        }

        for (var firstIndex = 0; firstIndex < placements.Count; firstIndex++)
        {
            var first = placements[firstIndex];
            for (var secondIndex = firstIndex + 1; secondIndex < placements.Count; secondIndex++)
            {
                var second = placements[secondIndex];
                if (Intersects(first.Rect, second.Rect))
                {
                    conflicts.Add(new MapBuildingPlacementConflict(
                        MapBuildingPlacementConflictKind.Overlap,
                        first.Building,
                        first.GridX,
                        first.GridY,
                        second.Building));
                    continue;
                }

                if (clearance > 0
                    && Intersects(Inflate(first.Rect, clearance * 0.5f), Inflate(second.Rect, clearance * 0.5f)))
                {
                    conflicts.Add(new MapBuildingPlacementConflict(
                        MapBuildingPlacementConflictKind.Clearance,
                        first.Building,
                        first.GridX,
                        first.GridY,
                        second.Building));
                }
            }
        }

        return conflicts;
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

    private static PlacementRect Inflate(PlacementRect rect, float amount)
    {
        return new PlacementRect(
            rect.X - amount,
            rect.Y - amount,
            rect.Width + amount * 2,
            rect.Height + amount * 2);
    }

    private static bool Intersects(PlacementRect first, PlacementRect second)
    {
        return first.X < second.EndX
            && first.EndX > second.X
            && first.Y < second.EndY
            && first.EndY > second.Y;
    }

    private static bool NearlyEqual(float first, float second)
    {
        return MathF.Abs(first - second) <= 0.001f;
    }

    private sealed record Placement(
        MapBuildingSeedSpec Building,
        PlacementRect Rect,
        int GridX,
        int GridY);
}
