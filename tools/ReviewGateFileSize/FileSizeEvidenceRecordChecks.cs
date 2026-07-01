static class FileSizeEvidenceRecordChecks
{
    public static void Check(
        IReadOnlyList<FileSizeSourceFile> files,
        string todo,
        string review,
        GateResult result)
    {
        var reviewGateRunnerFiles = files
            .Where(file => file.Path.StartsWith("tools/ReviewGate/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(file => file.Lines)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (reviewGateRunnerFiles.Length == 0)
        {
            result.Error("ReviewGate source budget evidence has no ReviewGate source files to inspect.");
            return;
        }

        var summary = CurrentBudgetSummary(reviewGateRunnerFiles);
        FileSizeEvidence.RequireContains(
            todo,
            summary,
            $"TODO must contain the exact current ReviewGate runner source budget summary: {summary}",
            result);
        FileSizeEvidence.RequireContains(
            review,
            summary,
            $"ReviewGate runner budget record must contain the exact current source budget summary: {summary}",
            result);
    }

    private static string CurrentBudgetSummary(IReadOnlyList<FileSizeSourceFile> reviewGateRunnerFiles)
    {
        var totalLines = reviewGateRunnerFiles.Sum(file => file.Lines);
        var largest = reviewGateRunnerFiles[0];
        return $"ReviewGate runner current source budget: {reviewGateRunnerFiles.Count} C# source files / {totalLines} total lines; largest C# file {largest.Path} has {largest.Lines} lines.";
    }
}
