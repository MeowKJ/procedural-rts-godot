using Godot;

namespace ProceduralRts.Core;

public sealed partial class CommandSystem
{
    private static void ApplyHarvest(EntityWorld world, HarvestEntityCommand command)
    {
        if (!world.TryGet(command.ResourceTarget, out var resource)
            || !resource.Components.TryGet<ResourceNodeComponentState>(out var node)
            || node.Amount <= 0)
        {
            return;
        }

        foreach (var entity in OwnedSubjects(world, command.Issuer, command.Subjects))
        {
            if (!entity.Components.Has<HarvesterComponentState>()
                || !entity.Components.Has<ResourceCargoComponentState>())
            {
                continue;
            }

            entity.Components.Set(new HarvesterComponentState(
                HarvesterMode.MovingToField,
                FieldId: command.ResourceTarget.Value));
            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();

            var movement = entity.Components.TryGet<MovementComponentState>(out var existingMovement)
                ? existingMovement
                : new MovementComponentState(Vector2.Zero);
            entity.Components.Set(movement with
            {
                MoveTarget = resource.Transform.Position,
                FormationSlot = null,
            });

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var existingCommandable)
                ? existingCommandable
                : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = resource.Transform.Position,
                CommandVisualTarget = resource.Transform.Position,
                MoveMode = MoveCommandMode.Direct,
            });

            if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
            {
                entity.Components.Set(weapon with
                {
                    AttackTarget = default,
                    AttackTargetIsManual = false,
                    AutoReacquireCooldownRemaining = 0,
                });
            }
        }
    }

    private static void ApplyAutoHarvest(EntityWorld world, AutoHarvestEntityCommand command)
    {
        foreach (var entity in OwnedSubjects(world, command.Issuer, command.Subjects))
        {
            if (!entity.Components.Has<HarvesterComponentState>()
                || !entity.Components.Has<ResourceCargoComponentState>()
                || !ResourceMiningMath.TryFindNearestAvailableResourceNode(
                    world,
                    entity.Transform.Position,
                    out var resource,
                    out _))
            {
                continue;
            }

            ApplyHarvest(world, new HarvestEntityCommand(
                command.Issuer,
                [entity.Id],
                command.Tick,
                resource.Id));
        }
    }

    private static void ApplyRepair(EntityWorld world, RepairEntityCommand command)
    {
        if (!world.TryGet(command.Target, out var target)
            || !target.Components.TryGet<HealthComponentState>(out var targetHealth)
            || targetHealth.Hp <= 0)
        {
            return;
        }

        var restartable = IsRestartableObjective(world, target);
        if (targetHealth.Hp >= targetHealth.MaxHp && !restartable)
        {
            return;
        }

        foreach (var entity in OwnedSubjects(world, command.Issuer, command.Subjects))
        {
            if (entity.OwnerId.Value != command.Issuer.Value
                || !TryGetRepairAbility(world, entity, out var repairAbility))
            {
                continue;
            }

            var canCaptureNeutral = restartable && target.OwnerId.Value == OwnerId.None.Value;
            if (!canCaptureNeutral
                && world.Relations.Relation(entity.OwnerId, target.OwnerId) is not (PlayerRelation.Self or PlayerRelation.Allied))
            {
                continue;
            }

            if (canCaptureNeutral)
            {
                world.ChangeOwner(target.Id, command.Issuer);
            }

            if (targetHealth.Hp >= targetHealth.MaxHp
                && !IsRestartableObjective(world, target))
            {
                continue;
            }

            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();
            entity.Components.Set(new RepairOrderComponentState(
                TargetId: command.Target.Value,
                Range: repairAbility.Radius > 0 ? repairAbility.Radius : 96,
                RepairPerSecond: repairAbility.Value > 0 ? repairAbility.Value : 12));

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var existingCommandable)
                ? existingCommandable
                : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = target.Transform.Position,
                CommandVisualTarget = target.Transform.Position,
                MoveMode = MoveCommandMode.Direct,
            });
        }
    }

    private static bool TryGetRepairAbility(EntityWorld world, EntityInstance entity, out AbilitySpec ability)
    {
        if (world.TryGetSpec(entity.SpecId, out var spec))
        {
            foreach (var candidate in spec.Abilities)
            {
                if (candidate.Kind == AbilityKind.RepairField)
                {
                    ability = candidate;
                    return true;
                }
            }
        }

        ability = default!;
        return false;
    }

    private static bool IsRestartableObjective(EntityWorld world, EntityInstance target)
    {
        return world.TryGetSpec(target.SpecId, out var spec)
            && spec.Kind == EntityKind.Objective
            && target.Components.TryGet<ConstructionComponentState>(out var construction)
            && construction.Phase == ConstructionPhase.RestartCapture
            && construction.Progress < 1;
    }
}
