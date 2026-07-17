using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class MapAuthoringExportBoundaryScenarios
{
    private static readonly string[] ForbiddenNamespaces =
    [
        "ProceduralRts.MapAuthoring.Editor",
        "ProceduralRts.MapAuthoring.Nodes",
        "ProceduralRts.MapAuthoring.Projection",
        "ProceduralRts.MapAuthoring.Qa",
    ];

    public static void Run(IReadOnlyList<string> paths, List<string> failures)
    {
        if (paths.Count != 2)
        {
            failures.Add("Export boundary QA requires ExportDebug and ExportRelease assembly paths.");
            return;
        }

        foreach (var path in paths)
        {
            Inspect(Path.GetFullPath(path), failures);
        }
    }

    private static void Inspect(string path, List<string> failures)
    {
        if (!File.Exists(path))
        {
            failures.Add($"Export assembly is missing: {path}");
            return;
        }

        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var types = metadata.TypeDefinitions
            .Select(handle => metadata.GetTypeDefinition(handle))
            .Select(type => (Namespace: metadata.GetString(type.Namespace), Name: metadata.GetString(type.Name)))
            .ToArray();
        foreach (var forbidden in ForbiddenNamespaces)
        {
            if (types.Any(type => type.Namespace.StartsWith(forbidden, StringComparison.Ordinal)))
            {
                failures.Add($"Export assembly {Path.GetFileName(path)} contains editor-only namespace {forbidden}.");
            }
        }

        Require(types.Any(type => type.Name == "GodotMapSpecBaker"), path, "formal runtime baker", failures);
        Require(types.Any(type => type.Name == "IMapSpecSceneProjector"), path, "runtime projector contract", failures);
    }

    private static void Require(bool condition, string path, string label, List<string> failures)
    {
        if (!condition) failures.Add($"Export assembly {Path.GetFileName(path)} is missing {label}.");
    }
}
