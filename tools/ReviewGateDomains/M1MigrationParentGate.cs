static class M1MigrationParentGate
{
    public static void Check(string root, GateResult result)
    {
        var todoPath = Path.Combine(root, "TODO.md");
        var reviewPath = Path.Combine(root, "docs", "reviews", "2026-07-01-m1-migration-parent-complete.md");

        foreach (var path in new[] { todoPath, reviewPath })
        {
            if (!File.Exists(path))
            {
                result.Error($"Required M1 migration parent file is missing: {Path.GetRelativePath(root, path)}.");
                return;
            }
        }

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

        var todo = ReviewGateEvidence.ReadTodoEvidence(root, todoPath);
        var review = File.ReadAllText(reviewPath);
        RequireContains(todo, "[x] Migration cleanup: merge `BuildingDefinition`+`BuildDefinition`", "TODO must mark the M1 migration parent complete.", result);
        RequireContains(todo, "M1 migration parent completion", "TODO must record the parent completion slice.", result);
        RequireContains(review, "m1migrationparentcomplete", "Review record must include the narrow m1migrationparentcomplete gate.", result);
        RequireContains(review, "UnitBattlefieldBuildingTarget.cs", "Review record must name the deleted second building runtime file.", result);
        RequireContains(review, "BuildingDefinition.cs", "Review record must name the deleted building runtime compatibility file.", result);
        RequireContains(review, "BuildDefinition.cs", "Review record must name the deleted build/economy compatibility file.", result);
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

    private static void RequireContains(string text, string expected, string message, GateResult result)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            result.Error(message);
        }
    }
}


