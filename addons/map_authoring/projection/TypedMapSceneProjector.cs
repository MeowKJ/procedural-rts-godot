using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Projection;

public sealed class TypedMapSceneProjector : IMapSpecSceneProjector
{
    public static TypedMapSceneProjector Instance { get; } = new();

    public MapSpec Project(Node root)
    {
        var mapRoot = root as MapRoot
            ?? throw new InvalidOperationException("Typed projection requires MapRoot as the supplied root.");
        RejectUnsupportedMetadata(mapRoot);
        var owners = new List<MapOwnerStartSpec>(); var terrain = new List<MapTerrainCellSpec>();
        var resources = new List<MapResourceNodeSpec>(); var obstacles = new List<MapObstacleSpec>();
        var buildings = new List<MapBuildingSeedSpec>(); var units = new List<MapUnitSeedSpec>();
        var triggers = new List<MapTriggerAreaSpec>(); var objectives = new List<MapObjectiveNodeSpec>();
        var narrative = new List<MapNarrativeNodeSpec>();
        foreach (var node in MapSceneProjection.SceneOrder(mapRoot).Skip(1))
        {
            RejectUnsupportedMetadata(node);

            switch (node)
            {
                case OwnerStart value: owners.Add(TypedMapEntityProjection.Owner(mapRoot, value)); break;
                case TerrainRegion value: terrain.Add(TypedMapEnvironmentProjection.Terrain(mapRoot, value)); break;
                case AuthoringResource value: resources.Add(TypedMapEnvironmentProjection.Resource(mapRoot, value)); break;
                case Obstacle value: obstacles.Add(TypedMapEnvironmentProjection.Obstacle(mapRoot, value)); break;
                case Building value: buildings.Add(TypedMapEntityProjection.Building(mapRoot, value)); break;
                case Unit value: units.Add(TypedMapEntityProjection.Unit(mapRoot, value)); break;
                case Trigger value: triggers.Add(TypedMapEnvironmentProjection.Trigger(mapRoot, value)); break;
                case Objective value: objectives.Add(TypedMapEntityProjection.Objective(mapRoot, value)); break;
                case Narrative value: narrative.Add(TypedMapEntityProjection.Narrative(mapRoot, value)); break;
            }
        }

        return new MapSpec
        {
            Id = mapRoot.Id,
            Seed = mapRoot.Seed,
            WorldSize = new MapSize(mapRoot.WorldSize.X, mapRoot.WorldSize.Y),
            OwnerStarts = owners.ToArray(), TerrainCells = terrain.ToArray(), Resources = resources.ToArray(),
            Obstacles = obstacles.ToArray(), Buildings = buildings.ToArray(), Units = units.ToArray(),
            Triggers = triggers.ToArray(), Objectives = objectives.ToArray(), NarrativeNodes = narrative.ToArray(),
        };
    }

    private static void RejectUnsupportedMetadata(Node node)
    {
        if (node.HasMeta("map_kind"))
        {
            throw new InvalidOperationException($"Typed map node '{node.Name}' must not use metadata/map_kind fallback.");
        }
    }
}
