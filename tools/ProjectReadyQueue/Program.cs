using System.Text.Json;
using ProjectReadyQueue;

return Run(args);

static int Run(string[] args)
{
    if (args.SequenceEqual(["--self-test"]))
    {
        return SelfTest();
    }

    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: ProjectReadyQueue <project-state.json> <ai-ready.json> <ai-ready.md> | --self-test");
        return 2;
    }

    try
    {
        var options = JsonOptions();
        var input = JsonSerializer.Deserialize<ProjectStateInput>(File.ReadAllText(args[0]), options)
            ?? throw new InvalidDataException("project state input is empty.");
        var output = ProjectReadyEvaluator.Evaluate(input);
        File.WriteAllText(args[1], JsonSerializer.Serialize(output, options) + Environment.NewLine);
        File.WriteAllText(args[2], ProjectReadyReport.Render(output));
        Console.WriteLine($"ProjectReadyQueue wrote {output.EligibleCount} eligible issues from {output.EvaluatedCount} evaluated issues.");
        return 0;
    }
    catch (JsonException exception)
    {
        Console.Error.WriteLine($"ProjectReadyQueue input error: {exception.Message}");
        return 3;
    }
    catch (InvalidDataException exception)
    {
        Console.Error.WriteLine($"ProjectReadyQueue input error: {exception.Message}");
        return 3;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"ProjectReadyQueue failed: {exception.Message}");
        return 1;
    }
}

static int SelfTest()
{
    const string accepted = "## Acceptance\n- VerifyAll passes.";
    var issues = new List<ProjectIssue>
    {
        Issue(1, size: "S", priority: "P1 Next", labels: ["status:ready", "size:S"]),
        Issue(2, size: "M", priority: "P0 Now"),
        Issue(3, state: "CLOSED"),
        Issue(4, projectItemCount: 0, workflow: null, size: null, agent: null, verificationGate: null),
        Issue(5, projectItemCount: 2),
        Issue(6, workflow: null),
        Issue(7, workflow: "In Progress", labels: ["status:ready"]),
        Issue(8, size: null, labels: ["size:S"]),
        Issue(9, size: "L"),
        Issue(10, agent: null),
        Issue(11, agent: "Local Codex"),
        Issue(12, labels: ["status:paused"]),
        Issue(13, openSubIssues: [101]),
        Issue(14, openBlockers: [999]),
        Issue(15, body: "## 验收标准\n-\n\n## 范围\n- x"),
        Issue(16, verificationGate: null),
        Issue(17, body: "## 验收标准\n- [ ]\n\n## 范围\n- x"),
    };
    var output = ProjectReadyEvaluator.Evaluate(new ProjectStateInput(1, 3, issues));
    Require(output.Eligible.Select(item => item.Number).SequenceEqual([2, 1]), "eligible ordering must use Priority then issue number");

    var foundReasons = output.Evaluated.SelectMany(item => item.ReasonCodes).ToHashSet(StringComparer.Ordinal);
    Require(ProjectReadyEvaluator.ReasonCodeOrder.All(foundReasons.Contains), "every reason code needs a negative fixture");
    Require(output.Evaluated.Single(item => item.Number == 7).Warnings.Contains(ProjectReadyEvaluator.ReadyLabelWorkflowConflict), "ready label warning missing");
    Require(output.Evaluated.Single(item => item.Number == 8).Warnings.Contains(ProjectReadyEvaluator.SizeLabelProjectConflict), "size label warning missing");
    Require(output.Evaluated.Single(item => item.Number == 14).ReasonCodes.Contains(ProjectReadyEvaluator.OpenBlocker), "out-of-scope open blocker must block");
    Require(output.Evaluated.Single(item => item.Number == 15).ReasonCodes.Contains(ProjectReadyEvaluator.AcceptanceMissing), "empty acceptance section must fail");
    Require(output.Evaluated.Single(item => item.Number == 17).ReasonCodes.Contains(ProjectReadyEvaluator.AcceptanceMissing), "empty checkbox acceptance must fail");

    var options = JsonOptions();
    var first = JsonSerializer.Serialize(output, options);
    var second = JsonSerializer.Serialize(ProjectReadyEvaluator.Evaluate(new ProjectStateInput(1, 3, issues)), options);
    Require(first == second, "JSON output must be deterministic");
    Require(!first.Contains("\"status\":", StringComparison.Ordinal), "evaluation JSON must expose Workflow without a second status field");
    var report = ProjectReadyReport.Render(output);
    Require(report.Contains("可由 AI 领取：2", StringComparison.Ordinal), "Chinese report summary missing");
    foreach (var heading in new[] { "## 可领取", "## 等待审查", "## 受阻或暂停", "## 已完成", "## 下一步" })
    {
        Require(report.Contains(heading, StringComparison.Ordinal), $"Chinese report section missing: {heading}");
    }
    Require(ProjectReadyEvaluator.ReasonCodeOrder.All(code => !report.Contains(code, StringComparison.Ordinal)), "every reason code needs a Chinese report label");
    Require(new[] { ProjectReadyEvaluator.ReadyLabelWorkflowConflict, ProjectReadyEvaluator.SizeLabelProjectConflict }
        .All(code => !report.Contains(code, StringComparison.Ordinal)), "every warning code needs a Chinese report label");
    var inProgressOnly = ProjectReadyEvaluator.Evaluate(new ProjectStateInput(
        1,
        3,
        [Issue(18, workflow: "In Progress", agent: "Local Codex")]));
    Require(ProjectReadyReport.Render(inProgressOnly).Contains("继续推进", StringComparison.Ordinal), "in-progress-only report needs an accurate next action");
    var reviewSelection = ProjectReadyEvaluator.Evaluate(new ProjectStateInput(
        1,
        3,
        [
            Issue(19, workflow: "Review", agent: "Local Codex", openBlockers: [999]),
            Issue(20, workflow: "Review", agent: "Local Codex", labels: ["status:paused"]),
            Issue(21, workflow: "Review", agent: "Local Codex"),
        ]));
    Require(ProjectReadyReport.Render(reviewSelection).Contains("先完成 [#21", StringComparison.Ordinal), "next action must skip blocked and paused review items");
    var inProgressSelection = ProjectReadyEvaluator.Evaluate(new ProjectStateInput(
        1,
        3,
        [
            Issue(22, workflow: "In Progress", agent: "Local Codex", openBlockers: [999]),
            Issue(23, workflow: "In Progress", agent: "Local Codex"),
        ]));
    Require(ProjectReadyReport.Render(inProgressSelection).Contains("继续推进 [#23", StringComparison.Ordinal), "next action must skip blocked in-progress items");
    var blockedOnly = ProjectReadyEvaluator.Evaluate(new ProjectStateInput(
        1,
        3,
        [Issue(24, workflow: "Review", agent: "Local Codex", labels: ["status:paused"])]));
    Require(ProjectReadyReport.Render(blockedOnly).Contains("先解除 [#24", StringComparison.Ordinal), "blocked-only report must surface the blocker instead of review work");

    Console.WriteLine("ProjectReadyQueue self-test PASSED.");
    return 0;

    static ProjectIssue Issue(
        int number,
        string state = "OPEN",
        int? projectItemCount = 1,
        string? workflow = "Ready",
        string? priority = "P2 Later",
        string? size = "S",
        string? agent = "Unassigned",
        string? verificationGate = "VerifyAll",
        string? body = accepted,
        List<string>? labels = null,
        List<int>? openSubIssues = null,
        List<int>? openBlockers = null) =>
        new(number, $"Issue {number}", $"https://example.test/issues/{number}", state, projectItemCount, workflow, priority, size, agent, verificationGate, body, labels ?? [], openSubIssues ?? [], openBlockers ?? []);
}

static JsonSerializerOptions JsonOptions() => new()
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
};

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException($"ProjectReadyQueue self-test failed: {message}");
    }
}
