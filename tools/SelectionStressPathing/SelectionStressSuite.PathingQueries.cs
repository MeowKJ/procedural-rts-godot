using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static void RunPathingQueries()
    {
        var directPath = PathfindingMath.FindPath(32, 32, 352, 32, 512, 512, 64, []);
        if (!AdvancedPathingPolicy.PreferDirectLineBeforeAStar
            || !AdvancedPathingPolicy.UseAStarOnlyWhenDirectLineBlocked
            || !AdvancedPathingPolicy.SmoothCollinearWaypoints
            || !AdvancedPathingPolicy.PruneWaypointsByLineOfSight
            || !AdvancedPathingPolicy.TreatCombatAnchorsAsGlobalBlockers
            || !AdvancedPathingPolicy.RouteAroundDenseIdleUnitBlobs
            || !AdvancedPathingPolicy.UseSpatialGridLocalAvoidance)
        {
            throw new InvalidOperationException("advanced pathing policy should explicitly enable direct-first planning, smoothed A* fallback, local avoidance, and dynamic blocker handling");
        }

        if (AdvancedPathingPolicy.OrderedStages.Length != 8
            || AdvancedPathingPolicy.OrderedStages[0] != "direct-line-first"
            || AdvancedPathingPolicy.OrderedStages[^1] != "repath-throttling")
        {
            throw new InvalidOperationException("advanced pathing policy should preserve the intended planning stage order");
        }

        if (AdvancedPathingPolicy.LineOfSightProbeCellFraction <= 0
            || AdvancedPathingPolicy.LineOfSightProbeCellFraction > 0.5f
            || AdvancedPathingPolicy.StuckRepathAfterSeconds <= 0
            || AdvancedPathingPolicy.RepathCooldownSeconds <= AdvancedPathingPolicy.StuckRepathAfterSeconds
            || AdvancedPathingPolicy.RepathProgressEpsilon <= 0)
        {
            throw new InvalidOperationException("advanced pathing policy should define conservative LOS sampling and throttled repath timing");
        }

        if (directPath.Count == 0 || MathF.Abs(directPath[^1].X - 352) > 0.001f || MathF.Abs(directPath[^1].Y - 32) > 0.001f)
        {
            throw new InvalidOperationException("pathfinding should reach direct destination");
        }

        if (directPath.Count != 1)
        {
            throw new InvalidOperationException("clear pathfinding should prefer one direct goal waypoint instead of grid stepping");
        }

        var directQuality = PathQualityMath.Measure(32, 32, directPath);
        AssertClose(directQuality.TravelInflation, 1, "direct path travel inflation");
        AssertClose(directQuality.Straightness, 1, "direct path straightness");
        if (directQuality.CornerCount != 0)
        {
            throw new InvalidOperationException("direct path quality should report zero corners");
        }

        var wallPath = PathfindingMath.FindPath(
            32,
            160,
            416,
            160,
            512,
            512,
            64,
            [
                new GridObstacle(2, 0),
                new GridObstacle(2, 1),
                new GridObstacle(2, 2),
                new GridObstacle(2, 3),
                new GridObstacle(2, 4),
            ]);

        var wallPathDebug = PathfindingMath.FindPathWithDebug(
            32,
            160,
            416,
            160,
            512,
            512,
            64,
            [
                new GridObstacle(2, 0),
                new GridObstacle(2, 1),
                new GridObstacle(2, 2),
                new GridObstacle(2, 3),
                new GridObstacle(2, 4),
            ],
            MovementDomain.Land,
            []);

        if (wallPath.Count < 3)
        {
            throw new InvalidOperationException("pathfinding should create multiple waypoints around a wall");
        }

        if (wallPathDebug.RawCells.Count <= wallPathDebug.Path.Count)
        {
            throw new InvalidOperationException("A* fallback should preserve raw debug cells while exposing a smoothed corridor to movement");
        }

        if (wallPath.Count > 5)
        {
            throw new InvalidOperationException("pathfinding should prune unnecessary grid-corner waypoints around a wall");
        }

        if (wallPath.Any(point => MathF.Floor(point.X / 64) == 2 && MathF.Floor(point.Y / 64) is >= 0 and <= 4))
        {
            throw new InvalidOperationException("pathfinding should avoid blocked grid cells");
        }

        var wallQuality = PathQualityMath.Measure(32, 160, wallPath);
        if (wallQuality.CornerCount > 4 || wallQuality.TravelInflation > 1.95f || wallQuality.Straightness < 0.51f)
        {
            throw new InvalidOperationException("smoothed wall path quality should limit corners and travel inflation");
        }

        var clearancePath = PathfindingMath.FindPath(
            32,
            160,
            480,
            160,
            512,
            384,
            64,
            [
                new GridObstacle(3, 1),
                new GridObstacle(3, 2),
                new GridObstacle(3, 3),
                new GridObstacle(4, 1),
                new GridObstacle(4, 2),
                new GridObstacle(4, 3),
            ]);

        if (!clearancePath.Any(point => point.Y >= 288))
        {
            throw new InvalidOperationException("clearance-aware pathfinding should prefer the wider corridor instead of hugging a squeezed building edge");
        }

        var compactness = PathQualityMath.FinalCompactness([
            new PathPoint(500, 500),
            new PathPoint(532, 500),
            new PathPoint(500, 532),
            new PathPoint(532, 532),
        ]);
        if (compactness > 46)
        {
            throw new InvalidOperationException("path quality compactness metric should report tight final formations");
        }

        var jitter = PathQualityMath.JitterAfterArrival([
            new PathPoint(648, 500),
            new PathPoint(649.2f, 500.4f),
            new PathPoint(647.7f, 499.6f),
        ]);
        if (jitter > 1.6f)
        {
            throw new InvalidOperationException("path quality jitter metric should capture small post-arrival drift");
        }

        var waterBarrier = new[]
        {
            new GridTerrain(2, 0, TerrainLayer.Water),
            new GridTerrain(2, 1, TerrainLayer.Water),
            new GridTerrain(2, 2, TerrainLayer.Water),
            new GridTerrain(2, 3, TerrainLayer.Water),
            new GridTerrain(2, 4, TerrainLayer.Water),
        };
        var landAroundWater = PathfindingMath.FindPath(
            32,
            160,
            416,
            160,
            512,
            512,
            64,
            [],
            MovementDomain.Land,
            waterBarrier);
        if (landAroundWater.Any(point => MathF.Floor(point.X / 64) == 2 && MathF.Floor(point.Y / 64) is >= 0 and <= 4))
        {
            throw new InvalidOperationException("land pathfinding should avoid water terrain cells");
        }

        var waterLane = new[]
        {
            new GridTerrain(0, 2, TerrainLayer.Water),
            new GridTerrain(1, 2, TerrainLayer.Water),
            new GridTerrain(2, 2, TerrainLayer.Water),
            new GridTerrain(3, 2, TerrainLayer.Water),
            new GridTerrain(4, 2, TerrainLayer.Water),
            new GridTerrain(5, 2, TerrainLayer.Water),
            new GridTerrain(6, 2, TerrainLayer.Water),
        };
        var navalPath = PathfindingMath.FindPath(
            32,
            160,
            416,
            160,
            512,
            512,
            64,
            [],
            MovementDomain.Naval,
            waterLane);
        if (navalPath.Count == 0 || MathF.Abs(navalPath[^1].X - 416) > 0.001f || MathF.Abs(navalPath[^1].Y - 160) > 0.001f)
        {
            throw new InvalidOperationException("naval pathfinding should traverse water terrain cells");
        }

        var airPath = PathfindingMath.FindPath(
            32,
            160,
            416,
            160,
            512,
            512,
            64,
            [
                new GridObstacle(1, 2),
                new GridObstacle(2, 2),
                new GridObstacle(3, 2),
                new GridObstacle(4, 2),
            ],
            MovementDomain.Air,
            []);
        if (airPath.Count == 0 || MathF.Abs(airPath[^1].X - 416) > 0.001f || MathF.Abs(airPath[^1].Y - 160) > 0.001f)
        {
            throw new InvalidOperationException("air pathfinding should ignore building blockers and reach the destination");
        }
    }
}
