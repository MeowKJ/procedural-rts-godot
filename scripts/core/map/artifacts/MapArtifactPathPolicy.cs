namespace ProceduralRts.Core;

public static class MapArtifactPathPolicy
{
    public const string ResourcePrefix = "res://assets/maps/";
    public const string Suffix = ".mapspec.json";

    public static string ResolveResourcePath(string projectRoot, string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)
            || resourcePath.Contains('\\')
            || !resourcePath.StartsWith(ResourcePrefix, StringComparison.Ordinal)
            || !resourcePath.EndsWith(Suffix, StringComparison.Ordinal))
            throw new InvalidOperationException($"Artifact path must match {ResourcePrefix}*{Suffix}.");
        var fileName = resourcePath[ResourcePrefix.Length..];
        if (fileName.Length == Suffix.Length || fileName.Contains('/') || Path.GetFileName(fileName) != fileName)
            throw new InvalidOperationException("Artifact path must name one direct assets/maps file.");
        return RequireAbsolute(projectRoot, Path.Combine(projectRoot, "assets", "maps", fileName));
    }

    public static string RequireAbsolute(string projectRoot, string artifactPath)
    {
        var root = Path.GetFullPath(projectRoot);
        var assets = Path.GetFullPath(Path.Combine(root, "assets"));
        var maps = Path.GetFullPath(Path.Combine(assets, "maps"));
        var artifact = Path.GetFullPath(artifactPath);
        var relative = Path.GetRelativePath(maps, artifact);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal)
            || relative.Contains(Path.DirectorySeparatorChar)
            || !relative.EndsWith(Suffix, StringComparison.Ordinal))
            throw new InvalidOperationException("Artifact must be one mapspec directly under project assets/maps.");
        RejectLinkOrReparse(assets, "assets");
        RejectLinkOrReparse(maps, "assets/maps");
        RejectLinkOrReparse(artifact, "artifact target");
        return artifact;
    }

    private static void RejectLinkOrReparse(string path, string label)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return;
        var attributes = File.GetAttributes(path);
        var linkTarget = Directory.Exists(path)
            ? new DirectoryInfo(path).LinkTarget
            : new FileInfo(path).LinkTarget;
        if ((attributes & FileAttributes.ReparsePoint) != 0 || linkTarget is not null)
            throw new InvalidOperationException($"Project {label} must not be a link or reparse point.");
    }
}
