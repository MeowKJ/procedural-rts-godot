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
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        battlefield.CollectAvailableCombatUnitsNearEither(playerSlotId, baseCenter, targetPosition, DefenseRadius, result);
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
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        battlefield.CollectAvailableCombatUnits(playerSlotId, _waveCandidateUnits);

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

    private static void CollectEntityIds(IReadOnlyList<UnitInstance> units, List<EntityId> result)
    {
        result.Clear();
        foreach (var unit in units)
        {
            result.Add(unit.EntityId);
        }
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
