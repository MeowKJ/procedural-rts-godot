namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
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
                ClearEntityAttackTarget(unit);
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
                || !_removedBuildingIdBuffer.Add(identity.BuildingId)
                || BuildingSnapshot(identity.BuildingId) is not { Hp: <= 0 } snapshot)
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
            if (building.RemovalCause != UnitBattlefieldBuildingRemovalCause.Destroyed)
            {
                continue;
            }

            if (building.Kind == BuildingDesignIds.Headquarters && Relations.CanAttack(OutcomeViewer, building.PlayerSlotId))
            {
                Outcome = GameOutcome.Victory;
                OutcomeChanged?.Invoke(Outcome);
                return;
            }
        }

        foreach (var building in removedBuildings)
        {
            if (building.RemovalCause != UnitBattlefieldBuildingRemovalCause.Destroyed)
            {
                continue;
            }

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
                unit.LastDamageAmmoId,
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

        _units.RemoveAll(IsRemovedUnit);
        foreach (var unit in Units)
        {
            if (unit.AttackTargetId is not null && _removedUnitIdBuffer.Contains(unit.AttackTargetId.Value))
            {
                ClearEntityAttackTarget(unit);
            }
        }

        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
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
