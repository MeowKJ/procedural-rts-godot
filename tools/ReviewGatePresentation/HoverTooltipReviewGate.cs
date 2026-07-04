static class HoverTooltipReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var preview = ReviewGateSource.Read(root, "scripts", "controllers", "SelectionController.Preview.cs");
        var tooltips = ReviewGateSource.Read(root, "scripts", "controllers", "SelectionController.HoverTooltips.cs");
        var hotkeys = ReviewGateSource.Read(root, "scripts", "ui", "HotkeyLegendLayer.cs");
        var hudPreview = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.CommandControls.cs");

        RequireText(preview, "RuntimeUnitAttackPreviewLabel(hoveredUnitInstance.Spec)", "Runtime hostile unit hover must surface matchup text through the command preview.", result);
        RequireText(preview, "LegacyUnitAttackPreviewLabel(hoveredUnit)", "Legacy hostile unit hover must surface matchup text through the command preview.", result);
        RequireText(preview, "RuntimeBuildingAttackPreviewLabel(buildingProjection)", "Runtime hostile structure hover must surface matchup text through the command preview.", result);
        RequireText(preview, "LegacyBuildingAttackPreviewLabel(hoveredBuilding)", "Legacy hostile structure hover must surface matchup text through the command preview.", result);
        RequireText(tooltips, "MatchupFromScore(selectedArmed, targeters, bestScore)", "Hover matchup labels must be derived from selected-unit target coverage and combat profile score.", result);
        RequireText(tooltips, "preview.matchup.cannotTarget", "Hover matchup labels must expose cannot-target feedback.", result);
        RequireText(hotkeys, "_hint.Visible = false", "Closed hotkey legend must not leave persistent instructional HUD copy.", result);
        RequireText(hotkeys, "_hint.Visible = _open", "Hotkey legend hint may appear only while the transient legend panel is open.", result);
        RequireText(hudPreview, "Preview.ScreenPosition + new Vector2(18, 18)", "Hover guidance must remain a transient cursor-side command preview instead of persistent HUD copy.", result);
    }
}
