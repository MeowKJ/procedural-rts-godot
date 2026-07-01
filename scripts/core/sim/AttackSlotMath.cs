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
        if (units.Count == 0)
        {
            return assignments;
        }

        // Anchors: already within firing range - keep them put so rear units do
        // not shove a firing unit forward.
        var anchors = new List<AttackSlotUnit>();
        var movers = new List<AttackSlotUnit>();
        foreach (var unit in units.OrderBy(unit => unit.Id))
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
            return assignments;
        }

        // Ring radius from the average mover weapon range so a mixed group still
        // forms one readable ring; each mover keeps its own firing range in mind.
        var avgRange = movers.Average(unit => unit.WeaponRange);
        var ringRadius = StandoffRadius(avgRange, targetRadius);

        // Reserve anchor bearings, then assign each mover to the remaining ring
        // angle nearest its current bearing.
        var slotCount = movers.Count + anchors.Count;
        var freeSlots = new List<Vector2>(slotCount);
        for (var i = 0; i < slotCount; i++)
        {
            var angle = MathF.Tau * i / slotCount;
            freeSlots.Add(targetCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ringRadius);
        }

        foreach (var anchor in anchors.OrderBy(unit => unit.Id))
        {
            ReserveNearestSlot(freeSlots, AnchorSlotPoint(anchor, targetCenter, ringRadius));
        }

        foreach (var unit in movers
            .OrderByDescending(unit => unit.Position.DistanceTo(targetCenter))
            .ThenBy(unit => unit.Id))
        {
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
        return assignments.OrderBy(a => a.Id).ToList();
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
}
