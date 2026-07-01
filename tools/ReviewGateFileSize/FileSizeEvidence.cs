static class FileSizeEvidence
{
    public static void RequireContains(string text, string expected, string message, GateResult result)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            result.Error(message);
        }
    }
}
