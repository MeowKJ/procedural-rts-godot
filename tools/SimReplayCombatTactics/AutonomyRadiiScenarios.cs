static partial class Program
{
    static void RunAutonomyRadiiScenario()
    {
        const int AutonomyRadiiTicks = 260;

        EntityWorld BuildAutonomyRadii()
        {
            var world = new EntityWorld(seed: 6262);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            SpawnAutonomyUnit(world, spec, new Vector2(0, 200), UnitStance.Hold, acquireRange: 500, leashRange: 220);
            SpawnAutonomyTarget(world, spec, new Vector2(160, 200));

            SpawnAutonomyUnit(world, spec, new Vector2(0, 600), UnitStance.Aggressive, acquireRange: 500, leashRange: 500);
            SpawnAutonomyTarget(world, spec, new Vector2(350, 600));

            SpawnAutonomyUnit(world, spec, new Vector2(0, 1000), UnitStance.ReturnGuard, acquireRange: 500, leashRange: 260);
            SpawnAutonomyTarget(world, spec, new Vector2(220, 1000), new Vector2(520, 1000), speed: 160);

            SpawnAutonomyUnit(world, spec, new Vector2(0, 1400), UnitStance.Ignore, acquireRange: 500, leashRange: 500);
            SpawnAutonomyTarget(world, spec, new Vector2(100, 1400));

            return world;
        }

        static void SpawnAutonomyUnit(
            EntityWorld world,
            EntitySpec spec,
            Vector2 position,
            UnitStance stance,
            float acquireRange,
            float leashRange)
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(position), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 120, ArriveRadius: 2),
                new VisionComponentState(900),
                new StanceComponentState(stance, position),
                new AutonomyComponentState(acquireRange, leashRange, position),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, 0, 0),
                }),
            });
        }

        static void SpawnAutonomyTarget(
            EntityWorld world,
            EntitySpec spec,
            Vector2 position,
            Vector2? moveTarget = null,
            float speed = 0)
        {
            var components = new List<EntityComponentState>
            {
                new HealthComponentState(10000, 10000),
                new CollisionComponentState(Radius: 12, Mass: 1, PushPriority: 1, BlocksMovement: true),
            };

            if (moveTarget is { } target)
            {
                components.Add(new MovementComponentState(Vector2.Zero, target));
                components.Add(new MovementProfileComponentState(MaxSpeed: speed, ArriveRadius: 1));
            }

            world.Spawn(spec, new OwnerId(2), EntityTransform.At(position), components);
        }

        AssertDeterministic("autonomy-radii", BuildAutonomyRadii, AutonomyRadiiTicks, 20);

        var autonomyWorld = BuildAutonomyRadii();
        var autonomyClock = new SimClock();
        for (var tick = 1; tick <= AutonomyRadiiTicks; tick++)
        {
            autonomyWorld.Step(tick, autonomyClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            autonomyWorld.Events.Drain();
        }

        var hold = autonomyWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var aggressive = autonomyWorld.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var returnGuard = autonomyWorld.OrderedEntities.Single(entity => entity.Id.Value == 5);
        var ignore = autonomyWorld.OrderedEntities.Single(entity => entity.Id.Value == 7);

        Assert(hold.Transform.Position.DistanceTo(new Vector2(0, 200)) <= 0.01f, $"Hold should not leave anchor, got {hold.Transform.Position}");
        Assert(hold.Components.Require<WeaponUserComponentState>().AttackTarget.Value == 2, "Hold should still acquire a hostile inside weapon range");
        Assert(aggressive.Transform.Position.X > 120, $"Aggressive should chase toward acquired target, got {aggressive.Transform.Position}");
        Assert(aggressive.Components.Require<WeaponUserComponentState>().AttackTarget.Value == 4, "Aggressive should auto-acquire inside explicit acquire range");
        Assert(returnGuard.Components.Require<WeaponUserComponentState>().AttackTarget.IsValid == false, "ReturnGuard should drop auto target once it exceeds leash");
        Assert(returnGuard.Transform.Position.DistanceTo(new Vector2(0, 1000)) <= 10f, $"ReturnGuard should return to anchor after leash break, got {returnGuard.Transform.Position}");
        Assert(ignore.Components.Require<WeaponUserComponentState>().AttackTarget.IsValid == false, "Ignore should not auto-acquire");
        Assert(ignore.Transform.Position.DistanceTo(new Vector2(0, 1400)) <= 0.01f, $"Ignore should remain idle, got {ignore.Transform.Position}");
        Console.WriteLine("OK [autonomy-radii]: Hold anchored, Aggressive chased, ReturnGuard leashed home, Ignore stayed passive.");
    }
}
