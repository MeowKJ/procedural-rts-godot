static class HoverTooltipReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var preview = ReviewGateSource.Read(root, "scripts", "controllers", "SelectionController.Preview.cs");
        var tooltips = ReviewGateSource.Read(root, "scripts", "controllers", "SelectionController.HoverTooltips.cs");
        var hotkeys = ReviewGateSource.Read(root, "scripts", "ui", "HotkeyLegendLayer.cs");
        var previewKinds = ReviewGateSource.Read(root, "scripts", "core", "commands", "CommandPreviewKind.cs");
        var buildPlacement = ReviewGateSource.Read(root, "scripts", "controllers", "BuildPlacementController.cs");
        var battleRootAlerts = ReviewGateSource.Read(root, "scripts", "BattleRoot.Alerts.cs");
        var hudPreview = ReviewGateSource.Read(root, "scripts", "ui", "hud", "HudLayer.CommandControls.cs");

        RequireText(previewKinds, "Move,\n    Attack,\n    Repair,\n    Rally,\n    Harvest,\n    BuildValid,\n    BuildInvalid", "Command preview kinds must cover move/attack/repair/rally/harvest/build states.", result);
        RequireText(preview, "RuntimeUnitAttackPreviewLabel(hoveredUnitInstance.Spec)", "Runtime hostile unit hover must surface matchup text through the command preview.", result);
        RequireText(preview, "LegacyUnitAttackPreviewLabel(hoveredUnit)", "Legacy hostile unit hover must surface matchup text through the command preview.", result);
        RequireText(preview, "RuntimeBuildingAttackPreviewLabel(buildingProjection)", "Runtime hostile structure hover must surface matchup text through the command preview.", result);
        RequireText(preview, "LegacyBuildingAttackPreviewLabel(hoveredBuilding)", "Legacy hostile structure hover must surface matchup text through the command preview.", result);
        RequireText(preview, "CommandPreviewKind.Repair", "Selection preview must expose repair affordance state.", result);
        RequireText(preview, "CommandPreviewKind.Harvest", "Selection preview must expose harvester resource state.", result);
        RequireText(preview, "CommandPreviewKind.Rally", "Selection preview must expose producer rally state.", result);
        RequireText(preview, "CommandPreviewKind.Move", "Selection preview must expose move state.", result);
        RequireText(buildPlacement, "CommandPreviewKind.BuildValid", "Build placement preview must expose valid placement state.", result);
        RequireText(buildPlacement, "CommandPreviewKind.BuildInvalid", "Build placement preview must expose invalid placement state.", result);
        RequireText(buildPlacement, "DrawPlacementCursor(rect, accent, placementValid)", "Build placement must draw a distinct placement cursor.", result);
        RequireText(buildPlacement, "ReadyConstructionTickets(LocalPlayerSlotId)", "Build placement cycling must surface ready sidebar construction tickets.", result);
        RequireText(buildPlacement, "QueueConstructionTicket(LocalPlayerSlotId, kind", "Sidebar construction mode must queue a ticket before placement.", result);
        RequireText(buildPlacement, "PlaceReadyConstructionTicket(", "Build placement must place ready construction tickets through the runtime backend.", result);
        RequireText(buildPlacement, "DefaultMethodFor(LocalFaction)", "Build placement must choose deploy-vs-sidebar behavior from faction construction policy.", result);
        RequireText(buildPlacement, "HasEnoughCreditsForPreview(spec)", "Build placement preview must surface insufficient credits before commit.", result);
        RequireText(buildPlacement, "PlacementStatusLabel(\"placement.needCredits\"", "Build placement preview must reuse localized need-credit feedback.", result);
        RequireText(battleRootAlerts, "_buildPlacement.IsActive ? _buildPlacement.PreviewState : _selection.PreviewState", "BattleRoot must route active build preview ahead of selection preview.", result);
        RequireText(tooltips, "MatchupFromScore(selectedArmed, targeters, bestScore)", "Hover matchup labels must be derived from selected-unit target coverage and combat profile score.", result);
        RequireText(tooltips, "preview.matchup.cannotTarget", "Hover matchup labels must expose cannot-target feedback.", result);
        RequireText(hotkeys, "_hint.Visible = false", "Closed hotkey legend must not leave persistent instructional HUD copy.", result);
        RequireText(hotkeys, "_hint.Visible = _open", "Hotkey legend hint may appear only while the transient legend panel is open.", result);
        RequireText(hudPreview, "Preview.ScreenPosition + new Vector2(18, 18)", "Hover guidance must remain a transient cursor-side command preview instead of persistent HUD copy.", result);
        RequireText(hudPreview, "case CommandPreviewKind.Move:", "HUD command preview must render move as a distinct mode.", result);
        RequireText(hudPreview, "case CommandPreviewKind.Attack:", "HUD command preview must render attack as a distinct mode.", result);
        RequireText(hudPreview, "case CommandPreviewKind.Repair:", "HUD command preview must render repair as a distinct mode.", result);
        RequireText(hudPreview, "case CommandPreviewKind.Harvest:", "HUD command preview must render harvest as a distinct mode.", result);
        RequireText(hudPreview, "case CommandPreviewKind.Rally:", "HUD command preview must render rally as a distinct mode.", result);
        RequireText(hudPreview, "case CommandPreviewKind.BuildValid:", "HUD command preview must render build-valid as a distinct mode.", result);
        RequireText(hudPreview, "case CommandPreviewKind.BuildInvalid:", "HUD command preview must render build-invalid as a distinct mode.", result);
    }
}
