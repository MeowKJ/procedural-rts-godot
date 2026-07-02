using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static IReadOnlyList<PlacementBuildAnchor> BuildAnchors(EntityWorld world, OwnerId ownerId)
    {
        return world.OrderedEntities
            .Where(entity => entity.OwnerId.Value == ownerId.Value
                && entity.Components.TryGet<BuildRadiusComponentState>(out var radius)
                && radius.Radius > 0
                && IsActiveBuildAuthority(world, entity))
            .Select(entity =>
            {
                var radius = entity.Components.Require<BuildRadiusComponentState>();
                var powered = !entity.Components.TryGet<PowerComponentState>(out var power) || power.Powered;
                return new PlacementBuildAnchor(entity.Transform.Position.X, entity.Transform.Position.Y, radius.Radius, powered);
            })
            .ToList();
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
        return world.TryGetSpec(entity.SpecId, out var spec)
            && spec.Abilities.Any(ability => ability.Kind == AbilityKind.Deploy);
    }

    private static IReadOnlyList<PlacementBuildVisibility> BuildVisibilitySources(EntityWorld world, OwnerId ownerId)
    {
        return world.OrderedEntities
            .Where(entity => world.Relations.Relation(ownerId, entity.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied
                && IsLiveBuildVisibilitySource(entity))
            .Select(entity =>
            {
                var vision = entity.Components.Require<VisionComponentState>();
                return new PlacementBuildVisibility(entity.Transform.Position.X, entity.Transform.Position.Y, vision.SightRange);
            })
            .ToList();
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

    private static IReadOnlyList<PlacementObstacle> FootprintObstacles(EntityWorld world)
    {
        return world.OrderedEntities
            .Where(entity => entity.Components.TryGet<FootprintComponentState>(out _)
                && (!entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0))
            .Select(entity =>
            {
                var footprint = entity.Components.Require<FootprintComponentState>();
                var rect = PlacementMath.RectFromCenter(
                    entity.Transform.Position.X,
                    entity.Transform.Position.Y,
                    footprint.Size.X,
                    footprint.Size.Y);
                return new PlacementObstacle(rect.X, rect.Y, rect.Width, rect.Height);
            })
            .ToList();
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
