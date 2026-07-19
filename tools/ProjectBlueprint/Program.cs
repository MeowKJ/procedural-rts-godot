using System.Text.Json;
using System.Text.Json.Serialization;

if (args.SequenceEqual(["--self-test"]))
{
    var fixture = new BlueprintInput(
        "测试任务蓝图",
        [
            new BlueprintIssue(10, "父任务", "https://example.test/issues/10", "OPEN", "In Progress", null, [11], [], [], []),
            new BlueprintIssue(11, "受阻子任务", "https://example.test/issues/11", "OPEN", "Ready", 10, [], [10], [], [10]),
            new BlueprintIssue(12, "审查任务", "https://example.test/issues/12", "OPEN", "Review", null, [], [], [], []),
        ]);
    var rendered = Render(fixture);
    Require(rendered.Contains("I10 --> I11", StringComparison.Ordinal), "parent edge is missing");
    Require(rendered.Contains("I10 -. 阻塞 .-> I11", StringComparison.Ordinal), "dependency edge is missing");
    Require(rendered.Contains("click I11 \"https://example.test/issues/11\" \"打开 Issue #11\"", StringComparison.Ordinal), "issue link is missing");
    Require(rendered.Contains("class I12 Review", StringComparison.Ordinal), "Review must have its own stable class");
    Require(rendered.Contains("数据来自 GitHub 原生父子关系", StringComparison.Ordinal), "Chinese explanation is missing");
    Console.WriteLine("ProjectBlueprint self-test PASSED.");
    return;
}

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ProjectBlueprint <project-state.json> <project-blueprint.md> | --self-test");
    Environment.Exit(2);
}

var input = JsonSerializer.Deserialize<BlueprintInput>(File.ReadAllText(args[0]), new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
}) ?? throw new InvalidOperationException("Blueprint input is empty.");
File.WriteAllText(args[1], Render(input));
Console.WriteLine($"ProjectBlueprint wrote {args[1]} for {input.Issues.Count} issues.");

static string Render(BlueprintInput input)
{
    var issues = input.Issues.OrderBy(issue => issue.Number).ToList();
    var inScope = issues.Select(issue => issue.Number).ToHashSet();
    var issuesByNumber = issues.ToDictionary(issue => issue.Number);
    var classes = issues.ToDictionary(issue => issue.Number, issue => ClassFor(issue, issuesByNumber));
    var lines = new List<string>
    {
        $"# {input.Title}",
        string.Empty,
        "数据来自 GitHub 原生父子关系、依赖关系及 Project 的 `Workflow` 字段。",
        string.Empty,
        "图例：待规划 / 可领取 / 正在实现 / 等待审查 / 受阻 / 已完成。",
        string.Empty,
        "```mermaid",
        "flowchart TD",
    };

    foreach (var issue in issues)
    {
        lines.Add($"  I{issue.Number}[\"#{issue.Number} {Escape(issue.Title)}\\n{ClassLabel(classes[issue.Number])}\"]");
    }

    var edges = new HashSet<string>(StringComparer.Ordinal);
    foreach (var issue in issues)
    {
        if (issue.Parent is { } parent && inScope.Contains(parent))
        {
            edges.Add($"  I{parent} --> I{issue.Number}");
        }

        foreach (var child in issue.SubIssues.Where(inScope.Contains))
        {
            edges.Add($"  I{issue.Number} --> I{child}");
        }

        foreach (var blocker in issue.BlockedBy.Where(inScope.Contains))
        {
            edges.Add($"  I{blocker} -. 阻塞 .-> I{issue.Number}");
        }
    }

    lines.AddRange(edges.OrderBy(edge => edge, StringComparer.Ordinal));
    foreach (var issue in issues)
    {
        lines.Add($"  click I{issue.Number} \"{issue.Url}\" \"打开 Issue #{issue.Number}\"");
    }

    lines.Add("  classDef Planned fill:#f3f4f6,stroke:#6b7280,color:#111827");
    lines.Add("  classDef Ready fill:#dbeafe,stroke:#1d4ed8,color:#172554");
    lines.Add("  classDef InProgress fill:#fef3c7,stroke:#b45309,color:#451a03");
    lines.Add("  classDef Review fill:#ede9fe,stroke:#7c3aed,color:#2e1065");
    lines.Add("  classDef Blocked fill:#fee2e2,stroke:#b91c1c,color:#450a0a");
    lines.Add("  classDef Done fill:#d1fae5,stroke:#047857,color:#064e3b");
    foreach (var issue in issues)
    {
        lines.Add($"  class I{issue.Number} {classes[issue.Number]}");
    }

    lines.Add("```");
    lines.Add(string.Empty);
    lines.Add("## 进度统计");
    lines.Add(string.Empty);
    lines.Add($"- Issue 总数：{issues.Count}");
    foreach (var className in new[] { "Planned", "Ready", "InProgress", "Review", "Blocked", "Done" })
    {
        lines.Add($"- {ClassLabel(className)}：{classes.Values.Count(value => value == className)}");
    }

    lines.Add(string.Empty);
    lines.Add("## 下一步");
    lines.Add(string.Empty);
    lines.Add("打开同一 artifact 中的中文 `ai-ready.md` 查看下一项工作；机器领取只以 `ai-ready.json` 的判定结果为准。本图只展示关系和进度。");
    return string.Join(Environment.NewLine, lines) + Environment.NewLine;
}

static string ClassFor(BlueprintIssue issue, IReadOnlyDictionary<int, BlueprintIssue> issuesByNumber)
{
    if (string.Equals(issue.State, "CLOSED", StringComparison.OrdinalIgnoreCase) || issue.Workflow == "Done")
    {
        return "Done";
    }

    var openBlockers = issue.OpenBlockers ?? issue.BlockedBy.Where(number =>
        issuesByNumber.TryGetValue(number, out var blocker) &&
        !string.Equals(blocker.State, "CLOSED", StringComparison.OrdinalIgnoreCase) &&
        blocker.Workflow != "Done").ToList();
    if (openBlockers.Count > 0)
    {
        return "Blocked";
    }

    return issue.Workflow switch
    {
        "Ready" => "Ready",
        "In Progress" => "InProgress",
        "Review" => "Review",
        _ => "Planned",
    };
}

static string ClassLabel(string className) => className switch
{
    "Ready" => "可领取",
    "InProgress" => "正在实现",
    "Review" => "等待审查",
    "Blocked" => "受阻",
    "Done" => "已完成",
    _ => "待规划",
};

static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
    .Replace("\"", "'", StringComparison.Ordinal)
    .Replace("[", "(", StringComparison.Ordinal)
    .Replace("]", ")", StringComparison.Ordinal)
    .Replace("\r", " ", StringComparison.Ordinal)
    .Replace("\n", " ", StringComparison.Ordinal);

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException($"ProjectBlueprint self-test failed: {message}");
    }
}

sealed record BlueprintInput(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("issues")] List<BlueprintIssue> Issues);

sealed record BlueprintIssue(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("workflow")] string? Workflow,
    [property: JsonPropertyName("parent")] int? Parent,
    [property: JsonPropertyName("subIssues")] List<int> SubIssues,
    [property: JsonPropertyName("blockedBy")] List<int> BlockedBy,
    [property: JsonPropertyName("blocking")] List<int> Blocking,
    [property: JsonPropertyName("openBlockers")] List<int>? OpenBlockers);
