static partial class Program
{
    static void RunCombatScenario()
    {
        // ---- Scenario 2: combat ------------------------------------------------------
        // Two hostile teams of armed units spawned facing each other. Aggressive stance
        // drives auto-acquire, chase to weapon range, fire on cooldown, seeded damage,
        // and deaths. Owner 1 vs owner 2.
        const int PerTeam = 12;
        const int CombatTicks = 4000;

        EntityWorld BuildCombat()
        {
            var world = new EntityWorld(seed: 4242);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            for (var i = 0; i < PerTeam; i++)
            {
                // Spawn within sight range (500px < 700) so aggressive units engage.
                SpawnSoldier(world, spec, new OwnerId(1), new Vector2(1400, 700 + (i * 60)));
                SpawnSoldier(world, spec, new OwnerId(2), new Vector2(1900, 700 + (i * 60)));
            }

            return world;
        }

        AssertDeterministic("combat", BuildCombat, CombatTicks, 400);

        // Confirm combat actually resolved: by the end at least some units died.
        var combatWorld = BuildCombat();
        var combatClock = new SimClock();
        var startCount = combatWorld.Count;
        var totalDeaths = 0;
        for (var tick = 1; tick <= CombatTicks; tick++)
        {
            combatWorld.Step(tick, combatClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            var drained = combatWorld.Events.Drain();
            combatWorld.Metrics.Consume(drained);
            foreach (var evt in drained)
            {
                if (evt is EntityDestroyedEvent)
                {
                    totalDeaths++;
                }
            }
        }

        if (totalDeaths == 0)
        {
            Fail("combat scenario produced no deaths; combat system inert");
        }

        // Metrics derived purely from the event stream must agree with observed events.
        var metrics = combatWorld.Metrics;
        if (metrics.Kills != totalDeaths)
        {
            Fail($"metrics kills {metrics.Kills} != observed deaths {totalDeaths}");
        }

        if (metrics.ShotsFired <= 0 || metrics.TimeToFirstShotTick <= 0)
        {
            Fail($"metrics implausible: shots={metrics.ShotsFired}, firstShotTick={metrics.TimeToFirstShotTick}");
        }

        Console.WriteLine($"OK [combat outcome]: {startCount} units -> {combatWorld.Count} survivors, {totalDeaths} deaths.");
        Console.WriteLine($"OK [combat metrics]: shots={metrics.ShotsFired}, kills={metrics.Kills}, firstShot@tick {metrics.TimeToFirstShotTick}, totalDamage={metrics.TotalDamage:0}.");
    }
}
