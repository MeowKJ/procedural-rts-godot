static partial class Program
{
    private static readonly Vector2 EntityAttackMoveTarget = new(980, 500);
    private static readonly Vector2 EntityAttackMoveManualTargetPosition = new(360, 500);

    private static void RunEntityIndependentTurretAttackMoveScenario()
    {
        RunEntityIndependentTurretAttackMoveContinuationScenario();
        RunManualAttackReplacesEntityAttackMoveIntentScenario();
    }

    private static void RunEntityIndependentTurretAttackMoveContinuationScenario()
    {
        IReadOnlyList<EntityCommand> attackMoveLog =
        [
            new AttackMoveEntityCommand(new OwnerId(1), [new EntityId(1)], 1, EntityAttackMoveTarget, MoveCommandMode.Attack),
        ];

        AssertDeterministic("entity-independent-turret-attack-move-continuation", BuildEntityAttackMoveWorld, attackMoveLog, 180, 30);

        var world = BuildEntityAttackMoveWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in attackMoveLog)
        {
            buffer.Enqueue(command);
        }

        var firedTick = -1;
        var removedTick = -1;
        var resumedTick = -1;
        var positionAtRemoval = EntityAttackMoveManualTargetPosition;
        for (var tick = 1; tick <= 180; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            foreach (var simEvent in world.Events.Drain())
            {
                if (simEvent is WeaponFiredEvent { Source.Value: 1 })
                {
                    firedTick = firedTick < 0 ? tick : firedTick;
                }
            }

            if (removedTick < 0 && !world.TryGet(new EntityId(2), out _))
            {
                removedTick = tick;
                positionAtRemoval = world.TryGet(new EntityId(1), out var remover)
                    ? remover.Transform.Position
                    : positionAtRemoval;
            }

            if (removedTick >= 0
                && resumedTick < 0
                && world.TryGet(new EntityId(1), out var resumed)
                && resumed.Components.Require<MovementComponentState>().MoveTarget is not null
                && resumed.Components.Require<CommandableComponentState>() is { MoveMode: MoveCommandMode.Attack, PlayerIntentTarget: { } intent }
                && intent.DistanceSquaredTo(EntityAttackMoveTarget) <= 1f)
            {
                resumedTick = tick;
            }
        }

        Assert(firedTick > 0, "independent turret attack-move should fire at the intercepted hostile");
        Assert(removedTick > 0, "independent turret attack-move should remove the intercepted hostile");
        Assert(resumedTick > removedTick, $"attack-move should resume after fire-anchor release; removedTick={removedTick}, resumedTick={resumedTick}");
        Assert(world.TryGet(new EntityId(1), out var attacker), "attack-move attacker should survive");

        var finalDistance = attacker.Transform.Position.DistanceTo(EntityAttackMoveTarget);
        var removalDistance = positionAtRemoval.DistanceTo(EntityAttackMoveTarget);
        var commandable = attacker.Components.Require<CommandableComponentState>();
        var weapon = attacker.Components.Require<WeaponUserComponentState>();
        Assert(
            finalDistance <= removalDistance - 48f,
            $"independent turret attack-move should continue toward original target after combat; removedTick={removedTick}, resumedTick={resumedTick}, removalDistance={removalDistance:0.0}, finalDistance={finalDistance:0.0}");
        Assert(commandable.MoveMode == MoveCommandMode.Attack, "attack-move intent should remain Attack after target cleanup");
        Assert(commandable.PlayerIntentTarget is { } intentTarget && intentTarget.DistanceSquaredTo(EntityAttackMoveTarget) <= 1f, "attack-move intent target should remain the original destination");
        Assert(!weapon.AttackTarget.IsValid && !weapon.AttackTargetIsManual, "attack-move auto target should clear after the intercepted hostile is gone");
        Console.WriteLine($"OK [entity independent turret attack-move]: fired@{firedTick}, removed@{removedTick}, resumed@{resumedTick}, progressed {removalDistance - finalDistance:0.0}px.");
    }

    private static void RunManualAttackReplacesEntityAttackMoveIntentScenario()
    {
        IReadOnlyList<EntityCommand> manualReplacementLog =
        [
            new AttackMoveEntityCommand(new OwnerId(1), [new EntityId(1)], 1, EntityAttackMoveTarget, MoveCommandMode.Attack),
            new AttackEntityCommand(new OwnerId(1), [new EntityId(1)], 1, new EntityId(2), CombatTargetKind.Unit),
        ];

        AssertDeterministic("manual-attack-replaces-entity-attack-move-intent", BuildEntityAttackMoveWorld, manualReplacementLog, 180, 30);

        var world = BuildEntityAttackMoveWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in manualReplacementLog)
        {
            buffer.Enqueue(command);
        }

        var startDistance = world.StableEntities.Single(entity => entity.Id.Value == 1).Transform.Position.DistanceTo(EntityAttackMoveTarget);
        for (var tick = 1; tick <= 180; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            world.Events.Drain();
        }

        Assert(world.TryGet(new EntityId(1), out var attacker), "manual replacement attacker should survive");
        var finalDistance = attacker.Transform.Position.DistanceTo(EntityAttackMoveTarget);
        var movement = attacker.Components.Require<MovementComponentState>();
        var commandable = attacker.Components.Require<CommandableComponentState>();
        Assert(
            finalDistance >= startDistance - 8f,
            $"manual attack should replace, not resume, the prior attack-move route; startDistance={startDistance:0.0}, finalDistance={finalDistance:0.0}, move={movement.MoveTarget}");
        Assert(movement.MoveTarget is null, "manual attack should not leave a stale attack-move target after target removal");
        Assert(commandable.MoveMode == MoveCommandMode.Direct, "manual attack should replace attack-move command mode");
        Assert(commandable.PlayerIntentTarget is { } intent && intent.DistanceSquaredTo(EntityAttackMoveManualTargetPosition) <= 1f, "manual attack visual intent should point at the attacked target");
        Console.WriteLine($"OK [manual attack replaces attack-move]: held after manual target removal at {attacker.Transform.Position}.");
    }

    private static EntityWorld BuildEntityAttackMoveWorld()
    {
        var world = new EntityWorld(seed: 303);
        world.AddSystem(new CommandSystem());
        world.AddSystem(new VisionSystem());
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());
        world.AddSystem(new MovementSystem());
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

        var tankSpec = UnitDesignCatalog.Spec("dog.guard_tank");
        var targetSpec = UnitDesignCatalog.Spec("cat.basic");
        world.SpawnUnit(tankSpec, new OwnerId(1), new Vector2(120, 500));
        var target = world.SpawnUnit(targetSpec, new OwnerId(2), EntityAttackMoveManualTargetPosition, MathF.PI);
        target.Components.Set(new HealthComponentState(12, targetSpec.Stats.MaxHp));
        return world;
    }
}
