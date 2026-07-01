static partial class Program
{
    static void RunAuthoredContentScenario()
    {
        // ---- Scenario 3: authored content (dog vs cat) -------------------------------
        // Proves the full authoring -> EntitySpec -> components -> systems -> projection
        // loop with REAL game designs. A new unit being "just a spec" means no system
        // edits: we spawn authored UnitDesigns via the bridge and the generic systems
        // drive them. Determinism must hold and combat must resolve.
        const int AuthoredTicks = 5000;

        EntityWorld BuildAuthored()
        {
            var world = new EntityWorld(seed: 7);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var dog = UnitDesignCatalog.Spec("dog.infantry");
            var cat = UnitDesignCatalog.Spec("cat.basic");

            for (var i = 0; i < 8; i++)
            {
                world.SpawnUnit(dog, new OwnerId(1), new Vector2(1500, 800 + (i * 50)));
                world.SpawnUnit(cat, new OwnerId(2), new Vector2(1800, 800 + (i * 50)));
            }

            return world;
        }

        AssertDeterministic("authored", BuildAuthored, AuthoredTicks, 500);

        // Projection sanity: the read-model must reflect the live world.
        var authored = BuildAuthored();
        var authoredClock = new SimClock();
        var authoredDeaths = 0;
        for (var tick = 1; tick <= AuthoredTicks; tick++)
        {
            authored.Step(tick, authoredClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            foreach (var evt in authored.Events.Drain())
            {
                if (evt is EntityDestroyedEvent)
                {
                    authoredDeaths++;
                }
            }
        }

        var projections = EntityProjector.Project(authored);
        if (projections.Count != authored.Count)
        {
            Fail($"projection count {projections.Count} != world count {authored.Count}");
        }

        if (authoredDeaths == 0)
        {
            Fail("authored dog-vs-cat produced no deaths");
        }

        Console.WriteLine($"OK [authored]: dog.infantry vs cat.basic, {authoredDeaths} deaths, {projections.Count} projected survivors.");

    }
}
