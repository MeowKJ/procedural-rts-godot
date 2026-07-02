using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void ApplySelectionCommandStateToUnits(SetSelectionEntityCommand command)
    {
        foreach (var unit in Units.Where(unit => unit.PlayerSlotId == command.Issuer.ToPlayerSlot()))
        {
            if (!_entityWorld.TryGet(unit.EntityId, out var entity)
                || !entity.Components.TryGet<SelectableComponentState>(out var selectable))
            {
                unit.Selected = false;
                continue;
            }

            unit.Selected = selectable.Selected;
        }
    }

    private void ApplyEntityCommandStateToUnit(UnitInstance unit, EntityInstance entity, EntityCommand command)
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

        if (command is GroupMoveEntityCommand or MoveEntityCommand or AttackMoveEntityCommand or RepairEntityCommand or StopEntityCommand)
        {
            unit.AttackTargetId = null;
            unit.AttackTargetKind = CombatTargetKind.Unit;
            unit.AttackTargetIsManual = false;
        }
        else if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            unit.AttackTargetKind = weapon.AttackTargetKind;
            unit.AttackTargetIsManual = weapon.AttackTargetIsManual;
            unit.AttackTargetId = LegacyTargetId(weapon.AttackTarget, weapon.AttackTargetKind);
            if (weapon.AttackTargetIsManual)
            {
                unit.MoveMode = MoveCommandMode.Attack;
            }
        }

        if (entity.Components.TryGet<StanceComponentState>(out var stance))
        {
            unit.Stance = stance.Stance;
        }

        if (entity.Components.TryGet<HarvesterComponentState>(out var harvester))
        {
            unit.HarvesterMode = harvester.Mode;
            unit.HarvestFieldId = LegacyResourceFieldId(harvester.FieldId);
            unit.HarvestRefineryId = LegacyBuildingTargetId(harvester.RefineryId);
            unit.HarvestPulse = harvester.HarvestPulse;
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            unit.Cargo = cargo.Cargo;
        }

        if (command is GroupMoveEntityCommand or MoveEntityCommand or AttackMoveEntityCommand or GroupAttackEntityCommand or AttackEntityCommand or RepairEntityCommand or StopEntityCommand)
        {
            StopHarvesting(unit);
        }

        unit.CommandPulse = 1;
    }

    private int? LegacyTargetId(EntityId entityId, CombatTargetKind targetKind)
    {
        if (!entityId.IsValid)
        {
            return null;
        }

        if (targetKind == CombatTargetKind.Building)
        {
            return _buildingTargetIdsByEntityId.TryGetValue(entityId, out var buildingId)
                ? buildingId
                : null;
        }

        return UnitByEntityId(entityId)?.Id;
    }

    private int? LegacyResourceFieldId(int? entityId)
    {
        if (entityId is not int id)
        {
            return null;
        }

        foreach (var pair in _resourceFieldEntityIds)
        {
            if (pair.Value.Value == id)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private int? LegacyBuildingTargetId(int? entityId)
    {
        if (entityId is not int id)
        {
            return null;
        }

        return _buildingTargetIdsByEntityId.TryGetValue(new EntityId(id), out var buildingId)
            ? buildingId
            : null;
    }

    private UnitInstance? UnitByEntityId(EntityId entityId)
    {
        return Units.FirstOrDefault(unit => unit.EntityId == entityId);
    }

    private int? BuildingTargetIdByEntityId(EntityId entityId)
    {
        return _buildingTargetIdsByEntityId.TryGetValue(entityId, out var buildingId)
            ? buildingId
            : null;
    }

    private ResourceFieldModel? ResourceFieldById(int id)
    {
        return ResourceFields.FirstOrDefault(field => field.Id == id);
    }

    private int? FindBestRefineryIdForHarvester(PlayerSlotId playerSlotId, Vector2 position)
    {
        int? bestId = null;
        var bestDistance = 0f;
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || identity.PlayerSlotId != playerSlotId
                || identity.Kind != BuildingDesignIds.Refinery
                || BuildingSnapshot(identity.LegacyBuildingId) is not { } building
                || building.Hp <= 0
                || BuildingBuildProgress(building.Id) < 1)
            {
                continue;
            }

            var distance = building.Position.DistanceTo(position);
            if (bestId is null || distance < bestDistance)
            {
                bestId = building.Id;
                bestDistance = distance;
            }
        }

        return bestId;
    }

    private void ClearRefineryDockClaim(int harvesterId)
    {
        var harvesterEntityId = UnitEntityId(harvesterId);
        if (harvesterEntityId is null)
        {
            return;
        }

        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || identity.Kind != BuildingDesignIds.Refinery
                || !entity.Components.TryGet<DockComponentState>(out var dock))
            {
                continue;
            }

            entity.Components.Set(dock with
            {
                ReservedByEntityId = dock.ReservedByEntityId == harvesterEntityId.Value ? null : dock.ReservedByEntityId,
                DockedEntityId = dock.DockedEntityId == harvesterEntityId.Value ? null : dock.DockedEntityId,
            });
        }
    }

    private void StopHarvesting(UnitInstance unit)
    {
        ClearRefineryDockClaim(unit.Id);
        unit.HarvesterMode = HarvesterMode.Idle;
        unit.HarvestFieldId = null;
        unit.HarvestRefineryId = null;
        unit.HarvestPulse = 0;
    }

    private void ApplyDamage(UnitInstance attacker, UnitInstance target, WeaponDefinition weapon)
    {
        var ammo = WeaponCatalog.Ammo[weapon.AmmoKind];
        var damage = EffectiveDamageAgainst(ammo, target.Spec);
        target.Hp -= damage;
        target.LastDamageAmount = damage;
        target.LastDamageAmmoKind = ammo.Kind;
        target.DeathOverkillDamage = MathF.Max(0, -target.Hp);
        target.HitPulse = 1;
        target.AlertPulse = 1;
        UnitAttacked?.Invoke(target, attacker);
    }

    private UnitBattlefieldBuildingDeathInfo? BuildingDeathInfo(int buildingId)
    {
        if (BuildingSnapshot(buildingId) is not { } snapshot || snapshot.Hp > 0)
        {
            return null;
        }

        return new UnitBattlefieldBuildingDeathInfo(
            snapshot.Id,
            snapshot.Kind,
            snapshot.PlayerSlotId,
            snapshot.Faction,
            snapshot.Position,
            snapshot.Footprint);
    }

    private void RemoveDeadBuildingTargets(IReadOnlyList<int> deadBuildingIds)
    {
        _buildingDeathBuffer.Clear();
        _removedBuildingIdBuffer.Clear();
        foreach (var buildingId in deadBuildingIds)
        {
            if (BuildingDeathInfo(buildingId) is not { } death)
            {
                continue;
            }

            _buildingDeathBuffer.Add(death);
            _removedBuildingIdBuffer.Add(death.Id);
        }

        if (_buildingDeathBuffer.Count == 0)
        {
            return;
        }

        foreach (var removedId in _removedBuildingIdBuffer)
        {
            RemoveBuildingEntity(removedId);
        }

        foreach (var unit in Units)
        {
            if (unit.AttackTargetKind == CombatTargetKind.Building
                && unit.AttackTargetId is not null
                && _removedBuildingIdBuffer.Contains(unit.AttackTargetId.Value))
            {
                ClearAttackTarget(unit);
            }
        }

        BuildingsRemoved?.Invoke(_buildingDeathBuffer);
        UpdateOutcomeAfterRemovedBuildings(_buildingDeathBuffer);
    }

    private void RemoveDeadBuildingTargetsFromEntities()
    {
        _deadBuildingIdBuffer.Clear();
        _removedBuildingIdBuffer.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || !_removedBuildingIdBuffer.Add(identity.LegacyBuildingId)
                || BuildingSnapshot(identity.LegacyBuildingId) is not { Hp: <= 0 } snapshot)
            {
                continue;
            }

            _deadBuildingIdBuffer.Add(snapshot.Id);
        }

        if (_deadBuildingIdBuffer.Count > 0)
        {
            RemoveDeadBuildingTargets(_deadBuildingIdBuffer);
        }
    }

    private void UpdateOutcomeAfterRemovedBuildings(IReadOnlyList<UnitBattlefieldBuildingDeathInfo> removedBuildings)
    {
        if (Outcome != GameOutcome.InProgress)
        {
            return;
        }

        foreach (var building in removedBuildings)
        {
            if (building.Kind == BuildingDesignIds.Headquarters && Relations.CanAttack(OutcomeViewer, building.PlayerSlotId))
            {
                Outcome = GameOutcome.Victory;
                OutcomeChanged?.Invoke(Outcome);
                return;
            }
        }

        foreach (var building in removedBuildings)
        {
            if (building.Kind == BuildingDesignIds.Headquarters && building.PlayerSlotId == OutcomeViewer)
            {
                Outcome = GameOutcome.Defeat;
                OutcomeChanged?.Invoke(Outcome);
                return;
            }
        }
    }

    private void RemoveDeadUnits()
    {
        _unitDeathBuffer.Clear();
        _removedUnitIdBuffer.Clear();
        foreach (var unit in Units)
        {
            if (unit.Hp > 0)
            {
                continue;
            }

            _unitDeathBuffer.Add(new UnitInstanceDeathInfo(
                unit.Id,
                unit.Spec.Id,
                unit.PlayerSlotId,
                unit.Spec.Faction,
                unit.Position,
                unit.Spec.Collision.Radius,
                unit.Spec.Stats.WeightClass,
                unit.Spec.Movement.Domain,
                unit.LastDamageAmmoKind,
                unit.DeathOverkillDamage));
            _removedUnitIdBuffer.Add(unit.Id);
        }

        if (_unitDeathBuffer.Count == 0)
        {
            return;
        }

        foreach (var unit in Units)
        {
            if (_removedUnitIdBuffer.Contains(unit.Id))
            {
                _entityWorld.Remove(unit.EntityId);
            }
        }

        Units.RemoveAll(IsRemovedUnit);
        foreach (var unit in Units)
        {
            if (unit.AttackTargetId is not null && _removedUnitIdBuffer.Contains(unit.AttackTargetId.Value))
            {
                ClearAttackTarget(unit);
            }
        }

        foreach (var buildingId in BuildingTargetIds())
        {
            if (BuildingAttackTargetKindCore(buildingId) == CombatTargetKind.Unit
                && BuildingAttackTargetIdCore(buildingId) is { } targetId
                && _removedUnitIdBuffer.Contains(targetId))
            {
                ClearBuildingAttackTargetCore(buildingId);
            }
        }

        UnitsRemoved?.Invoke(_unitDeathBuffer);
    }

    private bool IsRemovedUnit(UnitInstance unit)
    {
        return _removedUnitIdBuffer.Contains(unit.Id);
    }

}
