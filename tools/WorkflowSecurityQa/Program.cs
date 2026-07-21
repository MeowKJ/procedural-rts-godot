using System.Text.RegularExpressions;

var root = FindRoot();
var failures = new List<string>();

CheckHostedValidationWorkflow("preflight", Path.Combine(root, ".github", "workflows", "preflight.yml"), failures);
CheckHostedValidationWorkflow("verify-all", Path.Combine(root, ".github", "workflows", "verify-all.yml"), failures);
CheckHostedPrivilegedWorkflows(root, failures);
CheckEveryWorkflowUsesPinnedHostedActions(root, failures);
CheckPublicArtifactBoundary(root, failures);
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

Console.WriteLine("WorkflowSecurityQa PASSED: GitHub-hosted workflows use trusted triggers, pinned actions, and public-safe artifacts.");

static void CheckHostedValidationWorkflow(string name, string path, List<string> failures)
{
    Require(File.Exists(path), $"{name} workflow is missing: {path}", failures);
    if (!File.Exists(path))
    {
        return;
    }

    var source = File.ReadAllText(path);
    RequireExactSection(
        source,
        "on:\n",
        "\npermissions:",
        "on:\n  workflow_dispatch:\n  push:\n    branches:\n      - main\n      - 'codex/**'",
        $"{name} triggers",
        failures);
    RequireExactSection(
        source,
        "permissions:\n",
        "\nconcurrency:",
        "permissions:\n  contents: read",
        $"{name} permissions",
        failures);
    Require(source.Contains("runs-on: ubuntu-24.04", StringComparison.Ordinal), $"{name} must use the pinned GitHub-hosted Ubuntu image", failures);
    Require(!Regex.IsMatch(source, @"\bsecrets\b", RegexOptions.CultureInvariant), $"{name} must not receive repository secrets", failures);
}

static void CheckHostedPrivilegedWorkflows(string root, List<string> failures)
{
    var workflows = Path.Combine(root, ".github", "workflows");
    var blueprint = File.ReadAllText(Path.Combine(workflows, "project-blueprint.yml"));
    Require(blueprint.Contains("runs-on: ubuntu-24.04", StringComparison.Ordinal), "project blueprint must use the pinned GitHub-hosted Ubuntu image", failures);
    RequireExactSection(
        blueprint,
        "on:\n",
        "\npermissions:",
        "on:\n  workflow_dispatch:\n  schedule:\n    - cron: '17 3 * * 1'",
        "project blueprint triggers",
        failures);
    RequireExactSection(
        blueprint,
        "permissions:\n",
        "\nconcurrency:",
        "permissions:\n  contents: read\n  issues: write",
        "project blueprint permissions",
        failures);

    var status = File.ReadAllText(Path.Combine(workflows, "verify-all-status.yml"));
    Require(status.Contains("runs-on: ubuntu-24.04", StringComparison.Ordinal), "VerifyAll status must use the pinned GitHub-hosted Ubuntu image", failures);
    RequireExactSection(
        status,
        "on:\n",
        "\npermissions:",
        "on:\n  workflow_run:\n    workflows:\n      - VerifyAll\n    types:\n      - requested\n      - completed",
        "VerifyAll status triggers",
        failures);
    RequireExactSection(
        status,
        "permissions:\n",
        "\nconcurrency:",
        "permissions:\n  actions: read\n  pull-requests: write",
        "VerifyAll status permissions",
        failures);
    Require(!status.Contains("actions/checkout@", StringComparison.Ordinal), "VerifyAll status must not checkout upstream code", failures);
    Require(!status.Contains("actions/download-artifact@", StringComparison.Ordinal), "VerifyAll status must not execute or download upstream artifacts", failures);
}

static void CheckEveryWorkflowUsesPinnedHostedActions(string root, List<string> failures)
{
    var workflows = Path.Combine(root, ".github", "workflows");
    var hostedRunner = new Regex(@"^runs-on:\s+(ubuntu|windows|macos)-[A-Za-z0-9.]+$", RegexOptions.CultureInvariant);
    var pinnedAction = new Regex(@"^uses:\s+actions/[A-Za-z0-9_.-]+@[0-9a-f]{40}(?:\s+#\s+.+)?$", RegexOptions.CultureInvariant);

    foreach (var path in Directory.EnumerateFiles(workflows)
                 .Where(path => Path.GetExtension(path) is ".yml" or ".yaml"))
    {
        var source = File.ReadAllText(path);
        Require(!source.Contains("self-hosted", StringComparison.OrdinalIgnoreCase), $"{Path.GetFileName(path)} must not reference a self-hosted runner", failures);
        Require(!source.Contains("pull_request_target", StringComparison.Ordinal), $"{Path.GetFileName(path)} must not use pull_request_target", failures);

        foreach (var rawLine in source.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                line = line[2..].TrimStart();
            }

            if (line.StartsWith("runs-on:", StringComparison.Ordinal))
            {
                Require(hostedRunner.IsMatch(line), $"{Path.GetFileName(path)} must pin a GitHub-hosted runner image: {line}", failures);
            }

            if (line.StartsWith("uses:", StringComparison.Ordinal) && !line.StartsWith("uses: ./", StringComparison.Ordinal))
            {
                Require(pinnedAction.IsMatch(line), $"{Path.GetFileName(path)} must pin GitHub-owned actions to a full commit SHA: {line}", failures);
            }
        }
    }
}

static void CheckPublicArtifactBoundary(string root, List<string> failures)
{
    var path = Path.Combine(root, ".github", "workflows", "verify-all.yml");
    var source = File.ReadAllText(path);
    var lines = source.Split('\n').Select(line => line.Trim()).ToArray();

    Require(!lines.Contains("artifacts/**", StringComparer.Ordinal), "VerifyAll must not publish every generated artifact", failures);
    Require(!lines.Any(line => line.Contains("artifacts/dotnet", StringComparison.Ordinal)), "VerifyAll must not publish build intermediates or PDBs", failures);
    Require(lines.Contains("artifacts/**/*.json", StringComparer.Ordinal), "VerifyAll must publish structured JSON evidence", failures);
    Require(lines.Contains("artifacts/**/*.png", StringComparer.Ordinal), "VerifyAll must publish visual PNG evidence", failures);
    Require(source.Contains("retention-days: 14", StringComparison.Ordinal), "public CI artifacts must have bounded retention", failures);
}

static void RequireExactSection(
    string source,
    string startMarker,
    string endMarker,
    string expected,
    string name,
    List<string> failures)
{
    var start = source.IndexOf(startMarker, StringComparison.Ordinal);
    if (start < 0)
    {
        failures.Add($"{name} is missing {startMarker.Trim()}");
        return;
    }

    var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
    if (end < 0)
    {
        failures.Add($"{name} is missing {endMarker.Trim()}");
        return;
    }

    var actual = source[start..end].TrimEnd();
    Require(actual.Equals(expected, StringComparison.Ordinal), $"{name} changed outside its reviewed boundary", failures);
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
        "DOTNET_INSTALL_DIR: ~/.dotnet",
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
