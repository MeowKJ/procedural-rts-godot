using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Projection;

static class TypedMapEnvironmentProjection
{
    public static MapResourceNodeSpec Resource(MapRoot root, AuthoringResource node)
    {
        var transform = TypedMapTransformValidation.Circle(root, node);
        return new MapResourceNodeSpec(
            node.Id,
            Point(transform),
            node.Radius,
            node.Amount,
            new MapColor($"#{node.Accent.ToHtml(false)}"));
    }

    public static MapObstacleSpec Obstacle(MapRoot root, Obstacle node)
    {
        return new MapObstacleSpec(node.Id, Rect(root, node, node.Size));
    }

    public static MapTerrainCellSpec Terrain(MapRoot root, TerrainRegion node)
    {
        return new MapTerrainCellSpec(
            node.Id,
            Rect(root, node, node.Size),
            MapAuthoringKeyCatalog.Require(MapAuthoringKeyKind.Terrain, node.TerrainId),
            node.MovementCost,
            node.BlocksLand);
    }

    public static MapTriggerAreaSpec Trigger(MapRoot root, Trigger node)
    {
        return new MapTriggerAreaSpec(
            node.Id,
            Rect(root, node, node.Size),
            MapAuthoringKeyCatalog.Require(MapAuthoringKeyKind.Event, node.EventKey));
    }

    private static MapRect Rect(MapRoot root, Node2D node, Vector2 size)
    {
        var transform = TypedMapTransformValidation.Rect(root, node);
        return new MapRect(transform.Origin.X, transform.Origin.Y, size.X, size.Y);
    }

    private static MapPoint Point(Transform2D transform)
    {
        return new MapPoint(transform.Origin.X, transform.Origin.Y);
    }
}
