using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using ProceduralRts.MapAuthoring.Projection;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed record MapAuthoringValidationReport(
    ulong RootInstanceId,
    string ScenePath,
    long Generation,
    MapAuthoringSourceIndex Sources,
    IReadOnlyList<MapValidationDiagnostic> Diagnostics);

public static class MapAuthoringValidationRunner
{
    public static MapAuthoringValidationReport Validate(MapRoot root, long generation = 0)
    {
        var index = MapAuthoringSourceIndex.Build(root);
        var diagnostics = Preflight(root, index);
        if (diagnostics.Count == 0)
        {
            var map = TypedMapSceneProjector.Instance.Project(root);
            diagnostics.AddRange(MapValidationService.Validate(map));
        }
        var enriched = diagnostics.Select(value => value with
        {
            Source = index.Resolve(value.Source),
            Conflict = value.Conflict is null ? null : index.Resolve(value.Conflict),
        });
        return new MapAuthoringValidationReport(
            root.GetInstanceId(), root.SceneFilePath, generation, index, MapValidationOrdering.Sort(enriched));
    }

    private static List<MapValidationDiagnostic> Preflight(MapRoot root, MapAuthoringSourceIndex index)
    {
        var diagnostics = new List<MapValidationDiagnostic>();
        foreach (var node in MapSceneProjection.SceneOrder(root).Where(value => value.HasMeta("map_kind")))
        {
            var source = index.Entries.FirstOrDefault(value => value.Node == node)?.Source
                ?? index.Resolve(new MapValidationSource(MapValidationSourceKind.Root, 0, root.Id));
            diagnostics.Add(MapValidationService.UnrepresentableTransform(source, "legacy_metadata"));
        }
        foreach (var entry in index.Entries.OrderBy(value => value.Source.SceneOrder))
        {
            if (entry.Node.HasMeta("map_kind")) continue;
            if (entry.Node is Node2D node2D
                && !TypedMapTransformValidation.TryReason(root, node2D, out var reason))
            {
                diagnostics.Add(MapValidationService.UnrepresentableTransform(entry.Source, reason));
                continue;
            }
            if (entry.Node is Building building)
            {
                var rootRotation = MapSceneProjection.RootLocalTransform(root, building).Rotation;
                if (MapBuildingQuarterTurns.IndexOf(building.Rotation) < 0
                    || MapBuildingQuarterTurns.IndexOf(rootRotation) < 0)
                {
                    diagnostics.Add(MapValidationService.AuthoringDiagnostic(
                        MapValidationCodes.RotationNonCardinal, entry.Source, "non_cardinal"));
                    continue;
                }
            }
            foreach (var unknown in UnknownCatalogValues(entry.Node))
            {
                diagnostics.Add(MapValidationService.AuthoringDiagnostic(
                    MapValidationCodes.CatalogUnknown, entry.Source, unknown));
            }
        }
        return diagnostics;
    }

    private static IEnumerable<string> UnknownCatalogValues(Node node)
    {
        switch (node)
        {
            case OwnerStart value when !Known(MapAuthoringCatalog.FactionIds, value.FactionId):
                yield return $"faction={value.FactionId}"; break;
            case Building value:
                if (!Known(MapAuthoringCatalog.BuildingIds, value.BuildingId)) yield return $"building={value.BuildingId}";
                if (!Known(MapAuthoringCatalog.FactionIds, value.FactionId)) yield return $"faction={value.FactionId}";
                break;
            case Unit value when !Known(MapAuthoringCatalog.UnitIds, value.DesignId):
                yield return $"unit={value.DesignId}"; break;
            case TerrainRegion value when !Known(MapAuthoringKeyCatalog.TerrainIds, value.TerrainId):
                yield return $"terrain={value.TerrainId}"; break;
            case Trigger value when !Known(MapAuthoringKeyCatalog.EventKeys, value.EventKey):
                yield return $"event={value.EventKey}"; break;
            case Objective value when !Known(MapAuthoringKeyCatalog.ObjectiveKeys, value.ObjectiveKey):
                yield return $"objective={value.ObjectiveKey}"; break;
            case Narrative value when !Known(MapAuthoringKeyCatalog.NarrativeKeys, value.TextKey):
                yield return $"narrative={value.TextKey}"; break;
        }
    }

    private static bool Known(IReadOnlyList<string> options, string value)
        => options.Contains(value, StringComparer.Ordinal);
}
