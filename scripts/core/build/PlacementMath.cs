using Godot;

namespace ProceduralRts.Core;

public readonly record struct PlacementRect(float X, float Y, float Width, float Height)
{
    public float EndX => X + Width;
    public float EndY => Y + Height;
}

public readonly record struct PlacementObstacle(
    float X,
    float Y,
    float Width,
    float Height,
    int ClearanceCells = 0,
    bool IsMapEnvironment = false);

public readonly record struct PlacementReservationObstacle(
    float X,
    float Y,
    float Width,
    float Height,
    int ClearanceCells = 0);

public readonly record struct PlacementResourceObstacle(float X, float Y, float Radius);

public readonly record struct PlacementBuildAnchor(float X, float Y, float Radius, bool Powered = true);

public readonly record struct PlacementBuildVisibility(float X, float Y, float Radius, bool AllowsConstruction = true);

public readonly record struct PlacementResult(float X, float Y, bool IsValid, string Reason);

public enum ConstructionPlacementIntent
{
    Direct,
    ReadyTicket,
}

public readonly record struct PlacementGridFootprint(int WidthCells, int HeightCells)
{
    public bool IsValid => WidthCells > 0 && HeightCells > 0;

    public Vector2 WorldSize => new(
        WidthCells * PlacementMath.GridSize,
        HeightCells * PlacementMath.GridSize);

    public PlacementGridFootprint Rotated(float facing)
    {
        var quarterTurns = (int)MathF.Round(facing / (MathF.PI * 0.5f));
        quarterTurns = ((quarterTurns % 4) + 4) % 4;
        return quarterTurns % 2 == 0
            ? this
            : new PlacementGridFootprint(HeightCells, WidthCells);
    }
}

public static class PlacementMath
{
    public const float GridSize = 32;
    public const float TerrainSampleStep = 48;
    public const float CardinalFacingTolerance = 0.0001f;

    public static bool TryNormalizeCardinalFacing(
        float facing,
        out float cardinalFacing,
        float tolerance = CardinalFacingTolerance)
    {
        cardinalFacing = 0;
        if (!float.IsFinite(facing) || tolerance < 0)
        {
            return false;
        }

        var quarterTurn = MathF.PI * 0.5f;
        var quarterTurns = (int)MathF.Round(facing / quarterTurn);
        quarterTurns = ((quarterTurns % 4) + 4) % 4;
        cardinalFacing = quarterTurns * quarterTurn;

        var delta = facing - cardinalFacing;
        var angularDistance = MathF.Abs(MathF.Atan2(MathF.Sin(delta), MathF.Cos(delta)));
        return angularDistance <= tolerance;
    }

    public static PlacementResult Validate(
        float desiredX,
        float desiredY,
        float width,
        float height,
        float worldWidth,
        float worldHeight,
        IReadOnlyList<PlacementObstacle> obstacles,
        float gridSize = GridSize,
        float padding = 12,
        PlacementGridFootprint? logicalFootprint = null,
        float facing = 0)
    {
        var placedFootprint = logicalFootprint is { IsValid: true } gridFootprint
            ? gridFootprint.Rotated(facing)
            : default;
        if (placedFootprint.IsValid)
        {
            var logicalSize = placedFootprint.WorldSize;
            width = logicalSize.X;
            height = logicalSize.Y;
        }

        var snappedX = placedFootprint.IsValid
            ? SnapAnchor(desiredX, placedFootprint.WidthCells, gridSize)
            : Snap(desiredX, gridSize);
        var snappedY = placedFootprint.IsValid
            ? SnapAnchor(desiredY, placedFootprint.HeightCells, gridSize)
            : Snap(desiredY, gridSize);
        var candidate = RectFromCenter(snappedX, snappedY, width + padding * 2, height + padding * 2);

        if (candidate.X < 0 || candidate.Y < 0 || candidate.EndX > worldWidth || candidate.EndY > worldHeight)
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.outside");
        }

        foreach (var obstacle in obstacles)
        {
            var obstacleRect = new PlacementRect(obstacle.X, obstacle.Y, obstacle.Width, obstacle.Height);
            if (Intersects(candidate, obstacleRect))
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.blocked");
            }
        }

        return new PlacementResult(snappedX, snappedY, true, "placement.ready");
    }

    public static PlacementResult ValidateBuildableArea(
        float desiredX,
        float desiredY,
        float width,
        float height,
        float worldWidth,
        float worldHeight,
        MovementDomain placementDomain,
        IReadOnlyList<PlacementBuildAnchor> buildAnchors,
        IReadOnlyList<PlacementObstacle> obstacles,
        Func<float, float, TerrainLayer>? terrainAt = null,
        bool requiresBuildAuthority = true,
        IReadOnlyList<PlacementBuildVisibility>? buildVisibility = null,
        bool requiresBuildVisibility = false,
        float gridSize = GridSize,
        float padding = 0,
        PlacementGridFootprint? logicalFootprint = null,
        float facing = 0)
    {
        var placedFootprint = logicalFootprint is { IsValid: true } gridFootprint
            ? gridFootprint.Rotated(facing)
            : default;
        if (placedFootprint.IsValid)
        {
            var logicalSize = placedFootprint.WorldSize;
            width = logicalSize.X;
            height = logicalSize.Y;
        }

        var snappedX = placedFootprint.IsValid
            ? SnapAnchor(desiredX, placedFootprint.WidthCells, gridSize)
            : Snap(desiredX, gridSize);
        var snappedY = placedFootprint.IsValid
            ? SnapAnchor(desiredY, placedFootprint.HeightCells, gridSize)
            : Snap(desiredY, gridSize);
        var footprint = RectFromCenter(snappedX, snappedY, width, height);
        var candidate = RectFromCenter(snappedX, snappedY, width + padding * 2, height + padding * 2);

        if (candidate.X < 0 || candidate.Y < 0 || candidate.EndX > worldWidth || candidate.EndY > worldHeight)
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.outside");
        }

        if (requiresBuildAuthority)
        {
            var authority = BuildAuthorityAt(snappedX, snappedY, width, height, buildAnchors);
            if (authority == BuildAuthority.Unpowered)
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.unpowered");
            }

            if (authority == BuildAuthority.Outside)
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.outsideBuildRadius");
            }
        }

        if (!IsTerrainPassable(footprint, placementDomain, terrainAt))
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.impassable");
        }

        if (requiresBuildVisibility && !HasBuildVisibility(footprint, buildVisibility ?? Array.Empty<PlacementBuildVisibility>()))
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.notVisible");
        }

        foreach (var obstacle in obstacles)
        {
            var obstacleRect = new PlacementRect(obstacle.X, obstacle.Y, obstacle.Width, obstacle.Height);
            if (Intersects(candidate, obstacleRect))
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.blocked");
            }
        }

        return new PlacementResult(snappedX, snappedY, true, "placement.ready");
    }

    public static PlacementRect RectFromCenter(float centerX, float centerY, float width, float height)
    {
        return new PlacementRect(centerX - width / 2f, centerY - height / 2f, width, height);
    }

    public static bool Intersects(PlacementRect first, PlacementRect second)
    {
        return first.X < second.EndX
            && first.EndX > second.X
            && first.Y < second.EndY
            && first.EndY > second.Y;
    }

    public static bool ViolatesClearance(PlacementRect first, PlacementRect second, float clearance)
    {
        if (Intersects(first, second))
        {
            return true;
        }

        if (clearance <= 0)
        {
            return false;
        }

        return AxisGap(first.X, first.EndX, second.X, second.EndX) < clearance
            && AxisGap(first.Y, first.EndY, second.Y, second.EndY) < clearance;
    }

    public static bool ViolatesClearance(
        PlacementRect rect,
        PlacementResourceObstacle resource,
        float clearance)
    {
        var closestX = Math.Clamp(resource.X, rect.X, rect.EndX);
        var closestY = Math.Clamp(resource.Y, rect.Y, rect.EndY);
        var dx = resource.X - closestX;
        var dy = resource.Y - closestY;
        var requiredDistance = MathF.Max(0, resource.Radius) + MathF.Max(0, clearance);
        return dx * dx + dy * dy < requiredDistance * requiredDistance;
    }

    public static bool HasBuildVisibility(
        PlacementRect footprint,
        IReadOnlyList<PlacementBuildVisibility> buildVisibility)
    {
        foreach (var point in SampleFootprint(footprint))
        {
            if (!PointHasBuildVisibility(point.X, point.Y, buildVisibility))
            {
                return false;
            }
        }

        return true;
    }

    private static float Snap(float value, float gridSize)
    {
        return MathF.Round(value / gridSize) * gridSize;
    }

    public static float SnapAnchor(float value, int cellCount, float gridSize = GridSize)
    {
        if (cellCount <= 0 || gridSize <= 0)
        {
            return value;
        }

        var parityOffset = cellCount % 2 == 0 ? 0 : gridSize * 0.5f;
        return MathF.Round((value - parityOffset) / gridSize) * gridSize + parityOffset;
    }

    private enum BuildAuthority
    {
        Outside,
        Unpowered,
        Powered,
    }

    private static BuildAuthority BuildAuthorityAt(
        float centerX,
        float centerY,
        float width,
        float height,
        IReadOnlyList<PlacementBuildAnchor> buildAnchors)
    {
        var footprintRadius = MathF.Max(width, height) * 0.5f;
        var foundUnpoweredAnchor = false;
        foreach (var anchor in buildAnchors)
        {
            if (anchor.Radius <= 0)
            {
                continue;
            }

            var dx = centerX - anchor.X;
            var dy = centerY - anchor.Y;
            var allowed = anchor.Radius + footprintRadius;
            if (dx * dx + dy * dy <= allowed * allowed)
            {
                if (anchor.Powered)
                {
                    return BuildAuthority.Powered;
                }

                foundUnpoweredAnchor = true;
            }
        }

        return foundUnpoweredAnchor ? BuildAuthority.Unpowered : BuildAuthority.Outside;
    }

    private static bool IsTerrainPassable(
        PlacementRect footprint,
        MovementDomain placementDomain,
        Func<float, float, TerrainLayer>? terrainAt)
    {
        if (terrainAt is null)
        {
            return TerrainPassability.AllowedLayers(placementDomain).HasFlag(TerrainLayer.Ground);
        }

        var allowed = TerrainPassability.AllowedLayers(placementDomain);
        foreach (var point in SampleFootprint(footprint))
        {
            if ((terrainAt(point.X, point.Y) & allowed) == 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool PointHasBuildVisibility(
        float x,
        float y,
        IReadOnlyList<PlacementBuildVisibility> buildVisibility)
    {
        foreach (var source in buildVisibility)
        {
            if (!source.AllowsConstruction || source.Radius <= 0)
            {
                continue;
            }

            var dx = x - source.X;
            var dy = y - source.Y;
            if (dx * dx + dy * dy <= source.Radius * source.Radius)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<(float X, float Y)> SampleFootprint(PlacementRect footprint)
    {
        yield return (footprint.X, footprint.Y);
        yield return (footprint.EndX, footprint.Y);
        yield return (footprint.X, footprint.EndY);
        yield return (footprint.EndX, footprint.EndY);
        yield return (footprint.X + footprint.Width * 0.5f, footprint.Y + footprint.Height * 0.5f);

        var xSteps = Math.Max(0, (int)MathF.Ceiling(footprint.Width / TerrainSampleStep) - 1);
        var ySteps = Math.Max(0, (int)MathF.Ceiling(footprint.Height / TerrainSampleStep) - 1);
        for (var xStep = 1; xStep <= xSteps; xStep++)
        {
            var x = footprint.X + footprint.Width * xStep / (xSteps + 1);
            yield return (x, footprint.Y);
            yield return (x, footprint.EndY);
        }

        for (var yStep = 1; yStep <= ySteps; yStep++)
        {
            var y = footprint.Y + footprint.Height * yStep / (ySteps + 1);
            yield return (footprint.X, y);
            yield return (footprint.EndX, y);
        }
    }

    private static float AxisGap(float startA, float endA, float startB, float endB)
    {
        if (endA <= startB)
        {
            return startB - endA;
        }

        return endB <= startA ? startA - endB : 0;
    }
}
