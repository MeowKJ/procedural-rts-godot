static class ReviewGateModeCatalog
{
    private static readonly string[] CanonicalModes =
    [
        "all",
        "backlog",
        "filesize",
        "review",
        "architecture",
        "presentation",
        "unit-spec",
        "buildings",
        "buildingtarget",
        "commandscombat",
        "commandgateway",
        "economy",
        "sandbox",
        "mapauthoring",
        "regression",
    ];

    public static bool IsKnown(string mode, string root)
    {
        return IsSafeModeToken(mode)
            && CanonicalModes.Contains(mode, StringComparer.OrdinalIgnoreCase);
    }

    public static string Describe(string root)
    {
        return "Valid modes are " + string.Join(", ", CanonicalModes) + ".";
    }

    private static bool IsSafeModeToken(string mode)
    {
        return mode.All(ch => char.IsAsciiLetterOrDigit(ch) || ch == '-');
    }
}
