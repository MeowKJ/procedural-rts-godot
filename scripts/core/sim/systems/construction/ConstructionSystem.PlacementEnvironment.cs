using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static void CollectPlacementSnapshot(
        EntityWorld world,
        OwnerId ownerId,
        List<PlacementBuildAnchor> buildAnchors,
        List<PlacementObstacle> obstacles,
        List<PlacementReservationObstacle> reservations,
        List<PlacementResourceObstacle> resourceObstacles,
        List<PlacementBuildVisibility> visibility)
    {
        buildAnchors.Clear();
        obstacles.Clear();
        reservations.Clear();
        resourceObstacles.Clear();
        visibility.Clear();
        for (var obstacleIndex = 0; obstacleIndex < world.MapEnvironment.StaticObstacles.Count; obstacleIndex++)
        {
            var bounds = world.MapEnvironment.StaticObstacles[obstacleIndex].Bounds;
            obstacles.Add(new PlacementObstacle(
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                IsMapEnvironment: true));
        }

        for (var entityIndex = 0; entityIndex < world.OrderedEntities.Count; entityIndex++)
        {
            var entity = world.OrderedEntities[entityIndex];
            if (entity.Components.TryGet<ResourceNodeComponentState>(out _)
                && entity.Components.TryGet<CollisionComponentState>(out var resourceCollision)
                && resourceCollision.Radius > 0)
            {
                resourceObstacles.Add(new PlacementResourceObstacle(
                    entity.Transform.Position.X,
                    entity.Transform.Position.Y,
                    resourceCollision.Radius));
            }

            var isAlive = !entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0;
            BuildSpec? existingSpec = null;
            if (isAlive
                && BuildingSpecIdFor(world, entity) is { } buildingSpecId
                && TryGetBuildSpec(buildingSpecId, out var foundSpec))
            {
                existingSpec = foundSpec;
            }

            if (isAlive && entity.Components.TryGet<FootprintComponentState>(out var footprint))
            {
                var rect = PlacementMath.RectFromCenter(
                    entity.Transform.Position.X,
                    entity.Transform.Position.Y,
                    footprint.Size.X,
                    footprint.Size.Y);
                var clearanceCells = existingSpec?.PlacementClearanceCells ?? 0;
                obstacles.Add(new PlacementObstacle(rect.X, rect.Y, rect.Width, rect.Height, clearanceCells));

                if (existingSpec is not null)
                {
                    PlacementMath.TryNormalizeCardinalFacing(entity.Transform.Facing, out var cardinalFacing);
                    for (var reservationIndex = 0;
                         reservationIndex < existingSpec.PlacementReservations.Count;
                         reservationIndex++)
                    {
                        var reservation = PlacementReservationMath.WorldRect(
                            existingSpec,
                            existingSpec.PlacementReservations[reservationIndex],
                            entity.Transform.Position,
                            cardinalFacing);
                        reservations.Add(new PlacementReservationObstacle(
                            reservation.X,
                            reservation.Y,
                            reservation.Width,
                            reservation.Height,
                            existingSpec.PlacementClearanceCells));
                    }
                }
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
            if (spec.Abilities[index].Kind == AbilityKind.Deploy)
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

        return !entity.Components.TryGet<ConstructionComponentState>(out var construction)
            || construction.Phase == ConstructionPhase.Building && construction.Progress >= 1;
    }

    private static string? EnvironmentPlacementRejectionReason(
        EntityWorld world,
        BuildSpec spec,
        PlacementRect footprint,
        Vector2 candidatePosition,
        float cardinalFacing,
        List<PlacementBuildVisibility> visibility)
    {
        if (!IsTerrainPassable(world, footprint, spec.PlacementDomain))
        {
            return "placement.impassable";
        }

        for (var reservationIndex = 0; reservationIndex < spec.PlacementReservations.Count; reservationIndex++)
        {
            var reservation = PlacementReservationMath.WorldRect(
                spec,
                spec.PlacementReservations[reservationIndex],
                candidatePosition,
                cardinalFacing);
            if (!IsTerrainPassable(world, reservation, spec.PlacementDomain))
            {
                return "placement.impassable";
            }
        }

        if (!HasBuildVisibility(footprint, visibility))
        {
            return "placement.notVisible";
        }

        for (var reservationIndex = 0; reservationIndex < spec.PlacementReservations.Count; reservationIndex++)
        {
            var reservation = PlacementReservationMath.WorldRect(
                spec,
                spec.PlacementReservations[reservationIndex],
                candidatePosition,
                cardinalFacing);
            if (!HasBuildVisibility(reservation, visibility))
            {
                return "placement.notVisible";
            }
        }

        return null;
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

    private static PlacementRect ReservationRect(PlacementReservationObstacle reservation)
    {
        return new PlacementRect(reservation.X, reservation.Y, reservation.Width, reservation.Height);
    }

    private static TerrainLayer TerrainLayerAt(EntityWorld world, float x, float y)
    {
        return world.MapEnvironment.SampleTerrain(x, y, world.WorldWidth, world.WorldHeight).Layer;
    }
}
