static partial class Program
{
    static void RunTargetVisibilityScenario()
    {
        const int TargetVisibilityTicks = 120;

        EntityWorld BuildTargetVisibility()
        {
            var world = new EntityWorld(seed: 6464);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var spec = CombatSpec();
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), VisibilityArmed(UnitStance.Hold, sightRange: 80, acquireRange: 500));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(140, 0)), VisibilityTarget());

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 300)), VisibilityArmed(UnitStance.Hold, sightRange: 120, acquireRange: 500));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(170, 300)), VisibilityTarget());
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(90, 300)), VisibilityTarget());

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 600)), VisibilityArmed(UnitStance.Hold, sightRange: 80, acquireRange: 500));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(140, 600)), VisibilityTarget());

            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(0, 900)), VisibilityArmed(UnitStance.PassiveRetaliate, sightRange: 80, acquireRange: 500));
            world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(140, 900)), VisibilityArmed(UnitStance.Ignore, sightRange: 190, acquireRange: 0, facing: MathF.PI));

            return world;
        }

        static EntityComponentState[] VisibilityArmed(
            UnitStance stance,
            float sightRange,
            float acquireRange,
            float facing = 0)
        {
            return
            [
                new HealthComponentState(1000, 1000),
                new VisionComponentState(sightRange),
                new StanceComponentState(stance),
                new AutonomyComponentState(acquireRange, acquireRange),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, facing, 0),
                }),
            ];
        }

        static EntityComponentState[] VisibilityTarget()
        {
            return
            [
                new HealthComponentState(1000, 1000),
            ];
        }

        var targetVisibilityLog = new List<EntityCommand>
        {
            new AttackEntityCommand(new OwnerId(1), [new EntityId(6)], 1, new EntityId(7), CombatTargetKind.Unit),
            new AttackEntityCommand(new OwnerId(2), [new EntityId(9)], 1, new EntityId(8), CombatTargetKind.Unit),
        };

        AssertDeterministic("target-visibility", BuildTargetVisibility, targetVisibilityLog, TargetVisibilityTicks, 20);

        var targetVisibility = BuildTargetVisibility();
        var targetVisibilityClock = new SimClock();
        var targetVisibilityBuffer = new EntityCommandBuffer();
        foreach (var command in targetVisibilityLog)
        {
            targetVisibilityBuffer.Enqueue(command);
        }

        for (var tick = 1; tick <= TargetVisibilityTicks; tick++)
        {
            targetVisibility.Step(tick, targetVisibilityClock.FixedDelta, targetVisibilityBuffer.DrainUpToTick(tick));
            targetVisibility.Events.Drain();
        }

        var autoHiddenOnly = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var hiddenOnlyTarget = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var autoChoice = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var hiddenChoiceTarget = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 4);
        var visibleChoiceTarget = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 5);
        var manualHiddenAttacker = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 6);
        var manualHiddenTarget = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 7);
        var passiveHiddenVictim = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 8);
        var hiddenThreat = targetVisibility.OrderedEntities.Single(entity => entity.Id.Value == 9);

        var autoHiddenWeapon = autoHiddenOnly.Components.Require<WeaponUserComponentState>();
        var autoChoiceWeapon = autoChoice.Components.Require<WeaponUserComponentState>();
        var manualHiddenWeapon = manualHiddenAttacker.Components.Require<WeaponUserComponentState>();
        var passiveHiddenWeapon = passiveHiddenVictim.Components.Require<WeaponUserComponentState>();

        Assert(!targetVisibility.Visibility.IsVisible(new OwnerId(1), new EntityId(2)), "hidden-only target should be outside owner 1 gameplay visibility");
        Assert(!autoHiddenWeapon.AttackTarget.IsValid, "auto-acquire should not lock a hidden-only hostile");
        Assert(MathF.Abs(hiddenOnlyTarget.Components.Require<HealthComponentState>().Hp - 1000) <= 0.001f, "hidden-only target should not take auto-acquire damage");
        Assert(autoChoiceWeapon.AttackTarget.Value == 5, $"auto-acquire should choose the visible hostile target 5, got {autoChoiceWeapon.AttackTarget.Value}");
        Assert(hiddenChoiceTarget.Components.Require<HealthComponentState>().Hp >= 999.9f, "hidden choice target should not be damaged by visible-only auto-acquire");
        Assert(visibleChoiceTarget.Components.Require<HealthComponentState>().Hp < 1000, "visible choice target should take auto-acquire damage");
        Assert(manualHiddenWeapon.AttackTarget.Value == 7 && manualHiddenWeapon.AttackTargetIsManual, "manual attack should keep an explicitly assigned hidden target");
        Assert(manualHiddenTarget.Components.Require<HealthComponentState>().Hp < 1000, "manual hidden target should still take damage");
        Assert(!passiveHiddenWeapon.AttackTarget.IsValid, "PassiveRetaliate should not lock a hidden attacker");
        Assert(!passiveHiddenVictim.Components.Has<RetaliationComponentState>(), "PassiveRetaliate should not record an invisible threat as a target");
        Assert(hiddenThreat.Components.Require<HealthComponentState>().Hp >= 999.9f, "hidden attacker should not be counter-damaged by invisible PassiveRetaliate");
        Console.WriteLine("OK [target-visibility]: auto acquire and PassiveRetaliate require current visibility; manual focus still works.");
    }
}
