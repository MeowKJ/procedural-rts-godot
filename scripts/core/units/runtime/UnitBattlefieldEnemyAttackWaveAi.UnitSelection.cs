using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private static readonly Comparison<UnitInstance> WaveLaunchOrderComparison = CompareUnitsByXThenId;

    private void CollectAvailableDefenseUnits(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        Vector2 targetPosition,
        int maximumDefenders,
        List<UnitInstance> result)
    {
        result.Clear();
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        var defenseRadiusSquared = DefenseRadius * DefenseRadius;
        foreach (var unit in battlefield.Units)
        {
            if (!IsAvailableCombatUnit(unit, playerSlotId))
            {
                continue;
            }

            if (unit.Position.DistanceSquaredTo(baseCenter) > defenseRadiusSquared
                && unit.Position.DistanceSquaredTo(targetPosition) > defenseRadiusSquared)
            {
                continue;
            }

            result.Add(unit);
        }

        _unitDistanceComparer.Reset(targetPosition);
        result.Sort(_unitDistanceComparer);
        TrimToMax(result, maximumDefenders);
    }

    private void CollectAvailableWaveUnits(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        int minimumWaveUnits,
        int maximumWaveUnits,
        List<UnitInstance> result)
    {
        result.Clear();
        _waveCandidateUnits.Clear();
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        foreach (var unit in battlefield.Units)
        {
            if (IsAvailableCombatUnit(unit, playerSlotId))
            {
                _waveCandidateUnits.Add(unit);
            }
        }

        _unitDistanceComparer.Reset(baseCenter);
        _waveCandidateUnits.Sort(_unitDistanceComparer);
        var reserveCount = Math.Min(3, Math.Max(0, _waveCandidateUnits.Count - minimumWaveUnits));
        for (var index = reserveCount; index < _waveCandidateUnits.Count; index++)
        {
            result.Add(_waveCandidateUnits[index]);
        }

        result.Sort(WaveLaunchOrderComparison);
        TrimToMax(result, maximumWaveUnits);
    }

    private static void CollectUnitIds(IReadOnlyList<UnitInstance> units, List<int> result)
    {
        result.Clear();
        foreach (var unit in units)
        {
            result.Add(unit.Id);
        }
    }

    private static bool IsAvailableCombatUnit(UnitInstance unit, PlayerSlotId playerSlotId)
    {
        return unit.PlayerSlotId == playerSlotId
            && unit.Hp > 0
            && !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
            && (unit.AttackTargetId is null || !unit.AttackTargetIsManual);
    }

    private static int CompareUnitsByXThenId(UnitInstance? left, UnitInstance? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return 1;
        }

        if (right is null)
        {
            return -1;
        }

        var xCompare = left.Position.X.CompareTo(right.Position.X);
        return xCompare != 0 ? xCompare : left.Id.CompareTo(right.Id);
    }

    private static int CompareUnitsByDistanceThenId(UnitInstance? left, UnitInstance? right, Vector2 origin)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return 1;
        }

        if (right is null)
        {
            return -1;
        }

        var distanceCompare = left.Position.DistanceSquaredTo(origin).CompareTo(right.Position.DistanceSquaredTo(origin));
        return distanceCompare != 0 ? distanceCompare : left.Id.CompareTo(right.Id);
    }

    private static void TrimToMax<T>(List<T> result, int maximum)
    {
        if (result.Count > maximum)
        {
            result.RemoveRange(maximum, result.Count - maximum);
        }
    }

    private sealed class UnitDistanceComparer : IComparer<UnitInstance>
    {
        private Vector2 _origin;

        public void Reset(Vector2 origin)
        {
            _origin = origin;
        }

        public int Compare(UnitInstance? left, UnitInstance? right)
        {
            return CompareUnitsByDistanceThenId(left, right, _origin);
        }
    }
}
