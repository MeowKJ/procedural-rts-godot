using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring;

public static class GodotMapSpecBaker
{
    public static MapSpecArtifact Bake(Node root, IMapSpecSceneProjector projector)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(projector);
        return BakeProjected(projector.Project(root));
    }

    internal static MapSpecArtifact BakeProjected(MapSpec spec)
    {
        var snapshot = MapSpecSnapshot.Create(spec);
        MapLoader.Prepare(snapshot);
        return MapSpecArtifactCodec.Encode(snapshot);
    }
}
