static class UnitBattlefieldAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireBuildingTargetIdBuffers(root, result);
        RequireHarvestRepairCommandBuffers(root, result);
        RequireGroupCommandSubjectBuffers(root, result);
        RequireDeathRemovalBuffers(root, result);
        RequireProductionSyncBuffers(root, result);
        RequireConstructionSubjectBuffers(root, result);
        RequireSelectedBuildingRallyBuffers(root, result);
        RequireBuildingProjectionBuffers(root, result);
        RequireUnitResourceProjectionBuffers(root, result);
    }

    private static void RequireBuildingTargetIdBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<int> _buildingTargetIdBuffer", "UnitBattlefield building scans must reuse primary building-id storage.", result);
        RequireText(battlefield, "List<int> _buildingTargetIdSecondaryBuffer", "UnitBattlefield nested building scans must reuse secondary building-id storage.", result);
        RequireText(battlefield, "List<int> _buildingProjectionTargetIdBuffer", "UnitBattlefield projection scans must reuse building-id storage.", result);
        RequireText(battlefield, "List<int> _buildingVisibilityViewerIdBuffer", "UnitBattlefield visibility viewer scans must reuse building-id storage.", result);
        RequireText(battlefield, "List<int> _buildingVisibilityTargetIdBuffer", "UnitBattlefield visibility target scans must reuse building-id storage.", result);
        RequireText(battlefield, "CollectBuildingTargetIds(_buildingTargetIdBuffer)", "UnitBattlefield hot building scans must fill reusable buffers.", result);
        ForbidText(battlefield, "BuildingTargetIds()", "UnitBattlefield building scans must not use an allocating BuildingTargetIds helper.", result);

        var projection = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.BuildingProjection.cs");
        RequireText(projection, "private void CollectBuildingTargetIds(List<int> result)", "Building target id scans must use caller-owned buffers.", result);
        RequireText(projection, "result.Sort(CompareBuildingIds)", "Building target id scans must preserve stable building-id order.", result);
        ForbidText(projection, "var ids = new List<int>();", "Building target id scans must not allocate a fresh id list.", result);
        ForbidText(projection, "var seen = new HashSet<int>();", "Building target id scans must not allocate a fresh de-duplication set.", result);
        ForbidText(projection, "private IReadOnlyList<int> BuildingTargetIds()", "Building target id scans must not return allocating snapshots.", result);
    }

    private static void RequireBuildingProjectionBuffers(string root, GateResult result) {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<UnitBattlefieldBuildingSnapshot> _buildingSnapshotBuffer", "Building projection paths must reuse snapshot storage.", result);
        RequireText(battlefield, "List<BuildingRallyProjection> _buildingRallyProjectionBuffer", "Building projection paths must reuse rally storage.", result);
        RequireText(battlefield, "List<BuildingSelectionProjection> _buildingSelectionProjectionBuffer", "Building projection paths must reuse selection storage.", result);
        RequireText(battlefield, "List<BuildingMinimapProjection> _buildingMinimapProjectionSecondaryBuffer", "Building minimap projections must preserve adjacent snapshot comparisons with reusable storage.", result);
        var projection = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.BuildingProjection.cs");
        ForbidText(projection, ".ToArray()", "Building projection paths must not allocate arrays.", result);
        ForbidText(projection, ".ToList()", "Building projection paths must not allocate result lists.", result);
        ForbidText(projection, "private IEnumerable<EntityId> SelectedBuildingEntityIds", "Selected building entity ids must use caller-owned storage.", result);
        ForbidText(battlefield, ".Select(BuildingHitPulseProjection)", "Building hit-pulse projections must not allocate LINQ result chains.", result);
        ForbidText(battlefield, ".Select(buildingId => BuildingMinimapProjection", "Building minimap projections must not allocate LINQ result chains.", result);
    }
    private static void RequireHarvestRepairCommandBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<int> _unitCommandIdBuffer", "UnitBattlefield harvest/repair commands must reuse requested-id storage.", result);
        RequireText(battlefield, "List<UnitInstance> _unitCommandBuffer", "UnitBattlefield harvest/repair commands must reuse unit storage.", result);
        RequireText(battlefield, "List<EntityId> _unitCommandEntityBuffer", "UnitBattlefield harvest/repair commands must reuse entity subject storage.", result);

        var harvestRepair = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.HarvestRepair.cs");
        RequireText(harvestRepair, "CollectSelectedCommandUnits(playerSlotId, IsHarvester, _unitCommandBuffer)", "Selected harvest commands must fill the reusable unit buffer.", result);
        RequireText(harvestRepair, "CollectRequestedCommandUnits(playerSlotId, unitIds, IsHarvester, _unitCommandBuffer)", "Explicit harvest commands must fill the reusable unit buffer.", result);
        RequireText(harvestRepair, "CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer)", "Harvest/repair commands must fill reusable entity subject storage.", result);
        ForbidText(harvestRepair, ".ToHashSet()", "Harvest/repair commands must not allocate requested-id sets.", result);
        ForbidText(harvestRepair, ".ToList()", "Harvest/repair commands must not allocate unit or subject lists.", result);
        ForbidText(harvestRepair, ".OrderBy(unit => unit.Id)", "Harvest/repair commands must sort reusable buffers in place.", result);
        ForbidText(harvestRepair, ".Select(unit => unit.EntityId)", "Harvest/repair commands must fill entity subjects with an explicit loop.", result);
    }

    private static void RequireGroupCommandSubjectBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<UnitInstance> _unitCommandBuffer", "UnitBattlefield group commands must reuse unit subject storage.", result);
        RequireText(battlefield, "List<EntityId> _unitCommandEntityBuffer", "UnitBattlefield group commands must reuse entity subject storage.", result);

        var commands = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.Commands.cs");
        var buffers = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CommandSubjectBuffers.cs");
        RequireText(commands, "CollectSelectedCommandUnits(playerSlotId, _unitCommandBuffer)", "Selected move/stop commands must fill the reusable unit buffer.", result);
        RequireText(commands, "CollectSelectedArmedCommandUnits(playerSlotId, _unitCommandBuffer)", "Selected stance commands must fill the reusable armed-unit buffer.", result);
        RequireText(commands, "CollectSelectedCommandUnitsTargeting(playerSlotId, target, _unitCommandBuffer)", "Selected unit attacks must fill the reusable target-filtered buffer.", result);
        RequireText(commands, "CollectSelectedCommandUnitsTargeting(playerSlotId, targetSpec, _unitCommandBuffer)", "Selected building attacks must fill the reusable target-filtered buffer.", result);
        RequireText(commands, "CollectRequestedCommandUnits(playerSlotId, unitIds, _unitCommandBuffer)", "Explicit move commands must fill the reusable unit buffer.", result);
        RequireText(commands, "CollectRequestedCommandUnitsTargeting(playerSlotId, unitIds, target, _unitCommandBuffer)", "Explicit unit attacks must fill the reusable target-filtered buffer.", result);
        RequireText(commands, "CollectRequestedCommandUnitsTargeting(playerSlotId, unitIds, targetSpec, _unitCommandBuffer)", "Explicit building attacks must fill the reusable target-filtered buffer.", result);
        RequireText(commands, "CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer)", "Group commands must fill reusable entity subject storage.", result);
        RequireText(buffers, "CollectRequestedCommandIds(unitIds)", "Explicit group commands must reuse requested-id storage.", result);
        ForbidText(commands, "var requestedIds = unitIds.ToHashSet();", "Group commands must not allocate requested-id sets.", result);
        ForbidText(commands, ".Select(unit => unit.EntityId).ToList()", "Group commands must not allocate entity subject lists.", result);
        ForbidText(commands, ".OrderBy(unit => unit.Id)", "Group commands must sort reusable buffers in place.", result);
        ForbidText(commands, ".Where(unit => CanUnitTarget(unit, target))", "Group attack commands must not allocate LINQ targeting filters.", result);
        ForbidText(commands, ".Where(unit => CanUnitTarget(unit, targetSpec))", "Group attack commands must not allocate LINQ targeting filters.", result);
    }

    private static void RequireDeathRemovalBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<UnitInstanceDeathInfo> _unitDeathBuffer", "UnitBattlefield death removal must reuse unit death storage.", result);
        RequireText(battlefield, "List<UnitBattlefieldBuildingDeathInfo> _buildingDeathBuffer", "UnitBattlefield death removal must reuse building death storage.", result);
        RequireText(battlefield, "HashSet<int> _removedUnitIdBuffer", "UnitBattlefield death removal must reuse removed-unit id storage.", result);
        RequireText(battlefield, "HashSet<int> _removedBuildingIdBuffer", "UnitBattlefield death removal must reuse removed-building id storage.", result);

        RequireText(battlefield, "_buildingDeathBuffer.Clear();", "Building death removal must clear reusable death storage.", result);
        RequireText(battlefield, "_unitDeathBuffer.Clear();", "Unit death removal must clear reusable death storage.", result);
        RequireText(battlefield, "Units.RemoveAll(IsRemovedUnit)", "Unit removal must use the reusable removed-unit id set.", result);
        ForbidText(battlefield, ".Select(BuildingDeathInfo)", "Building death removal must not allocate LINQ death projections.", result);
        ForbidText(battlefield, ".Select(death => death.Id).ToHashSet()", "Death removal must not allocate removed-id sets.", result);
        ForbidText(battlefield, ".Where(unit => unit.Hp <= 0)", "Unit death removal must not allocate LINQ unit filters.", result);
        ForbidText(battlefield, "var deadIds = BuildingTargetIds()", "Dead building scan must not allocate BuildingTargetIds snapshots.", result);
    }

    private static void RequireProductionSyncBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<int> _productionActiveProducerIds", "Production sync must reuse active producer storage.", result);
        RequireText(battlefield, "HashSet<int> _productionKnownEntityIds", "Production sync must reuse known entity id storage.", result);
        RequireText(battlefield, "List<UnitBattlefieldProductionQueueSnapshot> _productionQueuedBefore", "Production sync must reuse queued-before snapshots.", result);
        RequireText(battlefield, "List<EntityInstance> _productionNewUnitEntities", "Production sync must reuse new unit entity storage.", result);

        var production = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.ProductionSync.cs");
        RequireText(production, "CollectActiveProducerIds(_productionActiveProducerIds)", "Production sync must fill the reusable active producer buffer.", result);
        RequireText(production, "CollectKnownProductionEntityIds(_productionKnownEntityIds)", "Production sync must fill the reusable known entity set.", result);
        RequireText(production, "CollectQueuedProductionSnapshots(_productionActiveProducerIds, _productionQueuedBefore)", "Production sync must fill queued snapshots explicitly.", result);
        RequireText(production, "CollectNewProductionUnitEntities(_productionKnownEntityIds, _productionNewUnitEntities)", "Production sync must fill new-unit storage explicitly.", result);
        ForbidText(production, ".ToList()", "Production sync must not allocate materialized LINQ lists.", result);
        ForbidText(production, ".ToHashSet()", "Production sync must not allocate known-entity sets.", result);
        ForbidText(production, "new\n            {", "Production sync must not use anonymous queued-before snapshots.", result);
        ForbidText(production, ".OrderBy(entry => entry.Snapshot.Position", "Production completion matching must not allocate ordered LINQ snapshots.", result);
    }

    private static void RequireConstructionSubjectBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<int> _constructionSubjectBuildingIds", "Construction command bridge must reuse subject building-id storage.", result);
        RequireText(battlefield, "List<EntityId> _constructionSubjectEntityBuffer", "Construction command bridge must reuse subject entity-id storage.", result);

        var commandBridge = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CommandBridge.cs");
        RequireText(commandBridge, "CollectConstructionSubjectEntities(playerSlotId, spec, _constructionSubjectBuildingIds, _constructionSubjectEntityBuffer)", "Construction commands must fill reusable subject buffers.", result);
        RequireText(commandBridge, "buildingIds.Sort(CompareBuildingIds)", "Construction subject building ids must sort the reusable buffer in place.", result);
        ForbidText(commandBridge, ".Select(BuildingSnapshot)\n            .Where(snapshot => snapshot is not null)", "Construction subject bridge must not allocate snapshot LINQ chains.", result);
        ForbidText(commandBridge, ".OrderBy(building => building.Id)\n            .Select(building =>", "Construction subject bridge must not allocate ordered subject LINQ chains.", result);
    }

    private static void RequireSelectedBuildingRallyBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<int> _selectedBuildingRallyProducerIds", "Selected building rally commands must reuse producer-id storage.", result);

        var rally = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.ProductionRally.cs");
        RequireText(battlefield, "CollectSelectedBuildingRallyProducerIds(playerSlotId, _selectedBuildingRallyProducerIds)", "Selected building rally commands must fill reusable producer storage.", result);
        RequireText(battlefield, "result.Sort(CompareBuildingIds)", "Selected building rally producers must sort the reusable buffer in place.", result);
        ForbidText(rally, "var selected = BuildingTargetIds()\n            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)\n            .Where(buildingId => BuildingProjection(buildingId)?.Selected == true)\n            .ToList();", "Selected building rally commands must not allocate selected-building lists.", result);
        ForbidText(rally, "var producers = selected\n            .Where(HasAnyProductionForCore)\n            .OrderBy(buildingId => buildingId)\n            .ToList();", "Selected building rally commands must not allocate producer lists.", result);
    }

    private static void RequireUnitResourceProjectionBuffers(string root, GateResult result) {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<EntityProjection> _unitProjectionBuffer", "UnitProjections must reuse result storage.", result);
        RequireText(battlefield, "List<UnitBattlefieldResourcePip> _resourcePipSecondaryBuffer", "ResourcePips must preserve adjacent snapshot comparisons with reusable storage.", result);
        RequireText(battlefield, "List<UnitMinimapPip> _unitMinimapPipSecondaryBuffer", "Unit minimap pips must preserve adjacent snapshot comparisons with reusable storage.", result);
        RequireText(battlefield, "List<UnitSelectionSummaryItem> _selectionSummaryBuffer", "SelectionSummary must reuse result storage.", result);
        RequireText(battlefield, "var units = new List<UnitInstance>(designs.Count)", "SpawnRoster must pre-size and fill its result list explicitly.", result);
        RequireText(battlefield, "units.Add(Spawn(designs[index].ToSpec(), playerSlotId, start + spacing * index))", "SpawnRoster must preserve roster order through an indexed loop.", result);
        var core = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CoreQueries.cs");
        ForbidText(battlefield, ".Select((design, index) => Spawn", "SpawnRoster must not allocate LINQ projection iterators.", result);
        ForbidText(core, ".OrderBy(unit => unit.EntityId.Value)", "UnitProjections must sort reusable storage in place.", result);
        ForbidText(core, ".ToList()", "Unit/resource projection paths must not allocate result lists.", result);
        var visibility = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.VisibilityCombat.cs");
        ForbidText(visibility, ".GroupBy(unit =>", "SelectionSummary must not allocate grouping enumerables.", result);
        ForbidText(visibility, ".ToList()", "Unit minimap and selection summary paths must not allocate result lists.", result);
    }

}
