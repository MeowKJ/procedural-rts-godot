static class GameStateAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var gameState = ReviewGateSource.Read(root, "scripts", "core", "GameState.cs");
        RequireText(gameState, "List<PlacementBuildAnchor> _legacyPlacementBuildAnchors", "Legacy GameState placement validation must reuse build-anchor storage.", result);
        RequireText(gameState, "List<(ProductionKind Kind, UnitSpec Spec, ProductionSpec Production)> _legacyProductionSpecBuffer", "Legacy GameState production option states must reuse spec ordering storage.", result);
        RequireText(gameState, "List<UnitDeathInfo> _legacyUnitDeathBuffer", "Legacy GameState unit removal must reuse death event storage.", result);
        RequireText(gameState, "HashSet<int> _legacyRemovedUnitIds", "Legacy GameState unit removal must reuse removed-unit id storage.", result);
        RequireText(gameState, "List<int> _legacyRemovedBuildingIds", "Legacy GameState building removal must reuse removed-building event storage.", result);
        RequireText(gameState, "HashSet<int> _legacyRemovedBuildingIdSet", "Legacy GameState building removal must reuse removed-building lookup storage.", result);
        RequireText(gameState, "List<BuildingModel> _legacyRemovedBuildings", "Legacy GameState building removal must reuse outcome snapshot storage.", result);
        RequireText(gameState, "List<ProductionLaneSnapshot> _legacyProductionLaneSnapshotBuffer", "Legacy GameState production lane snapshots must reuse lane storage.", result);
        RequireText(gameState, "Dictionary<int, List<ProductionQueueSnapshot>> _legacyProductionQueueSnapshotBuffers", "Legacy GameState production queue snapshots must reuse per-producer storage.", result);
        RequireText(gameState, "List<BuildOptionSnapshot> _legacyBuildOptionSnapshotBuffer", "Legacy GameState build option snapshots must reuse result storage.", result);
        RequireText(gameState, "HashSet<string> _legacyReadyBuildingKinds", "Legacy GameState build option snapshots must reuse prerequisite lookup storage.", result);
        RequireText(gameState, "List<PlacementObstacle> _legacyPlacementObstacles", "Legacy GameState placement/path checks must reuse obstacle storage.", result);
        RequireText(gameState, "HashSet<GridObstacle> _legacyPathObstacleSet", "Legacy GameState path obstacles must reuse de-duplication storage.", result);
        RequireText(gameState, "Dictionary<GridObstacle, DynamicBlobObstacleBounds> _legacyDenseBlobObstacles", "Legacy GameState dense-blob path obstacles must reuse aggregation storage.", result);
        RequireText(gameState, "List<(Vector2 Position, float SightRange)> _legacyFogVisionSources", "Legacy GameState fog updates must reuse vision source storage.", result);

        var economy = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.EconomyBuild.cs");
        RequireText(economy, "CollectProductionSpecsFor(MatchConfig.FactionForOwner(owner), _legacyProductionSpecBuffer)", "Legacy GameState production option states must fill reusable spec storage.", result);
        RequireText(economy, "ProductionOptionMetrics(owner, spec.Id, production)", "Legacy GameState production option states must scan producer metrics explicitly.", result);
        RequireText(economy, "result.Sort(CompareLegacyProductionSpecs)", "Legacy GameState production option specs must sort reusable storage in place.", result);
        RequireText(economy, "CollectBuildingBuildAnchors(owner, _legacyPlacementBuildAnchors)", "Legacy GameState placement validation must fill reusable build-anchor storage.", result);
        RequireText(economy, "CollectBuildingObstacles(_legacyPlacementObstacles)", "Legacy GameState placement validation must fill reusable obstacle storage.", result);
        ForbidText(economy, "ProductionSpecsFor(MatchConfig.FactionForOwner(owner))", "Legacy GameState production option states must not allocate production spec query chains.", result);
        ForbidText(economy, "var producers = Buildings", "Legacy GameState production option states must not materialize producer lists.", result);
        ForbidText(economy, "producers.Sum", "Legacy GameState production option metrics must not allocate LINQ Sum queries.", result);
        ForbidText(economy, ".DefaultIfEmpty(0)", "Legacy GameState production option progress must not allocate fallback query chains.", result);

        var productionSnapshots = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.ProductionSnapshots.cs");
        RequireText(productionSnapshots, "CollectProductionLaneSnapshots(owner, _legacyProductionLaneSnapshotBuffer)", "Legacy GameState production lane snapshots must fill reusable lane storage.", result);
        RequireText(productionSnapshots, "ProductionQueueSnapshotBufferFor(building.Id)", "Legacy GameState production lane snapshots must reuse per-producer queue storage.", result);
        RequireText(productionSnapshots, "CollectOwnedReadyBuildingKinds(owner, _legacyReadyBuildingKinds)", "Legacy GameState build option snapshots must fill reusable prerequisite storage.", result);
        RequireText(productionSnapshots, "result.Sort(CompareBuildOptionSnapshots)", "Legacy GameState build option snapshots must sort reusable storage in place.", result);
        ForbidText(productionSnapshots, ".OrderBy(building => building.Kind)", "Legacy GameState production lane snapshots must not allocate ordered building queries.", result);
        ForbidText(productionSnapshots, ".Select(item =>", "Legacy GameState production queue snapshots must not allocate item projection queries.", result);
        ForbidText(productionSnapshots, ".ToHashSet()", "Legacy GameState build option snapshots must not allocate prerequisite hashsets.", result);
        ForbidText(productionSnapshots, "RequiredBuildings.All", "Legacy GameState build option snapshots must scan prerequisites explicitly.", result);

        var picking = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.RelationsPickingFog.cs");
        RequireText(picking, "private void CollectBuildingBuildAnchors(Owner owner, List<PlacementBuildAnchor> result)", "Legacy GameState build-anchor collection must use caller-owned storage.", result);
        RequireText(picking, "CollectFogVisionSources(extraUnitSources, includeLegacyUnitSources, _legacyFogVisionSources)", "Legacy GameState fog updates must collect into reusable source storage.", result);
        RequireText(picking, "FogOfWar.Update(WorldSize, _legacyFogVisionSources)", "Legacy GameState fog updates must pass reusable source storage to FogOfWarMap.", result);
        RequireText(picking, "ResourceFieldModel? best = null", "Legacy GameState resource picking must use an explicit best-candidate scan.", result);
        RequireText(picking, "UnitModel? best = null", "Legacy GameState unit picking must use an explicit best-candidate scan.", result);
        RequireText(picking, "BuildingModel? best = null", "Legacy GameState building picking must use an explicit best-candidate scan.", result);
        RequireText(picking, "PickScore(distance, radius)", "Legacy GameState pick helpers must preserve normalized distance scoring.", result);
        ForbidText(picking, "private IReadOnlyList<PlacementBuildAnchor> BuildingBuildAnchors", "Legacy GameState build-anchor collection must not return allocating snapshots.", result);
        ForbidText(picking, "unitSources.Concat", "Legacy GameState fog updates must not allocate concatenated source enumerables.", result);
        ForbidText(picking, ".Select(unit => (unit.Position, unit.RuntimeDescriptor.SightRange))", "Legacy GameState fog updates must not allocate unit source projection queries.", result);
        ForbidText(picking, ".Select(building => (building.Position, BuildSpecCatalog.For(building.Kind).SightRange))", "Legacy GameState fog updates must not allocate building source projection queries.", result);
        ForbidText(picking, ".Where(candidate => candidate.Distance <= candidate.Radius)", "Legacy GameState pick helpers must not allocate candidate filter queries.", result);
        ForbidText(picking, ".OrderBy(candidate => candidate.Distance / Mathf.Max(candidate.Radius, 1))", "Legacy GameState pick helpers must not allocate ordered candidate queries.", result);
        ForbidText(picking, ".Select(candidate => candidate.", "Legacy GameState pick helpers must not allocate candidate projection queries.", result);

        var pathObstacles = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.PathObstacles.cs");
        RequireText(pathObstacles, "CollectPathPlacementObstacles(movingUnitId, movingUnitIds, _legacyPlacementObstacles)", "Legacy GameState path obstacles must fill reusable placement-obstacle storage.", result);
        RequireText(pathObstacles, "AppendGridCellsForObstacle(obstacle, PathCellSize, _legacyPathObstacles, _legacyPathObstacleSet)", "Legacy GameState path obstacles must append unique grid cells into reusable storage.", result);
        RequireText(pathObstacles, "_legacyDenseBlobObstacles.Clear()", "Legacy GameState dense-blob obstacles must reuse aggregation storage.", result);
        ForbidText(pathObstacles, ".Concat(BuildingObstacles())", "Legacy GameState path obstacles must not allocate concatenated obstacle enumerables.", result);
        ForbidText(pathObstacles, ".SelectMany(obstacle => GridCellsForObstacle", "Legacy GameState path obstacles must not allocate SelectMany grid-cell queries.", result);
        ForbidText(pathObstacles, ".GroupBy(unit => LocalAvoidanceMath.Cell", "Legacy GameState dense blob obstacles must not allocate grouping queries.", result);
        ForbidText(pathObstacles, "members.Min(unit =>", "Legacy GameState dense blob bounds must not allocate member lists/min queries.", result);
        var fogMap = ReviewGateSource.Read(root, "scripts", "core", "fog", "FogOfWarMap.cs");
        RequireText(fogMap, "visionSources as IReadOnlyList<(Vector2 Position, float SightRange)>", "FogOfWarMap.Update must not copy caller-owned source lists.", result);
        ForbidText(fogMap, "visionSources as (Vector2 Position, float SightRange)[] ?? visionSources.ToArray()", "FogOfWarMap.Update must not only recognize arrays as no-copy inputs.", result);
        ForbidText(fogMap, "sources.Length", "FogOfWarMap.Update must use IReadOnlyList.Count for source buffers.", result);

        var targeting = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.TargetingThreat.cs");
        var targetScans = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.TargetScans.cs");
        var targetingSources = targeting + targetScans;
        RequireText(targeting, "BestUnitTargetForWeapon(building.Owner, weapon, building.Position, weapon.Range, requirePositiveHp: true)", "Legacy GameState building targeting must use the shared best-candidate scan.", result);
        RequireText(targeting, "BestUnitTargetForWeapon(unit.Owner, weapon, unit.Position, range, requirePositiveHp: false)", "Legacy GameState unit targeting must use the shared best-candidate scan.", result);
        RequireText(targetScans, "UnitModel? best = null", "Legacy GameState targeting scan must use explicit best-candidate storage.", result);
        RequireText(targetScans, "var score = TargetScore(weapon, sourcePosition, CombatTargetKind.Unit, candidate.Id, range)", "Legacy GameState targeting scan must preserve TargetScore selection.", result);
        ForbidText(targetingSources, ".Where(unit => IsTargetableHostile(building.Owner, unit)", "Legacy GameState building targeting must not allocate LINQ filter chains.", result);
        ForbidText(targetingSources, ".OrderByDescending(unit => TargetScore", "Legacy GameState building targeting must not allocate ordered target queries.", result);
        ForbidText(targetingSources, ".Where(candidate => IsTargetableHostile(unit.Owner, candidate))", "Legacy GameState unit targeting must not allocate LINQ filter chains.", result);
        ForbidText(targetingSources, ".OrderByDescending(candidate => TargetScore", "Legacy GameState unit targeting must not allocate ordered target queries.", result);

        var removal = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.RemovalDamageUtilities.cs");
        RequireText(removal, "_legacyUnitDeathBuffer.Add(new UnitDeathInfo", "Legacy GameState unit removal must fill reusable death storage.", result);
        RequireText(removal, "UnitsRemoved?.Invoke(_legacyUnitDeathBuffer)", "Legacy GameState unit removal must publish the reusable death buffer.", result);
        RequireText(removal, "BuildingsRemoved?.Invoke(_legacyRemovedBuildingIds)", "Legacy GameState building removal must publish the reusable id buffer.", result);
        RequireText(removal, "UpdateOutcomeAfterRemovedBuildings(_legacyRemovedBuildings)", "Legacy GameState building removal must reuse outcome snapshot storage.", result);
        ForbidText(removal, "Units.Where(unit => unit.Hp <= 0).ToList()", "Legacy GameState unit removal must not materialize removed-unit lists.", result);
        ForbidText(removal, "removedUnits.Select(unit => unit.Id).ToList()", "Legacy GameState unit removal must not materialize removed-unit id lists.", result);
        ForbidText(removal, "removedUnits\n            .Select(unit =>", "Legacy GameState unit removal must not materialize death projection lists.", result);
        ForbidText(removal, "Buildings.Where(building => building.Hp <= 0).Select(building => building.Id).ToList()", "Legacy GameState building removal must not materialize removed-building id lists.", result);
        ForbidText(removal, "Buildings.Where(building => removedIds.Contains(building.Id)).ToList()", "Legacy GameState building removal must not materialize removed-building snapshots.", result);
        ForbidText(removal, "removedBuildings.Any(", "Legacy GameState outcome checks must scan removed buildings explicitly.", result);

        var harvest = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.ProductionHarvest.cs");
        RequireText(harvest, "BuildingModel? best = null", "Legacy GameState refinery selection must use an explicit best-candidate scan.", result);
        RequireText(harvest, "var load = RefineryDockLoad(building, harvesterId)", "Legacy GameState refinery selection must preserve dock-load priority.", result);
        RequireText(harvest, "load < bestLoad || (load == bestLoad && distance < bestDistance)", "Legacy GameState refinery selection must preserve distance tie-breaks.", result);
        ForbidText(harvest, ".Where(building => building.Owner == owner)", "Legacy GameState refinery selection must not allocate owner filter chains.", result);
        ForbidText(harvest, ".OrderBy(building => RefineryDockLoad(building, harvesterId))", "Legacy GameState refinery selection must not allocate ordered refinery queries.", result);
        ForbidText(harvest, ".ThenBy(building => building.Position.DistanceTo(position))", "Legacy GameState refinery selection must not allocate secondary ordered refinery queries.", result);
        ForbidText(harvest, "Buildings.Where(building => building.Kind == BuildingDesignIds.Refinery)", "Legacy GameState dock cleanup must not allocate refinery filter chains.", result);
    }
}
