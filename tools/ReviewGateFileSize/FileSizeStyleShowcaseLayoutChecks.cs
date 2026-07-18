static class FileSizeStyleShowcaseLayoutChecks
{
    public static void Check(string root, GateResult result)
    {
        foreach (var entrypoint in new[]
        {
            "StyleTestRoot.cs",
            "StyleCandidateDeckRoot.cs",
            "StyleFamilyShowcaseRoot.cs",
            "OverallStyleShowcaseRoot.cs",
        })
        {
            if (!File.Exists(Path.Combine(root, "scripts", entrypoint)))
            {
                result.Error($"Style showcase scene entrypoint must remain rooted: scripts/{entrypoint}.");
            }
        }

        foreach (var partial in new[]
        {
            "StyleCandidateDeckRoot.Commands.cs",
            "StyleCandidateDeckRoot.Design.cs",
            "StyleCandidateDeckRoot.Draw.cs",
            "StyleCandidateDeckRoot.Layout.cs",
            "StyleTestRoot.Commands.cs",
            "StyleTestRoot.GeometryProjection.cs",
            "StyleTestRoot.MapProjection.cs",
            "StyleTestRoot.Spec.cs",
            "StyleTestRoot.UiDesign.cs",
            "StyleTestRoot.UnitDesign.cs",
        })
        {
            if (File.Exists(Path.Combine(root, "scripts", partial)))
            {
                result.Error($"Style showcase partial must not return to scripts root: scripts/{partial}.");
            }

            if (!File.Exists(Path.Combine(root, "scripts", "style-showcase", partial)))
            {
                result.Error($"Style showcase partial is missing from feature folder: scripts/style-showcase/{partial}.");
            }
        }
    }
}
