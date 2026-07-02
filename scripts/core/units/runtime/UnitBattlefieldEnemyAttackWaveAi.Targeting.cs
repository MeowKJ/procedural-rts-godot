using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private bool TryFindTarget(
        UnitBattlefield battlefield,
        PlayerSlotId enemyPlayerSlotId,
        float aggressionRadius,
        out CombatTargetKind targetKind,
        out UnitInstance? targetUnit,
        out UnitBattlefieldBuildingSnapshot? targetBuilding,
        out Vector2 targetPosition)
    {
        var enemyCenter = EnemyCenter(battlefield, enemyPlayerSlotId);
        var buildings = battlefield.BuildingSnapshots();
        targetBuilding = buildings
            .Where(building => battlefield.Relations.CanAttack(enemyPlayerSlotId, building.PlayerSlotId))
            .Where(building => building.Hp > 0)
            .Where(building => battlefield.IsVisibleTo(enemyPlayerSlotId, building.Id))
            .Where(building => building.Kind == BuildingDesignIds.Headquarters
                && IsInsideAggressionRadius(building.Position, enemyCenter, aggressionRadius))
            .Select(building => (UnitBattlefieldBuildingSnapshot?)building)
            .FirstOrDefault();
        if (targetBuilding is { } headquarters)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = headquarters.Position;
            return true;
        }

        targetBuilding = buildings
            .Where(building => battlefield.Relations.CanAttack(enemyPlayerSlotId, building.PlayerSlotId) && building.Hp > 0)
            .Where(building => battlefield.IsVisibleTo(enemyPlayerSlotId, building.Id))
            .Where(building => IsInsideAggressionRadius(building.Position, enemyCenter, aggressionRadius))
            .OrderBy(building => building.Position.DistanceSquaredTo(enemyCenter))
            .Select(building => (UnitBattlefieldBuildingSnapshot?)building)
            .FirstOrDefault();
        if (targetBuilding is { } nearestBuilding)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = nearestBuilding.Position;
            return true;
        }

        targetUnit = battlefield.Units
            .Where(unit => battlefield.Relations.CanAttack(enemyPlayerSlotId, unit.PlayerSlotId) && unit.Hp > 0)
            .Where(unit => battlefield.IsVisibleTo(enemyPlayerSlotId, unit))
            .Where(unit => IsInsideAggressionRadius(unit.Position, enemyCenter, aggressionRadius))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(enemyCenter))
            .FirstOrDefault();
        if (targetUnit is not null)
        {
            targetKind = CombatTargetKind.Unit;
            targetBuilding = null;
            targetPosition = targetUnit.Position;
            return true;
        }

        targetKind = CombatTargetKind.Unit;
        targetPosition = Vector2.Zero;
        return false;
    }

    private static bool TryIssueScoutWave(
        UnitBattlefield battlefield,
        PlayerSlotId enemyPlayerSlotId,
        IReadOnlyList<UnitInstance> waveUnits,
        List<int> unitIds,
        out string status)
    {
        status = string.Empty;
        var scoutPoint = ScoutPoint(battlefield, enemyPlayerSlotId);
        CollectUnitIds(waveUnits, unitIds);
        var moved = battlefield.CommandMoveUnits(
            enemyPlayerSlotId,
            unitIds,
            scoutPoint,
            battlefield.WorldSize,
            MoveCommandMode.Attack);
        if (moved == 0)
        {
            return false;
        }

        foreach (var unit in waveUnits)
        {
            unit.PlayerIntentTarget = scoutPoint;
            unit.CommandVisualTarget = scoutPoint;
            unit.CommandPulse = 1;
        }

        status = $"Enemy scout wave launched ({moved} units)";
        return true;
    }

    private static bool TryFindDefenseTarget(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        out CombatTargetKind targetKind,
        out UnitInstance? targetUnit,
        out UnitBattlefieldBuildingSnapshot? targetBuilding,
        out Vector2 targetPosition)
    {
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        targetUnit = battlefield.Units
            .Where(unit => battlefield.Relations.CanAttack(playerSlotId, unit.PlayerSlotId) && unit.Hp > 0)
            .Where(unit => battlefield.IsVisibleTo(playerSlotId, unit))
            .Where(unit => unit.Position.DistanceSquaredTo(baseCenter) <= DefenseRadius * DefenseRadius
                || IsNearOwnedBuilding(battlefield, playerSlotId, unit.Position, DefenseRadius))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(baseCenter))
            .ThenBy(unit => unit.Id)
            .FirstOrDefault();
        if (targetUnit is not null)
        {
            targetKind = CombatTargetKind.Unit;
            targetBuilding = null;
            targetPosition = targetUnit.Position;
            return true;
        }

        targetBuilding = battlefield.BuildingSnapshots()
            .Where(building => battlefield.Relations.CanAttack(playerSlotId, building.PlayerSlotId) && building.Hp > 0)
            .Where(building => battlefield.IsVisibleTo(playerSlotId, building.Id))
            .Where(building => building.Position.DistanceSquaredTo(baseCenter) <= DefenseRadius * DefenseRadius)
            .OrderBy(building => building.Position.DistanceSquaredTo(baseCenter))
            .ThenBy(building => building.Id)
            .Select(building => (UnitBattlefieldBuildingSnapshot?)building)
            .FirstOrDefault();
        if (targetBuilding is { } defendedBuilding)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = defendedBuilding.Position;
            return true;
        }

        targetKind = CombatTargetKind.Unit;
        targetPosition = Vector2.Zero;
        return false;
    }
}
