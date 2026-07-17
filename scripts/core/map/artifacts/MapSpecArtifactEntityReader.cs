using System.Text.Json;

namespace ProceduralRts.Core;

static partial class MapSpecArtifactReader
{
    private static MapOwnerStartSpec[] ReadOwnerStarts(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "ownerStarts").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "ownerId", "faction", "position", "facing", "startingCredits");
            return new MapOwnerStartSpec(ReadOwner(value), ReadFaction(value), ReadPoint(value.Element("position")), value.Single("facing"), value.Int32("startingCredits"));
        }).ToArray();
    }

    private static MapBuildingSeedSpec[] ReadBuildings(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "buildings").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(
                item, "kind", "ownerId", "faction", "position", "facing", "hp", "buildProgress", "legacyId");
            return new MapBuildingSeedSpec(
                value.String("kind"), ReadOwner(value), ReadFaction(value), ReadPoint(value.Element("position")),
                value.Single("facing"), value.NullableSingle("hp"), value.Single("buildProgress"), value.NullableInt32("legacyId"));
        }).ToArray();
    }

    private static MapUnitSeedSpec[] ReadUnits(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "units").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "designId", "ownerId", "position", "facing");
            return new MapUnitSeedSpec(value.String("designId"), ReadOwner(value), ReadPoint(value.Element("position")), value.Single("facing"));
        }).ToArray();
    }

    private static MapObjectiveNodeSpec[] ReadObjectives(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "objectives").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "id", "position", "objectiveKey", "primary");
            return new MapObjectiveNodeSpec(value.String("id"), ReadPoint(value.Element("position")), value.String("objectiveKey"), value.Boolean("primary"));
        }).ToArray();
    }

    private static MapNarrativeNodeSpec[] ReadNarrative(JsonElement element)
    {
        return MapSpecArtifactJsonCursor.Array(element, "narrativeNodes").Select(item =>
        {
            var value = new MapSpecArtifactJsonCursor(item, "id", "position", "textKey", "triggerId");
            return new MapNarrativeNodeSpec(value.String("id"), ReadPoint(value.Element("position")), value.String("textKey"), value.NullableString("triggerId"));
        }).ToArray();
    }
}
