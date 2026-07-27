using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Projection;

public static class TypedMapEntityProjection
{
    public static MapOwnerStartSpec Owner(MapRoot root, OwnerStart node)
    {
        var transform = TypedMapTransformValidation.Entity(root, node);
        return new MapOwnerStartSpec(
            new OwnerId(node.OwnerId),
            MapAuthoringCatalog.RequireFaction(node.FactionId),
            Point(transform),
            transform.Rotation,
            node.StartingCredits);
    }

    public static MapBuildingSeedSpec Building(MapRoot root, Building node)
    {
        MapBuildingQuarterTurns.RequirePersisted(node.Rotation);
        var transform = TypedMapTransformValidation.Entity(root, node);
        return new MapBuildingSeedSpec(
            MapAuthoringCatalog.RequireBuilding(node.BuildingId),
            new OwnerId(node.OwnerId),
            MapAuthoringCatalog.RequireFaction(node.FactionId),
            Point(transform),
            MapBuildingQuarterTurns.RequireRootLocal(transform.Rotation),
            node.OverrideHp ? node.Hp : null,
            node.BuildProgress,
            node.HasRuntimeId ? node.RuntimeId : null);
    }

    public static MapUnitSeedSpec Unit(MapRoot root, Unit node)
    {
        var transform = TypedMapTransformValidation.Entity(root, node);
        return new MapUnitSeedSpec(
            MapAuthoringCatalog.RequireUnit(node.DesignId),
            new OwnerId(node.OwnerId),
            Point(transform),
            transform.Rotation);
    }

    public static MapObjectiveNodeSpec Objective(MapRoot root, Objective node)
    {
        return new MapObjectiveNodeSpec(
            node.Id,
            MapSceneProjection.RootLocalPoint(root, node),
            MapAuthoringKeyCatalog.Require(MapAuthoringKeyKind.Objective, node.ObjectiveKey),
            node.Primary);
    }

    public static MapNarrativeNodeSpec Narrative(MapRoot root, Narrative node)
    {
        return new MapNarrativeNodeSpec(
            node.Id,
            MapSceneProjection.RootLocalPoint(root, node),
            MapAuthoringKeyCatalog.Require(MapAuthoringKeyKind.Narrative, node.TextKey),
            string.IsNullOrWhiteSpace(node.TriggerId) ? null : node.TriggerId);
    }

    private static MapPoint Point(Transform2D transform)
    {
        return new MapPoint(transform.Origin.X, transform.Origin.Y);
    }
}
