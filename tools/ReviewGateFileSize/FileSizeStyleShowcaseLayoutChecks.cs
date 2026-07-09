static class FileSizeStyleShowcaseLayoutChecks
{
    private static readonly string[] RootEntrypoints =
    [
        "StyleTestRoot.cs",
        "StyleCandidateDeckRoot.cs",
        "StyleFamilyShowcaseRoot.cs",
        "OverallStyleShowcaseRoot.cs",
    ];

    private static readonly string[] ShowcasePartials =
    [
        "StyleTestRoot.Commands.cs",
        "StyleTestRoot.GeometryProjection.cs",
        "StyleTestRoot.MapProjection.cs",
        "StyleTestRoot.Spec.cs",
        "StyleTestRoot.UiDesign.cs",
        "StyleTestRoot.UnitDesign.cs",
        "StyleCandidateDeckRoot.Commands.cs",
        "StyleCandidateDeckRoot.Design.cs",
        "StyleCandidateDeckRoot.Draw.cs",
        "StyleCandidateDeckRoot.Layout.cs",
    ];

    public static void Check(string root, GateResult result)
    {
        foreach (var entrypoint in RootEntrypoints)
        {
            RequireFile(root, result, "scripts", entrypoint);
        }

        foreach (var partial in ShowcasePartials)
        {
            ForbidFile(root, result, "scripts", partial);
            RequireFile(root, result, "scripts", "style-showcase", partial);
        }
    }

    private static void RequireFile(string root, GateResult result, params string[] parts)
    {
        var relative = Path.Combine(parts);
        if (!File.Exists(Path.Combine(root, relative)))
        {
            result.Error($"Style showcase layout is missing required file: {relative}.");
        }
    }

    private static void ForbidFile(string root, GateResult result, params string[] parts)
    {
        var relative = Path.Combine(parts);
        if (File.Exists(Path.Combine(root, relative)))
        {
            result.Error($"Style showcase partial must live under scripts/style-showcase/: {relative}.");
        }
    }
}
