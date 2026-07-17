using System.Buffers;
using System.Text.Json;

namespace ProceduralRts.Core;

static partial class MapSpecArtifactWriter
{
    public static byte[] Write(MapSpec map)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", MapSpecArtifactCodec.Format);
            writer.WriteNumber("schemaVersion", MapSpecArtifactCodec.SchemaVersion);
            writer.WritePropertyName("map");
            WriteMap(writer, map);
            writer.WriteEndObject();
        }

        var bytes = new byte[buffer.WrittenCount + 1];
        buffer.WrittenSpan.CopyTo(bytes);
        bytes[^1] = (byte)'\n';
        return bytes;
    }

    private static void WriteMap(Utf8JsonWriter writer, MapSpec map)
    {
        writer.WriteStartObject();
        Text(writer, "id", map.Id);
        writer.WriteNumber("seed", map.Seed);
        writer.WritePropertyName("worldSize");
        WriteSize(writer, map.WorldSize);
        WriteOwnerStarts(writer, map.OwnerStarts);
        WriteTerrain(writer, map.TerrainCells);
        WriteResources(writer, map.Resources);
        WriteObstacles(writer, map.Obstacles);
        WriteBuildings(writer, map.Buildings);
        WriteUnits(writer, map.Units);
        WriteTriggers(writer, map.Triggers);
        WriteObjectives(writer, map.Objectives);
        WriteNarrative(writer, map.NarrativeNodes);
        writer.WriteEndObject();
    }

    private static void WriteSize(Utf8JsonWriter writer, MapSize value)
    {
        writer.WriteStartObject();
        Number(writer, "width", value.Width);
        Number(writer, "height", value.Height);
        writer.WriteEndObject();
    }

    private static void WritePoint(Utf8JsonWriter writer, MapPoint value)
    {
        writer.WriteStartObject();
        Number(writer, "x", value.X);
        Number(writer, "y", value.Y);
        writer.WriteEndObject();
    }

    private static void WriteRect(Utf8JsonWriter writer, MapRect value)
    {
        writer.WriteStartObject();
        Number(writer, "x", value.X);
        Number(writer, "y", value.Y);
        Number(writer, "width", value.Width);
        Number(writer, "height", value.Height);
        writer.WriteEndObject();
    }

    private static void Number(Utf8JsonWriter writer, string name, float value)
    {
        if (!float.IsFinite(value))
        {
            throw new MapSpecArtifactException($"MapSpec number '{name}' must be finite.");
        }

        writer.WriteNumber(name, value == 0f ? 0f : value);
    }

    private static void Text(Utf8JsonWriter writer, string name, string value)
    {
        if (value is null)
        {
            throw new MapSpecArtifactException($"MapSpec text '{name}' must not be null.");
        }

        writer.WriteString(name, value);
    }
}
