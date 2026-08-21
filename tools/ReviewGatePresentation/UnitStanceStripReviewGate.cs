static class UnitStanceStripReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var projection = ReviewGateSource.Read(root, "scripts", "core", "presentation", "ui", "UnitStanceStripProjection.cs");
        var strip = ReviewGateSource.Read(root, "scripts", "ui", "UnitStanceStrip.cs");
        var hudBuild = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.Build.cs");
        var hudIconCallers = string.Concat(
            ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.CommandControls.cs"),
            ReviewGateSource.Read(root, "scripts", "ui", "hud", "controls", "HudLayer.CatalogControls.cs"),
            ReviewGateSource.Read(root, "scripts", "ui", "hud", "controls", "HudLayer.SelectionControls.cs"),
            ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.ProviderLaneControls.cs"));
        var hudContext = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.CommandRibbonContext.cs");
        var iconRenderer = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.Icons.cs");
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
        RequireText(strip, "private static readonly Vector2 MinimumStripSize = new(220, 44);",
            "UnitStanceStrip must own its 220x44 minimum footprint.", result);
        RequireText(strip, "HudIconRenderer.Draw(this, Presentation.Glyph",
            "UnitStanceStrip must use the shared icon renderer.", result);
        ForbidText(strip, "HudLayer.DrawIconGlyph",
            "UnitStanceStrip must not depend back on its HudLayer container.", result);
        RequireText(iconRenderer, "internal static class HudIconRenderer",
            "HUD glyph drawing must live behind a small shared renderer boundary.", result);
        ForbidText(iconRenderer, "DrawIconGlyph",
            "HUD glyph callers must use HudIconRenderer directly without a forwarding wrapper.", result);
        RequireText(hudIconCallers, "HudIconRenderer.Draw(",
            "HudLayer controls must call the shared renderer directly.", result);
        ForbidText(hudIconCallers, "DrawIconGlyph",
            "HudLayer controls must not restore the retired forwarding wrapper.", result);
        if (CountOccurrences(strip, "IntentRequested?.Invoke") != 1)
        {
            result.Error("UnitStanceStrip must contain exactly one typed intent emission site.");
        }
        if (CountOccurrences(strip, "button.Pressed +=") != 1)
        {
            result.Error("UnitStanceStrip must contain exactly one button Pressed subscription.");
        }

        var applyProjection = SliceBetween(
            strip,
            "public void ApplyProjection(UnitStanceStripProjection projection)",
            "public void ApplyTheme(SoftOldCityHudPalette palette, int fontSize)");
        if (CountOccurrences(strip, "button.SetSelected(") != 1
            || !applyProjection.Contains("button.SetSelected(projection.IsSelected(button.Presentation.Stance));", StringComparison.Ordinal))
        {
            result.Error("UnitStanceStrip button.SetSelected must be reached only from ApplyProjection.");
        }

        ForbidText(strip, "SetSelectedUnitStance", "UnitStanceStrip must not call the HudLayer stance setter.", result);
        ForbidText(strip, "UnitBattlefield", "UnitStanceStrip must not read runtime authority.", result);
        ForbidText(strip, "SubmitLiveLocalPlayerCommand", "UnitStanceStrip must not submit player commands directly.", result);

        RequireText(hudBuild, "new UnitStanceStrip",
            "HudLayer must compose the reusable stance strip instead of rebuilding five buttons.", result);
        ForbidText(hudBuild, "CustomMinimumSize = new Vector2(220, 44)",
            "HudLayer must not own the reusable stance strip footprint.", result);
        var adapter = SliceBetween(
            hudBuild,
            "_unitStanceStrip = new UnitStanceStrip",
            "_unitStanceStrip.ApplyTheme(CurrentPalette, FontTiny);");
        RequireText(adapter, "IntentRequested = stance => UnitStanceRequested?.Invoke(stance),",
            "HudLayer stance adapter must be an exact typed-intent forwarder.", result);
        ForbidText(adapter, "SetSelectedUnitStance",
            "HudLayer stance adapter must not mutate the projected selection before forwarding intent.", result);
        if (CountOccurrences(adapter, "IntentRequested =") != 1)
        {
            result.Error("HudLayer stance adapter must declare exactly one intent forwarder.");
        }
        RequireText(hudContext, "_unitStanceStrip?.ApplyProjection(projection);",
            "HudLayer stance selection must flow through the immutable projection.", result);

        var stanceHandler = SliceBetween(
            battleEvents,
            "private void OnUnitStanceRequested(UnitStance stance)",
            "private static string GatewayStatus(CommandGatewayResult result, string acceptedStatus)");
        RequireText(stanceHandler, "var selectedCount = _unitBattlefield.SelectedCount(PlayerSlotId.One);",
            "BattleRoot stance intent must read selection count from UnitBattlefield.", result);
        RequireText(stanceHandler, "SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.SetStance, payload)",
            "BattleRoot must keep stance intent submission behind PlayerCommandGateway.", result);
        RequireText(stanceHandler, "RefreshSelectionInfo();",
            "Accepted runtime stance commands must re-project selection from authority.", result);
        var zeroCheck = stanceHandler.IndexOf("if (selectedCount == 0)", StringComparison.Ordinal);
        var submit = stanceHandler.IndexOf("SubmitLiveLocalPlayerCommand", StringComparison.Ordinal);
        if (zeroCheck < 0 || submit <= zeroCheck)
        {
            result.Error("UnitBattlefield no-selection stance intent must return before PlayerCommandGateway submission.");
        }
        ForbidText(stanceHandler, "_hud.SetSelectedUnitStance(stance",
            "BattleRoot stance acceptance must not project the requested value directly.", result);
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

    private static string SliceBetween(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        return end < 0 ? string.Empty : source[start..end];
    }
}
