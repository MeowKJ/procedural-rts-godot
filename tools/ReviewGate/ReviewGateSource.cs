static class ReviewGateSource
{
    public static string Read(string root, params string[] parts)
    {
        var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public static void RequireFile(string root, GateResult result, params string[] parts)
    {
        var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
        if (!File.Exists(path))
        {
            result.Error($"Required source file is missing: {Relative(root, path)}.");
        }
    }

    public static void RequireTextInFile(string root, GateResult result, string required, params string[] parts)
    {
        var text = Read(root, parts);
        if (!text.Contains(required, StringComparison.Ordinal))
        {
            result.Error($"{string.Join('/', parts)} must contain '{required}'.");
        }
    }

    public static void ForbidFile(string root, GateResult result, params string[] parts)
    {
        var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
        if (File.Exists(path))
        {
            result.Error($"Deleted source file must not return: {Relative(root, path)}.");
        }
    }

    public static void ForbidTextInSources(string root, GateResult result, string forbidden, params string[] roots)
    {
        foreach (var file in SourceFiles(root, roots))
        {
            if (ReviewGateEvidence.IsReviewGateSource(root, file))
            {
                continue;
            }

            if (File.ReadAllText(file).Contains(forbidden, StringComparison.Ordinal))
            {
                result.Error($"{Relative(root, file)} must not reference '{forbidden}'.");
            }
        }
    }

    public static void RequireAnyText(string root, GateResult result, string required, params string[] roots)
    {
        if (!SourceFiles(root, roots).Any(file => File.ReadAllText(file).Contains(required, StringComparison.Ordinal)))
        {
            result.Error($"Expected source evidence was not found: '{required}'.");
        }
    }

    private static IEnumerable<string> SourceFiles(string root, IReadOnlyList<string> roots)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativeRoot in roots)
        {
            foreach (var path in CandidateSourceRoots(root, relativeRoot))
            {
                foreach (var file in SourceFilesForRoot(path))
                {
                    if (seen.Add(file))
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    private static IEnumerable<string> CandidateSourceRoots(string root, string relativeRoot)
    {
        var path = Path.Combine(root, relativeRoot.Replace('/', Path.DirectorySeparatorChar));
        yield return path;

        var parts = relativeRoot.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !parts[0].Equals("tools", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var toolsPath = Path.Combine(root, "tools");
        if (!Directory.Exists(toolsPath))
        {
            yield break;
        }

        foreach (var directory in Directory.EnumerateDirectories(toolsPath, parts[1] + "*")
            .Where(directory => !directory.Equals(path, StringComparison.OrdinalIgnoreCase))
            .OrderBy(directory => directory))
        {
            yield return directory;
        }
    }

    private static IEnumerable<string> SourceFilesForRoot(string path)
    {
            if (File.Exists(path))
            {
                yield return path;
                yield break;
            }

            if (!Directory.Exists(path))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
            {
                yield return file;
            }
    }

    private static string Relative(string root, string path)
    {
        return Path.GetRelativePath(root, path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
