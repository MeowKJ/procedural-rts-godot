static class ReviewGateEvidence
{
    public static string FindCoreSourcePath(string root, string fileName)
    {
        var rootPath = Path.Combine(root, "scripts", "core", fileName);
        if (File.Exists(rootPath))
        {
            return rootPath;
        }

        var corePath = Path.Combine(root, "scripts", "core");
        if (!Directory.Exists(corePath))
        {
            return rootPath;
        }

        return Directory.EnumerateFiles(corePath, fileName, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path)
            .FirstOrDefault() ?? rootPath;
    }

    public static string ReadSourceWithPartials(string sourcePath)
    {
        var parts = new List<string>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(sourcePath))
        {
            parts.Add(File.ReadAllText(sourcePath));
            addedPaths.Add(sourcePath);
        }

        var directory = Path.GetDirectoryName(sourcePath);
        var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
        if (directory is not null && Directory.Exists(directory))
        {
            foreach (var partialPath in Directory.EnumerateFiles(directory, $"{sourceName}.*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path))
            {
                if (addedPaths.Add(partialPath))
                {
                    parts.Add(File.ReadAllText(partialPath));
                }
            }

            foreach (var partialPath in EnumerateDomainPartials(directory, sourceName))
            {
                if (addedPaths.Add(partialPath))
                {
                    parts.Add(File.ReadAllText(partialPath));
                }
            }

            if (sourceName.Equals("Program", StringComparison.OrdinalIgnoreCase)
                && directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("tools"))
            {
                foreach (var toolSourcePath in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !path.Equals(sourcePath, StringComparison.OrdinalIgnoreCase))
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path))
                {
                    if (addedPaths.Add(toolSourcePath))
                    {
                        parts.Add(File.ReadAllText(toolSourcePath));
                    }
                }
            }
        }

        return string.Join("\n\n", parts);
    }

    public static string ReadLivePipelineEvidence(string root)
    {
        var battleRootPath = Path.Combine(root, "scripts", "BattleRoot.cs");
        var pipelinePath = Path.Combine(root, "scripts", "core", "sim", "SimSystemPipeline.cs");
        return string.Join("\n\n", new[]
        {
            ReadSourceWithPartials(battleRootPath),
            File.Exists(pipelinePath) ? File.ReadAllText(pipelinePath) : string.Empty,
        });
    }

    private static IEnumerable<string> EnumerateDomainPartials(string directory, string sourceName)
    {
        var domainName = DomainDirectoryName(sourceName);
        if (domainName is null)
        {
            yield break;
        }

        var domainDirectory = Path.Combine(directory, domainName);
        if (!Directory.Exists(domainDirectory))
        {
            yield break;
        }

        foreach (var partialPath in Directory.EnumerateFiles(domainDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path))
        {
            yield return partialPath;
        }
    }

    private static string? DomainDirectoryName(string sourceName)
    {
        if (sourceName.Equals("SelectionController", StringComparison.Ordinal))
        {
            return "selection";
        }

        const string systemSuffix = "System";
        return sourceName.EndsWith(systemSuffix, StringComparison.Ordinal)
            && sourceName.Length > systemSuffix.Length
            ? sourceName[..^systemSuffix.Length].ToLowerInvariant()
            : null;
    }

    public static bool IsReviewGateSource(string root, string path)
    {
        var relativePath = Path.GetRelativePath(root, path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
        return relativePath.StartsWith("tools/ReviewGate/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("tools/ReviewGateCombat/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("tools/ReviewGateCore/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("tools/ReviewGateDomains/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("tools/ReviewGateFileSize/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("tools/ReviewGateMapAuthoring/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("tools/ReviewGateReservations/", StringComparison.OrdinalIgnoreCase);
    }
}
