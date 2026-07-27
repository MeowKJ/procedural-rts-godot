static partial class Program
{
    static void RunTargetReacquireCooldownScenario()
    {
        const int TargetReacquireCooldownTicks = 18;

        EntityWorld BuildTargetReacquireCooldown()
        {
            var world = new EntityWorld(seed: 6666);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), ReacquireAttacker());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(90, 0)), ReacquireTarget(hp: 2));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(130, 0)), ReacquireTarget(hp: 10000));

            return world;
        }

        static EntityComponentState[] ReacquireAttacker()
        {
            return
            [
                new HealthComponentState(1000, 1000),
                new VisionComponentState(500),
                new StanceComponentState(UnitStance.Hold),
                new AutonomyComponentState(AcquireRange: 500, LeashRange: 500),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, 0, 0),
                }),
            ];
        }

        static EntityComponentState[] ReacquireTarget(float hp)
        {
            return
            [
                new HealthComponentState(hp, hp),
            ];
        }

        AssertDeterministic("target-reacquire-cooldown", BuildTargetReacquireCooldown, TargetReacquireCooldownTicks, 3);

        var reacquire = BuildTargetReacquireCooldown();
        var reacquireClock = new SimClock();
        var reacquiredTick = -1;
        var targetRemovedTick = -1;
        var cooldownBlockedTicks = 0;
        var sawProjectileBeforeRemoval = false;
        for (var tick = 1; tick <= TargetReacquireCooldownTicks; tick++)
        {
            reacquire.Step(tick, reacquireClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            reacquire.Events.Drain();

            var cooldownAttacker = reacquire.OrderedEntities.Single(entity => entity.Id.Value == 1);
            var weapon = cooldownAttacker.Components.Require<WeaponUserComponentState>();
            if (tick == 1)
            {
                Assert(weapon.AttackTarget.Value == 2, "auto-acquire should first lock target 2");
                Assert(reacquire.TryGet(new EntityId(2), out _), "target 2 should survive the fire tick until projectile impact");
            }

            sawProjectileBeforeRemoval |= targetRemovedTick < 0
                && reacquire.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>());
            if (targetRemovedTick < 0 && !reacquire.TryGet(new EntityId(2), out _))
            {
                targetRemovedTick = tick;
            }

            if (targetRemovedTick >= 0
                && !weapon.AttackTarget.IsValid
                && weapon.AutoReacquireCooldownRemaining > 0)
            {
                cooldownBlockedTicks++;
            }

            if (weapon.AttackTarget.Value == 3 && reacquiredTick < 0)
            {
                reacquiredTick = tick;
            }
        }

        Assert(sawProjectileBeforeRemoval, "first auto shot should exist as a projectile before target 2 is removed");
        Assert(targetRemovedTick > 1, $"target 2 should be removed on impact after the fire tick, removed at {targetRemovedTick}");
        Assert(cooldownBlockedTicks >= 6, $"auto re-acquire should be blocked for several post-impact ticks, got {cooldownBlockedTicks}");
        Assert(reacquiredTick >= targetRemovedTick + 7, $"backup target should be reacquired only after post-impact cooldown, removed {targetRemovedTick}, reacquired {reacquiredTick}");
        Console.WriteLine($"OK [target-reacquire-cooldown]: blocked {cooldownBlockedTicks} ticks, reacquired target 3 at tick {reacquiredTick}.");

        var manualBypassLog = new List<EntityCommand>
        {
            new AttackEntityCommand(new OwnerId(1), [new EntityId(1)], 6, new EntityId(3), CombatTargetKind.Unit),
        };
        AssertDeterministic("manual-attack-reacquire-bypass", BuildTargetReacquireCooldown, manualBypassLog, TargetReacquireCooldownTicks, 3);

        var manualBypass = BuildTargetReacquireCooldown();
        var manualBypassClock = new SimClock();
        var manualBypassBuffer = new EntityCommandBuffer();
        foreach (var command in manualBypassLog)
        {
            manualBypassBuffer.Enqueue(command);
        }

        for (var tick = 1; tick <= 6; tick++)
        {
            manualBypass.Step(tick, manualBypassClock.FixedDelta, manualBypassBuffer.DrainUpToTick(tick));
            manualBypass.Events.Drain();
        }

        var manualBypassWeapon = manualBypass.OrderedEntities.Single(entity => entity.Id.Value == 1)
            .Components.Require<WeaponUserComponentState>();
        Assert(manualBypassWeapon.AttackTarget.Value == 3, $"manual attack should bypass auto re-acquire cooldown and lock target 3, got {manualBypassWeapon.AttackTarget.Value}");
        Assert(manualBypassWeapon.AttackTargetIsManual, "manual attack during auto cooldown should remain a manual focus target");
        Assert(MathF.Abs(manualBypassWeapon.AutoReacquireCooldownRemaining) <= 0.001f, $"manual attack should clear auto re-acquire cooldown, got {manualBypassWeapon.AutoReacquireCooldownRemaining:0.000}");
        Console.WriteLine("OK [manual-attack-reacquire-bypass]: manual focus locked during auto cooldown and cleared the cooldown state.");
    }
}
