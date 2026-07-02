static class BattleRootHudAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var battleRoot = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "BattleRoot.cs"));
        var process = ReviewGateSource.Read(root, "scripts", "BattleRoot.Process.cs");
        var minimap = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.HudMinimap.cs");
        var alerts = ReviewGateSource.Read(root, "scripts", "BattleRoot.Alerts.cs");

        RequireText(battleRoot, "List<(Vector2 Position, float SightRange)> _unitBattlefieldVisionSourceBuffer", "BattleRoot vision source bridge must reuse storage.", result);
        RequireText(battleRoot, "List<HudLayer.MinimapUnit> _minimapUnitBuffer", "BattleRoot minimap units must use reusable storage.", result);
        RequireText(battleRoot, "List<HudLayer.MinimapUnit> _minimapUnitSecondaryBuffer", "BattleRoot minimap units must be double-buffered for redraw safety.", result);
        RequireText(battleRoot, "List<HudLayer.MinimapBuilding> _minimapBuildingBuffer", "BattleRoot minimap buildings must use reusable storage.", result);
        RequireText(battleRoot, "List<HudLayer.MinimapResource> _minimapResourceBuffer", "BattleRoot minimap resources must use reusable storage.", result);
        RequireText(battleRoot, "List<HudLayer.AlertLine> _alertLineBuffer", "BattleRoot alert HUD sync must reuse alert line storage.", result);
        RequireText(process, "_unitBattlefieldVisionSourceBuffer.Clear();", "UnitBattlefieldVisionSources must clear and reuse the vision-source buffer.", result);
        RequireText(process, "foreach (var source in _unitBattlefield.VisionSources(PlayerSlotId.One))", "UnitBattlefieldVisionSources must copy vision sources explicitly.", result);
        RequireText(process, "_unitBattlefieldVisionSourceBuffer.Add((source.Position, source.SightRange));", "UnitBattlefieldVisionSources must fill the reusable vision-source buffer.", result);
        RequireText(battleRoot, "FillMinimapUnits(units)", "RefreshMinimap must fill the reusable unit buffer.", result);
        RequireText(battleRoot, "FillUnitBattlefieldMinimapBuildings(buildings)", "RefreshMinimap must fill the reusable runtime building buffer.", result);
        RequireText(battleRoot, "FillMinimapResources(resources)", "RefreshMinimap must fill the reusable resource buffer.", result);
        RequireText(minimap, "foreach (var unit in _state.Units)", "BattleRoot minimap units must use an explicit scan.", result);
        RequireText(minimap, "foreach (var building in projections)", "BattleRoot runtime minimap buildings must copy projections explicitly.", result);
        RequireText(minimap, "foreach (var resource in pips)", "BattleRoot minimap resources must copy pips explicitly.", result);
        RequireText(alerts, "_alertLineBuffer.Clear();", "RefreshAlerts must clear and reuse the alert line buffer.", result);
        RequireText(alerts, "_alertLineBuffer.Count < 4", "RefreshAlerts must cap HUD alert lines without LINQ Take.", result);
        RequireText(alerts, "_hud.SetAlerts(_alertLineBuffer)", "RefreshAlerts must pass reusable alert lines to HudLayer.", result);
        ForbidText(minimap, ".ToList()", "BattleRoot minimap sync must not allocate materialized lists.", result);
        ForbidText(minimap, ".Select(", "BattleRoot minimap sync must not allocate LINQ projection chains.", result);
        ForbidText(minimap, ".Where(", "BattleRoot minimap sync must not allocate LINQ filter chains.", result);
        ForbidText(process, ".Select(source => (source.Position, source.SightRange))", "BattleRoot vision source bridge must not allocate LINQ projection iterators.", result);
        ForbidText(alerts, ".OrderByDescending(alert => alert.CreatedAt)", "RefreshAlerts must not allocate ordered alert queries.", result);
        ForbidText(alerts, ".Take(4)", "RefreshAlerts must not allocate LINQ take queries.", result);
        ForbidText(alerts, ".Select(alert => new HudLayer.AlertLine", "RefreshAlerts must not allocate alert projection chains.", result);
        ForbidText(alerts, ".ToList()", "RefreshAlerts must not allocate materialized alert lists.", result);
    }
}
