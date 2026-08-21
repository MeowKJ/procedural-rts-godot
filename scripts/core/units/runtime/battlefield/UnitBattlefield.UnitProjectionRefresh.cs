using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void UpdateUnitRuntimeMotionFromEntityWorld(float dt)
    {
        if (Units.Count == 0)
        {
            return;
        }

        SyncOwnerRelations();

        var context = new SimContext(_entityWorld, _inputCommandTick, dt, []);
        _pathfindingSystem.Step(context);
        _movementSystem.Step(context);
        _separationSystem.Step(context);
        RefreshUnitProjections();
    }

    private void RefreshUnitProjections()
    {
        foreach (var unit in Units)
        {
            if (_entityWorld.TryGet(unit.EntityId, out var entity))
            {
                RefreshUnitProjection(unit, entity);
            }
        }
    }

    private void RefreshUnitProjection(UnitInstance unit, EntityInstance entity)
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
        else
        {
            unit.Velocity = Vector2.Zero;
            unit.MoveTarget = null;
            unit.FormationSlot = null;
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
            unit.AlertPulse = selectable.AlertPulse;
        }
        else
        {
            unit.Selected = false;
            unit.AlertPulse = 0;
        }

        if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            unit.MutableWeaponMounts.Clear();
            unit.MutableWeaponMounts.AddRange(weapon.Mounts);
            unit.AttackCooldownRemaining = weapon.Mounts.Count == 0 ? 0 : weapon.Mounts[0].CooldownRemaining;
            unit.AttackTargetKind = weapon.AttackTargetKind;
            unit.AttackTargetIsManual = weapon.AttackTargetIsManual;
            unit.AttackTargetId = TargetIdForEntity(weapon.AttackTarget, weapon.AttackTargetKind);
        }
        else
        {
            unit.MutableWeaponMounts.Clear();
            unit.AttackCooldownRemaining = 0;
            unit.AttackTargetKind = CombatTargetKind.Unit;
            unit.AttackTargetIsManual = false;
            unit.AttackTargetId = null;
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
        else
        {
            unit.CommandPulse = 0;
            unit.HitPulse = 0;
        }

        if (entity.Components.TryGet<HarvesterComponentState>(out var harvester))
        {
            unit.HarvesterMode = harvester.Mode;
            unit.HarvestFieldId = ResourceFieldIdForEntity(harvester.FieldId);
            unit.HarvestRefineryId = BuildingIdForEntity(harvester.RefineryId);
            unit.HarvestPulse = Mathf.Clamp(harvester.HarvestPulse, 0, 1);
            unit.HarvesterRetreating = harvester.Retreating;
        }
        else
        {
            unit.HarvesterMode = HarvesterMode.Idle;
            unit.HarvestFieldId = null;
            unit.HarvestRefineryId = null;
            unit.HarvestPulse = 0;
            unit.HarvesterRetreating = false;
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            unit.Cargo = cargo.Cargo;
        }
        else
        {
            unit.Cargo = 0;
        }

        SyncBodyFixedMountFacings(unit);
    }

    private void DecayUnitPresentationPulses(float dt)
    {
        foreach (var unit in Units)
        {
            if (!_entityWorld.TryGet(unit.EntityId, out var entity))
            {
                continue;
            }

            if (entity.Components.TryGet<PresentationPulseComponentState>(out var pulse))
            {
                entity.Components.Set(pulse with
                {
                    CommandPulse = Mathf.Max(0, pulse.CommandPulse - dt * CommandPulseDecay),
                    AlertPulse = Mathf.Max(0, pulse.AlertPulse - dt * AlertPulseDecay),
                    HitPulse = Mathf.Max(0, pulse.HitPulse - dt * 3.6f),
                });
            }

            if (entity.Components.TryGet<SelectableComponentState>(out var selectable) && selectable.AlertPulse > 0)
            {
                entity.Components.Set(selectable with { AlertPulse = Mathf.Max(0, selectable.AlertPulse - dt * AlertPulseDecay) });
            }

            if (entity.Components.TryGet<HarvesterComponentState>(out var harvester) && harvester.HarvestPulse > 0)
            {
                entity.Components.Set(harvester with { HarvestPulse = Mathf.Max(0, harvester.HarvestPulse - dt * 2.8f) });
            }
        }
    }

    private void ApplyUnitDamageProjection(
        UnitInstance unit,
        EntityInstance entity,
        float damage,
        string? ammoId)
    {
        var pulse = entity.Components.TryGet<PresentationPulseComponentState>(out var currentPulse)
            ? currentPulse
            : new PresentationPulseComponentState();
        entity.Components.Set(pulse with { HitPulse = 1, AlertPulse = 1 });

        var selectable = entity.Components.TryGet<SelectableComponentState>(out var currentSelectable)
            ? currentSelectable
            : new SelectableComponentState();
        entity.Components.Set(selectable with { AlertPulse = 1 });

        RefreshUnitProjection(unit, entity);
        unit.LastDamageAmount = damage;
        unit.LastDamageAmmoId = ammoId;
        unit.DeathOverkillDamage = MathF.Max(0, -unit.Hp);
    }
}
