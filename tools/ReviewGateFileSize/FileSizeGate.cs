static class FileSizeGate
{
    public static void Check(string root, GateResult result)
    {
        var governancePath = Path.Combine(root, "docs", "FileStructureGovernance.md");
        var governance = File.Exists(governancePath) ? File.ReadAllText(governancePath) : string.Empty;
        FileSizeEvidence.RequireContains(governance, "Size Bands", "File structure governance must define size bands.", result);
        FileSizeEvidence.RequireContains(governance, "< 200", "File structure governance must record the healthy file-size target.", result);
        FileSizeEvidence.RequireContains(governance, "200-400", "File structure governance must record the normal file-size band.", result);
        FileSizeEvidence.RequireContains(governance, "400-600", "File structure governance must record the yellow file-size band.", result);
        FileSizeEvidence.RequireContains(governance, "> 600", "File structure governance must record the red file-size band.", result);
        FileSizeEvidence.RequireContains(governance, "> 1000", "File structure governance must record the debt file-size band.", result);

        var files = FileSizeSourceCatalog.EnumerateSourceFiles(root)
            .Select(path => new FileSizeSourceFile(FileSizeSourceCatalog.RelativePath(root, path), File.ReadLines(path).Count()))
            .OrderByDescending(file => file.Lines)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        FileSizeStructureChecks.CheckStableEntrypoints(root, result);
        FileSizeStyleShowcaseLayoutChecks.Check(root, result);
        FileSizeConstructionPlacementLayoutChecks.Check(root, result);
        FileSizeStructureChecks.CheckReviewGateStructure(root, files, result);
        FileSizeToolBudgetChecks.Check(files, result);
        FileSizeEvidenceRecordChecks.Check(files, result);
        FileSizeStructureChecks.CheckForbiddenNames(files, result);
        FileSizeStructureChecks.CheckDirectoryShape(files, result);
        FileSizeThresholdChecks.CheckFileSizes(files, result);
    }
}
