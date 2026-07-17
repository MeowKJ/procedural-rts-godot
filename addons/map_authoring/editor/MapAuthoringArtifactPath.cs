using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.MapAuthoring.Editor;

public sealed record MapAuthoringArtifactTarget(
    string ResourcePath, string ProjectRoot, string AbsolutePath);

public static class MapAuthoringArtifactPath
{
    public const string Prefix = MapArtifactPathPolicy.ResourcePrefix;
    public const string Suffix = MapArtifactPathPolicy.Suffix;

    public static MapAuthoringArtifactTarget Resolve(string resourcePath)
    {
        var projectRoot = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
        var absolute = MapArtifactPathPolicy.ResolveResourcePath(projectRoot, resourcePath);
        return new MapAuthoringArtifactTarget(resourcePath, projectRoot, absolute);
    }
}
