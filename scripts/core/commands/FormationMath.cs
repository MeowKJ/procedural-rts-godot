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
        if (units.Count == 0)
        {
            return [];
        }

        if (units.Count == 1)
        {
            var unit = units[0];
            var destination = ClampToWorld(targetX, targetY, unit.Radius, worldWidth, worldHeight);
            return [new FormationDestination(unit.Id, destination.X, destination.Y)];
        }

        var rawDestinations = BuildCompactFormation(units, targetX, targetY);

        return rawDestinations
            .Select(destination =>
            {
                var unit = units.First(unit => unit.Id == destination.Id);
                var clamped = ClampToWorld(destination.X, destination.Y, unit.Radius, worldWidth, worldHeight);
                return new FormationDestination(destination.Id, clamped.X, clamped.Y);
            })
            .ToList();
    }

    private static IReadOnlyList<FormationDestination> BuildCompactFormation(
        IReadOnlyList<FormationUnit> units,
        float targetX,
        float targetY)
    {
        var maxRadius = units.Max(unit => unit.Radius);
        var spacing = MathF.Max(52, maxRadius * 2 + 18);
        var columns = (int)MathF.Ceiling(MathF.Sqrt(units.Count));
        var rows = (int)MathF.Ceiling(units.Count / (float)columns);
        var slots = new List<(float X, float Y)>(units.Count);

        for (var index = 0; index < units.Count; index++)
        {
            var col = index % columns;
            var row = index / columns;
            var offsetX = (col - (columns - 1) / 2f) * spacing;
            var offsetY = (row - (rows - 1) / 2f) * spacing;
            slots.Add((targetX + offsetX, targetY + offsetY));
        }

        var remainingSlots = slots.ToList();
        var destinations = new List<FormationDestination>(units.Count);
        foreach (var unit in units
            .OrderByDescending(unit => Distance(unit.X, unit.Y, targetX, targetY))
            .ThenBy(unit => unit.Id))
        {
            var nearestSlot = remainingSlots
                .OrderBy(slot => Distance(unit.X, unit.Y, slot.X, slot.Y))
                .ThenBy(slot => slot.Y)
                .ThenBy(slot => slot.X)
                .First();
            remainingSlots.Remove(nearestSlot);
            destinations.Add(new FormationDestination(unit.Id, nearestSlot.X, nearestSlot.Y));
        }

        return destinations;
    }

    private static (float X, float Y) ClampToWorld(float x, float y, float radius, float worldWidth, float worldHeight)
    {
        var margin = MathF.Max(80, radius + 28);
        return (
            Math.Clamp(x, margin, worldWidth - margin),
            Math.Clamp(y, margin, worldHeight - margin)
        );
    }

    private static float Distance(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
