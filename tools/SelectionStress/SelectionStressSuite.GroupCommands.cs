using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static void RunGroupCommandScenarios()
    {
        var compactNearbyFormation = FormationMath.CreateMoveDestinations(
            [
                new FormationUnit(1, 100, 100, 21),
                new FormationUnit(2, 160, 100, 21),
                new FormationUnit(3, 100, 160, 13),
            ],
            500,
            500,
            1000,
            1000);
        if (compactNearbyFormation.Select(destination => $"{destination.X:0.0},{destination.Y:0.0}").Distinct().Count() != 3
            || compactNearbyFormation.Any(destination => Distance(destination.X, destination.Y, 500, 500) > 76))
        {
            throw new InvalidOperationException("nearby formation move should assign distinct compact slots around the clicked target");
        }

        var compactFormation = FormationMath.CreateMoveDestinations(
            [
                new FormationUnit(1, 0, 0, 21),
                new FormationUnit(2, 900, 0, 21),
                new FormationUnit(3, 0, 900, 21),
                new FormationUnit(4, 900, 900, 21),
            ],
            36,
            36,
            1000,
            1000);

        if (compactFormation.Any(destination => destination.X < 80 || destination.Y < 80))
        {
            throw new InvalidOperationException("formation destinations should clamp away from world edges");
        }

        var avoidanceBodies = new[]
        {
            new LocalAvoidanceBody(1, 500, 500, 21, AnchorPriority: 0, CanBeDisplaced: true),
            new LocalAvoidanceBody(2, 532, 500, 21, AnchorPriority: 0, CanBeDisplaced: true),
            new LocalAvoidanceBody(3, 900, 900, 21, AnchorPriority: 0, CanBeDisplaced: true),
            new LocalAvoidanceBody(4, 508, 502, 21, AnchorPriority: 2, CanBeDisplaced: false),
        };
        var avoidanceGrid = AvoidanceGrid(avoidanceBodies);
        var nearAvoidance = LocalAvoidanceMath.ResolveVector(avoidanceBodies[0], avoidanceGrid);
        if (nearAvoidance.X >= 0 || MathF.Abs(nearAvoidance.Y) > 0.35f)
        {
            throw new InvalidOperationException("spatial-grid avoidance should push a moving unit away from nearby units only");
        }

        var normalAvoidanceGrid = AvoidanceGrid(
            [
                new LocalAvoidanceBody(1, 500, 500, 21, AnchorPriority: 0, CanBeDisplaced: true),
                new LocalAvoidanceBody(2, 508, 502, 21, AnchorPriority: 0, CanBeDisplaced: true),
            ]);
        var normalAvoidance = LocalAvoidanceMath.ResolveVector(
            new LocalAvoidanceBody(1, 500, 500, 21, AnchorPriority: 0, CanBeDisplaced: true),
            normalAvoidanceGrid);
        if (nearAvoidance.X >= normalAvoidance.X)
        {
            throw new InvalidOperationException("spatial-grid avoidance should bias away from anchored combat units more strongly than normal units");
        }

        var bucketBoundaryBodies = new[]
        {
            new LocalAvoidanceBody(1, 95, 96, 21, AnchorPriority: 0, CanBeDisplaced: true),
            new LocalAvoidanceBody(2, 101, 96, 21, AnchorPriority: 0, CanBeDisplaced: true),
        };
        var bucketBoundaryAvoidance = LocalAvoidanceMath.ResolveVector(bucketBoundaryBodies[0], AvoidanceGrid(bucketBoundaryBodies));
        if (bucketBoundaryAvoidance.X >= 0)
        {
            throw new InvalidOperationException("spatial-grid avoidance should query neighboring buckets across cell boundaries");
        }

        var limitedAvoidance = LocalAvoidanceMath.ResolveVector(
            new LocalAvoidanceBody(1, 500, 500, 21, AnchorPriority: 0, CanBeDisplaced: true),
            AvoidanceGrid(
                [
                    new LocalAvoidanceBody(1, 500, 500, 21, AnchorPriority: 0, CanBeDisplaced: true),
                    new LocalAvoidanceBody(2, 500, 500, 21, AnchorPriority: 2, CanBeDisplaced: false),
                    new LocalAvoidanceBody(3, 500, 500, 21, AnchorPriority: 2, CanBeDisplaced: false),
                ]));
        if (MathF.Sqrt(limitedAvoidance.X * limitedAvoidance.X + limitedAvoidance.Y * limitedAvoidance.Y) > 0.721f)
        {
            throw new InvalidOperationException("spatial-grid avoidance should stay softly limited instead of acting like physics pushing");
        }

        var distantAvoidance = LocalAvoidanceMath.ResolveVector(avoidanceBodies[2], avoidanceGrid);
        AssertClose(distantAvoidance.X, 0, "distant local avoidance x");
        AssertClose(distantAvoidance.Y, 0, "distant local avoidance y");

        var holdingAvoidance = LocalAvoidanceMath.ResolveVector(avoidanceBodies[3], avoidanceGrid);
        AssertClose(holdingAvoidance.X, 0, "combat anchor local avoidance x");
        AssertClose(holdingAvoidance.Y, 0, "combat anchor local avoidance y");
    }

    private static float Distance(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static SpatialGrid<LocalAvoidanceBody> AvoidanceGrid(IReadOnlyList<LocalAvoidanceBody> bodies)
    {
        var grid = new SpatialGrid<LocalAvoidanceBody>(96);
        foreach (var body in bodies)
        {
            grid.Add(body.X, body.Y, body);
        }

        return grid;
    }
}
