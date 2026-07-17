using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Nodes;
using AuthoringResource = ProceduralRts.MapAuthoring.Nodes.Resource;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed record MapAuthoringSourceEntry(
    MapValidationSource Source,
    NodePath Path,
    Node Node);

public sealed class MapAuthoringSourceIndex
{
    private readonly Dictionary<(MapValidationSourceKind Kind, int Index), MapAuthoringSourceEntry> _entries = [];

    private MapAuthoringSourceIndex(MapRoot root)
    {
        Root = root;
        var counts = new Dictionary<MapValidationSourceKind, int>();
        var sceneOrder = 0;
        foreach (var node in MapSceneProjection.SceneOrder(root))
        {
            var kind = Kind(node);
            if (kind is null)
            {
                sceneOrder++;
                continue;
            }
            var index = kind == MapValidationSourceKind.Root ? 0 : counts.GetValueOrDefault(kind.Value);
            counts[kind.Value] = index + 1;
            var path = root.GetPathTo(node);
            var source = new MapValidationSource(kind.Value, index, Id(node), sceneOrder, path.ToString());
            _entries[(kind.Value, index)] = new MapAuthoringSourceEntry(source, path, node);
            sceneOrder++;
        }
    }

    public MapRoot Root { get; }
    public IReadOnlyCollection<MapAuthoringSourceEntry> Entries => _entries.Values;

    public static MapAuthoringSourceIndex Build(MapRoot root) => new(root);

    public MapValidationSource Resolve(MapValidationSource source)
    {
        return _entries.TryGetValue((source.Kind, source.Index), out var entry)
            ? entry.Source with { Id = source.Id }
            : source;
    }

    public Node? ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Root.GetNodeOrNull(new NodePath(path));
    }

    private static MapValidationSourceKind? Kind(Node node) => node switch
    {
        MapRoot => MapValidationSourceKind.Root,
        OwnerStart => MapValidationSourceKind.OwnerStart,
        Building => MapValidationSourceKind.Building,
        Unit => MapValidationSourceKind.Unit,
        AuthoringResource => MapValidationSourceKind.Resource,
        Obstacle => MapValidationSourceKind.Obstacle,
        TerrainRegion => MapValidationSourceKind.Terrain,
        Trigger => MapValidationSourceKind.Trigger,
        Objective => MapValidationSourceKind.Objective,
        Narrative => MapValidationSourceKind.Narrative,
        _ => null,
    };

    private static string Id(Node node) => node switch
    {
        MapRoot value => value.Id,
        OwnerStart value => value.OwnerId.ToString(),
        Building value => value.BuildingId,
        Unit value => value.DesignId,
        AuthoringResource value => value.Id,
        Obstacle value => value.Id,
        TerrainRegion value => value.Id,
        Trigger value => value.Id,
        Objective value => value.Id,
        Narrative value => value.Id,
        _ => node.Name,
    };
}
