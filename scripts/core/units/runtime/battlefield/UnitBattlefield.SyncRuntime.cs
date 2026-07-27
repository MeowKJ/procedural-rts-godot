using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void SyncUnitEntities()
    {
        foreach (var unit in Units)
        {
            SyncUnitEntity(unit);
        }
    }

    private void UpdateUnitRuntimeMotionFromEntityWorld(float dt)
    {
        if (Units.Count == 0)
        {
            return;
        }

        SyncOwnerRelations();
        SyncUnitEntities();
        _entityWorld.WorldWidth = WorldSize.X;
        _entityWorld.WorldHeight = WorldSize.Y;

        var context = new SimContext(_entityWorld, _inputCommandTick, dt, []);
        _pathfindingSystem.Step(context);
        _movementSystem.Step(context);
        _separationSystem.Step(context);
        SyncUnitRuntimeStateFromEntities();
    }

    private void SyncUnitRuntimeStateFromEntities()
    {
        foreach (var unit in Units)
        {
            if (_entityWorld.TryGet(unit.EntityId, out var entity))
            {
                SyncUnitRuntimeStateFromEntity(unit, entity);
            }
        }
    }

    private void SyncUnitRuntimeStateFromEntity(UnitInstance unit, EntityInstance entity)
    {
        unit.Position = entity.Transform.Position;
        unit.Facing = entity.Transform.Facing;

        if (entity.Components.TryGet<HealthComponentState>(out var health))
        {
            unit.Hp = health.Hp;
        }

        if (entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            unit.Velocity = movement.Velocity;
            unit.MoveTarget = movement.MoveTarget;
            unit.FormationSlot = movement.FormationSlot;
        }

        if (entity.Components.TryGet<CommandableComponentState>(out var commandable))
        {
            unit.PlayerIntentTarget = commandable.PlayerIntentTarget;
            unit.CommandVisualTarget = commandable.CommandVisualTarget;
            unit.MoveMode = commandable.MoveMode;
        }

        if (entity.Components.TryGet<SelectableComponentState>(out var selectable))
        {
            unit.Selected = selectable.Selected;
            unit.AlertPulse = MathF.Max(unit.AlertPulse, selectable.AlertPulse);
        }

        if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            unit.WeaponMounts.Clear();
            unit.WeaponMounts.AddRange(weapon.Mounts);
            unit.AttackCooldownRemaining = weapon.Mounts.Count == 0 ? 0 : weapon.Mounts[0].CooldownRemaining;
            unit.AttackTargetKind = weapon.AttackTargetKind;
            unit.AttackTargetIsManual = weapon.AttackTargetIsManual;
            unit.AttackTargetId = TargetIdForEntity(weapon.AttackTarget, weapon.AttackTargetKind);
        }

        if (entity.Components.TryGet<StanceComponentState>(out var stance))
        {
            unit.Stance = stance.Stance;
        }

        if (entity.Components.TryGet<PresentationPulseComponentState>(out var pulse))
        {
            unit.CommandPulse = pulse.CommandPulse;
            unit.AlertPulse = MathF.Max(unit.AlertPulse, pulse.AlertPulse);
            unit.HitPulse = pulse.HitPulse;
        }

        if (entity.Components.TryGet<HarvesterComponentState>(out var harvester))
        {
            unit.HarvesterMode = harvester.Mode;
            unit.HarvestFieldId = ResourceFieldIdForEntity(harvester.FieldId);
            unit.HarvestRefineryId = BuildingIdForEntity(harvester.RefineryId);
            unit.HarvestPulse = Mathf.Clamp(harvester.HarvestPulse, 0, 1);
            unit.HarvesterRetreating = harvester.Retreating;
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            unit.Cargo = cargo.Cargo;
        }

        SyncBodyFixedMountFacings(unit);
    }

    private void SyncUnitEntity(UnitInstance unit)
    {
        if (!_entityWorld.TryGet(unit.EntityId, out var entity))
        {
            return;
        }

        var fireAnchorRemaining = entity.Components.TryGet<MovementComponentState>(out var previousMovement)
            ? previousMovement.FireAnchorRemaining
            : 0;

        entity.Transform = EntityTransform.At(unit.Position, unit.Facing);
        entity.Components.Set(new HealthComponentState(unit.Hp, unit.Spec.Stats.MaxHp));
        entity.Components.Set(new SelectableComponentState(unit.Selected, unit.AlertPulse));
        entity.Components.Set(new CommandableComponentState(
            unit.PlayerIntentTarget,
            unit.CommandVisualTarget,
            unit.MoveMode));
        entity.Components.Set(new MovementComponentState(
            unit.Velocity,
            unit.MoveTarget,
            unit.FormationSlot,
            fireAnchorRemaining));
        entity.Components.Set(new WeaponUserComponentState(
            WeaponMountsForEntity(unit),
            AttackTargetEntityId(unit),
            unit.AttackTargetKind,
            unit.AttackTargetIsManual));
        if (unit.WeaponMounts.Count > 0)
        {
            var anchor = unit.Stance == UnitStance.Hold ? unit.Position : (Vector2?)null;
            entity.Components.Set(new StanceComponentState(unit.Stance, anchor));
        }

        entity.Components.Set(new PresentationPulseComponentState(
            unit.CommandPulse,
            unit.AlertPulse,
            unit.HitPulse));

        if (unit.Spec.HasAbility(AbilityKind.Harvest))
        {
            entity.Components.Set(new HarvesterComponentState(
                unit.HarvesterMode,
                ResourceFieldEntityId(unit.HarvestFieldId),
                BuildingTargetEntityId(unit.HarvestRefineryId),
                unit.HarvestPulse,
                unit.HarvesterRetreating));
            entity.Components.Set(new ResourceCargoComponentState(unit.Cargo, HarvesterCargoCapacity));
        }
    }

}
