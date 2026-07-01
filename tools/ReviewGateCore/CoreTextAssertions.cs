using System.Text.RegularExpressions;

static class CoreTextAssertions
{
    public static void RequireText(string text, string required, string message, GateResult result)
    {
        if (!text.Contains(required, StringComparison.OrdinalIgnoreCase))
        {
            result.Error(message);
        }
    }

    public static void RequireRegex(string text, string pattern, string message, GateResult result)
    {
        if (!Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            result.Error(message);
        }
    }

    public static void ForbidText(string text, string forbidden, string message, GateResult result)
    {
        if (text.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
        {
            result.Error(message);
        }
    }

    public static void ForbidRegex(string text, string pattern, string message, GateResult result)
    {
        if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            result.Error(message);
        }
    }

    public static void RequireFileAbsent(string root, string path, string message, GateResult result)
    {
        if (File.Exists(path))
        {
            result.Error($"{message}: {Path.GetRelativePath(root, path)}.");
        }
    }
}
