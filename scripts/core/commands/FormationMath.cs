namespace ProceduralRts.Core;

public readonly record struct FormationUnit(int Id, float X, float Y, float Radius);

public readonly record struct FormationDestination(int Id, float X, float Y);

public static class FormationMath
{
    public static IReadOnlyList<FormationDestination> CreateMoveDestinations(
        IReadOnlyList<FormationUnit> units,
        float targetX,
        float targetY,
        float worldWidth,
        float worldHeight)
    {
        var destinations = new List<FormationDestination>(units.Count);
        CreateMoveDestinationsInto(
            units,
            targetX,
            targetY,
            worldWidth,
            worldHeight,
            destinations,
            new List<FormationUnit>(units.Count),
            new List<(float X, float Y)>(units.Count),
            new List<(float X, float Y)>(units.Count));
        return destinations;
    }

    public static void CreateMoveDestinationsInto(
        IReadOnlyList<FormationUnit> units,
        float targetX,
        float targetY,
        float worldWidth,
        float worldHeight,
        List<FormationDestination> destinations,
        List<FormationUnit> orderedUnits,
        List<(float X, float Y)> slots,
        List<(float X, float Y)> remainingSlots)
    {
        destinations.Clear();
        orderedUnits.Clear();
        slots.Clear();
        remainingSlots.Clear();
        if (units.Count == 0)
        {
            return;
        }

        if (units.Count == 1)
        {
            var unit = units[0];
            var destination = ClampToWorld(targetX, targetY, unit.Radius, worldWidth, worldHeight);
            destinations.Add(new FormationDestination(unit.Id, destination.X, destination.Y));
            return;
        }

        var maxRadius = 0f;
        for (var index = 0; index < units.Count; index++)
        {
            maxRadius = MathF.Max(maxRadius, units[index].Radius);
            orderedUnits.Add(units[index]);
        }

        var spacing = MathF.Max(52, maxRadius * 2 + 18);
        var columns = (int)MathF.Ceiling(MathF.Sqrt(units.Count));
        var rows = (int)MathF.Ceiling(units.Count / (float)columns);

        for (var index = 0; index < units.Count; index++)
        {
            var col = index % columns;
            var row = index / columns;
            var offsetX = (col - (columns - 1) / 2f) * spacing;
            var offsetY = (row - (rows - 1) / 2f) * spacing;
            var slot = (targetX + offsetX, targetY + offsetY);
            slots.Add(slot);
            remainingSlots.Add(slot);
        }

        while (orderedUnits.Count > 0)
        {
            var unitIndex = IndexOfFarthestUnit(orderedUnits, targetX, targetY);
            var unit = orderedUnits[unitIndex];
            orderedUnits.RemoveAt(unitIndex);

            var slotIndex = IndexOfNearestSlot(remainingSlots, unit);
            var nearestSlot = remainingSlots[slotIndex];
            remainingSlots.RemoveAt(slotIndex);

            var clamped = ClampToWorld(nearestSlot.X, nearestSlot.Y, unit.Radius, worldWidth, worldHeight);
            destinations.Add(new FormationDestination(unit.Id, clamped.X, clamped.Y));
        }

        orderedUnits.Clear();
        slots.Clear();
        remainingSlots.Clear();
    }

    private static (float X, float Y) ClampToWorld(float x, float y, float radius, float worldWidth, float worldHeight)
    {
        var margin = MathF.Max(80, radius + 28);
        return (
            Math.Clamp(x, margin, worldWidth - margin),
            Math.Clamp(y, margin, worldHeight - margin)
        );
    }

    private static int IndexOfFarthestUnit(List<FormationUnit> units, float targetX, float targetY)
    {
        var bestIndex = 0;
        var bestDistSq = float.MinValue;
        var bestId = int.MaxValue;
        for (var index = 0; index < units.Count; index++)
        {
            var unit = units[index];
            var distSq = DistanceSquared(unit.X, unit.Y, targetX, targetY);
            if (distSq > bestDistSq || (distSq == bestDistSq && unit.Id < bestId))
            {
                bestDistSq = distSq;
                bestId = unit.Id;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static int IndexOfNearestSlot(List<(float X, float Y)> remainingSlots, FormationUnit unit)
    {
        var bestIndex = 0;
        var bestDistSq = float.MaxValue;
        var bestY = float.MaxValue;
        var bestX = float.MaxValue;
        for (var index = 0; index < remainingSlots.Count; index++)
        {
            var slot = remainingSlots[index];
            var distSq = DistanceSquared(unit.X, unit.Y, slot.X, slot.Y);
            if (distSq < bestDistSq
                || (distSq == bestDistSq && (slot.Y < bestY || (slot.Y == bestY && slot.X < bestX))))
            {
                bestDistSq = distSq;
                bestY = slot.Y;
                bestX = slot.X;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static float DistanceSquared(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return dx * dx + dy * dy;
    }
}
