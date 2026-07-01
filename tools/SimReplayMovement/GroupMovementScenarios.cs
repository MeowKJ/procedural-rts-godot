static partial class Program
{
    static void RunGroupMoveScenario()
    {
        // ---- Scenario 4: 30-unit group move (no clumping) ----------------------------
        // Marquee 99-point criterion: a box-selected group attacking/moving must arrive
        // compact but NOT stacked. We move 30 units to one point and assert every pair of
        // final positions keeps a sane minimum separation.
        const int GroupTicks = 1200;

        EntityWorld BuildGroupMove() => BuildGroup(GroupSize, seed: 11).World;

        var groupMoveLog = new List<EntityCommand>
        {
            new GroupMoveEntityCommand(new OwnerId(1), GroupIds, 1, GroupTarget, MoveCommandMode.Direct),
        };

        AssertDeterministic("group-move", BuildGroupMove, groupMoveLog, GroupTicks, 200);

        // Metric: minimum pairwise separation at rest must stay above unit diameter.
        var gm = BuildGroupMove();
        var gmClock = new SimClock();
        var gmBuffer = new EntityCommandBuffer();
        var gmTransitMin = float.MaxValue;
        foreach (var c in groupMoveLog)
        {
            gmBuffer.Enqueue(c);
        }

        for (var tick = 1; tick <= GroupTicks; tick++)
        {
            gm.Step(tick, gmClock.FixedDelta, gmBuffer.DrainUpToTick(tick));
            if (tick is >= 60 and <= 400) // sample during convergence
            {
                var live = EntityProjector.Project(gm);
                gm.Metrics.RecordCompactnessSample(live.Select(p => p.Position).ToArray());
                for (var i = 0; i < live.Count; i++)
                {
                    for (var j = i + 1; j < live.Count; j++)
                    {
                        gmTransitMin = MathF.Min(gmTransitMin, live[i].Position.DistanceTo(live[j].Position));
                    }
                }
            }
        }

        var gmFinal = EntityProjector.Project(gm);
        gm.Metrics.RecordCompactnessSample(gmFinal.Select(p => p.Position).ToArray());
        var minSep = float.MaxValue;
        for (var i = 0; i < gmFinal.Count; i++)
        {
            for (var j = i + 1; j < gmFinal.Count; j++)
            {
                minSep = MathF.Min(minSep, gmFinal[i].Position.DistanceTo(gmFinal[j].Position));
            }
        }

        if (minSep < 24f)
        {
            Fail($"group-move clumped: min pairwise separation {minSep:0.0} < 24");
        }

        // Avoidance must prevent units from fully overlapping mid-travel too.
        if (gmTransitMin < 12f)
        {
            Fail($"group-move overlapped in transit: min separation {gmTransitMin:0.0} < 12");
        }

        var groupMoveMetrics = gm.Metrics;
        if (groupMoveMetrics.PathInflationRatio is < 1 or > 1.85)
        {
            Fail($"group-move path inflation out of band: {groupMoveMetrics.PathInflationRatio:0.00}");
        }

        if (groupMoveMetrics.MovementCornerCount > 2000)
        {
            Fail($"group-move corner count too high: {groupMoveMetrics.MovementCornerCount}");
        }

        if (groupMoveMetrics.ArrivalSamples < GroupSize || groupMoveMetrics.AverageArrivalJitterDistance > 0.01)
        {
            Fail($"group-move arrival jitter bad: samples {groupMoveMetrics.ArrivalSamples}, avg jitter {groupMoveMetrics.AverageArrivalJitterDistance:0.000}");
        }

        if (groupMoveMetrics.MovementStuckSeconds > 1.0 || groupMoveMetrics.MovementRepathCount != 0)
        {
            Fail($"group-move stuck/repath bad: stuck {groupMoveMetrics.MovementStuckSeconds:0.00}s, repaths {groupMoveMetrics.MovementRepathCount}");
        }

        if (groupMoveMetrics.CompactnessSamples <= 0 || groupMoveMetrics.AverageCompactnessRadius > 650)
        {
            Fail($"group-move compactness bad: samples {groupMoveMetrics.CompactnessSamples}, avg radius {groupMoveMetrics.AverageCompactnessRadius:0.0}");
        }

        Console.WriteLine($"OK [group-move metric]: {GroupSize} units, rest min {minSep:0.0}px, transit min {gmTransitMin:0.0}px (no clumping).");
        Console.WriteLine($"OK [command-feel metrics]: path inflation {groupMoveMetrics.PathInflationRatio:0.00}, corners {groupMoveMetrics.MovementCornerCount}, arrivals {groupMoveMetrics.ArrivalSamples}, compactness {groupMoveMetrics.AverageCompactnessRadius:0.0}px.");

        // Direct same-point move should degrade gracefully too: this covers repeated
        // non-group move orders or command sources that have not been slot-decomposed.
        const int SamePointTicks = 1200;

        EntityWorld BuildSamePointMove() => BuildGroup(GroupSize, seed: 37).World;

        var samePointLog = new List<EntityCommand>
        {
            new MoveEntityCommand(new OwnerId(1), GroupIds, 1, GroupTarget, MoveCommandMode.Direct),
        };

        AssertDeterministic("same-point-move", BuildSamePointMove, samePointLog, SamePointTicks, 200);

        var sp = BuildSamePointMove();
        var spClock = new SimClock();
        var spBuffer = new EntityCommandBuffer();
        foreach (var c in samePointLog)
        {
            spBuffer.Enqueue(c);
        }

        List<Vector2>? spLatePositions = null;
        for (var tick = 1; tick <= SamePointTicks; tick++)
        {
            sp.Step(tick, spClock.FixedDelta, spBuffer.DrainUpToTick(tick));
            if (tick == SamePointTicks - 120)
            {
                spLatePositions = EntityProjector.Project(sp)
                    .Where(p => p.Kind == EntityKind.Unit && p.Owner.Value == 1)
                    .OrderBy(p => p.Id.Value)
                    .Select(p => p.Position)
                    .ToList();
            }
        }

        var spFinal = EntityProjector.Project(sp)
            .Where(p => p.Kind == EntityKind.Unit && p.Owner.Value == 1)
            .OrderBy(p => p.Id.Value)
            .ToList();
        var spMinSep = float.MaxValue;
        var spMaxRadius = 0f;
        var spAvgRadius = 0f;
        var spMaxLateDrift = 0f;
        for (var i = 0; i < spFinal.Count; i++)
        {
            var radius = spFinal[i].Position.DistanceTo(GroupTarget);
            spMaxRadius = MathF.Max(spMaxRadius, radius);
            spAvgRadius += radius;
            if (spLatePositions is not null)
            {
                spMaxLateDrift = MathF.Max(spMaxLateDrift, spFinal[i].Position.DistanceTo(spLatePositions[i]));
            }

            for (var j = i + 1; j < spFinal.Count; j++)
            {
                spMinSep = MathF.Min(spMinSep, spFinal[i].Position.DistanceTo(spFinal[j].Position));
            }
        }

        spAvgRadius /= Math.Max(1, spFinal.Count);
        var spActiveMovers = sp.OrderedEntities.Count(entity =>
            entity.OwnerId.Value == 1
            && entity.Components.TryGet<MovementComponentState>(out var movement)
            && movement.MoveTarget is not null);

        Assert(spActiveMovers == 0, $"same-point move should settle all orders, active movers {spActiveMovers}");
        Assert(spMinSep >= 24f, $"same-point move clumped after arrival: min separation {spMinSep:0.0} < 24");
        Assert(spAvgRadius <= 115f && spMaxRadius <= 180f, $"same-point move too loose: avg radius {spAvgRadius:0.0}, max {spMaxRadius:0.0}");
        Assert(spMaxLateDrift <= 0.5f, $"same-point move kept jittering late: max drift {spMaxLateDrift:0.00}px");
        Console.WriteLine($"OK [same-point-move metric]: {GroupSize} units settled around one target, min {spMinSep:0.0}px, avg radius {spAvgRadius:0.0}px, late drift {spMaxLateDrift:0.00}px.");
    }
}
