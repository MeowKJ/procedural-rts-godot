using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private static IEnumerable<UnitInstance> AvailableDefenseUnits(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 targetPosition)
    {
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        return AvailableCombatUnits(battlefield, playerSlotId)
            .Where(unit => unit.Position.DistanceSquaredTo(baseCenter) <= DefenseRadius * DefenseRadius
                || unit.Position.DistanceSquaredTo(targetPosition) <= DefenseRadius * DefenseRadius)
            .OrderBy(unit => unit.Position.DistanceSquaredTo(targetPosition))
            .ThenBy(unit => unit.Id);
    }

    private static IEnumerable<UnitInstance> AvailableWaveUnits(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        int minimumWaveUnits,
        int maximumWaveUnits)
    {
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        var candidates = AvailableCombatUnits(battlefield, playerSlotId)
            .OrderBy(unit => unit.Position.DistanceSquaredTo(baseCenter))
            .ThenBy(unit => unit.Id)
            .ToList();
        var reserveCount = Math.Min(3, Math.Max(0, candidates.Count - minimumWaveUnits));
        var reserved = candidates
            .Take(reserveCount)
            .Select(unit => unit.Id)
            .ToHashSet();

        return candidates
            .Where(unit => !reserved.Contains(unit.Id))
            .OrderBy(unit => unit.Position.X)
            .ThenBy(unit => unit.Id)
            .Take(maximumWaveUnits);
    }

    private static IEnumerable<UnitInstance> AvailableCombatUnits(UnitBattlefield battlefield, PlayerSlotId playerSlotId)
    {
        return battlefield.Units
            .Where(unit => unit.PlayerSlotId == playerSlotId)
            .Where(unit => unit.Hp > 0)
            .Where(unit => !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            .Where(unit => unit.AttackTargetId is null || !unit.AttackTargetIsManual);
    }
}
