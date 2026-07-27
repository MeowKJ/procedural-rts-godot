namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void ApplyBuildingDamageEvents(IReadOnlyList<SimEvent> events)
    {
        _combatDamagedBuildingIds.Clear();
        _combatDestroyedBuildingIds.Clear();

        foreach (var simEvent in events)
        {
            if (simEvent is EntityDestroyedEvent destroyed)
            {
                if (BuildingTargetIdByEntityId(destroyed.Entity) is { } destroyedBuildingId)
                {
                    _combatDestroyedBuildingIds.Add(destroyedBuildingId);
                }

                continue;
            }

            if (simEvent is not EntityDamagedEvent damaged)
            {
                continue;
            }

            var targetId = BuildingTargetIdByEntityId(damaged.Target);
            var attacker = UnitByEntityId(damaged.Attacker);
            if (targetId is null || attacker is null)
            {
                continue;
            }

            SetBuildingHitPulse(targetId.Value, 1);
            if (BuildingSnapshot(targetId.Value) is not { } targetSnapshot)
            {
                continue;
            }

            BuildingAttacked?.Invoke(targetSnapshot, attacker);
            _combatDamagedBuildingIds.Add(targetId.Value);
        }

        CollectDeadBuildingTargetIdsFromCombatEvents();
        if (_deadBuildingIdBuffer.Count > 0)
        {
            RemoveDeadBuildingTargets(_deadBuildingIdBuffer);
        }
    }

    private void CollectDeadBuildingTargetIdsFromCombatEvents()
    {
        _deadBuildingIdBuffer.Clear();
        _combatDeadBuildingIds.Clear();
        foreach (var buildingId in _combatDamagedBuildingIds)
        {
            if (BuildingSnapshot(buildingId) is { Hp: <= 0 } snapshot)
            {
                AddCombatDeadBuildingId(snapshot.Id);
            }
        }

        foreach (var buildingId in _combatDestroyedBuildingIds)
        {
            AddCombatDeadBuildingId(buildingId);
        }
    }

    private void AddCombatDeadBuildingId(int buildingId)
    {
        if (_combatDeadBuildingIds.Add(buildingId))
        {
            _deadBuildingIdBuffer.Add(buildingId);
        }
    }
}
