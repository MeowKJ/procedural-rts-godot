using System.Text.RegularExpressions;

static class ReviewGateModeCatalog
{
    private static readonly string[] CanonicalModes =
    [
        "all",
        "todo",
        "filesize",
        "review",
        "architecture",
        "presentation",
        "unit-spec",
        "buildings",
        "buildingtarget",
        "commandscombat",
        "economy",
        "sandbox",
        "mapauthoring",
        "regression",
        "m1migrationparentcomplete",
    ];

    private static readonly string[] StopWords =
    [
        "now",
        "must",
        "should",
        "itself",
        "output",
        "passed",
        "passes",
        "fails",
        "updated",
        "prevents",
        "locks",
        "runs",
        "verifies",
    ];

    public static bool IsKnown(string mode, string root)
    {
        return IsSafeModeToken(mode)
            && (CanonicalModes.Contains(mode, StringComparer.OrdinalIgnoreCase)
                || HistoricalModes(root).Contains(mode));
    }

    public static string Describe(string root)
    {
        var historicalCount = HistoricalModes(root).Count;
        return "Valid modes are core modes "
            + string.Join(", ", CanonicalModes)
            + $" plus {historicalCount} historical ReviewGate modes found in TODO/docs.";
    }

    private static HashSet<string> HistoricalModes(string root)
    {
        var modes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in EvidenceMarkdownFiles(root))
        {
            AddModes(File.ReadAllText(path), modes);
        }

        return modes;
    }

    private static IEnumerable<string> EvidenceMarkdownFiles(string root)
    {
        var todo = Path.Combine(root, "TODO.md");
        if (File.Exists(todo))
        {
            yield return todo;
        }

        var docs = Path.Combine(root, "docs");
        if (!Directory.Exists(docs))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories))
        {
            yield return path;
        }
    }

    private static void AddModes(string text, HashSet<string> modes)
    {
        foreach (Match match in Regex.Matches(
            text,
            @"ReviewGate(?:/ReviewGate\.csproj|\.csproj)?[^\r\n`]*?(?:--\s+|\s+)([a-z][a-z0-9-]{2,})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var mode = match.Groups[1].Value.ToLowerInvariant();
            if (!StopWords.Contains(mode, StringComparer.OrdinalIgnoreCase))
            {
                modes.Add(mode);
            }
        }
    }

    private static bool IsSafeModeToken(string mode)
    {
        return Regex.IsMatch(mode, "^[a-z0-9-]+$", RegexOptions.CultureInvariant);
    }
}
