internal static class DesktopHudQaRepoRoot
{
    public static string Resolve(string[] arguments)
    {
        if (arguments.Length > 0)
        {
            if (arguments.Length != 2 || !string.Equals(arguments[0], "--repo-root", StringComparison.Ordinal))
            {
                throw new ArgumentException("Usage: DesktopHudQa [--repo-root <path>]");
            }

            var explicitRoot = Path.GetFullPath(arguments[1]);
            if (IsRepoRoot(explicitRoot))
            {
                return explicitRoot;
            }

            throw new InvalidOperationException(
                "The explicit DesktopHudQa repository root is invalid. Checked paths:\n- " + explicitRoot);
        }

        var checkedPaths = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(Path.GetFullPath(start));
            while (current is not null)
            {
                if (visited.Add(current.FullName))
                {
                    checkedPaths.Add(current.FullName);
                    if (IsRepoRoot(current.FullName))
                    {
                        return current.FullName;
                    }
                }

                current = current.Parent;
            }
        }

        throw new InvalidOperationException(
            "Could not find procedural-rts-godot repository root for HUD source checks. Checked paths:\n- "
            + string.Join("\n- ", checkedPaths));
    }

    private static bool IsRepoRoot(string path)
    {
        return File.Exists(Path.Combine(path, "ProceduralRts.csproj"))
            && File.Exists(Path.Combine(path, "scripts", "ui", "HudLayer.cs"));
    }
}
