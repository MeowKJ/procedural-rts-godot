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
        targetBuilding = VisibleAttackableHeadquarters(battlefield, enemyPlayerSlotId, enemyCenter, aggressionRadius);
        if (targetBuilding is { } headquarters)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = headquarters.Position;
            return true;
        }

        targetBuilding = NearestVisibleAttackableBuilding(battlefield, enemyPlayerSlotId, enemyCenter, aggressionRadius);
        if (targetBuilding is { } nearestBuilding)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = nearestBuilding.Position;
            return true;
        }

        targetUnit = NearestVisibleAttackableUnit(battlefield, enemyPlayerSlotId, enemyCenter, aggressionRadius);
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

        status = $"Enemy scout wave launched ({moved} units)";
        return true;
    }

    private bool TryFindDefenseTarget(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        out CombatTargetKind targetKind,
        out UnitInstance? targetUnit,
        out UnitBattlefieldBuildingSnapshot? targetBuilding,
        out Vector2 targetPosition)
    {
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        targetUnit = NearestVisibleDefenseThreatUnit(battlefield, playerSlotId, baseCenter);
        if (targetUnit is not null)
        {
            targetKind = CombatTargetKind.Unit;
            targetBuilding = null;
            targetPosition = targetUnit.Position;
            return true;
        }

        targetBuilding = NearestVisibleDefenseThreatBuilding(battlefield, playerSlotId, baseCenter);
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
