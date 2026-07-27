using System.Text.RegularExpressions;

static class CoreReviewChecks
{
    public static void CheckReviewTemplate(string root, GateResult result, string? requiredRecord)
    {
        CheckGitHubReviewSurface(root, result);
        CheckCommentDiscipline(root, result);

        if (requiredRecord is null)
        {
            return;
        }

        var reviewsPath = Path.Combine(root, "docs", "reviews");
        if (!Directory.Exists(reviewsPath))
        {
            result.Error($"Retired review record '{requiredRecord}' was requested, but docs/reviews is missing.");
            return;
        }

        var records = Directory
            .EnumerateFiles(reviewsPath, "*.md", SearchOption.TopDirectoryOnly)
            .Where(path => !Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .Where(path => Path.GetFileName(path).Contains(requiredRecord, StringComparison.OrdinalIgnoreCase)
                || Path.GetRelativePath(root, path).Replace('\\', '/').Contains(requiredRecord, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (records.Length == 0)
        {
            result.Error($"Required retired review record '{requiredRecord}' was not found in docs/reviews/*.md.");
            return;
        }

        foreach (var recordPath in records)
        {
            CheckReviewRecord(root, recordPath, result);
        }
    }

    private static void CheckGitHubReviewSurface(string root, GateResult result)
    {
        var issueTemplatePath = Path.Combine(root, ".github", "ISSUE_TEMPLATE", "codex-slice.md");
        var prTemplatePath = Path.Combine(root, ".github", "pull_request_template.md");
        if (!File.Exists(issueTemplatePath))
        {
            result.Error(".github/ISSUE_TEMPLATE/codex-slice.md is missing.");
            return;
        }

        if (!File.Exists(prTemplatePath))
        {
            result.Error(".github/pull_request_template.md is missing.");
            return;
        }

        var issueTemplate = File.ReadAllText(issueTemplatePath);
        var prTemplate = File.ReadAllText(prTemplatePath);
        RequireText(issueTemplate, "Goal", "Issue template must capture the slice goal.", result);
        RequireText(issueTemplate, "Context pack", "Issue template must provide compact context.", result);
        RequireText(issueTemplate, "Required gates", "Issue template must name required gates.", result);
        RequireText(issueTemplate, "Evidence destination", "Issue template must route evidence to GitHub/CI.", result);
        RequireText(prTemplate, "Issue", "PR template must link the issue.", result);
        RequireText(prTemplate, "Scope", "PR template must name scope.", result);
        RequireText(prTemplate, "Verification", "PR template must capture verification.", result);
        RequireText(prTemplate, "Async CI", "PR template must capture async CI state.", result);
        RequireText(prTemplate, "Risk", "PR template must capture risk.", result);
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
        };

        foreach (var field in requiredFields)
        {
            if (!text.Contains(field, StringComparison.OrdinalIgnoreCase))
            {
                result.Error($"{relative} is missing required review field '{field}'.");
            }
        }

        if (!text.Contains("Issue / PR update:", StringComparison.OrdinalIgnoreCase)
            && !text.Contains("TODO update:", StringComparison.OrdinalIgnoreCase))
        {
            result.Error($"{relative} is missing required review field 'Issue / PR update:'; older records may use their retired progress field.");
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
