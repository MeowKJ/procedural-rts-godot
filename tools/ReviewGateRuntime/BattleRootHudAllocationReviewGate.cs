static class BattleRootHudAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var battleRoot = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "BattleRoot.cs"));
        var minimap = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.HudMinimap.cs");

        RequireText(battleRoot, "List<HudLayer.MinimapUnit> _minimapUnitBuffer", "BattleRoot minimap units must use reusable storage.", result);
        RequireText(battleRoot, "List<HudLayer.MinimapUnit> _minimapUnitSecondaryBuffer", "BattleRoot minimap units must be double-buffered for redraw safety.", result);
        RequireText(battleRoot, "List<HudLayer.MinimapBuilding> _minimapBuildingBuffer", "BattleRoot minimap buildings must use reusable storage.", result);
        RequireText(battleRoot, "List<HudLayer.MinimapResource> _minimapResourceBuffer", "BattleRoot minimap resources must use reusable storage.", result);
        RequireText(battleRoot, "FillMinimapUnits(units)", "RefreshMinimap must fill the reusable unit buffer.", result);
        RequireText(battleRoot, "FillUnitBattlefieldMinimapBuildings(buildings)", "RefreshMinimap must fill the reusable runtime building buffer.", result);
        RequireText(battleRoot, "FillMinimapResources(resources)", "RefreshMinimap must fill the reusable resource buffer.", result);
        RequireText(minimap, "foreach (var unit in _state.Units)", "BattleRoot minimap units must use an explicit scan.", result);
        RequireText(minimap, "foreach (var building in projections)", "BattleRoot runtime minimap buildings must copy projections explicitly.", result);
        RequireText(minimap, "foreach (var resource in pips)", "BattleRoot minimap resources must copy pips explicitly.", result);
        ForbidText(minimap, ".ToList()", "BattleRoot minimap sync must not allocate materialized lists.", result);
        ForbidText(minimap, ".Select(", "BattleRoot minimap sync must not allocate LINQ projection chains.", result);
        ForbidText(minimap, ".Where(", "BattleRoot minimap sync must not allocate LINQ filter chains.", result);
    }
}
