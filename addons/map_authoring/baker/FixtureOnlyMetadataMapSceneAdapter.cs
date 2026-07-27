using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring;

static class FixtureOnlyMetadataMapSceneAdapter
{
    public static MapSpec Read(Node root, string id, int seed)
    {
        var mapRoot = root as Node2D
            ?? throw new InvalidOperationException($"Map root '{root.Name}' must be Node2D.");
        var rootMetadata = new FixtureMapMetadata(root);
        var owners = new List<MapOwnerStartSpec>(); var terrain = new List<MapTerrainCellSpec>();
        var resources = new List<MapResourceNodeSpec>(); var obstacles = new List<MapObstacleSpec>();
        var buildings = new List<MapBuildingSeedSpec>(); var units = new List<MapUnitSeedSpec>();
        var triggers = new List<MapTriggerAreaSpec>(); var objectives = new List<MapObjectiveNodeSpec>();
        var narrative = new List<MapNarrativeNodeSpec>();
        foreach (var node in MapSceneProjection.SceneOrder(root).Where(node => node.HasMeta("map_kind")))
        {
            var metadata = new FixtureMapMetadata(node);
            var node2D = node as Node2D
                ?? throw new InvalidOperationException($"Map contributor '{node.Name}' must be Node2D.");
            var point = MapSceneProjection.RootLocalPoint(mapRoot, node2D);
            var name = node.Name.ToString();
            switch (metadata.RequiredString("map_kind"))
            {
                case "owner_start": owners.Add(Owner(metadata, point)); break;
                case "terrain": terrain.Add(new MapTerrainCellSpec(name, Rect(metadata, point), metadata.RequiredString("terrain_id"), metadata.Single("movement_cost", 1), metadata.Boolean("blocks_land"))); break;
                case "resource": resources.Add(new MapResourceNodeSpec(name, point, metadata.Single("radius", 120), metadata.Int32("amount", 1000), new MapColor(metadata.String("accent", "#8fffe1")))); break;
                case "obstacle": obstacles.Add(new MapObstacleSpec(name, Rect(metadata, point))); break;
                case "building": buildings.Add(Building(metadata, point)); break;
                case "unit": units.Add(new MapUnitSeedSpec(metadata.RequiredString("design_id"), OwnerId(metadata), point, metadata.Single("facing"))); break;
                case "trigger": triggers.Add(new MapTriggerAreaSpec(name, Rect(metadata, point), metadata.RequiredString("event_key"))); break;
                case "objective": objectives.Add(new MapObjectiveNodeSpec(name, point, metadata.RequiredString("objective_key"), metadata.Boolean("primary", true))); break;
                case "narrative": narrative.Add(new MapNarrativeNodeSpec(name, point, metadata.RequiredString("text_key"), metadata.OptionalString("trigger_id"))); break;
                default: throw new InvalidOperationException($"Node '{name}' has unknown map_kind '{metadata.RequiredString("map_kind")}'.");
            }
        }

        return new MapSpec
        {
            Id = id, Seed = seed,
            WorldSize = new MapSize(rootMetadata.Single("world_width", 3600), rootMetadata.Single("world_height", 2400)),
            OwnerStarts = owners.ToArray(), TerrainCells = terrain.ToArray(), Resources = resources.ToArray(),
            Obstacles = obstacles.ToArray(), Buildings = buildings.ToArray(), Units = units.ToArray(),
            Triggers = triggers.ToArray(), Objectives = objectives.ToArray(), NarrativeNodes = narrative.ToArray(),
        };
    }

    private static MapOwnerStartSpec Owner(FixtureMapMetadata value, MapPoint point)
    {
        return new MapOwnerStartSpec(OwnerId(value), Faction(value), point, value.Single("facing"), value.Int32("credits", 0));
    }

    private static MapBuildingSeedSpec Building(FixtureMapMetadata value, MapPoint point)
    {
        return new MapBuildingSeedSpec(
            value.RequiredString("building_kind"), OwnerId(value), Faction(value), point,
            value.Single("facing"), value.OptionalSingle("hp"), value.Single("build_progress", 1), value.OptionalInt32("runtime_id"));
    }

    private static OwnerId OwnerId(FixtureMapMetadata value) => new(value.RequiredInt32("owner_id"));

    private static FactionId Faction(FixtureMapMetadata value)
    {
        return MapSpecArtifactFactionWire.Read(value.RequiredString("faction"));
    }

    private static MapRect Rect(FixtureMapMetadata value, MapPoint point)
    {
        return new MapRect(point.X, point.Y, value.Single("width", 1), value.Single("height", 1));
    }

}
