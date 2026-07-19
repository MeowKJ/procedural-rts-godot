namespace ProjectReadyQueue;

public static class ProjectReadyEvaluator
{
    public const string IssueClosed = "ISSUE_CLOSED";
    public const string ProjectItemMissing = "PROJECT_ITEM_MISSING";
    public const string ProjectItemAmbiguous = "PROJECT_ITEM_AMBIGUOUS";
    public const string WorkflowMissing = "WORKFLOW_MISSING";
    public const string WorkflowNotReady = "WORKFLOW_NOT_READY";
    public const string SizeMissing = "SIZE_MISSING";
    public const string SizeNotEligible = "SIZE_NOT_ELIGIBLE";
    public const string AgentMissing = "AGENT_MISSING";
    public const string AgentActive = "AGENT_ACTIVE";
    public const string Paused = "PAUSED";
    public const string OpenSubIssue = "OPEN_SUBISSUE";
    public const string OpenBlocker = "OPEN_BLOCKER";
    public const string AcceptanceMissing = "ACCEPTANCE_MISSING";
    public const string VerificationGateMissing = "VERIFICATION_GATE_MISSING";

    public const string StatusWorkflowConflict = "STATUS_WORKFLOW_CONFLICT";
    public const string ReadyLabelWorkflowConflict = "READY_LABEL_WORKFLOW_CONFLICT";
    public const string SizeLabelProjectConflict = "SIZE_LABEL_PROJECT_CONFLICT";

    public static readonly IReadOnlyList<string> ReasonCodeOrder =
    [
        IssueClosed,
        ProjectItemMissing,
        ProjectItemAmbiguous,
        WorkflowMissing,
        WorkflowNotReady,
        SizeMissing,
        SizeNotEligible,
        AgentMissing,
        AgentActive,
        Paused,
        OpenSubIssue,
        OpenBlocker,
        AcceptanceMissing,
        VerificationGateMissing,
    ];

    public static ProjectReadyOutput Evaluate(ProjectStateInput input)
    {
        Validate(input);
        var evaluated = input.Issues
            .OrderBy(issue => issue.Number)
            .Select(EvaluateOne)
            .ToList();
        var eligible = evaluated
            .Where(item => item.Eligible)
            .OrderBy(item => PriorityRank(item.Priority))
            .ThenBy(item => item.Number)
            .ToList();
        return new ProjectReadyOutput(
            input.SchemaVersion!.Value,
            input.ProjectNumber!.Value,
            eligible.Count,
            evaluated.Count,
            eligible,
            evaluated);
    }

    private static ProjectReadyEvaluation EvaluateOne(ProjectIssue issue)
    {
        var labels = issue.Labels!;
        var openSubIssues = issue.OpenSubIssues!.Order().ToList();
        var openBlockers = issue.OpenBlockers!.Order().ToList();
        var paused = labels.Contains("status:paused", StringComparer.OrdinalIgnoreCase);
        var reasons = new List<string>();

        Add(!string.Equals(issue.State, "OPEN", StringComparison.OrdinalIgnoreCase), IssueClosed, reasons);
        Add(issue.ProjectItemCount == 0, ProjectItemMissing, reasons);
        Add(issue.ProjectItemCount > 1, ProjectItemAmbiguous, reasons);
        Add(string.IsNullOrWhiteSpace(issue.Workflow), WorkflowMissing, reasons);
        Add(!string.IsNullOrWhiteSpace(issue.Workflow) &&
            !string.Equals(issue.Workflow, "Ready", StringComparison.Ordinal), WorkflowNotReady, reasons);
        Add(string.IsNullOrWhiteSpace(issue.Size), SizeMissing, reasons);
        Add(!string.IsNullOrWhiteSpace(issue.Size) && issue.Size is not ("S" or "M"), SizeNotEligible, reasons);
        Add(string.IsNullOrWhiteSpace(issue.Agent), AgentMissing, reasons);
        Add(!string.IsNullOrWhiteSpace(issue.Agent) &&
            !string.Equals(issue.Agent, "Unassigned", StringComparison.Ordinal), AgentActive, reasons);
        Add(paused, Paused, reasons);
        Add(openSubIssues.Count > 0, OpenSubIssue, reasons);
        Add(openBlockers.Count > 0, OpenBlocker, reasons);
        Add(!HasNonEmptyAcceptance(issue.Body), AcceptanceMissing, reasons);
        Add(string.IsNullOrWhiteSpace(issue.VerificationGate), VerificationGateMissing, reasons);

        var warnings = WarningsFor(issue, labels);
        return new ProjectReadyEvaluation(
            issue.Number,
            issue.Title,
            issue.Url,
            issue.State,
            issue.Workflow,
            issue.Status,
            issue.Priority,
            issue.Size,
            issue.Agent,
            issue.VerificationGate,
            paused,
            openSubIssues,
            openBlockers,
            reasons.Count == 0,
            reasons,
            warnings);
    }

    private static IReadOnlyList<string> WarningsFor(ProjectIssue issue, IReadOnlyList<string> labels)
    {
        var warnings = new List<string>();
        var statusConflict = issue.Status switch
        {
            "Done" => issue.Workflow != "Done",
            "In Progress" => issue.Workflow is not ("In Progress" or "Review"),
            "Todo" => issue.Workflow is not ("Backlog" or "Ready"),
            _ => false,
        } || issue.Workflow == "Done" && issue.Status != "Done";
        Add(statusConflict, StatusWorkflowConflict, warnings);

        var hasReadyLabel = labels.Contains("status:ready", StringComparer.OrdinalIgnoreCase);
        Add(hasReadyLabel && issue.Workflow != "Ready", ReadyLabelWorkflowConflict, warnings);

        var sizeLabels = labels
            .Where(label => label.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
            .Select(label => label["size:".Length..])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Add(sizeLabels.Count > 0 &&
            (sizeLabels.Count != 1 || !string.Equals(sizeLabels[0], issue.Size, StringComparison.OrdinalIgnoreCase)),
            SizeLabelProjectConflict,
            warnings);
        return warnings;
    }

    private static bool HasNonEmptyAcceptance(string? body)
    {
        var lines = (body ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var heading = lines[index].Trim();
            if (heading is not ("## Acceptance" or "## 验收标准"))
            {
                continue;
            }

            for (var contentIndex = index + 1; contentIndex < lines.Length; contentIndex++)
            {
                var content = lines[contentIndex].Trim();
                if (content.StartsWith("## ", StringComparison.Ordinal))
                {
                    break;
                }

                if (content.Length == 0 || content.StartsWith("<!--", StringComparison.Ordinal))
                {
                    continue;
                }

                var meaningful = content.TrimStart('-', '*').Trim();
                if (meaningful.StartsWith("[", StringComparison.Ordinal) &&
                    meaningful.Length >= 3 &&
                    meaningful[2] == ']')
                {
                    meaningful = meaningful[3..].Trim();
                }

                if (meaningful.Length > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void Validate(ProjectStateInput input)
    {
        if (input.SchemaVersion != 1)
        {
            throw new InvalidDataException($"unsupported or missing schemaVersion: {input.SchemaVersion?.ToString() ?? "null"}.");
        }

        if (input.ProjectNumber != 3)
        {
            throw new InvalidDataException($"unsupported or missing projectNumber: {input.ProjectNumber?.ToString() ?? "null"}.");
        }

        if (input.Issues is null)
        {
            throw new InvalidDataException("issues must be an array.");
        }

        var duplicates = input.Issues.GroupBy(issue => issue.Number).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidDataException($"duplicate issue numbers: {string.Join(", ", duplicates)}");
        }

        foreach (var issue in input.Issues)
        {
            if (issue.Number <= 0 || string.IsNullOrWhiteSpace(issue.Title) || string.IsNullOrWhiteSpace(issue.Url) || string.IsNullOrWhiteSpace(issue.State))
            {
                throw new InvalidDataException($"issue #{issue.Number} is missing required identity fields.");
            }

            if (issue.ProjectItemCount is null or < 0 || issue.Labels is null || issue.OpenSubIssues is null || issue.OpenBlockers is null)
            {
                throw new InvalidDataException($"issue #{issue.Number} has an incomplete normalized project state.");
            }
        }
    }

    private static int PriorityRank(string? priority) => priority switch
    {
        "P0 Now" => 0,
        "P1 Next" => 1,
        "P2 Later" => 2,
        "P3 Backlog" => 3,
        _ => 4,
    };

    private static void Add(bool condition, string code, ICollection<string> codes)
    {
        if (condition)
        {
            codes.Add(code);
        }
    }
}
