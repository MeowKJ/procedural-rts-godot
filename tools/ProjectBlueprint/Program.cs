using System.Text.Json;
using System.Text.Json.Serialization;

if (args.SequenceEqual(["--self-test"]))
{
    var fixture = new BlueprintInput(
        "Fixture",
        [
            new BlueprintIssue(10, "Parent", "https://example.test/issues/10", "OPEN", "In Progress", null, [11], [], []),
            new BlueprintIssue(11, "Child", "https://example.test/issues/11", "OPEN", "Ready", 10, [], [10], [])
        ]);
    var rendered = Render(fixture);
    Require(rendered.Contains("I10 --> I11", StringComparison.Ordinal), "parent edge is missing");
    Require(rendered.Contains("I10 -. blocks .-> I11", StringComparison.Ordinal), "dependency edge is missing");
    Require(rendered.Contains("click I11 \"https://example.test/issues/11\"", StringComparison.Ordinal), "issue link is missing");
    Console.WriteLine("ProjectBlueprint self-test PASSED.");
    return;
}

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ProjectBlueprint <input.json> <output.md> | --self-test");
    Environment.Exit(2);
}

var input = JsonSerializer.Deserialize<BlueprintInput>(File.ReadAllText(args[0]), new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
})
    ?? throw new InvalidOperationException("Blueprint input is empty.");
File.WriteAllText(args[1], Render(input));
Console.WriteLine($"ProjectBlueprint wrote {args[1]} for {input.Issues.Count} issues.");

static string Render(BlueprintInput input)
{
    var issues = input.Issues.OrderBy(issue => issue.Number).ToList();
    var inScope = issues.Select(issue => issue.Number).ToHashSet();
    var issuesByNumber = issues.ToDictionary(issue => issue.Number);
    var lines = new List<string>
    {
        $"# {input.Title}",
        string.Empty,
        "Generated from GitHub-native issue parents, sub-issues, dependencies, and Project Workflow values.",
        string.Empty,
        "```mermaid",
        "flowchart TD"
    };

    foreach (var issue in issues)
    {
        lines.Add($"  I{issue.Number}[\"#{issue.Number} {Escape(issue.Title)}\\n{Escape(issue.Workflow ?? issue.State)}\"]");
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
            edges.Add($"  I{blocker} -. blocks .-> I{issue.Number}");
        }
    }

    lines.AddRange(edges.OrderBy(edge => edge, StringComparer.Ordinal));
    foreach (var issue in issues)
    {
        lines.Add($"  click I{issue.Number} \"{issue.Url}\" \"Open issue #{issue.Number}\"");
    }

    lines.Add("  classDef Done fill:#d1fae5,stroke:#047857,color:#064e3b");
    lines.Add("  classDef Ready fill:#dbeafe,stroke:#1d4ed8,color:#172554");
    lines.Add("  classDef Active fill:#fef3c7,stroke:#b45309,color:#451a03");
    lines.Add("  classDef Blocked fill:#fee2e2,stroke:#b91c1c,color:#450a0a");
    foreach (var issue in issues)
    {
        lines.Add($"  class I{issue.Number} {ClassFor(issue, issuesByNumber)}");
    }

    lines.Add("```");
    lines.Add(string.Empty);
    lines.Add($"Issue count: {issues.Count}.");
    return string.Join(Environment.NewLine, lines) + Environment.NewLine;
}

static string ClassFor(BlueprintIssue issue, IReadOnlyDictionary<int, BlueprintIssue> issuesByNumber)
{
    if (string.Equals(issue.State, "CLOSED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(issue.Workflow, "Done", StringComparison.OrdinalIgnoreCase))
    {
        return "Done";
    }

    if (issue.BlockedBy.Any(number =>
        issuesByNumber.TryGetValue(number, out var blocker) &&
        !string.Equals(blocker.State, "CLOSED", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(blocker.Workflow, "Done", StringComparison.OrdinalIgnoreCase)))
    {
        return "Blocked";
    }

    return string.Equals(issue.Workflow, "Ready", StringComparison.OrdinalIgnoreCase) ? "Ready" : "Active";
}

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

sealed record BlueprintInput([property: JsonPropertyName("title")] string Title, [property: JsonPropertyName("issues")] List<BlueprintIssue> Issues);

sealed record BlueprintIssue(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("workflow")] string? Workflow,
    [property: JsonPropertyName("parent")] int? Parent,
    [property: JsonPropertyName("subIssues")] List<int> SubIssues,
    [property: JsonPropertyName("blockedBy")] List<int> BlockedBy,
    [property: JsonPropertyName("blocking")] List<int> Blocking);
