static class GodotExecutableLocator
{
    public const string DefaultDisplayName = "godot";

    public const string MissingMessage =
        "Godot executable not found. Set GODOT_BIN to a Godot 4.7 Mono executable, or put godot/godot4/godot-mono/Godot_v4.7-stable_mono_* on PATH.";

    public static GodotExecutable? Find()
    {
        foreach (var variable in new[] { "GODOT_BIN", "GODOT4_BIN" })
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(variable);
            if (TryResolve(fromEnvironment, out var resolved))
            {
                return new GodotExecutable(resolved, variable);
            }
        }

        foreach (var candidate in CandidateNames())
        {
            if (TryResolve(candidate, out var resolved))
            {
                return new GodotExecutable(resolved, "PATH");
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateNames()
    {
        yield return "godot";
        yield return "godot4";
        yield return "godot-mono";
        yield return "godot4-mono";
        yield return "Godot_v4.7-stable_mono_linux.x86_64";
        yield return "Godot_v4.7-stable_mono_win64_console.exe";
        yield return "Godot_v4.7-stable_mono_win64.exe";
    }

    private static bool TryResolve(string? candidate, out string resolved)
    {
        resolved = "";
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var expanded = Environment.ExpandEnvironmentVariables(candidate.Trim().Trim('"'));
        if (File.Exists(expanded))
        {
            resolved = expanded;
            return true;
        }

        if (Path.GetDirectoryName(expanded) is not null)
        {
            return false;
        }

        return TryResolveFromPath(expanded, out resolved);
    }

    private static bool TryResolveFromPath(string fileName, out string resolved)
    {
        resolved = "";
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                resolved = candidate;
                return true;
            }
        }

        return false;
    }
}

sealed record GodotExecutable(string Path, string Source);
