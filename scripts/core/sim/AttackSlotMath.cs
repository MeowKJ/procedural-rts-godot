using Godot;

namespace ProceduralRts.Core;

public readonly record struct AttackSlotUnit(int Id, Vector2 Position, float WeaponRange);

public readonly record struct AttackSlotAssignment(int Id, Vector2 Slot, bool IsAnchor);

/// <summary>
/// Range-aware group attack positioning. Units
/// already inside their weapon range become firing anchors and hold position;
/// the rest are distributed onto a ring at ~standoff range around the target so
/// the group surrounds it instead of stacking on its center. Fully deterministic:
/// ordering and tie-breaks key on entity Id.
/// </summary>
public static class AttackSlotMath
{
    private const float StandoffFraction = 0.85f;

    public static IReadOnlyList<AttackSlotAssignment> AssignAttackSlots(
        IReadOnlyList<AttackSlotUnit> units,
        Vector2 targetCenter,
        float targetRadius)
    {
        var assignments = new List<AttackSlotAssignment>(units.Count);
        AssignAttackSlotsInto(
            units,
            targetCenter,
            targetRadius,
            assignments,
            new List<AttackSlotUnit>(units.Count),
            new List<AttackSlotUnit>(units.Count),
            new List<AttackSlotUnit>(units.Count),
            new List<Vector2>(units.Count));
        return assignments;
    }

    public static void AssignAttackSlotsInto(
        IReadOnlyList<AttackSlotUnit> units,
        Vector2 targetCenter,
        float targetRadius,
        List<AttackSlotAssignment> assignments,
        List<AttackSlotUnit> orderedUnits,
        List<AttackSlotUnit> anchors,
        List<AttackSlotUnit> movers,
        List<Vector2> freeSlots)
    {
        assignments.Clear();
        orderedUnits.Clear();
        anchors.Clear();
        movers.Clear();
        freeSlots.Clear();
        if (units.Count == 0)
        {
            return;
        }

        for (var index = 0; index < units.Count; index++)
        {
            orderedUnits.Add(units[index]);
        }

        orderedUnits.Sort(static (left, right) => left.Id.CompareTo(right.Id));

        // Anchors: already within firing range - keep them put so rear units do
        // not shove a firing unit forward.
        foreach (var unit in orderedUnits)
        {
            var firingRange = unit.WeaponRange;
            if (unit.Position.DistanceTo(targetCenter) <= firingRange)
            {
                anchors.Add(unit);
                assignments.Add(new AttackSlotAssignment(unit.Id, unit.Position, IsAnchor: true));
            }
            else
            {
                movers.Add(unit);
            }
        }

        if (movers.Count == 0)
        {
            orderedUnits.Clear();
            anchors.Clear();
            return;
        }

        // Ring radius from the average mover weapon range so a mixed group still
        // forms one readable ring; each mover keeps its own firing range in mind.
        var rangeSum = 0f;
        for (var index = 0; index < movers.Count; index++)
        {
            rangeSum += movers[index].WeaponRange;
        }

        var avgRange = rangeSum / movers.Count;
        var ringRadius = StandoffRadius(avgRange, targetRadius);

        // Reserve anchor bearings, then assign each mover to the remaining ring
        // angle nearest its current bearing.
        var slotCount = movers.Count + anchors.Count;
        for (var i = 0; i < slotCount; i++)
        {
            var angle = MathF.Tau * i / slotCount;
            freeSlots.Add(targetCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ringRadius);
        }

        foreach (var anchor in anchors)
        {
            ReserveNearestSlot(freeSlots, AnchorSlotPoint(anchor, targetCenter, ringRadius));
        }

        while (movers.Count > 0)
        {
            var bestMoverIndex = IndexOfFarthestMover(movers, targetCenter);
            var unit = movers[bestMoverIndex];
            movers.RemoveAt(bestMoverIndex);

            var bestIndex = 0;
            var bestDistSq = float.MaxValue;
            for (var i = 0; i < freeSlots.Count; i++)
            {
                var distSq = unit.Position.DistanceSquaredTo(freeSlots[i]);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            assignments.Add(new AttackSlotAssignment(unit.Id, freeSlots[bestIndex], IsAnchor: false));
            freeSlots.RemoveAt(bestIndex);
        }

        // Stable output order by Id for deterministic downstream consumption.
        assignments.Sort(static (left, right) => left.Id.CompareTo(right.Id));
        orderedUnits.Clear();
        anchors.Clear();
        movers.Clear();
        freeSlots.Clear();
    }

    public static float StandoffRadius(float weaponRange, float targetRadius)
    {
        var range = MathF.Max(weaponRange, 1f);
        var clampedTargetRadius = Math.Clamp(targetRadius, 0f, range * 0.8f);
        var openBand = MathF.Max(range - clampedTargetRadius, range * 0.2f);
        return MathF.Min(range * 0.95f, clampedTargetRadius + (openBand * StandoffFraction));
    }

    private static Vector2 AnchorSlotPoint(AttackSlotUnit anchor, Vector2 targetCenter, float ringRadius)
    {
        var fromTarget = anchor.Position - targetCenter;
        if (fromTarget.LengthSquared() <= 0.0001f)
        {
            var angle = (anchor.Id % 360) * MathF.PI / 180f;
            fromTarget = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        return targetCenter + fromTarget.Normalized() * ringRadius;
    }

    private static void ReserveNearestSlot(List<Vector2> freeSlots, Vector2 desired)
    {
        if (freeSlots.Count == 0)
        {
            return;
        }

        var bestIndex = 0;
        var bestDistSq = float.MaxValue;
        for (var i = 0; i < freeSlots.Count; i++)
        {
            var distSq = freeSlots[i].DistanceSquaredTo(desired);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        freeSlots.RemoveAt(bestIndex);
    }

    private static int IndexOfFarthestMover(List<AttackSlotUnit> movers, Vector2 targetCenter)
    {
        var bestIndex = 0;
        var bestDistSq = float.MinValue;
        var bestId = int.MaxValue;
        for (var index = 0; index < movers.Count; index++)
        {
            var unit = movers[index];
            var distSq = unit.Position.DistanceSquaredTo(targetCenter);
            if (distSq > bestDistSq || (distSq == bestDistSq && unit.Id < bestId))
            {
                bestDistSq = distSq;
                bestId = unit.Id;
                bestIndex = index;
            }
        }

        return bestIndex;
    }
}
