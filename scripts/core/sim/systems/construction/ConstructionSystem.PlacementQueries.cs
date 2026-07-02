using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static void BuildAnchors(EntityWorld world, OwnerId ownerId, List<PlacementBuildAnchor> result)
    {
        result.Clear();
        foreach (var entity in world.OrderedEntities)
        {
            if (entity.OwnerId.Value != ownerId.Value
                || !entity.Components.TryGet<BuildRadiusComponentState>(out var radius)
                || radius.Radius <= 0
                || !IsActiveBuildAuthority(world, entity))
            {
                continue;
            }

            var powered = !entity.Components.TryGet<PowerComponentState>(out var power) || power.Powered;
            result.Add(new PlacementBuildAnchor(entity.Transform.Position.X, entity.Transform.Position.Y, radius.Radius, powered));
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

        foreach (var ability in spec.Abilities)
        {
            if (ability.Kind == AbilityKind.Deploy)
            {
                return true;
            }
        }

        return false;
    }

    private static void BuildVisibilitySources(EntityWorld world, OwnerId ownerId, List<PlacementBuildVisibility> result)
    {
        result.Clear();
        foreach (var entity in world.OrderedEntities)
        {
            if (world.Relations.Relation(ownerId, entity.OwnerId) is not (PlayerRelation.Self or PlayerRelation.Allied)
                || !IsLiveBuildVisibilitySource(entity))
            {
                continue;
            }

            var vision = entity.Components.Require<VisionComponentState>();
            result.Add(new PlacementBuildVisibility(entity.Transform.Position.X, entity.Transform.Position.Y, vision.SightRange));
        }
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

    private static void FootprintObstacles(EntityWorld world, List<PlacementObstacle> result)
    {
        result.Clear();
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<FootprintComponentState>(out var footprint)
                || (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0))
            {
                continue;
            }

            var rect = PlacementMath.RectFromCenter(
                entity.Transform.Position.X,
                entity.Transform.Position.Y,
                footprint.Size.X,
                footprint.Size.Y);
            result.Add(new PlacementObstacle(rect.X, rect.Y, rect.Width, rect.Height));
        }
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
