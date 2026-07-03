static class M1MigrationParentGate
{
    public static void Check(string root, GateResult result)
    {
        foreach (var deletedPath in DeletedPaths(root))
        {
            if (File.Exists(deletedPath))
            {
                result.Error($"{RelativePath(root, deletedPath)} must stay deleted for the completed M1 migration parent.");
            }
        }

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "scripts"), "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (text.Contains("UnitBattlefieldBuildingTarget", StringComparison.Ordinal)
                || text.Contains("BuildingDefinition", StringComparison.Ordinal)
                || text.Contains("BuildDefinition", StringComparison.Ordinal)
                || text.Contains("BuildCatalog", StringComparison.Ordinal)
                || text.Contains("BuildingDefinitions", StringComparison.Ordinal))
            {
                result.Error($"{RelativePath(root, file)} must not reference deleted M1 migration compatibility/runtime symbols.");
            }
        }
    }

    private static IEnumerable<string> DeletedPaths(string root)
    {
        yield return Path.Combine(root, "scripts", "core", "BuildDefinition.cs");
        yield return Path.Combine(root, "scripts", "core", "BuildingDefinition.cs");
        yield return Path.Combine(root, "scripts", "core", "BuildCatalog.cs");
        yield return Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefieldBuildingTarget.cs");
    }

    private static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

}
