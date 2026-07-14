using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    public PlacementResult QueryBuildingPlacement(
        EntityWorld world,
        OwnerId ownerId,
        BuildSpec spec,
        Vector2 desiredPosition,
        float facing,
        ConstructionPlacementIntent intent)
    {
        var isCardinal = PlacementMath.TryNormalizeCardinalFacing(facing, out var cardinalFacing);
        var footprintCells = spec.FootprintCells.Rotated(cardinalFacing);
        var footprintSize = footprintCells.IsValid ? footprintCells.WorldSize : spec.Footprint;
        var snappedX = PlacementMath.SnapAnchor(desiredPosition.X, footprintCells.WidthCells);
        var snappedY = PlacementMath.SnapAnchor(desiredPosition.Y, footprintCells.HeightCells);
        var footprint = PlacementMath.RectFromCenter(snappedX, snappedY, footprintSize.X, footprintSize.Y);

        if (!isCardinal)
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.rotation");
        }

        if (footprint.X < 0
            || footprint.Y < 0
            || footprint.EndX > world.WorldWidth
            || footprint.EndY > world.WorldHeight)
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.outside");
        }

        CollectPlacementSnapshot(
            world,
            ownerId,
            _placementBuildAnchors,
            _placementObstacles,
            _placementVisibility);

        if (intent == ConstructionPlacementIntent.ReadyTicket || RequiresBuildAuthority(spec))
        {
            var authority = BuildAuthorityAt(snappedX, snappedY, footprintSize.X, footprintSize.Y, _placementBuildAnchors);
            if (authority == PlacementBuildAuthority.Unpowered)
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.unpowered");
            }

            if (authority == PlacementBuildAuthority.Outside)
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.outsideBuildRadius");
            }
        }

        if (!IsTerrainPassable(world, footprint, spec.PlacementDomain))
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.impassable");
        }

        if (!HasBuildVisibility(footprint, _placementVisibility))
        {
            return new PlacementResult(snappedX, snappedY, false, "placement.notVisible");
        }

        for (var index = 0; index < _placementObstacles.Count; index++)
        {
            var obstacle = _placementObstacles[index];
            if (Intersects(footprint, ObstacleRect(obstacle)))
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.blocked");
            }
        }

        for (var index = 0; index < _placementObstacles.Count; index++)
        {
            var obstacle = _placementObstacles[index];
            var clearanceCells = Math.Max(spec.PlacementClearanceCells, obstacle.ClearanceCells);
            if (clearanceCells <= 0)
            {
                continue;
            }

            var clearance = clearanceCells * PlacementMath.GridSize;
            var clearanceRect = PlacementMath.RectFromCenter(
                snappedX,
                snappedY,
                footprintSize.X + clearance * 2,
                footprintSize.Y + clearance * 2);
            if (Intersects(clearanceRect, ObstacleRect(obstacle)))
            {
                return new PlacementResult(snappedX, snappedY, false, "placement.clearance");
            }
        }

        return new PlacementResult(snappedX, snappedY, true, "placement.ready");
    }

    private static void CollectPlacementSnapshot(
        EntityWorld world,
        OwnerId ownerId,
        List<PlacementBuildAnchor> buildAnchors,
        List<PlacementObstacle> obstacles,
        List<PlacementBuildVisibility> visibility)
    {
        buildAnchors.Clear();
        obstacles.Clear();
        visibility.Clear();
        for (var entityIndex = 0; entityIndex < world.OrderedEntities.Count; entityIndex++)
        {
            var entity = world.OrderedEntities[entityIndex];
            var isAlive = !entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0;
            if (isAlive && entity.Components.TryGet<FootprintComponentState>(out var footprint))
            {
                var rect = PlacementMath.RectFromCenter(
                    entity.Transform.Position.X,
                    entity.Transform.Position.Y,
                    footprint.Size.X,
                    footprint.Size.Y);
                var clearanceCells = BuildingSpecIdFor(world, entity) is { } kind
                    && TryGetBuildSpec(kind, out var existingSpec)
                        ? existingSpec.PlacementClearanceCells
                        : 0;
                obstacles.Add(new PlacementObstacle(rect.X, rect.Y, rect.Width, rect.Height, clearanceCells));
            }

            if (entity.OwnerId.Value == ownerId.Value
                && entity.Components.TryGet<BuildRadiusComponentState>(out var radius)
                && radius.Radius > 0
                && IsActiveBuildAuthority(world, entity))
            {
                var powered = !entity.Components.TryGet<PowerComponentState>(out var power) || power.Powered;
                buildAnchors.Add(new PlacementBuildAnchor(
                    entity.Transform.Position.X,
                    entity.Transform.Position.Y,
                    radius.Radius,
                    powered));
            }

            if (world.Relations.Relation(ownerId, entity.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied
                && IsLiveBuildVisibilitySource(entity))
            {
                var vision = entity.Components.Require<VisionComponentState>();
                visibility.Add(new PlacementBuildVisibility(
                    entity.Transform.Position.X,
                    entity.Transform.Position.Y,
                    vision.SightRange));
            }
        }
    }

    private static bool IsActiveBuildAuthority(EntityWorld world, EntityInstance entity)
    {
        if (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return false;
        }

        if (BuildingSpecIdFor(world, entity) is not null)
        {
            return IsCompletedAnyBuilding(world, entity);
        }

        if (entity.Components.TryGet<ConstructionComponentState>(out var construction)
            && (construction.Phase != ConstructionPhase.Building || construction.Progress < 1))
        {
            return false;
        }

        if (!IsDeployGatedBuildAuthority(world, entity))
        {
            return true;
        }

        return entity.Components.TryGet<DeployComponentState>(out var deploy)
            && deploy.IsDeployed
            && deploy.SetupRemaining <= 0;
    }

    private static bool IsDeployGatedBuildAuthority(EntityWorld world, EntityInstance entity)
    {
        if (!world.TryGetSpec(entity.SpecId, out var spec))
        {
            return false;
        }

        for (var index = 0; index < spec.Abilities.Count; index++)
        {
            var ability = spec.Abilities[index];
            if (ability.Kind == AbilityKind.Deploy)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLiveBuildVisibilitySource(EntityInstance entity)
    {
        if (!entity.Components.TryGet<VisionComponentState>(out var vision) || vision.SightRange <= 0)
        {
            return false;
        }

        if (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return false;
        }

        if (entity.Components.TryGet<ConstructionComponentState>(out var construction)
            && (construction.Phase != ConstructionPhase.Building || construction.Progress < 1))
        {
            return false;
        }

        return true;
    }

    private enum PlacementBuildAuthority
    {
        Outside,
        Unpowered,
        Powered,
    }

    private static PlacementBuildAuthority BuildAuthorityAt(
        float centerX,
        float centerY,
        float width,
        float height,
        List<PlacementBuildAnchor> buildAnchors)
    {
        var footprintRadius = MathF.Max(width, height) * 0.5f;
        var foundUnpoweredAnchor = false;
        for (var index = 0; index < buildAnchors.Count; index++)
        {
            var anchor = buildAnchors[index];
            if (anchor.Radius <= 0)
            {
                continue;
            }

            var dx = centerX - anchor.X;
            var dy = centerY - anchor.Y;
            var allowed = anchor.Radius + footprintRadius;
            if (dx * dx + dy * dy > allowed * allowed)
            {
                continue;
            }

            if (anchor.Powered)
            {
                return PlacementBuildAuthority.Powered;
            }

            foundUnpoweredAnchor = true;
        }

        return foundUnpoweredAnchor ? PlacementBuildAuthority.Unpowered : PlacementBuildAuthority.Outside;
    }

    private static bool IsTerrainPassable(EntityWorld world, PlacementRect footprint, MovementDomain placementDomain)
    {
        var allowed = TerrainPassability.AllowedLayers(placementDomain);
        if (!IsTerrainPointPassable(world, footprint.X, footprint.Y, allowed)
            || !IsTerrainPointPassable(world, footprint.EndX, footprint.Y, allowed)
            || !IsTerrainPointPassable(world, footprint.X, footprint.EndY, allowed)
            || !IsTerrainPointPassable(world, footprint.EndX, footprint.EndY, allowed)
            || !IsTerrainPointPassable(
                world,
                footprint.X + footprint.Width * 0.5f,
                footprint.Y + footprint.Height * 0.5f,
                allowed))
        {
            return false;
        }

        var xSteps = Math.Max(0, (int)MathF.Ceiling(footprint.Width / PlacementMath.TerrainSampleStep) - 1);
        var ySteps = Math.Max(0, (int)MathF.Ceiling(footprint.Height / PlacementMath.TerrainSampleStep) - 1);
        for (var xStep = 1; xStep <= xSteps; xStep++)
        {
            var x = footprint.X + footprint.Width * xStep / (xSteps + 1);
            if (!IsTerrainPointPassable(world, x, footprint.Y, allowed)
                || !IsTerrainPointPassable(world, x, footprint.EndY, allowed))
            {
                return false;
            }
        }

        for (var yStep = 1; yStep <= ySteps; yStep++)
        {
            var y = footprint.Y + footprint.Height * yStep / (ySteps + 1);
            if (!IsTerrainPointPassable(world, footprint.X, y, allowed)
                || !IsTerrainPointPassable(world, footprint.EndX, y, allowed))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTerrainPointPassable(EntityWorld world, float x, float y, TerrainLayer allowed)
    {
        return (TerrainLayerAt(world, x, y) & allowed) != 0;
    }

    private static bool HasBuildVisibility(PlacementRect footprint, List<PlacementBuildVisibility> visibility)
    {
        if (!PointHasBuildVisibility(footprint.X, footprint.Y, visibility)
            || !PointHasBuildVisibility(footprint.EndX, footprint.Y, visibility)
            || !PointHasBuildVisibility(footprint.X, footprint.EndY, visibility)
            || !PointHasBuildVisibility(footprint.EndX, footprint.EndY, visibility)
            || !PointHasBuildVisibility(
                footprint.X + footprint.Width * 0.5f,
                footprint.Y + footprint.Height * 0.5f,
                visibility))
        {
            return false;
        }

        var xSteps = Math.Max(0, (int)MathF.Ceiling(footprint.Width / PlacementMath.TerrainSampleStep) - 1);
        var ySteps = Math.Max(0, (int)MathF.Ceiling(footprint.Height / PlacementMath.TerrainSampleStep) - 1);
        for (var xStep = 1; xStep <= xSteps; xStep++)
        {
            var x = footprint.X + footprint.Width * xStep / (xSteps + 1);
            if (!PointHasBuildVisibility(x, footprint.Y, visibility)
                || !PointHasBuildVisibility(x, footprint.EndY, visibility))
            {
                return false;
            }
        }

        for (var yStep = 1; yStep <= ySteps; yStep++)
        {
            var y = footprint.Y + footprint.Height * yStep / (ySteps + 1);
            if (!PointHasBuildVisibility(footprint.X, y, visibility)
                || !PointHasBuildVisibility(footprint.EndX, y, visibility))
            {
                return false;
            }
        }

        return true;
    }

    private static bool PointHasBuildVisibility(float x, float y, List<PlacementBuildVisibility> visibility)
    {
        for (var index = 0; index < visibility.Count; index++)
        {
            var source = visibility[index];
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

    private static PlacementRect ObstacleRect(PlacementObstacle obstacle)
    {
        return new PlacementRect(obstacle.X, obstacle.Y, obstacle.Width, obstacle.Height);
    }

    private static bool Intersects(PlacementRect a, PlacementRect b)
    {
        return a.X < b.EndX && a.EndX > b.X && a.Y < b.EndY && a.EndY > b.Y;
    }

    private static TerrainLayer TerrainLayerAt(EntityWorld world, float x, float y)
    {
        var kind = TerrainFloorMath.KindAt(new Vector2(x, y), new Vector2(world.WorldWidth, world.WorldHeight));
        return kind switch
        {
            TerrainFloorKind.Water => TerrainLayer.Water,
            TerrainFloorKind.Coast => TerrainLayer.Coast,
            _ => TerrainLayer.Ground,
        };
    }
}
