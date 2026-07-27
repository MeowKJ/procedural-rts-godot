using ProceduralRts.Core;

static class RightCommandDeckQa
{
    public static void AppendLayoutFailures(int viewportWidth, int viewportHeight, float uiScale, string name, List<string> failures)
    {
        var issues = HudLayoutMath.ValidateRightDeckControls(viewportHeight, uiScale);
        if (issues.Count > 0)
        {
            failures.Add($"{name} right command deck: {string.Join("; ", issues)}");
        }

        var bottomIssues = HudLayoutMath.ValidateBottomCommandControls(viewportWidth, uiScale);
        if (bottomIssues.Count > 0)
        {
            failures.Add($"{name} bottom command ribbon: {string.Join("; ", bottomIssues)}");
        }

        if (HudLayoutMath.ContrastRatio(HudLayoutMath.ConsoleTextRgb, HudLayoutMath.ConsoleBaseRgb) < 4.5f)
        {
            failures.Add($"{name} command-console body text contrast is below 4.5:1");
        }

        if (HudLayoutMath.ContrastRatio(HudLayoutMath.ConsoleActionRgb, HudLayoutMath.ConsoleBaseRgb) < 3f
            || HudLayoutMath.ContrastRatio(HudLayoutMath.ConsoleBrassRgb, HudLayoutMath.ConsoleBaseRgb) < 3f)
        {
            failures.Add($"{name} command-console icon contrast is below 3:1");
        }

        if (HudLayoutMath.CompactFieldText("ALPHA BRAVO CHARLIE", 10) != "ALPHA"
            || HudLayoutMath.CompactFieldText("SUPERCALIFRAGILISTIC", 8) != ""
            || HudLayoutMath.CompactFieldText("\u72B6\u6001\u9632\u5FA1\u786E\u8BA4", 4) != "\u72B6\u6001\u9632\u5FA1"
            || HudLayoutMath.CompactFieldText("\U0001F680ROCKET", 2) != "\U0001F680")
        {
            failures.Add($"{name} compact HUD fields must preserve word and Unicode boundaries without ellipsis");
        }

        if (HudLayoutMath.CommandRibbonSurfaceText("MODE Direct advance") != "MODE Direct"
            || HudLayoutMath.CommandRibbonSurfaceText("Direct advance order") != "Direct advance"
            || HudLayoutMath.CommandRibbonSurfaceText("MODE Direct advance").Length > HudLayoutMath.CommandRibbonSurfaceMaxChars)
        {
            failures.Add($"{name} command ribbon surface text must remain complete inside its 92px field");
        }

        if (HudLayoutMath.ProductionDrawerHeight(0) != 172
            || HudLayoutMath.ProductionDrawerHeight(3) >= HudLayoutMath.ProductionDrawerHeight(9)
            || HudLayoutMath.ProductionDrawerHeight(9) > HudLayoutMath.ProductionPanelHeight)
        {
            failures.Add($"{name} sparse command drawers must contract while the dense nine-card grid remains complete");
        }
    }

    public static void AssertSource(string root, string hudLayer)
    {
        RequireText(hudLayer, "Name = \"QueueMiniStack\"", "Train provider lane state must render through the stable icon-first queue mini-stack.");
        RequireText(hudLayer, "private partial class QueueMiniStack : Control", "Queue presentation must use a dedicated icon/progress/badge control instead of narrow multiline text.");
        RequireText(hudLayer, "avoidRail: true", "The opened production drawer must remain left of the persistent rail.");
        RequireText(hudLayer, "_rightDetailPanel, _detailDrawerProgress, avoidRail: true", "The fixed detail inspector must remain left of the persistent rail.");
        RequireText(hudLayer, "HudLayoutMath.CommandRibbonWidth", "The command ribbon must use the shared bounded-width layout calculation.");
        RequireText(hudLayer, "HudLayoutMath.CommandRibbonSurfaceText", "Default and fixed-hover ribbon copy must use the 92px-safe surface text policy.");
        RequireText(hudLayer, "LayoutProductionDrawerDensity()", "Sparse command drawers must contract around their visible card rows.");
        RequireText(hudLayer, "statusText: hasDesign ? RepeatProductionStateText", "The no-card repeat state must not render an isolated CARD label.");
        RequireText(hudLayer, "visible && !string.IsNullOrWhiteSpace(statusText)", "Empty repeat status labels must remain hidden.");
        RequireText(hudLayer, "new Vector2(44, 44)", "Bottom command actions and mode buttons must keep 44px hit targets.");
        RequireText(hudLayer, "new Vector2(52, 44)", "The right rail command toggle and provider lanes must keep at least 44px hit targets.");
        RequireText(hudLayer, "private static readonly Color UnexploredSurface = new(\"#26313B\")", "The minimap must use a deep-gray unexplored surface instead of a pure-black hole.");
        RequireText(hudLayer, "_catalogSurfaceLabel.Visible = false", "The retired surface label must not overlap the fixed inspector row.");
        RequireText(hudLayer, "_catalogOverviewValue.Visible = false", "The retired overview label must not overlap the fixed inspector row.");
        RequireText(hudLayer, "Text = \"\";", "Command cards must suppress inherited multiline button text and draw compact metrics explicitly.");
        RequireText(hudLayer, "RefreshProductionProviderLaneSummary()", "Train provider lane selection/state changes must refresh the provider detail summary.");
        RequireText(hudLayer, "NonProviderLaneRailHintText()", "Non-provider catalog pages must render explicit rail hints instead of blank provider-lane state.");
        RequireText(hudLayer, "CatalogModeKind.Upgrades => GameText.T(\"ui.providerLane.upgradesNone\")", "Upgrades catalog mode must reject provider lanes in the right rail.");
        RequireText(hudLayer, "CatalogModeKind.Abilities => AbilityRailSourceContextText()", "Abilities catalog mode must explain selected-unit ability context in the right rail.");
        RequireText(hudLayer, "SetConstructionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)", "HUD must accept construction provider lanes separately from Train lanes.");
        RequireText(hudLayer, "SelectConstructionProviderLane(state)", "Build provider lane clicks must update construction lane selection without changing Train provider selection.");
        RequireText(hudLayer, "button.SetState(state, IsConstructionProviderLaneSelected(state), state.Available, constructionMode: true)", "Build catalog mode must render construction provider lanes in the right rail.");
        RequireText(hudLayer, "ui.constructionProviderLane.tooltip", "Build provider lanes must keep construction-specific copy for the fixed inspector.");
        RequireText(hudLayer, "ProviderLaneSummaryText(state)", "Train provider lane summary must render selected provider count, queue count, progress, and availability.");
        RequireText(hudLayer, "ProviderLaneSummaryDisabledReason(state.DisabledReasonKey)", "Train provider lane summary must use rail-safe disabled reason codes.");
        RequireText(hudLayer, "BindFixedHoverText", "HUD hover/focus help must route into a fixed information surface.");
        RequireText(hudLayer, "var lineBreak = summary.IndexOf('\\n');", "The live queue surface must collapse localized multi-line summaries to one compact line.");
        RequireText(hudLayer, "_cancelProduction.FixedHoverText = canCancel ? summary", "Complete queue detail must remain available through the fixed inspector rather than overflowing the drawer.");
        ForbidText(hudLayer, "TooltipText", "In-match HUD controls must not spawn pointer-following tooltip boxes.");
        ForbidText(hudLayer, "_queueValue.Text = CompactMultiline(summary, 28)", "The compact queue surface must not render a second line outside the drawer.");
        ForbidText(hudLayer, "DrawLabel(position", "Command preview must remain graphical and must not draw text next to the pointer.");
        ForbidText(hudLayer, "pointerNearRail", "The command deck must not open merely because the pointer approaches the right edge.");
        ForbidText(hudLayer, "+ \"...\"", "HUD surface compaction must not introduce visual ellipsis truncation.");
        ForbidText(hudLayer, "new Vector2(36, 34)", "Bottom command actions must not regress below the 44px hit target.");
        ForbidText(hudLayer, "CompactText(context.Text, 28)", "Default ribbon context must not exceed the real 92px label width.");
        ForbidText(hudLayer, "CompactText(text.Replace('\\n', ' '), 34)", "Fixed-hover ribbon context must not exceed the real 92px label width.");

        var uiFactory = File.ReadAllText(Path.Combine(root, "scripts", "ui", "UiFactory.cs"));
        ForbidText(uiFactory, "TooltipText", "Shared UI controls must not restore pointer-following tooltip boxes.");

        var minimap = File.ReadAllText(Path.Combine(root, "scripts", "ui", "hud", "HudLayer.Minimap.cs"));
        RequireText(minimap, "FogVeil = new(\"#26313B\", 1.0f)", "The minimap FOW veil must preserve the source mask's full opacity.");
        ForbidText(minimap, "0.58f", "The minimap must not weaken the real FOW mask opacity.");
        ForbidText(minimap, "FogLift", "The minimap must not wash a full-surface lift over explored markers.");
        RequireText(minimap, "DrawFog(rect);\n            DrawFogTacticalGrid(rect);", "A static tactical grid must be drawn after the opaque FOW veil.");

        var theme = File.ReadAllText(Path.Combine(root, "scripts", "ui", "SoftOldCityTheme.cs"));
        RequireText(theme, "new Color(\"#E9E1D1\")", "Command-console body text must keep the accessible warm-white token.");
        RequireText(theme, "new Color(\"#62C9C4\")", "Command-console primary interactions must keep the cyan-green token.");
        RequireText(theme, "new Color(\"#D75B5B\")", "Command-console danger actions must keep the danger-red token.");

        var visualQaCapture = File.ReadAllText(Path.Combine(root, "scripts", "VisualQaCaptureRoot.cs"));
        RequireText(visualQaCapture, "battle_hud_command_deck.png", "Visual QA must capture the explicitly opened icon-first command deck.");
        RequireText(visualQaCapture, "battle_hud_command_ribbon.png", "Visual QA must capture the bounded command-ribbon status surface.");
        RequireText(visualQaCapture, "battle_hud_command_deck_queue.png", "Visual QA must capture the command deck with a populated production queue.");
        RequireText(visualQaCapture, "battle_hud_command_deck_dense.png", "Visual QA must capture a deterministic dense three-column command grid.");
        RequireText(visualQaCapture, "battle_hud_selection_detail.png", "Visual QA must capture the fixed selected-unit inspector.");
        RequireText(visualQaCapture, "SetProductionProviderLaneState", "Populated queue Visual QA must seed provider-lane progress and count state.");
        RequireText(visualQaCapture, "SetProductionQueueSummary", "Populated queue Visual QA must expose the real cancel-enabled queue summary surface.");
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
