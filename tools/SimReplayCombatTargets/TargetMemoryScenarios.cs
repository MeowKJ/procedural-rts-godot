static partial class Program
{
    static void RunLastKnownTargetMemoryScenario()
    {
        const int LastKnownTargetTicks = 96;

        EntityWorld BuildLastKnownTargetMemory()
        {
            var world = new EntityWorld(seed: 6767);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), LastKnownAttacker(WeaponIds.ElectromagneticEmitter, sightRange: 135));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(80, 0)), LastKnownRunner(new Vector2(360, 0)));

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 300)), LastKnownAttacker(WeaponIds.NeedleRifle, sightRange: 135));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(80, 300)), LastKnownRunner(new Vector2(360, 300)));

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 600)), LastKnownAttacker(WeaponIds.RocketPod, sightRange: 135));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(80, 600)), LastKnownRunner(new Vector2(360, 600)));

            return world;
        }

        static EntityComponentState[] LastKnownAttacker(string weaponId, float sightRange)
        {
            return
            [
                new HealthComponentState(10000, 10000),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 120, ArriveRadius: 2),
                new VisionComponentState(sightRange),
                new StanceComponentState(UnitStance.Aggressive),
                new AutonomyComponentState(AcquireRange: 500, LeashRange: 500),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", weaponId, 0, 0),
                }),
            ];
        }

        static EntityComponentState[] LastKnownRunner(Vector2 moveTarget)
        {
            return
            [
                new HealthComponentState(10000, 10000),
                new MovementComponentState(Vector2.Zero, moveTarget),
                new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 1),
                new CollisionComponentState(Radius: 12, Mass: 1, PushPriority: 1, BlocksMovement: true),
            ];
        }

        AssertDeterministic("last-known-target-memory", BuildLastKnownTargetMemory, LastKnownTargetTicks, 12);

        var lastKnown = BuildLastKnownTargetMemory();
        var lastKnownClock = new SimClock();
        var shortLostTick = -1;
        var rangedLostTick = -1;
        var rocketLostTick = -1;
        Vector2? shortLostMoveTarget = null;
        Vector2? rangedLostMoveTarget = null;
        Vector2? rocketLostMoveTarget = null;
        Vector2 shortPositionAtLoss = Vector2.Zero;
        Vector2? shortMemoryAtLoss = null;
        float shortMemoryRemainingAtLoss = 0;
        float rangedMemoryRemainingAtLoss = 0;
        float rocketMemoryRemainingAtLoss = 0;

        for (var tick = 1; tick <= LastKnownTargetTicks; tick++)
        {
            lastKnown.Step(tick, lastKnownClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            lastKnown.Events.Drain();

            var shortAttacker = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 1);
            var shortTarget = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 2);
            var rangedAttacker = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 3);
            var rangedTarget = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 4);
            var rocketAttacker = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 5);
            var rocketTarget = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 6);

            if (shortLostTick < 0 && !lastKnown.Visibility.IsVisible(new OwnerId(1), shortTarget.Id))
            {
                var weapon = shortAttacker.Components.Require<WeaponUserComponentState>();
                var movement = shortAttacker.Components.Require<MovementComponentState>();
                shortLostTick = tick;
                shortLostMoveTarget = movement.MoveTarget;
                shortPositionAtLoss = shortAttacker.Transform.Position;
                shortMemoryAtLoss = weapon.LastKnownTargetPosition;
                shortMemoryRemainingAtLoss = weapon.LastKnownTargetRemaining;
                Assert(!weapon.AttackTarget.IsValid, "last-known short-range chase should clear fire authority when target enters fog");
            }

            if (rangedLostTick < 0 && !lastKnown.Visibility.IsVisible(new OwnerId(1), rangedTarget.Id))
            {
                var weapon = rangedAttacker.Components.Require<WeaponUserComponentState>();
                var movement = rangedAttacker.Components.Require<MovementComponentState>();
                rangedLostTick = tick;
                rangedLostMoveTarget = movement.MoveTarget;
                rangedMemoryRemainingAtLoss = weapon.LastKnownTargetRemaining;
                Assert(!weapon.AttackTarget.IsValid, "last-known ranged hold should clear fire authority when target enters fog");
            }

            if (rocketLostTick < 0 && !lastKnown.Visibility.IsVisible(new OwnerId(1), rocketTarget.Id))
            {
                var weapon = rocketAttacker.Components.Require<WeaponUserComponentState>();
                var movement = rocketAttacker.Components.Require<MovementComponentState>();
                rocketLostTick = tick;
                rocketLostMoveTarget = movement.MoveTarget;
                rocketMemoryRemainingAtLoss = weapon.LastKnownTargetRemaining;
                Assert(!weapon.AttackTarget.IsValid, "last-known missile rule should clear fire authority when target enters fog");
            }
        }

        var finalShort = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var finalRanged = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var finalRocket = lastKnown.OrderedEntities.Single(entity => entity.Id.Value == 5);
        var finalShortWeapon = finalShort.Components.Require<WeaponUserComponentState>();

        Assert(shortLostTick > 0, "last-known short-range target should enter fog during replay");
        Assert(rangedLostTick > 0, "last-known ranged target should enter fog during replay");
        Assert(rocketLostTick > 0, "last-known missile target should enter fog during replay");
        if (shortLostMoveTarget is not { } shortMove)
        {
            Fail("short-range unit should chase the decaying last-known target point");
            return;
        }

        if (shortMemoryAtLoss is not { } shortMemory)
        {
            Fail("short-range unit should retain a last-known target position");
            return;
        }

        Assert(shortMove.DistanceTo(shortMemory) <= 0.01f, $"short-range chase target {shortMove} should match last-known point {shortMemory}");
        Assert(shortMemoryRemainingAtLoss > 0, "short-range last-known memory should still be decaying when the target enters fog");
        Assert(finalShort.Transform.Position.X > shortPositionAtLoss.X + 20, $"short-range unit should move toward last-known point, loss={shortPositionAtLoss}, final={finalShort.Transform.Position}");
        Assert(rangedLostMoveTarget is null, $"ranged unit should hold instead of blind-chasing into fog, got {rangedLostMoveTarget}");
        Assert(rangedMemoryRemainingAtLoss > 0, "ranged unit should still retain decaying last-known memory for later reacquire logic");
        Assert(finalRanged.Transform.Position.DistanceTo(new Vector2(0, 300)) <= 0.01f, $"ranged unit should hold position after losing target, got {finalRanged.Transform.Position}");
        Assert(rocketLostMoveTarget is null, $"tracking missile unit should follow weapon projectile rule, not blind-chase, got {rocketLostMoveTarget}");
        Assert(rocketMemoryRemainingAtLoss > 0, "tracking missile unit should retain decaying last-known memory without forcing movement");
        Assert(finalRocket.Transform.Position.DistanceTo(new Vector2(0, 600)) <= 0.01f, $"tracking missile unit should hold position after losing target, got {finalRocket.Transform.Position}");
        Assert(finalShortWeapon.LastKnownTargetRemaining <= 0 && finalShortWeapon.LastKnownTargetPosition is null, "last-known target memory should decay and clear after its window");
        Console.WriteLine($"OK [last-known-target-memory]: short-range chased memory at tick {shortLostTick}; ranged held at tick {rangedLostTick}; tracking missile held at tick {rocketLostTick}.");
    }
}
