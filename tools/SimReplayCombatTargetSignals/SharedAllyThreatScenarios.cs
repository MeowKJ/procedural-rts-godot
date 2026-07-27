static partial class Program
{
    static void RunSharedAllyThreatScenario()
    {
        const int SharedAllyThreatTicks = 24;

        EntityWorld BuildSharedAllyThreat()
        {
            var world = new EntityWorld(seed: 7272);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
            world.Relations.Set(new OwnerId(1), new OwnerId(3), PlayerRelation.Allied);
            world.Relations.Set(new OwnerId(2), new OwnerId(3), PlayerRelation.Hostile);

            var spec = CombatSpec();

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), SharedThreatResponder(UnitStance.Hold));
            world.Spawn(spec, new OwnerId(3), EntityTransform.At(new Vector2(40, 0)), SharedThreatAlly());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(90, 0)), SharedThreatBait());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(170, 0)), SharedThreatSource(new EntityId(2)));

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 300)), SharedThreatResponder(UnitStance.Hold));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(90, 300)), SharedThreatBait());
            world.Spawn(spec, new OwnerId(3), EntityTransform.At(new Vector2(40, 300)), SharedThreatAlly());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(170, 300)), SharedThreatSource(new EntityId(7)));

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 600)), SharedThreatResponder(UnitStance.Ignore));
            world.Spawn(spec, new OwnerId(3), EntityTransform.At(new Vector2(40, 600)), SharedThreatAlly());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(90, 600)), SharedThreatBait());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(170, 600)), SharedThreatSource(new EntityId(10)));

            return world;
        }

        static EntityComponentState[] SharedThreatResponder(UnitStance stance)
        {
            return
            [
                new HealthComponentState(10000, 10000),
                new VisionComponentState(500),
                new StanceComponentState(stance),
                new AutonomyComponentState(AcquireRange: 500, LeashRange: 500),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, 0, 0),
                }),
            ];
        }

        static EntityComponentState[] SharedThreatAlly()
        {
            return
            [
                new HealthComponentState(10000, 10000),
                new VisionComponentState(500),
            ];
        }

        static EntityComponentState[] SharedThreatBait()
        {
            return
            [
                new HealthComponentState(10000, 10000),
            ];
        }

        static EntityComponentState[] SharedThreatSource(EntityId attackedAlly)
        {
            return
            [
                new HealthComponentState(10000, 10000),
                new VisionComponentState(500),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, MathF.PI, 0),
                }, attackedAlly, CombatTargetKind.Unit, AttackTargetIsManual: false),
            ];
        }

        var sharedAllyThreatLog = new List<EntityCommand>
        {
            new AttackEntityCommand(new OwnerId(1), [new EntityId(5)], 1, new EntityId(6), CombatTargetKind.Unit),
        };

        AssertDeterministic("shared-ally-threat", BuildSharedAllyThreat, sharedAllyThreatLog, SharedAllyThreatTicks, 8);

        var sharedThreatWorld = BuildSharedAllyThreat();
        var sharedThreatClock = new SimClock();
        var sharedThreatBuffer = new EntityCommandBuffer();
        foreach (var command in sharedAllyThreatLog)
        {
            sharedThreatBuffer.Enqueue(command);
        }

        for (var tick = 1; tick <= SharedAllyThreatTicks; tick++)
        {
            sharedThreatWorld.Step(tick, sharedThreatClock.FixedDelta, sharedThreatBuffer.DrainUpToTick(tick));
            sharedThreatWorld.Events.Drain();
        }

        var responder = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var closerBait = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var sharedThreat = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 4);
        var manualResponder = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 5);
        var manualBait = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 6);
        var manualSharedThreat = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 8);
        var ignoreResponder = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 9);
        var ignoreSharedThreat = sharedThreatWorld.OrderedEntities.Single(entity => entity.Id.Value == 12);

        var responderWeapon = responder.Components.Require<WeaponUserComponentState>();
        var manualWeapon = manualResponder.Components.Require<WeaponUserComponentState>();
        var ignoreWeapon = ignoreResponder.Components.Require<WeaponUserComponentState>();

        Assert(responderWeapon.AttackTarget.Value == 4, $"shared ally threat should beat closer bait 3, got {responderWeapon.AttackTarget.Value}");
        Assert(sharedThreat.Components.Require<HealthComponentState>().Hp < 10000, "shared ally threat source should take automatic damage");
        Assert(MathF.Abs(closerBait.Components.Require<HealthComponentState>().Hp - 10000) <= 0.001f, "closer non-threat bait should not take automatic damage");
        Assert(manualWeapon.AttackTarget.Value == 6 && manualWeapon.AttackTargetIsManual, "manual focus should not be stolen by shared ally threat");
        Assert(manualBait.Components.Require<HealthComponentState>().Hp < 10000, "manual target should take damage");
        Assert(MathF.Abs(manualSharedThreat.Components.Require<HealthComponentState>().Hp - 10000) <= 0.001f, "manual focus should leave shared threat untouched");
        Assert(!ignoreWeapon.AttackTarget.IsValid, "Ignore stance should not answer shared ally threat calls");
        Assert(MathF.Abs(ignoreSharedThreat.Components.Require<HealthComponentState>().Hp - 10000) <= 0.001f, "Ignore responder should not damage shared threat");
        Console.WriteLine("OK [shared-ally-threat]: shared ally threats are prioritized without stealing manual focus or Ignore stance.");
    }
}
