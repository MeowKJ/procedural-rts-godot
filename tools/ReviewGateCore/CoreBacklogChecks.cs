static class CoreBacklogChecks
{
    public static void CheckBacklogProtocol(string root, GateResult result)
    {
        var todoPath = Path.Combine(root, "TODO.md");
        var agentsPath = Path.Combine(root, "AGENTS.md");
        var aiDocsPath = Path.Combine(root, "docs", "ai");
        var issueTemplatePath = Path.Combine(root, ".github", "ISSUE_TEMPLATE", "codex-slice.md");
        var prTemplatePath = Path.Combine(root, ".github", "pull_request_template.md");

        if (File.Exists(todoPath))
        {
            result.Error("TODO.md must not exist; active work belongs in GitHub Issues and the GitHub Project.");
        }

        var docsPath = Path.Combine(root, "docs");
        if (Directory.Exists(docsPath))
        {
            foreach (var archivePath in Directory.EnumerateFiles(docsPath, "TODO-Archive-*.md", SearchOption.TopDirectoryOnly))
            {
                result.Error($"{Path.GetRelativePath(root, archivePath)} must not exist; historical backlog belongs in Git history or GitHub issues.");
            }
        }

        if (Directory.Exists(aiDocsPath)
            && Directory.EnumerateFileSystemEntries(aiDocsPath).Any())
        {
            result.Error("docs/ai must not contain local process docs; keep Codex workflow in AGENTS.md and GitHub templates.");
        }

        if (!File.Exists(agentsPath))
        {
            result.Error("AGENTS.md is missing.");
            return;
        }

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

        var agents = File.ReadAllText(agentsPath);
        var issueTemplate = File.ReadAllText(issueTemplatePath);
        var prTemplate = File.ReadAllText(prTemplatePath);

        RequireText(agents, "GitHub Issues", "AGENTS.md must route agents to GitHub Issues.", result);
        RequireText(agents, "GitHub Project", "AGENTS.md must route sequencing to the GitHub Project.", result);
        RequireText(agents, "Do not create a local backlog file", "AGENTS.md must forbid local backlog files.", result);
        RequireText(agents, "Do not create new local process docs", "AGENTS.md must forbid new local process docs.", result);
        RequireText(agents, "GitHub issue", "AGENTS.md must make GitHub issue context the default reading surface.", result);
        RequireText(issueTemplate, "Context pack", "Issue template must carry compact context instead of local process docs.", result);
        RequireText(issueTemplate, "Evidence destination", "Issue template must route evidence away from local markdown records.", result);
        RequireText(issueTemplate, "GitHub PR, issue comments, and CI artifacts only", "Issue template must keep evidence in GitHub/CI.", result);
        RequireText(prTemplate, "Async CI", "PR template must include async CI state.", result);
        RequireText(prTemplate, "Verification", "PR template must include verification evidence.", result);
    }
}
