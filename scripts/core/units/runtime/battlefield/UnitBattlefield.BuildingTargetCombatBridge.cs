namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void UpdateBuildingTargetCombatFromEntityWorld(float dt)
    {
        if (!HasBuildingTargetCombatWork())
        {
            return;
        }

        SyncBuildingTargetEntities();
        SyncUnitEntities();
        var context = new SimContext(_entityWorld, _inputCommandTick, dt, []);
        StepCombatBridge(context, _buildingTargetCombatSystem);
        SyncBuildingTargetCombatStateFromEntities();
    }

    private bool HasBuildingTargetCombatWork()
    {
        foreach (var unit in Units)
        {
            if ((unit.AttackTargetKind == CombatTargetKind.Building && unit.AttackTargetId is not null)
                || unit.MoveMode == MoveCommandMode.Attack)
            {
                return true;
            }
        }

        return false;
    }

    private void SyncBuildingTargetCombatStateFromEntities()
    {
        foreach (var unit in Units)
        {
            if (unit.AttackTargetKind != CombatTargetKind.Building && unit.MoveMode != MoveCommandMode.Attack)
            {
                continue;
            }

            if (!_entityWorld.TryGet(unit.EntityId, out var entity))
            {
                continue;
            }

            if (entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                unit.Velocity = movement.Velocity;
                unit.MoveTarget = movement.MoveTarget;
                unit.FormationSlot = movement.FormationSlot;
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
        }
    }

    private void ApplyBuildingTargetCombatEvents(IReadOnlyList<SimEvent> events)
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
