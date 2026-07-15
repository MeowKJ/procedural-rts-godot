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

        var barracks = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        if (!PlacementReservationMath.TryCenter(
                barracks,
                PlacementReservationKind.ProductionEgress,
                new Godot.Vector2(500, 500),
                0,
                out var clearSpawn)
            || MathF.Abs(clearSpawn.X - 596) > 0.001f
            || MathF.Abs(clearSpawn.Y - 500) > 0.001f)
        {
            throw new InvalidOperationException("spawn should use the exact shared production egress center");
        }

        if (!ProductionSpawnMath.IsSpawnPointAvailable(clearSpawn.X, clearSpawn.Y, 21, [])
            || ProductionSpawnMath.IsSpawnPointAvailable(
                clearSpawn.X,
                clearSpawn.Y,
                21,
                [new SpawnObstacle(clearSpawn.X, clearSpawn.Y, 40)]))
        {
            throw new InvalidOperationException("occupied production egress should wait without selecting a fallback");
        }

        AssertLegacyBlockedProductionRetriesAtFixedEgress();
    }

    private static void AssertLegacyBlockedProductionRetriesAtFixedEgress()
    {
        var state = new GameState();
        var producer = state.Buildings.First(building =>
            building.Owner == Owner.Player && building.Kind == BuildingDesignIds.Barracks);
        var producedSpec = ProductionKindDesignBridge.SpecFor(producer.FactionId, ProductionKind.InfantrySquad);
        var production = producedSpec.Production
            ?? throw new InvalidOperationException("legacy blocked-spawn fixture requires an infantry production spec");
        if (!PlacementReservationMath.TryCenter(
                BuildSpecCatalog.For(producer.Kind),
                PlacementReservationKind.ProductionEgress,
                producer.Position,
                producer.Facing,
                out var egress))
        {
            throw new InvalidOperationException("legacy blocked-spawn fixture requires a production egress");
        }

        producer.ProductionQueue.Clear();
        producer.ProductionQueue.Add(new ProductionQueueItem
        {
            Id = 549,
            Kind = ProductionKind.InfantrySquad,
            DesignId = producedSpec.Id,
            FactionId = producer.FactionId,
            Progress = production.Duration,
        });
        var blockerDescriptor = UnitDesignDefinitionCatalog.RuntimeDescriptors[producedSpec.Id];
        var blocker = new UnitModel
        {
            Id = state.Units.Max(unit => unit.Id) + 1000,
            DesignId = producedSpec.Id,
            Owner = Owner.Player,
            FactionId = producer.FactionId,
            Position = egress,
            AnchorPosition = egress,
            Hp = blockerDescriptor.MaxHp,
        };
        state.Units.Add(blocker);
        var completedBefore = state.CompletedProduction.Count;
        var unitCountWithBlocker = state.Units.Count;

        state.Update(0.05);
        if (producer.ProductionQueue.Count != 1
            || MathF.Abs(producer.ProductionQueue[0].Progress - production.Duration) > 0.001f
            || state.CompletedProduction.Count != completedBefore
            || state.Units.Count != unitCountWithBlocker)
        {
            throw new InvalidOperationException("legacy blocked egress should keep the completed queue item without spawning or dequeueing");
        }

        state.Units.Remove(blocker);
        state.Update(0.05);
        var produced = state.Units.SingleOrDefault(unit =>
            unit.Id != blocker.Id
            && unit.DesignId == producedSpec.Id
            && unit.Position.DistanceTo(egress) < 0.001f);
        if (producer.ProductionQueue.Count != 0
            || state.CompletedProduction.Count != completedBefore + 1
            || produced is null)
        {
            var nearest = state.Units
                .OrderBy(unit => unit.Position.DistanceSquaredTo(egress))
                .Take(3)
                .Select(unit => $"{unit.Id}:{unit.DesignId}@{unit.Position}/d={unit.Position.DistanceTo(egress):0.###}");
            throw new InvalidOperationException(
                $"legacy production should retry and spawn at the same fixed egress after the blocker clears; queue={producer.ProductionQueue.Count}, completed={state.CompletedProduction.Count - completedBefore}, nearest=[{string.Join(", ", nearest)}]");
        }
    }
}
