using System.Text;
using System.Text.Json;

namespace ProceduralRts.Core;

static partial class MapSpecArtifactReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static MapSpec Read(ReadOnlySpan<byte> bytes)
    {
        StrictUtf8.GetCharCount(bytes);
        using var document = JsonDocument.Parse(
            bytes.ToArray(),
            new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
        var envelope = new MapSpecArtifactJsonCursor(document.RootElement, "format", "schemaVersion", "map");
        if (envelope.String("format") != MapSpecArtifactCodec.Format)
        {
            throw new MapSpecArtifactException("Unknown MapSpec artifact format.");
        }

        if (envelope.Int32("schemaVersion") != MapSpecArtifactCodec.SchemaVersion)
        {
            throw new MapSpecArtifactException("Unknown MapSpec artifact schemaVersion.");
        }

        return ReadMap(envelope.Element("map"));
    }

    private static MapSpec ReadMap(JsonElement element)
    {
        var value = new MapSpecArtifactJsonCursor(
            element,
            "id", "seed", "worldSize", "ownerStarts", "terrainCells", "resources",
            "obstacles", "buildings", "units", "triggers", "objectives", "narrativeNodes");
        return new MapSpec
        {
            Id = value.String("id"),
            Seed = value.Int32("seed"),
            WorldSize = ReadSize(value.Element("worldSize")),
            OwnerStarts = ReadOwnerStarts(value.Element("ownerStarts")),
            TerrainCells = ReadTerrain(value.Element("terrainCells")),
            Resources = ReadResources(value.Element("resources")),
            Obstacles = ReadObstacles(value.Element("obstacles")),
            Buildings = ReadBuildings(value.Element("buildings")),
            Units = ReadUnits(value.Element("units")),
            Triggers = ReadTriggers(value.Element("triggers")),
            Objectives = ReadObjectives(value.Element("objectives")),
            NarrativeNodes = ReadNarrative(value.Element("narrativeNodes")),
        };
    }

    private static MapSize ReadSize(JsonElement element)
    {
        var value = new MapSpecArtifactJsonCursor(element, "width", "height");
        return new MapSize(value.Single("width"), value.Single("height"));
    }

    private static MapPoint ReadPoint(JsonElement element)
    {
        var value = new MapSpecArtifactJsonCursor(element, "x", "y");
        return new MapPoint(value.Single("x"), value.Single("y"));
    }

    private static MapRect ReadRect(JsonElement element)
    {
        var value = new MapSpecArtifactJsonCursor(element, "x", "y", "width", "height");
        return new MapRect(value.Single("x"), value.Single("y"), value.Single("width"), value.Single("height"));
    }

    private static OwnerId ReadOwner(MapSpecArtifactJsonCursor value)
    {
        return new OwnerId(value.Int32("ownerId"));
    }

    private static FactionId ReadFaction(MapSpecArtifactJsonCursor value)
    {
        return MapSpecArtifactFactionWire.Read(value.String("faction"));
    }
}
