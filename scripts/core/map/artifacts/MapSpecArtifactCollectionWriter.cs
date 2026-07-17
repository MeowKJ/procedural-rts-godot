using System.Text.Json;

namespace ProceduralRts.Core;

static partial class MapSpecArtifactWriter
{
    private static void WriteOwnerStarts(Utf8JsonWriter writer, IReadOnlyList<MapOwnerStartSpec> items)
    {
        writer.WriteStartArray("ownerStarts");
        foreach (var item in items)
        {
            writer.WriteStartObject();
            writer.WriteNumber("ownerId", item.OwnerId.Value);
            Text(writer, "faction", MapSpecArtifactFactionWire.Write(item.Faction));
            writer.WritePropertyName("position"); WritePoint(writer, item.Position);
            Number(writer, "facing", item.Facing);
            writer.WriteNumber("startingCredits", item.StartingCredits);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteTerrain(Utf8JsonWriter writer, IReadOnlyList<MapTerrainCellSpec> items)
    {
        writer.WriteStartArray("terrainCells");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "id", item.Id);
            writer.WritePropertyName("bounds"); WriteRect(writer, item.Bounds);
            Text(writer, "terrainId", item.TerrainId);
            Number(writer, "movementCost", item.MovementCost);
            writer.WriteBoolean("blocksLand", item.BlocksLand); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteResources(Utf8JsonWriter writer, IReadOnlyList<MapResourceNodeSpec> items)
    {
        writer.WriteStartArray("resources");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "id", item.Id);
            writer.WritePropertyName("position"); WritePoint(writer, item.Position);
            Number(writer, "radius", item.Radius); writer.WriteNumber("amount", item.Amount);
            Text(writer, "accent", item.Accent.Hex); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteObstacles(Utf8JsonWriter writer, IReadOnlyList<MapObstacleSpec> items)
    {
        writer.WriteStartArray("obstacles");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "id", item.Id);
            writer.WritePropertyName("bounds"); WriteRect(writer, item.Bounds); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteBuildings(Utf8JsonWriter writer, IReadOnlyList<MapBuildingSeedSpec> items)
    {
        writer.WriteStartArray("buildings");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "kind", item.Kind);
            writer.WriteNumber("ownerId", item.OwnerId.Value); Text(writer, "faction", MapSpecArtifactFactionWire.Write(item.Faction));
            writer.WritePropertyName("position"); WritePoint(writer, item.Position); Number(writer, "facing", item.Facing);
            if (item.Hp is { } hp) Number(writer, "hp", hp); else writer.WriteNull("hp");
            Number(writer, "buildProgress", item.BuildProgress);
            if (item.LegacyId is { } legacyId) writer.WriteNumber("legacyId", legacyId); else writer.WriteNull("legacyId");
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteUnits(Utf8JsonWriter writer, IReadOnlyList<MapUnitSeedSpec> items)
    {
        writer.WriteStartArray("units");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "designId", item.DesignId);
            writer.WriteNumber("ownerId", item.OwnerId.Value); writer.WritePropertyName("position"); WritePoint(writer, item.Position);
            Number(writer, "facing", item.Facing); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteTriggers(Utf8JsonWriter writer, IReadOnlyList<MapTriggerAreaSpec> items)
    {
        writer.WriteStartArray("triggers");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "id", item.Id); writer.WritePropertyName("bounds"); WriteRect(writer, item.Bounds);
            Text(writer, "eventKey", item.EventKey); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteObjectives(Utf8JsonWriter writer, IReadOnlyList<MapObjectiveNodeSpec> items)
    {
        writer.WriteStartArray("objectives");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "id", item.Id); writer.WritePropertyName("position"); WritePoint(writer, item.Position);
            Text(writer, "objectiveKey", item.ObjectiveKey); writer.WriteBoolean("primary", item.Primary); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteNarrative(Utf8JsonWriter writer, IReadOnlyList<MapNarrativeNodeSpec> items)
    {
        writer.WriteStartArray("narrativeNodes");
        foreach (var item in items)
        {
            writer.WriteStartObject(); Text(writer, "id", item.Id); writer.WritePropertyName("position"); WritePoint(writer, item.Position);
            Text(writer, "textKey", item.TextKey);
            if (item.TriggerId is { } triggerId) Text(writer, "triggerId", triggerId); else writer.WriteNull("triggerId");
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}
