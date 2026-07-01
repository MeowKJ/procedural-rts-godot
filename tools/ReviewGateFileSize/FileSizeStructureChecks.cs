using System.Text.RegularExpressions;
using System.Xml.Linq;

static class FileSizeStructureChecks
{
    public static void CheckStableEntrypoints(string root, GateResult result)
    {
        foreach (var entrypoint in FileSizePolicy.StableEntrypoints)
        {
            if (!File.Exists(Path.Combine(root, entrypoint.Replace('/', Path.DirectorySeparatorChar))))
            {
                result.Error($"Stable entrypoint required by FileStructureGovernance is missing: {entrypoint}.");
            }
        }

        CheckMainProjectCompileExclusions(root, result);
    }

    private static void CheckMainProjectCompileExclusions(string root, GateResult result)
    {
        var projectPath = Path.Combine(root, "ProceduralRts.csproj");
        if (!File.Exists(projectPath))
        {
            result.Error("ProceduralRts.csproj is required for generated-source exclusion checks.");
            return;
        }

        var removals = XDocument.Load(projectPath)
            .Descendants("Compile")
            .Select(element => element.Attribute("Remove")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in FileSizePolicy.MainProjectCompileExclusions)
        {
            if (!removals.Contains(required))
            {
                result.Error($"ProceduralRts.csproj must exclude generated/non-gameplay sources from compile: {required}.");
            }
        }
    }

    public static void CheckReviewGateStructure(string root, IReadOnlyList<FileSizeSourceFile> files, GateResult result)
    {
        CheckReviewGateGeneratedOutput(root, result);

        var reviewGateRunnerFiles = files
            .Where(file => file.Path.StartsWith("tools/ReviewGate/", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var reviewGateFamilyFiles = files
            .Where(file => FileSizePolicy.IsReviewGateSource(file.Path))
            .ToArray();
        var staticImportsPath = Path.Combine(root, "tools", "ReviewGate", "ReviewGateStaticImports.cs");
        if (!File.Exists(staticImportsPath))
        {
            result.Error("ReviewGate must keep explicit static imports for shared check helpers.");
        }

        var runnerLines = reviewGateRunnerFiles.Sum(file => file.Lines);
        if (runnerLines > FileSizePolicy.ReviewGateRunnerMax)
        {
            result.Error($"ReviewGate runner source exceeds budget: {runnerLines} lines, max {FileSizePolicy.ReviewGateRunnerMax}.");
        }

        var oldAggregateName = "ReviewGate" + "Checks";
        foreach (var file in reviewGateFamilyFiles)
        {
            var fullPath = Path.Combine(root, file.Path.Replace('/', Path.DirectorySeparatorChar));
            var text = File.ReadAllText(fullPath);
            if (text.Contains(oldAggregateName, StringComparison.Ordinal))
            {
                result.Error($"ReviewGate source must not recreate the historical {oldAggregateName} god class: {file.Path}.");
            }

            if (Regex.IsMatch(text, @"static\s+partial\s+class\s+\w+"))
            {
                result.Error($"ReviewGate check files must be independent classes, not partial aggregates: {file.Path}.");
            }
        }
    }

    private static void CheckReviewGateGeneratedOutput(string root, GateResult result)
    {
        foreach (var directory in new[] { "bin", "obj" })
        {
            var path = Path.Combine(root, "tools", "ReviewGate", directory);
            if (Directory.Exists(path))
            {
                result.Error($"ReviewGate generated output must live under artifacts/, not tools/ReviewGate/{directory}/.");
            }
        }
    }

    public static void CheckForbiddenNames(IReadOnlyList<FileSizeSourceFile> files, GateResult result)
    {
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file.Path);
            if (FileSizePolicy.ForbiddenFileNames.Any(forbidden => name.Equals(forbidden, StringComparison.OrdinalIgnoreCase)))
            {
                result.Error($"Forbidden vague source file name: {file.Path}.");
            }
        }
    }

    public static void CheckDirectoryShape(IReadOnlyList<FileSizeSourceFile> files, GateResult result)
    {
        foreach (var group in files.GroupBy(file => FileSizeSourceCatalog.DirectoryName(file.Path)))
        {
            WarnLargeDirectory(group, result);
            WarnDottedSplitFamily(group, result);
        }
    }

    private static void WarnLargeDirectory(IGrouping<string, FileSizeSourceFile> group, GateResult result)
    {
        var count = group.Count();
        if (count > 30)
        {
            result.Warning($"Source directory has {count} C# files; consider a domain subdirectory: {group.Key}/.");
        }
    }

    private static void WarnDottedSplitFamily(IGrouping<string, FileSizeSourceFile> group, GateResult result)
    {
        var dottedPrefixGroups = group
            .Select(file => Path.GetFileNameWithoutExtension(file.Path))
            .Where(name => name.Contains('.', StringComparison.Ordinal))
            .GroupBy(name => name[..name.IndexOf('.', StringComparison.Ordinal)])
            .Where(prefix => prefix.Count() > 8)
            .Where(prefix => !IsDomainDirectoryForPrefix(group.Key, prefix.Key));
        foreach (var prefix in dottedPrefixGroups)
        {
            result.Warning($"Same-prefix split family has {prefix.Count()} files in {group.Key}: {prefix.Key}.*.cs; consider a domain directory.");
        }
    }

    private static bool IsDomainDirectoryForPrefix(string directory, string prefix)
    {
        var lastSegment = directory
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
        if (lastSegment is null)
        {
            return false;
        }

        var normalizedDirectory = NormalizeDomainToken(lastSegment);
        var normalizedPrefix = NormalizeDomainToken(prefix);
        return normalizedDirectory.Equals(normalizedPrefix, StringComparison.Ordinal)
            || normalizedDirectory.Equals(TrimCommonTypeWords(normalizedPrefix), StringComparison.Ordinal);
    }

    private static string TrimCommonTypeWords(string token)
    {
        var trimmed = token;
        foreach (var suffix in new[] { "system", "layer", "state" })
        {
            if (trimmed.EndsWith(suffix, StringComparison.Ordinal)
                && trimmed.Length > suffix.Length)
            {
                return trimmed[..^suffix.Length];
            }
        }

        return trimmed.StartsWith("unit", StringComparison.Ordinal) && trimmed.Length > "unit".Length
            ? trimmed["unit".Length..]
            : trimmed;
    }

    private static string NormalizeDomainToken(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
