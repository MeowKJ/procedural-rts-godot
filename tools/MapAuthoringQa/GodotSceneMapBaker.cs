using System.Globalization;
using System.Text.RegularExpressions;
using ProceduralRts.Core;

static class GodotSceneMapBaker
{
    public static MapSpec Bake(string sceneText, string id, int seed)
    {
        var nodes = ParseNodes(sceneText);
        return new MapSpec
        {
            Id = id,
            Seed = seed,
            WorldSize = new MapSize(Number(nodes[0], "world_width", 3600), Number(nodes[0], "world_height", 2400)),
            OwnerStarts = NodesOf(nodes, "owner_start")
                .Select(node => new MapOwnerStartSpec(OwnerId(node), Faction(node), Position(node), Number(node, "facing"), Int(node, "credits", 0)))
                .ToArray(),
            TerrainCells = NodesOf(nodes, "terrain")
                .Select(node => new MapTerrainCellSpec(Name(node), Rect(node), Text(node, "terrain_id", "plain"), Number(node, "movement_cost", 1), Bool(node, "blocks_land")))
                .ToArray(),
            Resources = NodesOf(nodes, "resource")
                .Select(node => new MapResourceNodeSpec(Name(node), Position(node), Number(node, "radius", 120), Int(node, "amount", 1000), new MapColor(Text(node, "accent", "#8fffe1"))))
                .ToArray(),
            Obstacles = NodesOf(nodes, "obstacle")
                .Select(node => new MapObstacleSpec(Name(node), Rect(node)))
                .ToArray(),
            Buildings = NodesOf(nodes, "building")
                .Select(node => new MapBuildingSeedSpec(Text(node, "building_kind"), OwnerId(node), Faction(node), Position(node), Number(node, "facing")))
                .ToArray(),
            Units = NodesOf(nodes, "unit")
                .Select(node => new MapUnitSeedSpec(Text(node, "design_id"), OwnerId(node), Position(node), Number(node, "facing")))
                .ToArray(),
            Triggers = NodesOf(nodes, "trigger")
                .Select(node => new MapTriggerAreaSpec(Name(node), Rect(node), Text(node, "event_key")))
                .ToArray(),
            Objectives = NodesOf(nodes, "objective")
                .Select(node => new MapObjectiveNodeSpec(Name(node), Position(node), Text(node, "objective_key"), Bool(node, "primary", true)))
                .ToArray(),
            NarrativeNodes = NodesOf(nodes, "narrative")
                .Select(node => new MapNarrativeNodeSpec(Name(node), Position(node), Text(node, "text_key"), OptionalText(node, "trigger_id")))
                .ToArray(),
        };
    }

    private static List<Dictionary<string, string>> ParseNodes(string text)
    {
        var nodes = new List<Dictionary<string, string>>();
        Dictionary<string, string>? current = null;
        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("[node ", StringComparison.Ordinal))
            {
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["name"] = NodeName(line) };
                nodes.Add(current);
                continue;
            }

            if (current is null || !line.Contains('=', StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            current[parts[0].Trim().Replace("metadata/", "", StringComparison.Ordinal)] = parts[1].Trim().Trim('"');
        }

        return nodes;
    }

    private static IEnumerable<Dictionary<string, string>> NodesOf(IEnumerable<Dictionary<string, string>> nodes, string kind)
    {
        return nodes.Where(node => Text(node, "map_kind", "") == kind);
    }

    private static string NodeName(string line)
    {
        var match = Regex.Match(line, "name=\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : "node";
    }

    private static string Name(Dictionary<string, string> node)
    {
        return Text(node, "name");
    }

    private static OwnerId OwnerId(Dictionary<string, string> node)
    {
        return new OwnerId(Int(node, "owner_id", 0));
    }

    private static FactionId Faction(Dictionary<string, string> node)
    {
        return Enum.Parse<FactionId>(Text(node, "faction", nameof(FactionId.Dog)), ignoreCase: true);
    }

    private static MapPoint Position(Dictionary<string, string> node)
    {
        var match = Regex.Match(Text(node, "position", "Vector2(0, 0)"), @"Vector2\(([^,]+),\s*([^)]+)\)");
        return new MapPoint(Parse(match.Groups[1].Value), Parse(match.Groups[2].Value));
    }

    private static MapRect Rect(Dictionary<string, string> node)
    {
        var origin = Position(node);
        return new MapRect(origin.X, origin.Y, Number(node, "width", 1), Number(node, "height", 1));
    }

    private static string Text(Dictionary<string, string> node, string key, string? fallback = null)
    {
        return node.TryGetValue(key, out var value) ? value : fallback ?? throw new InvalidOperationException($"Missing scene metadata '{key}'.");
    }

    private static string? OptionalText(Dictionary<string, string> node, string key)
    {
        return node.TryGetValue(key, out var value) && value.Length > 0 ? value : null;
    }

    private static int Int(Dictionary<string, string> node, string key, int fallback)
    {
        return node.TryGetValue(key, out var value) ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;
    }

    private static float Number(Dictionary<string, string> node, string key, float fallback = 0)
    {
        return node.TryGetValue(key, out var value) ? Parse(value) : fallback;
    }

    private static bool Bool(Dictionary<string, string> node, string key, bool fallback = false)
    {
        return node.TryGetValue(key, out var value) ? bool.Parse(value) : fallback;
    }

    private static float Parse(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }
}
