using System.Text.Json;

namespace ProceduralRts.Core;

static partial class MapSpecArtifactReader
{
    private static MapTerrainCellSpec[] ReadTerrain(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "terrainCells").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "id", "bounds", "terrainId", "movementCost", "blocksLand");
            return new MapTerrainCellSpec(value.String("id"), ReadRect(value.Element("bounds")), value.String("terrainId"), value.Single("movementCost"), value.Boolean("blocksLand"));
        }).ToArray();
    }

    private static MapResourceNodeSpec[] ReadResources(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "resources").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "id", "position", "radius", "amount", "accent");
            return new MapResourceNodeSpec(value.String("id"), ReadPoint(value.Element("position")), value.Single("radius"), value.Int32("amount"), new MapColor(value.String("accent")));
        }).ToArray();
    }

    private static MapObstacleSpec[] ReadObstacles(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "obstacles").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "id", "bounds");
            return new MapObstacleSpec(value.String("id"), ReadRect(value.Element("bounds")));
        }).ToArray();
    }

    private static MapTriggerAreaSpec[] ReadTriggers(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "triggers").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "id", "bounds", "eventKey");
            return new MapTriggerAreaSpec(value.String("id"), ReadRect(value.Element("bounds")), value.String("eventKey"));
        }).ToArray();
    }
}
