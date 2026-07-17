using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring;

internal static class FixtureOnlyMetadataMapBaker
{
    internal static MapSpecArtifact BakeFixture(Node fixtureRoot, string id, int seed)
    {
        ArgumentNullException.ThrowIfNull(fixtureRoot);
        var spec = FixtureOnlyMetadataMapSceneAdapter.Read(fixtureRoot, id, seed);
        return GodotMapSpecBaker.BakeProjected(spec);
    }
}
