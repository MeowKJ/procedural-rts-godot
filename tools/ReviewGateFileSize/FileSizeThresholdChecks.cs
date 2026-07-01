static class FileSizeThresholdChecks
{
    public static void CheckFileSizes(IReadOnlyList<FileSizeSourceFile> files, GateResult result)
    {
        foreach (var file in files)
        {
            if (FileSizePolicy.IsReviewGateSource(file.Path)
                && file.Lines > FileSizePolicy.ReviewGateFileMax)
            {
                result.Error($"ReviewGate source file exceeds the validation-system limit: {file.Path} has {file.Lines} lines, max {FileSizePolicy.ReviewGateFileMax}.");
                continue;
            }

            if (FileSizePolicy.KnownRedLineCeilings.TryGetValue(file.Path, out var ceiling))
            {
                if (file.Lines > ceiling)
                {
                    result.Error($"Known file-size debt grew beyond its ceiling: {file.Path} has {file.Lines} lines, ceiling {ceiling}.");
                }

                continue;
            }

            if (file.Lines > FileSizePolicy.YellowMax)
            {
                result.Error($"Untracked red-line source file: {file.Path} has {file.Lines} lines; split or add an explicit debt ceiling.");
            }
        }

        WarnKnownDebt(files, result);
        WarnYellowFiles(files, result);
    }

    public static void CheckBridgeLegacyBaseline(
        IReadOnlyList<FileSizeSourceFile> files,
        string todo,
        string review,
        GateResult result)
    {
        var bridgeFiles = files
            .Where(file => Path.GetFileName(file.Path).Contains("Bridge", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(file.Path).Contains("Legacy", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(file.Path).Contains("Compatibility", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (bridgeFiles.Length > FileSizePolicy.BridgeLegacyCompatibilityBaseline)
        {
            result.Error($"Bridge/Legacy/Compatibility source count rose above baseline {FileSizePolicy.BridgeLegacyCompatibilityBaseline}: {bridgeFiles.Length}.");
        }

        if (bridgeFiles.Length > 0
            && !todo.Contains("Bridge", StringComparison.Ordinal)
            && !review.Contains("Bridge", StringComparison.Ordinal))
        {
            result.Warning("Bridge/Legacy/Compatibility files exist but no deletion-condition evidence was found in TODO or the file-size review record.");
        }
    }

    private static void WarnKnownDebt(IReadOnlyList<FileSizeSourceFile> files, GateResult result)
    {
        var knownDebt = files
            .Where(file => FileSizePolicy.KnownRedLineCeilings.ContainsKey(file.Path))
            .Where(file => file.Lines > FileSizePolicy.YellowMax)
            .ToArray();
        if (knownDebt.Length == 0)
        {
            return;
        }

        result.Warning(
            $"Known file-size debt remains ({knownDebt.Length} files over {FileSizePolicy.YellowMax} lines): " +
            string.Join(", ", knownDebt.Take(5).Select(file => $"{file.Path}={file.Lines}")) +
            (knownDebt.Length > 5 ? ", ..." : ""));
    }

    private static void WarnYellowFiles(IReadOnlyList<FileSizeSourceFile> files, GateResult result)
    {
        var yellowFiles = files
            .Where(file => file.Lines > FileSizePolicy.NormalMax && file.Lines <= FileSizePolicy.YellowMax)
            .ToArray();
        if (yellowFiles.Length == 0)
        {
            return;
        }

        result.Warning(
            $"Yellow file-size watchlist ({yellowFiles.Length} files at {FileSizePolicy.NormalMax + 1}-{FileSizePolicy.YellowMax} lines; healthy target <= {FileSizePolicy.HealthyMax}): " +
            string.Join(", ", yellowFiles.Take(5).Select(file => $"{file.Path}={file.Lines}")) +
            (yellowFiles.Length > 5 ? ", ..." : ""));
    }
}
