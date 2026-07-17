using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using ProceduralRts.MapAuthoring.Projection;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Editor;

public static class MapAuthoringOverlayPlanner
{
    public static MapAuthoringOverlayPlan Build(
        MapRoot root,
        MapAuthoringSourceIndex sources,
        MapAuthoringValidationReport? report,
        Node? selected)
    {
        var result = new List<MapOverlayPrimitive>();
        var errors = report?.Diagnostics
            .SelectMany(value => value.Conflict is null
                ? new[] { value.Source.Path }
                : new[] { value.Source.Path, value.Conflict.Path })
            .ToHashSet(StringComparer.Ordinal) ?? [];
        AddWorld(root, result);
        foreach (var entry in sources.Entries.OrderBy(value => value.Source.SceneOrder))
        {
            var path = entry.Path;
            var isSelected = entry.Node == selected;
            var isError = errors.Contains(path.ToString());
            AddNode(root, entry.Node, path, isSelected, isError, result);
        }
        AddBuildings(root, sources, selected, errors, result);
        return new MapAuthoringOverlayPlan(Array.AsReadOnly(result.ToArray()));
    }

    private static void AddWorld(MapRoot root, List<MapOverlayPrimitive> result)
    {
        var path = new NodePath(".");
        var width = Math.Max(0, root.WorldSize.X);
        var height = Math.Max(0, root.WorldSize.Y);
        for (var x = 0f; x <= width && result.Count < 512; x += PlacementMath.GridSize)
            result.Add(Line(MapOverlayPrimitiveKind.Grid, new(x, 0), new(x, height), path));
        for (var y = 0f; y <= height && result.Count < 512; y += PlacementMath.GridSize)
            result.Add(Line(MapOverlayPrimitiveKind.Grid, new(0, y), new(width, y), path));
        result.Add(Rect(MapOverlayPrimitiveKind.World, new Rect2(0, 0, width, height), path));
    }

    private static void AddNode(
        MapRoot root, Node node, NodePath path, bool selected, bool error,
        List<MapOverlayPrimitive> result)
    {
        switch (node)
        {
            case AuthoringResource value:
                result.Add(Circle(MapOverlayPrimitiveKind.ResourceRadius, Point(root, value), value.Radius, path, selected, error)); break;
            case Obstacle value:
                result.Add(Rect(MapOverlayPrimitiveKind.Obstacle, AxisRect(root, value, value.Size), path, selected, error)); break;
            case TerrainRegion value:
                result.Add(Rect(MapOverlayPrimitiveKind.Terrain, AxisRect(root, value, value.Size), path, selected, error)); break;
            case Trigger value:
                result.Add(Rect(MapOverlayPrimitiveKind.Trigger, AxisRect(root, value, value.Size), path, selected, error)); break;
            case OwnerStart value:
                var start = Point(root, value);
                var facing = MapSceneProjection.RootLocalTransform(root, value).Rotation;
                result.Add(Line(MapOverlayPrimitiveKind.OwnerFacing, start, start + Vector2.Right.Rotated(facing) * 72, path, selected, error)); break;
            case Unit value:
                result.Add(Circle(MapOverlayPrimitiveKind.Unit, Point(root, value), 12, path, selected, error)); break;
            case Objective value:
                result.Add(Circle(MapOverlayPrimitiveKind.Objective, Point(root, value), 16, path, selected, error)); break;
            case Narrative value:
                result.Add(Circle(MapOverlayPrimitiveKind.Narrative, Point(root, value), 10, path, selected, error)); break;
        }
    }

    private static void AddBuildings(
        MapRoot root, MapAuthoringSourceIndex sources,
        Node? selected, HashSet<string> errors, List<MapOverlayPrimitive> result)
    {
        var entries = sources.Entries.Where(value => value.Source.Kind == MapValidationSourceKind.Building)
            .OrderBy(value => value.Source.Index).ToArray();
        foreach (var entry in entries)
        {
            var path = entry.Path;
            var isSelected = entry.Node == selected;
            var isError = errors.Contains(path.ToString());
            var node = (Building)entry.Node;
            if (!IsRepresentableBuilding(root, node))
            {
                result.Add(Circle(
                    MapOverlayPrimitiveKind.InvalidBuildingFallback,
                    Point(root, node), PlacementMath.GridSize * 0.75f,
                    path, isSelected, error: true));
                continue;
            }
            var geometry = MapBuildingPlacementGeometry.Create(TypedMapEntityProjection.Building(root, node));
            result.Add(Rect(MapOverlayPrimitiveKind.HardFootprint, ToRect(geometry.Hard), path, isSelected, isError));
            result.Add(Rect(MapOverlayPrimitiveKind.Clearance, ToRect(geometry.Clearance), path, isSelected, isError));
            for (var reservationIndex = 0; reservationIndex < geometry.Reservations.Count; reservationIndex++)
            {
                var kind = geometry.Spec.PlacementReservations[reservationIndex].Kind
                    == PlacementReservationKind.RefineryDock
                    ? MapOverlayPrimitiveKind.RefineryDock
                    : MapOverlayPrimitiveKind.ProductionEgress;
                result.Add(Rect(kind, ToRect(geometry.Reservations[reservationIndex]), path, isSelected, isError));
            }
        }
    }

    private static bool IsRepresentableBuilding(MapRoot root, Building node)
    {
        if (!MapAuthoringCatalog.BuildingIds.Contains(node.BuildingId, StringComparer.Ordinal)
            || !MapAuthoringCatalog.FactionIds.Contains(node.FactionId, StringComparer.Ordinal)
            || !TypedMapTransformValidation.TryReason(root, node, out _)) return false;
        var rootRotation = MapSceneProjection.RootLocalTransform(root, node).Rotation;
        return MapBuildingQuarterTurns.IndexOf(node.Rotation) >= 0
            && MapBuildingQuarterTurns.IndexOf(rootRotation) >= 0;
    }

    private static Vector2 Point(MapRoot root, Node2D node)
        => MapSceneProjection.RootLocalTransform(root, node).Origin;
    private static Rect2 AxisRect(MapRoot root, Node2D node, Vector2 size)
        => new(Point(root, node), size);
    private static Rect2 ToRect(PlacementRect value) => new(value.X, value.Y, value.Width, value.Height);
    private static MapOverlayPrimitive Rect(MapOverlayPrimitiveKind kind, Rect2 rect, NodePath path, bool selected = false, bool error = false)
        => new(kind, rect, default, default, 0, path, selected, error);
    private static MapOverlayPrimitive Circle(MapOverlayPrimitiveKind kind, Vector2 center, float radius, NodePath path, bool selected, bool error)
        => new(kind, default, center, default, radius, path, selected, error);
    private static MapOverlayPrimitive Line(MapOverlayPrimitiveKind kind, Vector2 start, Vector2 end, NodePath path, bool selected = false, bool error = false)
        => new(kind, default, start, end, 0, path, selected, error);
}
