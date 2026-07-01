static class CoreArgumentParsing
{
    public static int? ParseMaxWarnings(string[] args)
    {
        const string prefix = "--max-warnings=";
        var value = args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (value is null)
        {
            return null;
        }

        if (int.TryParse(value[prefix.Length..], out var parsed) && parsed >= 0)
        {
            return parsed;
        }

        throw new ArgumentException("--max-warnings must be a non-negative integer.");
    }

    public static string? ParseRequiredRecord(string[] args)
    {
        const string prefix = "--require-record=";
        var value = args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (value is null)
        {
            return null;
        }

        var record = value[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(record))
        {
            throw new ArgumentException("--require-record must name a review record file or substring.");
        }

        return record;
    }
}
