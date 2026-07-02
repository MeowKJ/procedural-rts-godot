static class UnitBattlefieldRuntimeAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireConstructBuildingAdoptionBuffers(root, result);
        RequireOwnerRelationSyncBuffers(root, result);
        RequireResourceHarvestSyncBuffers(root, result);
        RequireAutoAcquireTargetScan(root, result);
        RequireBuildingTargetCombatEventBuffers(root, result);
        RequireSimEventDrainBuffer(root, result);
        RequireExplicitUnitBridgeFilters(root, result);
        RequirePlacementQueryBuffers(root, result);
        RequireConstructionWorkScan(root, result);
    }

    private static void RequireConstructBuildingAdoptionBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<int> _constructionEntityIdsBefore", "Direct construction adoption must reuse the construction entity-id snapshot buffer.", result);

        var lifecycle = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.BuildingLifecycle.cs");
        RequireText(lifecycle, "CollectEntityIds(_constructionEntityIdsBefore)", "ConstructBuilding must fill the reusable before-entity snapshot.", result);
        RequireText(lifecycle, "LastNewConstructedEntity(owner, kind, _constructionEntityIdsBefore)", "ConstructBuilding must reuse the explicit constructed-entity scan.", result);
        RequireText(lifecycle, "DrainConstructionRejection(command.Tick, owner, kind)", "ConstructBuilding must reuse the shared construction rejection drain.", result);
        ForbidText(lifecycle, ".Select(entity => entity.Id.Value)\n            .ToHashSet()", "ConstructBuilding must not allocate a before-entity HashSet.", result);
        ForbidText(lifecycle, ".Where(entity => !before.Contains(entity.Id.Value))", "ConstructBuilding must not allocate a LINQ new-entity filter chain.", result);
        ForbidText(lifecycle, ".OrderBy(entity => entity.Id.Value)\n            .LastOrDefault();", "ConstructBuilding must not allocate an ordered new-entity query.", result);
    }

    private static void RequireOwnerRelationSyncBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<PlayerSlotId> _ownerRelationSlots", "Owner relation sync must reuse slot storage.", result);

        var commandBridge = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.CommandBridge.cs");
        RequireText(commandBridge, "CollectOwnerRelationSlots(_ownerRelationSlots)", "SyncOwnerRelations must fill reusable owner slot storage.", result);
        RequireText(commandBridge, "AddOwnerRelationSlot(result, unit.PlayerSlotId)", "Owner relation sync must scan unit owners explicitly.", result);
        RequireText(commandBridge, "AddOwnerRelationSlot(result, identity.PlayerSlotId)", "Owner relation sync must scan building owners explicitly.", result);
        RequireText(commandBridge, "result.Sort(ComparePlayerSlotIds)", "Owner relation sync must sort the reusable slot buffer in place.", result);
        ForbidText(commandBridge, ".Concat(BuildingTargetIds()", "Owner relation sync must not allocate chained slot enumerables.", result);
        ForbidText(commandBridge, ".Distinct()\n            .OrderBy(slot => slot.Value)\n            .ToList();", "Owner relation sync must not materialize distinct ordered slot lists.", result);
    }

    private static void RequireResourceHarvestSyncBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "Dictionary<PlayerSlotId, int> _resourceCreditsBefore", "Resource harvest sync must reuse credits-before storage.", result);
        RequireText(battlefield, "List<int> _resourceCreditOwnerIds", "Resource harvest sync must reuse owner-id storage.", result);
        RequireText(battlefield, "private bool HasHarvesters()", "Harvester update must use an explicit early-exit harvester scan.", result);

        RequireText(battlefield, "CollectResourceCreditsBefore(_resourceCreditsBefore)", "Harvester update must fill the reusable credits-before snapshot.", result);
        RequireText(battlefield, "SyncAllCreditsFromEntityWorld(_resourceCreditsBefore)", "Harvester update must reuse the credits-before snapshot for notifications.", result);
        ForbidText(battlefield, "ResourceInventories.ToDictionary", "Harvester update must not allocate credits-before dictionaries.", result);
        ForbidText(battlefield, "Units.Where(IsHarvester)", "Harvester sync must not allocate harvester filter enumerables.", result);
        ForbidText(battlefield, "BuildingTargetIds()\n            .Where(buildingId => BuildingIdentity(buildingId)?.Kind == BuildingDesignIds.Refinery)", "Dock sync must not allocate refinery filter enumerables.", result);

        var legacy = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.LegacyUtilities.cs");
        RequireText(legacy, "CollectResourceCreditOwnerIds(_resourceCreditOwnerIds)", "Credit sync must fill reusable owner-id storage.", result);
        RequireText(legacy, "AddResourceCreditOwnerId(result, entity.OwnerId.Value)", "Credit sync must scan entity owners explicitly.", result);
        ForbidText(legacy, "_entityWorld.ResourceInventories.Keys\n            .Concat(_entityWorld.OrderedEntities.Select(entity => entity.OwnerId.Value))", "Credit sync must not allocate owner concat chains.", result);
        ForbidText(legacy, ".Distinct()\n            .OrderBy(owner => owner)", "Credit sync must not allocate distinct ordered owner enumerables.", result);
    }

    private static void RequireAutoAcquireTargetScan(string root, GateResult result)
    {
        var visibility = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.VisibilityCombat.cs");
        RequireText(visibility, "UnitInstance? bestTarget = null;", "Auto-acquire must use an explicit best-target scan.", result);
        RequireText(visibility, "var bestPriority = 0f;", "Auto-acquire must track target priority without ordered LINQ.", result);
        RequireText(visibility, "var bestDistanceSquared = float.PositiveInfinity;", "Auto-acquire must track nearest same-priority target without ordered LINQ.", result);
        ForbidText(visibility, ".Select(target => new", "Auto-acquire must not allocate anonymous target candidates.", result);
        ForbidText(visibility, ".OrderByDescending(candidate => candidate.Priority)", "Auto-acquire must not allocate ordered target candidate chains.", result);
        ForbidText(visibility, ".ThenBy(candidate => candidate.Distance)", "Auto-acquire must not allocate secondary ordered target candidate chains.", result);
        ForbidText(visibility, ".FirstOrDefault();", "Auto-acquire must not materialize candidate queries.", result);
    }

    private static void RequireBuildingTargetCombatEventBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<int> _combatDamagedBuildingIds", "Building-target combat events must reuse damaged-building id storage.", result);
        RequireText(battlefield, "HashSet<int> _combatDestroyedBuildingIds", "Building-target combat events must reuse destroyed-building id storage.", result);
        RequireText(battlefield, "HashSet<int> _combatDeadBuildingIds", "Building-target combat events must reuse dead-building de-duplication storage.", result);

        var bridge = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.BuildingTargetCombatBridge.cs");
        RequireText(bridge, "private bool HasBuildingTargetCombatWork()", "Building-target combat bridge must use an explicit work check.", result);
        RequireText(bridge, "CollectDeadBuildingTargetIdsFromCombatEvents()", "Building-target combat events must collect dead ids through reusable buffers.", result);
        RequireText(bridge, "AddCombatDeadBuildingId", "Building-target combat event de-duplication must use reusable storage.", result);
        ForbidText(bridge, "Units.Any(unit =>", "Building-target combat work check must not allocate LINQ enumerators.", result);
        ForbidText(bridge, "foreach (var unit in Units.Where(unit => unit.AttackTargetKind == CombatTargetKind.Building || unit.MoveMode == MoveCommandMode.Attack))", "Building-target combat state sync must not allocate filtered unit enumerables.", result);
        ForbidText(bridge, "new HashSet<int>()", "Building-target combat events must not allocate local HashSet instances.", result);
        ForbidText(bridge, ".Concat(_combatDestroyedBuildingIds)", "Building-target combat events must not allocate chained dead-id enumerables.", result);
        ForbidText(bridge, ".Distinct()\n            .ToList();", "Building-target combat events must not materialize distinct dead-id lists.", result);
    }

    private static void RequireSimEventDrainBuffer(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<SimEvent> _simEventDrainBuffer", "UnitBattlefield must reuse a sim-event drain buffer.", result);

        var buildingCombat = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.BuildingTargetCombatBridge.cs");
        var turretCombat = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.TurretCombat.cs");
        var constructionTickets = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.ConstructionTickets.cs");
        var bridgeSources = buildingCombat + turretCombat + constructionTickets;
        RequireText(bridgeSources, "_entityWorld.Events.DrainInto(_simEventDrainBuffer)", "UnitBattlefield bridge paths must drain sim events into reusable storage.", result);
        RequireText(constructionTickets, "for (var index = _simEventDrainBuffer.Count - 1; index >= 0; index--)", "Construction rejection drain must preserve last-match semantics with an explicit reverse scan.", result);
        ForbidText(bridgeSources, "_entityWorld.Events.Drain()", "UnitBattlefield bridge paths must not allocate SimEvent snapshot arrays.", result);
        ForbidText(constructionTickets, ".OfType<ConstructionRejectedEvent>()", "Construction rejection drain must not allocate LINQ event filters.", result);
        ForbidText(constructionTickets, ".LastOrDefault(", "Construction rejection drain must not use LINQ last-match queries.", result);
    }

    private static void RequireExplicitUnitBridgeFilters(string root, GateResult result)
    {
        var lifecycle = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.BuildingLifecycle.cs");
        var removal = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CommandApplyRemoval.cs");
        ForbidText(lifecycle, "Units.Where(unit => unit.AttackTargetKind == CombatTargetKind.Building && unit.AttackTargetId == id)", "Building removal must scan units explicitly.", result);
        ForbidText(removal, "Units.Where(unit => unit.PlayerSlotId == command.Issuer.ToPlayerSlot())", "Selection command state sync must scan units explicitly.", result);
    }

    private static void RequirePlacementQueryBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<PlacementBuildAnchor> _placementBuildAnchors", "UnitBattlefield placement validation must reuse build-anchor storage.", result);
        RequireText(battlefield, "List<PlacementObstacle> _placementObstacles", "UnitBattlefield placement validation must reuse obstacle storage.", result);

        var lifecycle = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.BuildingLifecycle.cs");
        RequireText(lifecycle, "CollectBuildingBuildAnchors(playerSlotId, _placementBuildAnchors)", "ValidateBuildingPlacement must fill reusable build-anchor storage.", result);
        RequireText(lifecycle, "CollectBuildingPlacementObstacles(_placementObstacles)", "ValidateBuildingPlacement must fill reusable obstacle storage.", result);

        var systems = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.EntityWorldSystems.cs");
        RequireText(systems, "CollectBuildingBuildAnchors(PlayerSlotId playerSlotId, List<PlacementBuildAnchor> result)", "Building build-anchor collection must use a caller-owned buffer.", result);
        RequireText(systems, "CollectBuildingPlacementObstacles(List<PlacementObstacle> result)", "Building placement obstacle collection must use a caller-owned buffer.", result);
        ForbidText(systems, "private IReadOnlyList<SpawnObstacle> SpawnObstacles", "UnitBattlefield must not keep the unused spawn-obstacle allocation helper.", result);
        ForbidText(systems, "private IReadOnlyList<PlacementBuildAnchor> BuildingBuildAnchors", "Building build-anchor collection must not allocate lists per placement validation.", result);
        ForbidText(systems, "private IReadOnlyList<PlacementObstacle> BuildingPlacementObstacles", "Building placement obstacle collection must not allocate lists per placement validation.", result);
        ForbidText(systems, "BuildingTargetIds()\n            .Select(BuildingSnapshot)", "Placement query helpers must not allocate snapshot LINQ chains.", result);
        ForbidText(systems, ".ToList();", "Placement query helpers must not materialize lists with LINQ.", result);
    }

    private static void RequireConstructionWorkScan(string root, GateResult result)
    {
        var systems = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.EntityWorldSystems.cs");
        RequireText(systems, "private bool HasActiveConstructionWork()", "UnitBattlefield construction updates must use an explicit work scan helper.", result);
        RequireText(systems, "construction.Phase is ConstructionPhase.Building or ConstructionPhase.Queued", "Construction work scan must preserve building/queued phase filtering.", result);
        ForbidText(systems, "_entityWorld.OrderedEntities.Any(entity =>", "UnitBattlefield construction work checks must not allocate LINQ Any iterators.", result);
    }
}
