using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static void RunEconomyCommandScenarios()
    {
        AssertClose(BeamMath.Fade(0, 0.2f), 1, "beam fade at birth");
        AssertClose(BeamMath.Fade(0.2f, 0.2f), 0, "beam fade at death");

        var midPulse = BeamMath.Pulse(0.1f, 0.2f);
        if (midPulse < 0.99f)
        {
            throw new InvalidOperationException($"beam pulse should peak near half life, got {midPulse}");
        }

        var snappedPlacement = PlacementMath.Validate(117, 145, 96, 96, 1000, 1000, []);
        AssertClose(snappedPlacement.X, 128, "placement snap x");
        AssertClose(snappedPlacement.Y, 160, "placement snap y");
        if (!snappedPlacement.IsValid)
        {
            throw new InvalidOperationException("snapped placement should be valid");
        }

        var blockedPlacement = PlacementMath.Validate(
            128,
            160,
            96,
            96,
            1000,
            1000,
            [new PlacementObstacle(80, 112, 112, 112)]);
        if (blockedPlacement.IsValid)
        {
            throw new InvalidOperationException("placement should be blocked by obstacle");
        }

        var outsidePlacement = PlacementMath.Validate(8, 8, 180, 120, 1000, 1000, []);
        if (outsidePlacement.IsValid)
        {
            throw new InvalidOperationException("placement outside world should be invalid");
        }

        var partialProduction = ProductionMath.Advance(1.5f, 2.0f, 5.0f);
        AssertClose(partialProduction.Progress, 3.5f, "partial production progress");
        if (partialProduction.IsComplete)
        {
            throw new InvalidOperationException("partial production should not complete");
        }

        var completedProduction = ProductionMath.Advance(4.5f, 1.0f, 5.0f);
        AssertClose(completedProduction.Progress, 5.0f, "completed production clamps progress");
        if (!completedProduction.IsComplete)
        {
            throw new InvalidOperationException("production should complete at duration");
        }

        var clearSpawn = ProductionSpawnMath.FindSpawnPoint(500, 500, 0, 160, 120, 21, 1000, 1000, []);
        if (clearSpawn.X <= 500)
        {
            throw new InvalidOperationException("spawn should prefer producer facing direction");
        }

        var blockedForwardSpawn = ProductionSpawnMath.FindSpawnPoint(
            500,
            500,
            0,
            160,
            120,
            21,
            1000,
            1000,
            [new SpawnObstacle(clearSpawn.X, clearSpawn.Y, 40)]);
        if (MathF.Abs(blockedForwardSpawn.X - clearSpawn.X) < 0.001f && MathF.Abs(blockedForwardSpawn.Y - clearSpawn.Y) < 0.001f)
        {
            throw new InvalidOperationException("spawn should avoid occupied forward point");
        }
    }
}
