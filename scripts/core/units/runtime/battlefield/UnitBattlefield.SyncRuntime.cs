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
            unit.AttackTargetId = LegacyTargetId(weapon.AttackTarget, weapon.AttackTargetKind);
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
            unit.HarvestFieldId = LegacyResourceFieldId(harvester.FieldId);
            unit.HarvestRefineryId = LegacyBuildingTargetId(harvester.RefineryId);
            unit.HarvestPulse = Mathf.Clamp(harvester.HarvestPulse, 0, 1);
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            unit.Cargo = cargo.Cargo;
        }

        SyncBodyFixedMountFacings(unit);
    }

    private UnitInstance AdoptUnitEntity(EntityInstance entity)
    {
        if (UnitByEntityId(entity.Id) is { } existing)
        {
            return existing;
        }

        var spec = UnitDesignCatalog.Spec(entity.SpecId);
        var unit = new UnitInstance
        {
            Id = _nextUnitId++,
            EntityId = entity.Id,
            Spec = spec,
            PlayerSlotId = entity.OwnerId.ToPlayerSlot(),
            Position = entity.Transform.Position,
            Facing = entity.Transform.Facing,
            Velocity = entity.Components.TryGet<MovementComponentState>(out var movement) ? movement.Velocity : Vector2.Zero,
            Hp = entity.Components.TryGet<HealthComponentState>(out var health) ? health.Hp : spec.Stats.MaxHp,
            Selected = entity.Components.TryGet<SelectableComponentState>(out var selectable) && selectable.Selected,
            PlayerIntentTarget = entity.Components.TryGet<CommandableComponentState>(out var commandable) ? commandable.PlayerIntentTarget : null,
            FormationSlot = movement?.FormationSlot,
            CommandVisualTarget = commandable?.CommandVisualTarget,
            MoveTarget = movement?.MoveTarget,
            MoveMode = commandable?.MoveMode ?? MoveCommandMode.Direct,
            Stance = entity.Components.TryGet<StanceComponentState>(out var stance) ? stance.Stance : spec.Weapons.Count > 0 ? UnitStance.Aggressive : UnitStance.Ignore,
            WeaponMounts = entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
                ? weapon.Mounts.ToList()
                : spec.Weapons.Select(mount => new WeaponMountRuntimeState(mount.MountId, mount.WeaponId, entity.Transform.Facing, 0, mount.LegacyWeaponKind)).ToList(),
            HarvesterMode = entity.Components.TryGet<HarvesterComponentState>(out var harvester) ? harvester.Mode : HarvesterMode.Idle,
            HarvestFieldId = harvester is null ? null : LegacyResourceFieldId(harvester.FieldId),
            HarvestRefineryId = harvester is null ? null : LegacyBuildingTargetId(harvester.RefineryId),
            HarvestPulse = harvester?.HarvestPulse ?? 0,
            Cargo = entity.Components.TryGet<ResourceCargoComponentState>(out var cargo) ? cargo.Cargo : 0,
        };

        Units.Add(unit);
        return unit;
    }

    private void SyncResourceFieldEntities()
    {
        foreach (var field in ResourceFields)
        {
            SyncResourceFieldEntity(field);
        }
    }

    private void SyncResourceFieldEntity(ResourceFieldModel field)
    {
        var spec = ResourceFieldEntitySpec(field);
        var components = new EntityComponentState[]
        {
            new ResourceNodeComponentState(field.Amount, field.MaxAmount),
            new CollisionComponentState(field.Radius, 10, 0, BlocksMovement: false),
        };

        if (_resourceFieldEntityIds.TryGetValue(field.Id, out var entityId) && _entityWorld.TryGet(entityId, out var existing))
        {
            existing.Transform = EntityTransform.At(field.Position);
            existing.Components.Clear();
            foreach (var component in components)
            {
                existing.Components.Set(component);
            }

            return;
        }

        var entity = _entityWorld.Spawn(spec, OwnerId.None, EntityTransform.At(field.Position), components);
        _resourceFieldEntityIds[field.Id] = entity.Id;
    }

    private void SyncResourceFieldFromEntity(ResourceFieldModel field)
    {
        if (!_resourceFieldEntityIds.TryGetValue(field.Id, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<ResourceNodeComponentState>(out var node))
        {
            return;
        }

        if (node.Amount != field.Amount)
        {
            field.Pulse = 1;
        }

        field.Amount = node.Amount;
    }

    private void SyncResourceFieldsFromEntities()
    {
        foreach (var field in ResourceFields)
        {
            SyncResourceFieldFromEntity(field);
        }
    }

    private static EntitySpec ResourceFieldEntitySpec(ResourceFieldModel field)
    {
        return new EntitySpec
        {
            Id = $"resource.field.{field.Id}",
            Kind = EntityKind.Resource,
            Display = new EntityDisplaySpec(
                $"Resource Field {field.Id}",
                "resource.field.name",
                "resource.field.role",
                $"R{field.Id}",
                IconGlyph.Harvester),
            Tags = new HashSet<string> { "Resource", "Credit" },
            Collision = new CollisionSpec(field.Radius, 10, 0, BlocksMovement: false),
        };
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

        if (unit.Spec.Abilities.Any(ability => ability.Kind == AbilityKind.Harvest))
        {
            entity.Components.Set(new HarvesterComponentState(
                unit.HarvesterMode,
                ResourceFieldEntityId(unit.HarvestFieldId),
                BuildingTargetEntityId(unit.HarvestRefineryId),
                unit.HarvestPulse));
            entity.Components.Set(new ResourceCargoComponentState(unit.Cargo, HarvesterCargoCapacity));
        }
    }

    private int? ResourceFieldEntityId(int? legacyFieldId)
    {
        return legacyFieldId is int id && _resourceFieldEntityIds.TryGetValue(id, out var entityId)
            ? entityId.Value
            : null;
    }

    private int? BuildingTargetEntityId(int? legacyBuildingId)
    {
        return legacyBuildingId is int id && _buildingTargetEntityIds.TryGetValue(id, out var entityId)
            ? entityId.Value
            : null;
    }

    private int? UnitEntityId(int? legacyUnitId)
    {
        return legacyUnitId is int id
            ? UnitById(id)?.EntityId.Value
            : null;
    }

    private void UpdateResourceHarvestersFromEntityWorld(float dt)
    {
        if (!HasHarvesters())
        {
            return;
        }

        CollectResourceCreditsBefore(_resourceCreditsBefore);
        SyncResourceFieldEntities();
        SyncBuildingTargetEntities();
        SyncUnitEntities();
        _resourceSystem.Step(new SimContext(_entityWorld, _inputCommandTick, dt, []));
        SyncResourceFieldsFromEntities();
        SyncDockStateFromEntities();
        SyncHarvestersFromEntities();
        SyncAllCreditsFromEntityWorld(_resourceCreditsBefore);
    }

    private void SyncHarvestersFromEntities()
    {
        foreach (var unit in Units)
        {
            if (IsHarvester(unit) && _entityWorld.TryGet(unit.EntityId, out var entity))
            {
                ApplyEntityResourceStateToUnit(unit, entity);
            }
        }
    }

    private void ApplyEntityResourceStateToUnit(UnitInstance unit, EntityInstance entity)
    {
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

        if (entity.Components.TryGet<HarvesterComponentState>(out var harvester))
        {
            unit.HarvesterMode = harvester.Mode;
            unit.HarvestFieldId = LegacyResourceFieldId(harvester.FieldId);
            unit.HarvestRefineryId = LegacyBuildingTargetId(harvester.RefineryId);
            unit.HarvestPulse = Mathf.Clamp(harvester.HarvestPulse, 0, 1);
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            unit.Cargo = cargo.Cargo;
        }
    }

    private void SyncDockStateFromEntities()
    {
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var refineryId in _buildingTargetIdBuffer)
        {
            if (BuildingIdentity(refineryId)?.Kind != BuildingDesignIds.Refinery)
            {
                continue;
            }

            if (BuildingEntityByTargetId(refineryId) is not { } entity
                || !entity.Components.TryGet<DockComponentState>(out var dock))
            {
                continue;
            }

            var docked = LegacyUnitId(dock.DockedEntityId);
            var wasDocked = _lastDockedHarvesterIds.TryGetValue(refineryId, out var previous)
                ? previous
                : null;
            _lastDockedHarvesterIds[refineryId] = docked;
            if (docked is not null || wasDocked != docked)
            {
                SetBuildingDeliveryPulseCore(refineryId, 1);
            }
        }
    }

    private int? LegacyUnitId(int? entityId)
    {
        return entityId is int id
            ? UnitByEntityId(new EntityId(id))?.Id
            : null;
    }

}
