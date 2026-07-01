static partial class Program
{
    static void RunOutcomeScenario()
    {
        // ---- Scenario 7: vision + outcome --------------------------------------------
        // One side's armed units destroy the other's victory-critical HQ. Asserts the
        // VisionSystem marks the enemy HQ visible to the attacker, and OutcomeSystem
        // flips to Victory from owner 1's perspective. Deterministic.
        const int OutcomeTicks = 3000;

        EntityWorld BuildOutcome()
        {
            var world = new EntityWorld(seed: 555) { WorldWidth = 3600, WorldHeight = 2400 };
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.AddSystem(new SeparationSystem());
            world.AddSystem(new OutcomeSystem(new OwnerId(1)));
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var attacker = UnitDesignCatalog.Spec("dog.infantry");
            for (var i = 0; i < 6; i++)
            {
                world.SpawnUnit(attacker, new OwnerId(1), new Vector2(1500, 1100 + (i * 40)));
            }

            // Player HQ (owner 1) and enemy HQ (owner 2), both victory-critical.
            var hqSpec = new EntitySpec
            {
                Id = "replay.hq",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("HQ", "hq.name", "hq.role", "HQ", IconGlyph.Building),
            };
            world.Spawn(hqSpec, new OwnerId(1), EntityTransform.At(new Vector2(700, 1200)), new EntityComponentState[]
            {
                new HealthComponentState(5000, 5000),
                new CollisionComponentState(40, 50, 8, true),
                new ObjectiveComponentState(IsVictoryCritical: true),
            });
            world.Spawn(hqSpec, new OwnerId(2), EntityTransform.At(new Vector2(1750, 1200)), new EntityComponentState[]
            {
                new HealthComponentState(600, 600),
                new CollisionComponentState(40, 50, 8, true),
                new ObjectiveComponentState(IsVictoryCritical: true),
            });

            return world;
        }

        AssertDeterministic("outcome", BuildOutcome, OutcomeTicks, 500);

        var oc = BuildOutcome();
        var ocClock = new SimClock();
        var enemyHqVisibleAtSomePoint = false;
        var enemyHq = new EntityId(8); // 6 dogs + player HQ(7) + enemy HQ(8)
        for (var tick = 1; tick <= OutcomeTicks; tick++)
        {
            oc.Step(tick, ocClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            oc.Events.Drain();
            if (oc.Visibility.IsVisible(new OwnerId(1), enemyHq))
            {
                enemyHqVisibleAtSomePoint = true;
            }

            if (oc.Outcome != GameOutcome.InProgress)
            {
                break;
            }
        }

        if (!enemyHqVisibleAtSomePoint)
        {
            Fail("vision: attacker never saw the enemy HQ");
        }

        if (oc.Outcome != GameOutcome.Victory)
        {
            Fail($"outcome: expected Victory for owner 1, got {oc.Outcome}");
        }

        Console.WriteLine($"OK [outcome]: enemy HQ became visible, owner 1 reached {oc.Outcome}.");
        Console.WriteLine("SimReplay PASSED.");
    }
}
