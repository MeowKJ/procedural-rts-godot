static class CoreReviewChecks
{
    public static void CheckReviewTemplate(string root, GateResult result)
    {
        CheckGitHubReviewSurface(root, result);
        CheckCommentDiscipline(root, result);
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

}
