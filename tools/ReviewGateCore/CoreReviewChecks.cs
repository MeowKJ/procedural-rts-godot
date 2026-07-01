using System.Text.RegularExpressions;

static class CoreReviewChecks
{
    public static void CheckReviewTemplate(string root, GateResult result, string? requiredRecord)
    {
        var templatePath = Path.Combine(root, "docs", "reviews", "README.md");
        if (!File.Exists(templatePath))
        {
            result.Error("docs/reviews/README.md review template is missing.");
            return;
        }

        var template = File.ReadAllText(templatePath);
        RequireText(template, "Automated gates", "Review template must require automated gate evidence.", result);
        RequireText(template, "Reviewer result", "Review template must require reviewer result.", result);
        RequireText(template, "Residual risks", "Review template must track residual risks.", result);
        RequireText(template, "TODO update", "Review template must connect review evidence to TODO changes.", result);

        var records = Directory
            .EnumerateFiles(Path.Combine(root, "docs", "reviews"), "*.md", SearchOption.TopDirectoryOnly)
            .Where(path => !Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (records.Length == 0)
        {
            result.Error("No concrete review records found in docs/reviews/*.md.");
            return;
        }

        var recordsToCheck = records;
        if (requiredRecord is not null)
        {
            recordsToCheck = records
                .Where(path => Path.GetFileName(path).Contains(requiredRecord, StringComparison.OrdinalIgnoreCase)
                    || Path.GetRelativePath(root, path).Replace('\\', '/').Contains(requiredRecord, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (recordsToCheck.Length == 0)
            {
                result.Error($"Required review record '{requiredRecord}' was not found in docs/reviews/*.md.");
            }
        }

        foreach (var recordPath in recordsToCheck)
        {
            CheckReviewRecord(root, recordPath, result);
        }
    }

    public static void CheckReviewRecord(string root, string recordPath, GateResult result)
    {
        var relative = Path.GetRelativePath(root, recordPath).Replace('\\', '/');
        var text = File.ReadAllText(recordPath);
        var requiredFields = new[]
        {
            "Step:",
            "Milestone:",
            "Owner AI:",
            "Reviewer AI:",
            "Integrator AI:",
            "Scope:",
            "Automated gates:",
            "Reviewer result:",
            "Status:",
            "Residual risks:",
            "TODO update:",
        };

        foreach (var field in requiredFields)
        {
            if (!text.Contains(field, StringComparison.OrdinalIgnoreCase))
            {
                result.Error($"{relative} is missing required review field '{field}'.");
            }
        }

        if (Regex.IsMatch(text, @"Status:\s*(pass\s*/|pass\s+/\s+pass-with-warnings\s+/\s+fail)", RegexOptions.IgnoreCase))
        {
            result.Error($"{relative} still has placeholder reviewer status.");
        }

        if (!Regex.IsMatch(text, @"Command:\s*`?\s*dotnet\s+", RegexOptions.IgnoreCase))
        {
            result.Error($"{relative} must include at least one dotnet automated gate command.");
        }
    }
}
