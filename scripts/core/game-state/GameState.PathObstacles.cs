namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private readonly record struct DynamicBlobObstacleBounds(int Count, float MinX, float MinY, float MaxX, float MaxY)
    {
        public DynamicBlobObstacleBounds Add(UnitModel unit)
        {
            var radius = unit.RuntimeDescriptor.Radius;
            var minX = unit.Position.X - radius;
            var minY = unit.Position.Y - radius;
            var maxX = unit.Position.X + radius;
            var maxY = unit.Position.Y + radius;
            if (Count == 0)
            {
                return new DynamicBlobObstacleBounds(1, minX, minY, maxX, maxY);
            }

            return new DynamicBlobObstacleBounds(
                Count + 1,
                MathF.Min(MinX, minX),
                MathF.Min(MinY, minY),
                MathF.Max(MaxX, maxX),
                MathF.Max(MaxY, maxY));
        }

        public PlacementObstacle ToObstacle(float padding)
        {
            return new PlacementObstacle(
                MinX - padding,
                MinY - padding,
                MaxX - MinX + padding * 2,
                MaxY - MinY + padding * 2);
        }
    }

    private void CollectBuildingObstacles(List<PlacementObstacle> result)
    {
        result.Clear();
        AppendBuildingObstacles(result);
    }

    private IReadOnlyList<GridObstacle> PathObstacles(
        MovementDomain domain,
        int? movingUnitId = null,
        IReadOnlySet<int>? movingUnitIds = null)
    {
        if (TerrainPassability.IgnoresBuildingBlockers(domain))
        {
            return Array.Empty<GridObstacle>();
        }

        CollectPathPlacementObstacles(movingUnitId, movingUnitIds, _legacyPlacementObstacles);
        _legacyPathObstacles.Clear();
        _legacyPathObstacleSet.Clear();
        foreach (var obstacle in _legacyPlacementObstacles)
        {
            AppendGridCellsForObstacle(obstacle, PathCellSize, _legacyPathObstacles, _legacyPathObstacleSet);
        }

        return _legacyPathObstacles;
    }

    private static bool IsMovingPathSubject(UnitModel unit, int? movingUnitId, IReadOnlySet<int>? movingUnitIds)
    {
        return unit.Id == movingUnitId || (movingUnitIds is not null && movingUnitIds.Contains(unit.Id));
    }

    private void CollectPathPlacementObstacles(
        int? movingUnitId,
        IReadOnlySet<int>? movingUnitIds,
        List<PlacementObstacle> result)
    {
        result.Clear();
        result.AddRange(_mapObstacles);
        AppendBuildingObstacles(result);
        AppendCombatAnchorObstacles(result, movingUnitId, movingUnitIds);
        AppendDenseUnitBlobObstacles(result, movingUnitId, movingUnitIds);
    }

    private void AppendBuildingObstacles(List<PlacementObstacle> result)
    {
        foreach (var building in Buildings)
        {
            if (building.Hp <= 0)
            {
                continue;
            }

            var spec = BuildSpecCatalog.For(building.Kind);
            var rect = PlacementMath.RectFromCenter(
                building.Position.X,
                building.Position.Y,
                spec.Footprint.X + 24,
                spec.Footprint.Y + 24);
            result.Add(new PlacementObstacle(rect.X, rect.Y, rect.Width, rect.Height));
        }
    }

    private void AppendCombatAnchorObstacles(
        List<PlacementObstacle> result,
        int? movingUnitId,
        IReadOnlySet<int>? movingUnitIds = null)
    {
        foreach (var unit in Units)
        {
            if (unit.Hp <= 0
                || IsMovingPathSubject(unit, movingUnitId, movingUnitIds)
                || unit.MovementState != UnitMovementState.CombatAnchor)
            {
                continue;
            }

            var radius = unit.RuntimeDescriptor.Radius + 18;
            result.Add(new PlacementObstacle(
                unit.Position.X - radius,
                unit.Position.Y - radius,
                radius * 2,
                radius * 2));
        }
    }

    private void AppendDenseUnitBlobObstacles(
        List<PlacementObstacle> result,
        int? movingUnitId,
        IReadOnlySet<int>? movingUnitIds = null)
    {
        _legacyDenseBlobObstacles.Clear();
        foreach (var unit in Units)
        {
            if (unit.Hp <= 0
                || IsMovingPathSubject(unit, movingUnitId, movingUnitIds)
                || unit.Selected
                || unit.MoveTarget is not null
                || unit.MovementState is not (UnitMovementState.Idle or UnitMovementState.HoldingSlot))
            {
                continue;
            }

            var cell = LocalAvoidanceMath.Cell(unit.Position.X, unit.Position.Y, DynamicBlobCellSize);
            _legacyDenseBlobObstacles.TryGetValue(cell, out var bounds);
            _legacyDenseBlobObstacles[cell] = bounds.Add(unit);
        }

        foreach (var bounds in _legacyDenseBlobObstacles.Values)
        {
            if (bounds.Count >= DynamicBlobMinimumUnits)
            {
                result.Add(bounds.ToObstacle(DynamicBlobObstaclePadding));
            }
        }
    }

    private IReadOnlyList<GridTerrain> TerrainCells()
    {
        return Array.Empty<GridTerrain>();
    }

    private static void AppendGridCellsForObstacle(
        PlacementObstacle obstacle,
        float cellSize,
        List<GridObstacle> result,
        HashSet<GridObstacle> seen)
    {
        var minX = (int)MathF.Floor(obstacle.X / cellSize);
        var minY = (int)MathF.Floor(obstacle.Y / cellSize);
        var maxX = (int)MathF.Floor((obstacle.X + obstacle.Width) / cellSize);
        var maxY = (int)MathF.Floor((obstacle.Y + obstacle.Height) / cellSize);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var cell = new GridObstacle(x, y);
                if (seen.Add(cell))
                {
                    result.Add(cell);
                }
            }
        }
    }
}
