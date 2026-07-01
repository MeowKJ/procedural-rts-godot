static class FileSizeToolBudgetChecks
{
    public static void Check(
        IReadOnlyList<FileSizeSourceFile> files,
        string todo,
        string review,
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

        var summary = CurrentBudgetSummary(toolFiles);
        FileSizeEvidence.RequireContains(
            todo,
            summary,
            $"TODO must contain the exact current validation tools source budget summary: {summary}",
            result);
        FileSizeEvidence.RequireContains(
            review,
            summary,
            $"File-size review record must contain the exact current validation tools source budget summary: {summary}",
            result);
    }

    private static string CurrentBudgetSummary(IReadOnlyList<FileSizeSourceFile> toolFiles)
    {
        var totalLines = toolFiles.Sum(file => file.Lines);
        var largestFile = toolFiles[0];
        var suites = ToolSuites(toolFiles).ToArray();
        var largestSuite = suites
            .OrderByDescending(suite => suite.Lines)
            .ThenBy(suite => suite.Name, StringComparer.OrdinalIgnoreCase)
            .First();
        return $"Validation tool suites current source budget: {toolFiles.Count} C# source files / {totalLines} total lines across {suites.Length} suites; largest C# file {largestFile.Path} has {largestFile.Lines} lines; largest suite {largestSuite.Name} has {largestSuite.Lines} lines.";
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
