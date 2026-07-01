static partial class Program
{
    static void RunTargetStickinessScenario()
    {
        // Auto-acquire should not flicker between nearly equivalent targets. The first
        // hostile starts slightly nearer, then drifts behind a second hostile; target
        // stickiness should keep the current valid target instead of re-picking nearest.
        const int TargetStickinessTicks = 220;

        EntityWorld BuildTargetStickiness()
        {
            var world = new EntityWorld(seed: 6161);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 120),
                new VisionComponentState(500),
                new StanceComponentState(UnitStance.Hold),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }),
            });
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(120, 0)), new EntityComponentState[]
            {
                new HealthComponentState(10000, 10000),
                new MovementComponentState(Vector2.Zero, new Vector2(180, 0)),
                new MovementProfileComponentState(MaxSpeed: 24, ArriveRadius: 1),
            });
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(145, 0)), new EntityComponentState[]
            {
                new HealthComponentState(10000, 10000),
            });

            return world;
        }

        AssertDeterministic("target-stickiness", BuildTargetStickiness, TargetStickinessTicks, 20);

        var sticky = BuildTargetStickiness();
        var stickyClock = new SimClock();
        for (var tick = 1; tick <= TargetStickinessTicks; tick++)
        {
            sticky.Step(tick, stickyClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            sticky.Metrics.Consume(sticky.Events.Drain());
        }

        var stickyAttacker = sticky.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var stickyWeapon = stickyAttacker.Components.Require<WeaponUserComponentState>();
        Assert(stickyWeapon.AttackTarget.Value == 2, $"no-target-flicker should keep first valid target 2, got {stickyWeapon.AttackTarget.Value}");
        Assert(!stickyWeapon.AttackTargetIsManual, "auto-acquired sticky target should remain non-manual");
        Assert(sticky.Metrics.TargetSwitchCount == 0, $"target-switch-count should remain 0 for sticky auto-acquire, got {sticky.Metrics.TargetSwitchCount}");
        Console.WriteLine($"OK [no-target-flicker]: auto target stayed on {stickyWeapon.AttackTarget.Value}; switches={sticky.Metrics.TargetSwitchCount}.");
    }
}
