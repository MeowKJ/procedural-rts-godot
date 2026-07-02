static class GameStateAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var gameState = ReviewGateSource.Read(root, "scripts", "core", "GameState.cs");
        RequireText(gameState, "List<PlacementBuildAnchor> _legacyPlacementBuildAnchors", "Legacy GameState placement validation must reuse build-anchor storage.", result);
        RequireText(gameState, "List<(ProductionKind Kind, UnitSpec Spec, ProductionSpec Production)> _legacyProductionSpecBuffer", "Legacy GameState production option states must reuse spec ordering storage.", result);

        var economy = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.EconomyBuild.cs");
        RequireText(economy, "CollectProductionSpecsFor(MatchConfig.FactionForOwner(owner), _legacyProductionSpecBuffer)", "Legacy GameState production option states must fill reusable spec storage.", result);
        RequireText(economy, "ProductionOptionMetrics(owner, spec.Id, production)", "Legacy GameState production option states must scan producer metrics explicitly.", result);
        RequireText(economy, "result.Sort(CompareLegacyProductionSpecs)", "Legacy GameState production option specs must sort reusable storage in place.", result);
        RequireText(economy, "CollectBuildingBuildAnchors(owner, _legacyPlacementBuildAnchors)", "Legacy GameState placement validation must fill reusable build-anchor storage.", result);
        ForbidText(economy, "ProductionSpecsFor(MatchConfig.FactionForOwner(owner))", "Legacy GameState production option states must not allocate production spec query chains.", result);
        ForbidText(economy, "var producers = Buildings", "Legacy GameState production option states must not materialize producer lists.", result);
        ForbidText(economy, "producers.Sum", "Legacy GameState production option metrics must not allocate LINQ Sum queries.", result);
        ForbidText(economy, ".DefaultIfEmpty(0)", "Legacy GameState production option progress must not allocate fallback query chains.", result);

        var picking = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.RelationsPickingFog.cs");
        RequireText(picking, "private void CollectBuildingBuildAnchors(Owner owner, List<PlacementBuildAnchor> result)", "Legacy GameState build-anchor collection must use caller-owned storage.", result);
        RequireText(picking, "ResourceFieldModel? best = null", "Legacy GameState resource picking must use an explicit best-candidate scan.", result);
        RequireText(picking, "UnitModel? best = null", "Legacy GameState unit picking must use an explicit best-candidate scan.", result);
        RequireText(picking, "BuildingModel? best = null", "Legacy GameState building picking must use an explicit best-candidate scan.", result);
        RequireText(picking, "PickScore(distance, radius)", "Legacy GameState pick helpers must preserve normalized distance scoring.", result);
        ForbidText(picking, "private IReadOnlyList<PlacementBuildAnchor> BuildingBuildAnchors", "Legacy GameState build-anchor collection must not return allocating snapshots.", result);
        ForbidText(picking, ".Where(candidate => candidate.Distance <= candidate.Radius)", "Legacy GameState pick helpers must not allocate candidate filter queries.", result);
        ForbidText(picking, ".OrderBy(candidate => candidate.Distance / Mathf.Max(candidate.Radius, 1))", "Legacy GameState pick helpers must not allocate ordered candidate queries.", result);
        ForbidText(picking, ".Select(candidate => candidate.", "Legacy GameState pick helpers must not allocate candidate projection queries.", result);
    }
}
