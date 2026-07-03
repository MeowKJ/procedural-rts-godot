static class FileSizeEvidenceRecordChecks
{
    public static void Check(
        IReadOnlyList<FileSizeSourceFile> files,
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

        var totalLines = reviewGateRunnerFiles.Sum(file => file.Lines);
        if (totalLines > FileSizePolicy.ReviewGateRunnerMax)
        {
            result.Error($"ReviewGate runner source budget exceeded: tools/ReviewGate has {totalLines} lines, max {FileSizePolicy.ReviewGateRunnerMax}.");
        }
    }
}
