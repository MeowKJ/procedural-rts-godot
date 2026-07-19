var root = FindRoot();
var failures = new List<string>();

CheckTrustedSelfHostedWorkflow("preflight", Path.Combine(root, ".github", "workflows", "preflight.yml"), failures);
CheckTrustedSelfHostedWorkflow("verify-all", Path.Combine(root, ".github", "workflows", "verify-all.yml"), failures);
CheckEverySelfHostedWorkflow(root, failures);

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
