static class FileSizeBattleRootLayoutChecks
{
    public static void Check(string root, GateResult result)
    {
        foreach (var fileName in new[]
        {
            "BattleRoot.Alerts.cs",
            "BattleRoot.EntityWorld.cs",
            "BattleRoot.Events.cs",
            "BattleRoot.HudSync.cs",
            "BattleRoot.Lifecycle.cs",
            "BattleRoot.Process.cs",
            "BattleRoot.Sandbox.cs",
            "BattleRoot.Selection.cs",
        })
        {
            RequireFile(root, result, "scripts", "battle-root", fileName);
            ForbidFile(root, result, "scripts", fileName);
        }
    }

    private static void RequireFile(string root, GateResult result, params string[] parts)
    {
        if (!File.Exists(SourcePath(root, parts)))
        {
            result.Error($"Expected source file is missing: {string.Join('/', parts)}.");
        }
    }

    private static void ForbidFile(string root, GateResult result, params string[] parts)
    {
        if (File.Exists(SourcePath(root, parts)))
        {
            result.Error($"Source file should live in its domain folder instead of root: {string.Join('/', parts)}.");
        }
    }

    private static string SourcePath(string root, IReadOnlyList<string> parts)
    {
        var path = root;
        foreach (var part in parts)
        {
            path = Path.Combine(path, part);
        }

        return path;
    }
}
