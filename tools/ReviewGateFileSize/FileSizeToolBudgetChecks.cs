static class FileSizeToolBudgetChecks
{
    public static void Check(
        IReadOnlyList<FileSizeSourceFile> files,
        GateResult result)
    {
        var toolFiles = files
            .Where(file => file.Path.StartsWith("tools/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(file => file.Lines)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (toolFiles.Length == 0)
        {
            result.Error("Validation tools source budget has no tools/**/*.cs source files to inspect.");
            return;
        }

        foreach (var suite in ToolSuites(toolFiles))
        {
            if (suite.Lines > FileSizePolicy.ValidationToolSuiteMax)
            {
                result.Error($"Validation tool suite exceeds budget: {suite.Name} has {suite.Lines} lines, max {FileSizePolicy.ValidationToolSuiteMax}.");
            }
        }
    }

    private static IEnumerable<ToolSuiteBudget> ToolSuites(IReadOnlyList<FileSizeSourceFile> toolFiles)
    {
        return toolFiles
            .GroupBy(file => ToolSuiteName(file.Path), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ToolSuiteBudget(group.Key, group.Sum(file => file.Lines)));
    }

    private static string ToolSuiteName(string path)
    {
        var parts = path.Split('/');
        return parts.Length >= 2 ? $"tools/{parts[1]}" : "tools";
    }

    private sealed record ToolSuiteBudget(string Name, int Lines);
}
