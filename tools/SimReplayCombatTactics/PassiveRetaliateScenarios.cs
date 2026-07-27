static partial class Program
{
    static void RunPassiveRetaliateScenario()
    {
        const int PassiveRetaliateTicks = 180;

        EntityWorld BuildPassiveRetaliate()
        {
            var world = new EntityWorld(seed: 6363);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), PassiveRetaliateArmed(spec, UnitStance.PassiveRetaliate, new Vector2(0, 0)));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(125, 0)), PassiveRetaliateTarget());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(150, 0)), PassiveRetaliateArmed(spec, UnitStance.Ignore, new Vector2(150, 0)));

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 400)), PassiveRetaliateArmed(spec, UnitStance.PassiveRetaliate, new Vector2(0, 400)));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(120, 400)), PassiveRetaliateTarget(hp: 10000));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(150, 400)), PassiveRetaliateArmed(spec, UnitStance.Ignore, new Vector2(150, 400)));

            return world;
        }

        static EntityComponentState[] PassiveRetaliateArmed(EntitySpec spec, UnitStance stance, Vector2 position, float hp = 1000)
        {
            return
            [
                new HealthComponentState(hp, hp),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: spec.Movement!.Speed, ArriveRadius: 2),
                new VisionComponentState(600),
                new StanceComponentState(stance, position),
                new AutonomyComponentState(AcquireRange: 500, LeashRange: 220, AnchorPosition: position),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, 0, 0),
                }),
            ];
        }

        static EntityComponentState[] PassiveRetaliateTarget(float hp = 1000)
        {
            return
            [
                new HealthComponentState(hp, hp),
                new CollisionComponentState(Radius: 12, Mass: 1, PushPriority: 1, BlocksMovement: true),
            ];
        }

        var passiveRetaliateLog = new List<EntityCommand>
        {
            new AttackEntityCommand(new OwnerId(1), [new EntityId(4)], 1, new EntityId(5), CombatTargetKind.Unit),
            new AttackEntityCommand(new OwnerId(2), [new EntityId(3)], 20, new EntityId(1), CombatTargetKind.Unit),
            new AttackEntityCommand(new OwnerId(2), [new EntityId(6)], 20, new EntityId(4), CombatTargetKind.Unit),
        };

        EntityWorld RunPassiveRetaliateWorld(int ticks)
        {
            var world = BuildPassiveRetaliate();
            var clock = new SimClock();
            var buffer = new EntityCommandBuffer();
            foreach (var command in passiveRetaliateLog)
            {
                buffer.Enqueue(command);
            }

            for (var tick = 1; tick <= ticks; tick++)
            {
                world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
                world.Events.Drain();
            }

            return world;
        }

        AssertDeterministic("passive-retaliate", BuildPassiveRetaliate, passiveRetaliateLog, PassiveRetaliateTicks, 20);

        var passiveEarly = RunPassiveRetaliateWorld(19);
        var earlyPassive = passiveEarly.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var earlyBait = passiveEarly.OrderedEntities.Single(entity => entity.Id.Value == 2);
        Assert(!earlyPassive.Components.Require<WeaponUserComponentState>().AttackTarget.IsValid, "PassiveRetaliate should not auto-acquire before being attacked");
        Assert(!earlyPassive.Components.Has<RetaliationComponentState>(), "PassiveRetaliate should not record a retaliation target before being attacked");
        Assert(MathF.Abs(earlyBait.Components.Require<HealthComponentState>().Hp - 1000) <= 0.001f, "PassiveRetaliate should not damage nearby bait before a threat exists");

        var passiveWorld = RunPassiveRetaliateWorld(PassiveRetaliateTicks);
        var passive = passiveWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var attacker = passiveWorld.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var manualPassive = passiveWorld.OrderedEntities.Single(entity => entity.Id.Value == 4);
        var manualFocus = passiveWorld.OrderedEntities.Single(entity => entity.Id.Value == 5);
        var manualThreat = passiveWorld.OrderedEntities.Single(entity => entity.Id.Value == 6);
        var passiveWeapon = passive.Components.Require<WeaponUserComponentState>();
        var retaliation = passive.Components.Require<RetaliationComponentState>();
        var manualWeapon = manualPassive.Components.Require<WeaponUserComponentState>();

        Assert(passiveWeapon.AttackTarget.Value == 3, $"PassiveRetaliate should target its attacker, got {passiveWeapon.AttackTarget.Value}");
        Assert(!passiveWeapon.AttackTargetIsManual, "PassiveRetaliate response should remain non-manual");
        Assert(retaliation.Target.Value == 3 && retaliation.LastThreatTick > 0, "Retaliation state should store the last attacker");
        Assert(attacker.Components.Require<HealthComponentState>().Hp < 1000, "PassiveRetaliate should damage the attacker after being attacked");
        Assert(manualWeapon.AttackTarget.Value == 5 && manualWeapon.AttackTargetIsManual, "manual command should keep priority over PassiveRetaliate");
        Assert(manualFocus.Components.Require<HealthComponentState>().Hp < 10000, "manual focus target should keep taking damage");
        Assert(MathF.Abs(manualThreat.Components.Require<HealthComponentState>().Hp - 1000) <= 0.001f, "manual focus should prevent retaliation from switching to the new threat");
        Console.WriteLine("OK [passive-retaliate]: passive stayed idle until hit, retaliated against its attacker, and preserved manual focus.");
    }
}
