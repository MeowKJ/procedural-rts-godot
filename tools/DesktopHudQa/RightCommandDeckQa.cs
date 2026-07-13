using ProceduralRts.Core;

static class RightCommandDeckQa
{
    public static void AppendLayoutFailures(int viewportHeight, float uiScale, string name, List<string> failures)
    {
        var issues = HudLayoutMath.ValidateRightDeckControls(viewportHeight, uiScale);
        if (issues.Count > 0)
        {
            failures.Add($"{name} right command deck: {string.Join("; ", issues)}");
        }
    }

    public static void AssertSource(string root, string hudLayer)
    {
        RequireText(hudLayer, "Name = \"QueueMiniStack\"", "Train provider lane state must render through the stable icon-first queue mini-stack.");
        RequireText(hudLayer, "private partial class QueueMiniStack : Control", "Queue presentation must use a dedicated icon/progress/badge control instead of narrow multiline text.");
        RequireText(hudLayer, "RefreshProductionProviderLaneSummary()", "Train provider lane selection/state changes must refresh the provider detail summary.");
        RequireText(hudLayer, "NonProviderLaneRailHintText()", "Non-provider catalog pages must render explicit rail hints instead of blank provider-lane state.");
        RequireText(hudLayer, "CatalogModeKind.Upgrades => GameText.T(\"ui.providerLane.upgradesNone\")", "Upgrades catalog mode must reject provider lanes in the right rail.");
        RequireText(hudLayer, "CatalogModeKind.Abilities => GameText.T(\"ui.providerLane.abilitiesNone\")", "Abilities catalog mode must explain selected-unit ability context in the right rail.");
        RequireText(hudLayer, "SetConstructionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)", "HUD must accept construction provider lanes separately from Train lanes.");
        RequireText(hudLayer, "SelectConstructionProviderLane(state)", "Build provider lane clicks must update construction lane selection without changing Train provider selection.");
        RequireText(hudLayer, "button.SetState(state, IsConstructionProviderLaneSelected(state), state.Available, constructionMode: true)", "Build catalog mode must render construction provider lanes in the right rail.");
        RequireText(hudLayer, "ui.constructionProviderLane.tooltip", "Build provider lanes must keep construction-specific copy for the fixed inspector.");
        RequireText(hudLayer, "ProviderLaneSummaryText(state)", "Train provider lane summary must render selected provider count, queue count, progress, and availability.");
        RequireText(hudLayer, "ProviderLaneSummaryDisabledReason(state.DisabledReasonKey)", "Train provider lane summary must use rail-safe disabled reason codes.");
        RequireText(hudLayer, "BindFixedHoverText", "HUD hover/focus help must route into a fixed information surface.");
        ForbidText(hudLayer, "TooltipText", "In-match HUD controls must not spawn pointer-following tooltip boxes.");
        ForbidText(hudLayer, "DrawLabel(position", "Command preview must remain graphical and must not draw text next to the pointer.");
        ForbidText(hudLayer, "pointerNearRail", "The command deck must not open merely because the pointer approaches the right edge.");

        var visualQaCapture = File.ReadAllText(Path.Combine(root, "scripts", "VisualQaCaptureRoot.cs"));
        RequireText(visualQaCapture, "battle_hud_command_deck.png", "Visual QA must capture the explicitly opened icon-first command deck.");
    }

    private static void RequireText(string source, string required, string message)
    {
        if (!source.Contains(required, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ForbidText(string source, string forbidden, string message)
    {
        if (source.Contains(forbidden, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }
}
