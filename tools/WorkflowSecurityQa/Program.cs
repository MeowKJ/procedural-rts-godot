var root = FindRoot();
var failures = new List<string>();

CheckTrustedSelfHostedWorkflow("preflight", Path.Combine(root, ".github", "workflows", "preflight.yml"), failures);
CheckTrustedSelfHostedWorkflow("verify-all", Path.Combine(root, ".github", "workflows", "verify-all.yml"), failures);
CheckEverySelfHostedWorkflow(root, failures);
CheckAiFriendlyGitHubSurface(root, failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("WorkflowSecurityQa FAILED:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Environment.Exit(1);
}

Console.WriteLine("WorkflowSecurityQa PASSED: self-hosted CI accepts only trusted pushes and maintainer dispatches.");

static void CheckTrustedSelfHostedWorkflow(string name, string path, List<string> failures)
{
    Require(File.Exists(path), $"{name} workflow is missing: {path}", failures);
    if (!File.Exists(path))
    {
        return;
    }

    var source = File.ReadAllText(path);
    var triggerSection = source.Split("permissions:", 2, StringSplitOptions.None)[0];
    Require(triggerSection.Contains("workflow_dispatch:", StringComparison.Ordinal), $"{name} must support maintainer dispatch", failures);
    Require(triggerSection.Contains("push:", StringComparison.Ordinal), $"{name} must run for trusted pushes", failures);
    Require(triggerSection.Contains("- main", StringComparison.Ordinal), $"{name} must cover main", failures);
    Require(triggerSection.Contains("- 'codex/**'", StringComparison.Ordinal), $"{name} must cover trusted Codex branches", failures);
    Require(!triggerSection.Contains("pull_request:", StringComparison.Ordinal), $"{name} must not run self-hosted jobs for pull requests", failures);
    Require(!triggerSection.Contains("pull_request_target:", StringComparison.Ordinal), $"{name} must not use pull_request_target", failures);
    Require(source.Contains("runs-on: [self-hosted, linux, x64, procedural-rts]", StringComparison.Ordinal), $"{name} must keep the trusted Linux runner", failures);
}

static void CheckEverySelfHostedWorkflow(string root, List<string> failures)
{
    var workflows = Path.Combine(root, ".github", "workflows");
    foreach (var path in Directory.EnumerateFiles(workflows, "*.yml"))
    {
        var source = File.ReadAllText(path);
        if (!source.Contains("runs-on: [self-hosted, linux, x64, procedural-rts]", StringComparison.Ordinal))
        {
            continue;
        }

        var triggerSection = source.Split("permissions:", 2, StringSplitOptions.None)[0];
        Require(!triggerSection.Contains("pull_request:", StringComparison.Ordinal), $"{Path.GetFileName(path)} must not run on pull_request", failures);
        Require(!triggerSection.Contains("pull_request_target:", StringComparison.Ordinal), $"{Path.GetFileName(path)} must not run on pull_request_target", failures);
    }
}

static void CheckAiFriendlyGitHubSurface(string root, List<string> failures)
{
    var issueTemplate = File.ReadAllText(Path.Combine(root, ".github", "ISSUE_TEMPLATE", "codex-slice.md"));
    var pullRequestTemplate = File.ReadAllText(Path.Combine(root, ".github", "pull_request_template.md"));
    var verifyAllStatus = File.ReadAllText(Path.Combine(root, ".github", "workflows", "verify-all-status.yml"));
    var projectBlueprint = File.ReadAllText(Path.Combine(root, ".github", "workflows", "project-blueprint.yml"));

    Require(issueTemplate.Contains("## 验收标准", StringComparison.Ordinal), "Codex issue template must expose a Chinese acceptance section", failures);
    Require(!issueTemplate.Contains("labels: \"status:ready", StringComparison.Ordinal), "new Codex issues must not claim Ready through a default label", failures);
    Require(pullRequestTemplate.Contains("## 验证 / Verification", StringComparison.Ordinal), "pull request template must show Chinese verification guidance", failures);

    Require(verifyAllStatus.Contains("<!-- verify-all-meta schema=1 run=", StringComparison.Ordinal), "VerifyAll status must keep stable hidden metadata", failures);
    Require(verifyAllStatus.Contains("metadataRunMatch || legacyRunMatch", StringComparison.Ordinal), "VerifyAll status must parse translated metadata with legacy fallback", failures);
    Require(verifyAllStatus.Contains("**完整验证：**", StringComparison.Ordinal), "VerifyAll status must show Chinese human progress", failures);
    Require(verifyAllStatus.StartsWith("name: VerifyAll PR Status", StringComparison.Ordinal), "VerifyAll status must keep its stable workflow identifier", failures);
    Require(verifyAllStatus.Contains("run-name: VerifyAll PR 中文状态", StringComparison.Ordinal), "VerifyAll runs must have a Chinese display name", failures);
    Require(verifyAllStatus.Contains("candidate.state === \"open\" && candidate.head.sha === run.head_sha", StringComparison.Ordinal), "VerifyAll status must filter every candidate to an open exact-head PR", failures);
    Require(verifyAllStatus.Contains("for (const pullRequest of pullRequests)", StringComparison.Ordinal), "VerifyAll status must update every matching pull request", failures);

    foreach (var token in new[]
    {
        "projectItemCount",
        "verificationGate",
        "openSubIssues",
        "openBlockers",
        "github-token: ${{ secrets.PROJECTS_TOKEN }}",
        "缺少 PROJECTS_TOKEN",
        "projectProbe.node?.id !== projectId || projectProbe.node?.number !== 3",
        "tools/ProjectReadyQueue/ProjectReadyQueue.csproj",
        "project-state.json",
        "ai-ready.json",
        "ai-ready.md",
        "project-blueprint.md",
    })
    {
        Require(projectBlueprint.Contains(token, StringComparison.Ordinal), $"project blueprint workflow is missing {token}", failures);
    }
}

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static string FindRoot()
{
    var current = Directory.GetCurrentDirectory();
    while (!string.IsNullOrEmpty(current))
    {
        if (File.Exists(Path.Combine(current, "ProceduralRts.csproj")))
        {
            return current;
        }

        current = Directory.GetParent(current)?.FullName ?? string.Empty;
    }

    throw new DirectoryNotFoundException("Could not locate ProceduralRts.csproj from the current directory.");
}
