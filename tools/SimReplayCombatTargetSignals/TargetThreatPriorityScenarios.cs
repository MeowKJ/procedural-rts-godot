static partial class Program
{
    static void RunTargetThreatPriorityScenario()
    {
        const int TargetThreatPriorityTicks = 24;

        EntityWorld BuildTargetThreatPriority()
        {
            var world = new EntityWorld(seed: 6565);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), ThreatPriorityAttacker());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(90, 0)), ThreatPriorityBait());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(170, 0)), ThreatPriorityThreat(new EntityId(1), facing: MathF.PI));

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 300)), ThreatPriorityAttacker());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(90, 300)), ThreatPriorityBait());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(170, 300)), ThreatPriorityThreat(new EntityId(4), facing: MathF.PI));

            return world;
        }

        static EntityComponentState[] ThreatPriorityAttacker()
        {
            return
            [
                new HealthComponentState(10000, 10000),
                new VisionComponentState(500),
                new StanceComponentState(UnitStance.Hold),
                new AutonomyComponentState(AcquireRange: 500, LeashRange: 500),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, 0, 0),
                }),
            ];
        }

        static EntityComponentState[] ThreatPriorityBait()
        {
            return
            [
                new HealthComponentState(10000, 10000),
            ];
        }

        static EntityComponentState[] ThreatPriorityThreat(EntityId attackTarget, float facing)
        {
            return
            [
                new HealthComponentState(10000, 10000),
                new VisionComponentState(500),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, facing, 0),
                }, attackTarget, CombatTargetKind.Unit, AttackTargetIsManual: false),
            ];
        }

        var targetThreatPriorityLog = new List<EntityCommand>
        {
            new AttackEntityCommand(new OwnerId(1), [new EntityId(4)], 1, new EntityId(5), CombatTargetKind.Unit),
        };

        // The second enemy is automatically attacking this unit; threat weighting must not override manual focus-fire.
        AssertDeterministic("target-threat-priority", BuildTargetThreatPriority, targetThreatPriorityLog, TargetThreatPriorityTicks, 8);

        var targetThreatPriority = BuildTargetThreatPriority();
        var targetThreatClock = new SimClock();
        var targetThreatBuffer = new EntityCommandBuffer();
        foreach (var command in targetThreatPriorityLog)
        {
            targetThreatBuffer.Enqueue(command);
        }

        for (var tick = 1; tick <= TargetThreatPriorityTicks; tick++)
        {
            targetThreatPriority.Step(tick, targetThreatClock.FixedDelta, targetThreatBuffer.DrainUpToTick(tick));
            targetThreatPriority.Events.Drain();
        }

        var autoThreatAttacker = targetThreatPriority.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var autoCloserBait = targetThreatPriority.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var autoThreat = targetThreatPriority.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var manualThreatAttacker = targetThreatPriority.OrderedEntities.Single(entity => entity.Id.Value == 4);
        var manualBait = targetThreatPriority.OrderedEntities.Single(entity => entity.Id.Value == 5);
        var manualThreatSource = targetThreatPriority.OrderedEntities.Single(entity => entity.Id.Value == 6);

        var autoThreatWeapon = autoThreatAttacker.Components.Require<WeaponUserComponentState>();
        var manualThreatWeapon = manualThreatAttacker.Components.Require<WeaponUserComponentState>();

        Assert(targetThreatPriority.Visibility.IsVisible(new OwnerId(1), new EntityId(2)), "closer non-threat target should be visible to owner 1");
        Assert(targetThreatPriority.Visibility.IsVisible(new OwnerId(1), new EntityId(3)), "farther threat source should be visible to owner 1");
        Assert(autoThreatWeapon.AttackTarget.Value == 3, $"auto-acquire should prefer visible threat target 3 over closer non-threat 2, got {autoThreatWeapon.AttackTarget.Value}");
        Assert(!autoThreatWeapon.AttackTargetIsManual, "threat-weighted auto-acquire should remain non-manual");
        Assert(autoThreat.Components.Require<HealthComponentState>().Hp < 10000, "threat source should take automatic damage");
        Assert(MathF.Abs(autoCloserBait.Components.Require<HealthComponentState>().Hp - 10000) <= 0.001f, "closer non-threat should not take automatic damage");
        Assert(manualThreatWeapon.AttackTarget.Value == 5 && manualThreatWeapon.AttackTargetIsManual, "manual attack should keep the specified non-threat target despite visible threat weighting");
        Assert(manualBait.Components.Require<HealthComponentState>().Hp < 10000, "manual non-threat target should keep taking damage");
        Assert(MathF.Abs(manualThreatSource.Components.Require<HealthComponentState>().Hp - 10000) <= 0.001f, "manual focus should not be reordered onto the threat source");
        Console.WriteLine("OK [target-threat-priority]: auto-acquire preferred the visible attacker threat, while manual focus stayed fixed.");
    }
}
