using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Editor;

public static class MapAuthoringInspectorCatalog
{
    public static bool Handles(GodotObject value)
    {
        return value is MapRoot
            or OwnerStart
            or Building
            or Unit
            or AuthoringResource
            or Obstacle
            or TerrainRegion
            or Trigger
            or Objective
            or Narrative;
    }

    public static bool TryOptions(GodotObject value, string propertyName, out IReadOnlyList<string> options)
    {
        options = value switch
        {
            Building when Matches(propertyName, "BuildingId") => MapAuthoringCatalog.BuildingIds,
            Unit when Matches(propertyName, "DesignId") => MapAuthoringCatalog.UnitIds,
            OwnerStart or Building when Matches(propertyName, "FactionId") => MapAuthoringCatalog.FactionIds,
            TerrainRegion when Matches(propertyName, "TerrainId") => MapAuthoringKeyCatalog.TerrainIds,
            Trigger when Matches(propertyName, "EventKey") => MapAuthoringKeyCatalog.EventKeys,
            Objective when Matches(propertyName, "ObjectiveKey") => MapAuthoringKeyCatalog.ObjectiveKeys,
            Narrative when Matches(propertyName, "TextKey") => MapAuthoringKeyCatalog.NarrativeKeys,
            _ => Array.Empty<string>(),
        };
        return options.Count > 0;
    }

    public static bool IsBuildingRotation(GodotObject value, string propertyName)
    {
        return value is Building && propertyName == "rotation";
    }

    private static bool Matches(string actual, string pascalCase)
    {
        return actual == pascalCase || actual == pascalCase.ToSnakeCase();
    }
}
