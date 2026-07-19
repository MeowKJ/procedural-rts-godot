static class UnitStanceStripReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var projection = ReviewGateSource.Read(root, "scripts", "core", "presentation", "ui", "UnitStanceStripProjection.cs");
        var strip = ReviewGateSource.Read(root, "scripts", "ui", "UnitStanceStrip.cs");
        var hudBuild = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.Build.cs");
        var hudControls = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.BuildControls.cs");
        var hudContext = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.CommandRibbonContext.cs");
        var battleEvents = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.Events.cs");

        RequireText(projection, "public readonly record struct UnitStanceStripProjection",
            "Unit stance UI state must cross the Godot boundary as an immutable projection.", result);
        RequireText(projection, "UnitStanceStripSelectionState.None",
            "Unit stance projection must represent zero selection explicitly.", result);
        RequireText(projection, "UnitStanceStripSelectionState.Mixed",
            "Unit stance projection must represent mixed selection explicitly.", result);
        RequireText(projection, "UnitStanceStripSelectionState.Uniform",
            "Unit stance projection must represent uniform selection explicitly.", result);

        RequireText(strip, "public partial class UnitStanceStrip : Control",
            "The stance pilot must remain a reusable top-level Godot Control.", result);
        RequireText(strip, "button.Pressed += () => IntentRequested?.Invoke(presentation.Stance);",
            "A stance press must only emit one typed intent.", result);
        RequireText(strip, "public void ApplyProjection(UnitStanceStripProjection projection)",
            "The reusable stance strip must expose one projection entry point.", result);
        RequireText(strip, "UnitStancePresentationCatalog.Definitions",
            "The reusable stance strip must reuse the shared stance catalog.", result);
        RequireText(strip, "UiFactory.ApplyHudStanceButtonTheme",
            "The reusable stance strip must reuse the HUD theme foundation.", result);
        if (CountOccurrences(strip, "IntentRequested?.Invoke") != 1)
        {
            result.Error("UnitStanceStrip must contain exactly one typed intent emission site.");
        }

        ForbidText(strip, "SetSelectedUnitStance", "UnitStanceStrip must not call the HudLayer stance setter.", result);
        ForbidText(strip, "UnitBattlefield", "UnitStanceStrip must not read runtime authority.", result);
        ForbidText(strip, "GameState", "UnitStanceStrip must not read legacy authority.", result);
        ForbidText(strip, "SubmitLiveLocalPlayerCommand", "UnitStanceStrip must not submit player commands directly.", result);
        ForbidText(hudControls, "SetSelectedUnitStance(presentation.Stance",
            "The retired HudLayer stance button must not restore optimistic highlighting.", result);

        RequireText(hudBuild, "new UnitStanceStrip",
            "HudLayer must compose the reusable stance strip instead of rebuilding five buttons.", result);
        RequireText(hudContext, "_unitStanceStrip?.ApplyProjection(projection);",
            "HudLayer stance selection must flow through the immutable projection.", result);

        var runtimeStart = battleEvents.IndexOf("if (_unitBattlefield.SelectedCount(PlayerSlotId.One) > 0)", StringComparison.Ordinal);
        var legacyStart = battleEvents.IndexOf("var selectedCount = _state.SelectedUnitCount();", StringComparison.Ordinal);
        if (runtimeStart < 0 || legacyStart <= runtimeStart)
        {
            result.Error("BattleRoot runtime stance command block is missing.");
        }
        else
        {
            var runtimeBlock = battleEvents[runtimeStart..legacyStart];
            RequireText(runtimeBlock, "SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.SetStance, payload)",
                "BattleRoot must keep stance intent submission behind PlayerCommandGateway.", result);
            RequireText(runtimeBlock, "RefreshSelectionInfo();",
                "Accepted runtime stance commands must re-project selection from authority.", result);
            ForbidText(runtimeBlock, "_hud.SetSelectedUnitStance(stance",
                "BattleRoot runtime stance acceptance must not project the requested value directly.", result);
        }
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }
}
