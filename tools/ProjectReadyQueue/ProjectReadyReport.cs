namespace ProjectReadyQueue;

public static class ProjectReadyReport
{
    private static readonly IReadOnlyDictionary<string, string> ReasonLabels = new Dictionary<string, string>
    {
        [ProjectReadyEvaluator.IssueClosed] = "Issue 已关闭",
        [ProjectReadyEvaluator.ProjectItemMissing] = "未加入目标 Project",
        [ProjectReadyEvaluator.ProjectItemAmbiguous] = "目标 Project 中存在重复条目",
        [ProjectReadyEvaluator.WorkflowMissing] = "未设置工作阶段",
        [ProjectReadyEvaluator.WorkflowNotReady] = "工作阶段不是可领取",
        [ProjectReadyEvaluator.SizeMissing] = "未设置任务规模",
        [ProjectReadyEvaluator.SizeNotEligible] = "任务规模不是 S/M",
        [ProjectReadyEvaluator.AgentMissing] = "未明确设置为无人执行",
        [ProjectReadyEvaluator.AgentActive] = "已有执行者",
        [ProjectReadyEvaluator.Paused] = "任务已暂停",
        [ProjectReadyEvaluator.OpenSubIssue] = "仍有开放子任务",
        [ProjectReadyEvaluator.OpenBlocker] = "存在未完成依赖",
        [ProjectReadyEvaluator.AcceptanceMissing] = "缺少非空验收标准",
        [ProjectReadyEvaluator.VerificationGateMissing] = "未设置验证门禁",
    };

    private static readonly IReadOnlyDictionary<string, string> WarningLabels = new Dictionary<string, string>
    {
        [ProjectReadyEvaluator.StatusWorkflowConflict] = "兼容状态与工作阶段冲突",
        [ProjectReadyEvaluator.ReadyLabelWorkflowConflict] = "ready 标签与工作阶段冲突",
        [ProjectReadyEvaluator.SizeLabelProjectConflict] = "规模标签与 Project 字段冲突",
    };

    public static string Render(ProjectReadyOutput output)
    {
        var open = output.Evaluated.Where(item => item.State == "OPEN").ToList();
        var inProgress = open.Where(item => item.Workflow == "In Progress").ToList();
        var review = open.Where(item => item.Workflow == "Review").ToList();
        var blocked = open.Where(IsBlocked).ToList();
        var actionableReview = review.Where(item => !IsBlocked(item)).ToList();
        var actionableInProgress = inProgress.Where(item => !IsBlocked(item)).ToList();
        var completed = output.Evaluated.Where(item => item.State == "CLOSED" || item.Workflow == "Done").ToList();
        var categorized = output.Eligible
            .Concat(inProgress)
            .Concat(review)
            .Concat(blocked)
            .Concat(completed)
            .Select(item => item.Number)
            .ToHashSet();
        var needsPlanning = open.Where(item => !categorized.Contains(item.Number)).ToList();
        var lines = new List<string>
        {
            "# AI 可领取队列与项目进度",
            string.Empty,
            $"- 可由 AI 领取：{output.EligibleCount}",
            $"- 正在实现：{inProgress.Count}",
            $"- 等待审查：{review.Count}",
            $"- 明确受阻或暂停：{blocked.Count}",
            $"- 已完成：{completed.Count}",
            string.Empty,
            "## 可领取",
            string.Empty,
        };

        AddTable(lines, output.Eligible, includeReasons: false);
        lines.Add(string.Empty);
        lines.Add("## 正在实现");
        lines.Add(string.Empty);
        AddTable(lines, inProgress, includeReasons: true);
        lines.Add(string.Empty);
        lines.Add("## 等待审查");
        lines.Add(string.Empty);
        AddTable(lines, review, includeReasons: true);
        lines.Add(string.Empty);
        lines.Add("## 受阻或暂停");
        lines.Add(string.Empty);
        AddTable(lines, blocked, includeReasons: true);
        lines.Add(string.Empty);
        lines.Add("## 待规划或待补字段");
        lines.Add(string.Empty);
        AddTable(lines, needsPlanning, includeReasons: true);
        lines.Add(string.Empty);
        lines.Add("## 已完成");
        lines.Add(string.Empty);
        AddTable(lines, completed, includeReasons: true);
        lines.Add(string.Empty);
        lines.Add("## 下一步");
        lines.Add(string.Empty);
        lines.Add(NextAction(output, actionableReview, actionableInProgress, blocked));
        lines.Add(string.Empty);
        lines.Add("机器判定只读取同一 artifact 中的 `ai-ready.json`；本中文报告、`status:ready`、`size:*` 和兼容 `Status` 字段均不参与领取判定。`status:paused` 仍是规范暂停信号。");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string NextAction(
        ProjectReadyOutput output,
        IReadOnlyList<ProjectReadyEvaluation> review,
        IReadOnlyList<ProjectReadyEvaluation> inProgress,
        IReadOnlyList<ProjectReadyEvaluation> blocked)
    {
        if (output.Eligible.FirstOrDefault() is { } ready)
        {
            return $"优先领取 [#{ready.Number} {Escape(ready.Title)}]({ready.Url})；领取时同步设置 `Agent` 和 `Workflow=In Progress`。";
        }

        if (review.FirstOrDefault() is { } reviewing)
        {
            return $"当前没有可安全领取任务；先完成 [#{reviewing.Number} {Escape(reviewing.Title)}]({reviewing.Url}) 的审查与证据核对。";
        }

        if (inProgress.FirstOrDefault() is { } active)
        {
            return $"当前没有可安全领取任务；继续推进 [#{active.Number} {Escape(active.Title)}]({active.Url})，完成后转入审查。";
        }

        if (blocked.FirstOrDefault() is { } blockedItem)
        {
            return $"当前没有可安全领取任务；先解除 [#{blockedItem.Number} {Escape(blockedItem.Title)}]({blockedItem.Url}) 的依赖或暂停状态。";
        }

        return "当前没有可安全领取任务；先补齐 Project 规范字段、验收标准和验证门禁。";
    }

    private static bool IsBlocked(ProjectReadyEvaluation item) =>
        item.Paused || item.OpenBlockers.Count > 0;

    private static void AddTable(List<string> lines, IReadOnlyList<ProjectReadyEvaluation> items, bool includeReasons)
    {
        if (items.Count == 0)
        {
            lines.Add("当前没有符合条件的任务。");
            return;
        }

        lines.Add(includeReasons ? "| Issue | 阶段 | 领取状态 | 数据提醒 |" : "| Issue | 优先级 | 规模 | 验证门禁 |");
        lines.Add(includeReasons ? "| --- | --- | --- | --- |" : "| --- | --- | --- | --- |");
        foreach (var item in items)
        {
            var issue = $"[#{item.Number} {Escape(item.Title)}]({item.Url})";
            if (includeReasons)
            {
                var reason = item.Eligible ? "可领取" : string.Join("、", item.ReasonCodes.Select(ReasonLabel));
                var warning = item.Warnings.Count == 0 ? "—" : string.Join("、", item.Warnings.Select(WarningLabel));
                lines.Add($"| {issue} | {WorkflowLabel(item.Workflow)} | {reason} | {warning} |");
            }
            else
            {
                lines.Add($"| {issue} | {item.Priority ?? "未设置"} | {item.Size} | {item.VerificationGate} |");
            }
        }
    }

    private static string ReasonLabel(string code) => ReasonLabels.GetValueOrDefault(code, code);
    private static string WarningLabel(string code) => WarningLabels.GetValueOrDefault(code, code);

    private static string WorkflowLabel(string? workflow) => workflow switch
    {
        "Backlog" => "待规划",
        "Ready" => "可领取",
        "In Progress" => "正在实现",
        "Review" => "等待审查",
        "Done" => "已完成",
        _ => "未设置",
    };

    private static string Escape(string value) => value
        .Replace("|", "／", StringComparison.Ordinal)
        .Replace("[", "［", StringComparison.Ordinal)
        .Replace("]", "］", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);
}
