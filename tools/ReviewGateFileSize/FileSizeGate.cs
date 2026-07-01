static class FileSizeGate
{
    public static void Check(string root, GateResult result)
    {
        var todoPath = Path.Combine(root, "TODO.md");
        var reviewPath = Path.Combine(root, "docs", "reviews", "2026-07-01-file-size-discipline-gate.md");
        if (!File.Exists(todoPath))
        {
            result.Error("TODO.md is required for the file-size discipline gate.");
            return;
        }

        if (!File.Exists(reviewPath))
        {
            result.Error($"Required file-size discipline review file is missing: {Path.GetRelativePath(root, reviewPath)}.");
            return;
        }

        var todo = ReviewGateEvidence.ReadTodoEvidence(root, todoPath);
        var review = File.ReadAllText(reviewPath);
        FileSizeEvidence.RequireContains(todo, "File-size discipline guard", "TODO must track the file-size discipline guard.", result);
        FileSizeEvidence.RequireContains(todo, "< 200 / 200-400 / 400-600 / > 600 / > 1000", "TODO must record the user's file-size thresholds.", result);
        FileSizeEvidence.RequireContains(review, "filesize", "Review record must include the filesize gate.", result);
        FileSizeEvidence.RequireContains(review, "FileStructureGovernance", "Review record must cite FileStructureGovernance.", result);

        var files = FileSizeSourceCatalog.EnumerateSourceFiles(root)
            .Select(path => new FileSizeSourceFile(FileSizeSourceCatalog.RelativePath(root, path), File.ReadLines(path).Count()))
            .OrderByDescending(file => file.Lines)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        FileSizeStructureChecks.CheckStableEntrypoints(root, result);
        FileSizeStructureChecks.CheckReviewGateStructure(root, files, result);
        FileSizeToolBudgetChecks.Check(files, todo, review, result);
        FileSizeEvidenceRecordChecks.Check(files, todo, review, result);
        FileSizeStructureChecks.CheckForbiddenNames(files, result);
        FileSizeStructureChecks.CheckDirectoryShape(files, result);
        FileSizeThresholdChecks.CheckBridgeLegacyBaseline(files, todo, review, result);
        FileSizeThresholdChecks.CheckFileSizes(files, result);
    }
}
