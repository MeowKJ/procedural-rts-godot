static class CoreTodoChecks
{
    public static void CheckTodoProtocol(string root, GateResult result)
    {
        var todoPath = Path.Combine(root, "TODO.md");
        var protocolPath = Path.Combine(root, "docs", "AICollaborationProtocol.md");
        if (!File.Exists(todoPath))
        {
            result.Error("TODO.md is missing.");
            return;
        }

        if (!File.Exists(protocolPath))
        {
            result.Error("docs/AICollaborationProtocol.md is missing.");
            return;
        }

        var todoBytes = File.ReadAllBytes(todoPath);
        if (todoBytes is [0xEF, 0xBB, 0xBF, ..])
        {
            result.Error("TODO.md must be UTF-8 without BOM so AI patching does not churn the file header.");
        }

        string todo;
        try
        {
            todo = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(todoBytes);
        }
        catch (System.Text.DecoderFallbackException ex)
        {
            result.Error($"TODO.md must be valid UTF-8: {ex.Message}");
            todo = string.Empty;
        }

        CheckTodoCheckboxEncoding(todo, result);
        var protocol = File.ReadAllText(protocolPath);
        RequireText(todo, "AI Collaboration", "TODO.md must reference the AI collaboration workflow.", result);
        RequireText(protocol, "Step Contract", "Protocol must define the per-step review contract.", result);
        RequireText(protocol, "Required Gates By Work Type", "Protocol must define automated gates by work type.", result);
        RequireText(protocol, "Reviewer AI", "Protocol must require an independent reviewer.", result);
        RequireText(protocol, "docs/reviews", "Protocol must mention persistent review records.", result);
    }

    public static void CheckTodoCheckboxEncoding(string todo, GateResult result)
    {
        RequireText(todo, "[ ]", "TODO.md must use ASCII open checkboxes `[ ]`.", result);
        RequireText(todo, "[x]", "TODO.md must use ASCII done checkboxes `[x]`.", result);

        var forbidden = new Dictionary<string, string>
        {
            ["\u2610"] = "Unicode open checkbox",
            ["\u2611"] = "Unicode checked checkbox",
            ["\u2612"] = "Unicode crossed checkbox",
            ["\u2705"] = "emoji checked checkbox",
            ["\u274c"] = "emoji crossed checkbox",
            ["\u2713"] = "checkmark glyph",
            ["\u2714"] = "heavy checkmark glyph",
        };

        foreach (var (token, label) in forbidden)
        {
            if (todo.Contains(token, StringComparison.Ordinal))
            {
                result.Error($"TODO.md must not use {label}; use `[ ]` or `[x]` only.");
            }
        }
    }
}
