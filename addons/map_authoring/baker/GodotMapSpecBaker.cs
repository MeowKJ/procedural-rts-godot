using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring;

public static class GodotMapSpecBaker
{
    public static MapSpecArtifact Bake(Node root, string id, int seed)
    {
        ArgumentNullException.ThrowIfNull(root);
        var snapshot = MapSpecSnapshot.Create(FixtureMetadataMapSceneAdapter.Read(root, id, seed));
        MapLoader.Prepare(snapshot);
        return MapSpecArtifactCodec.Encode(snapshot);
    }
}
