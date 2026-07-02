static class UnitBattlefieldAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireHarvestRepairCommandBuffers(root, result);
        RequireGroupCommandSubjectBuffers(root, result);
        RequireDeathRemovalBuffers(root, result);
        RequireProductionSyncBuffers(root, result);
        RequireConstructionSubjectBuffers(root, result);
        RequireSelectedBuildingRallyBuffers(root, result);
    }

    private static void RequireHarvestRepairCommandBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<int> _unitCommandIdBuffer", "UnitBattlefield harvest/repair commands must reuse requested-id storage.", result);
        RequireText(battlefield, "List<UnitInstance> _unitCommandBuffer", "UnitBattlefield harvest/repair commands must reuse unit storage.", result);
        RequireText(battlefield, "List<EntityId> _unitCommandEntityBuffer", "UnitBattlefield harvest/repair commands must reuse entity subject storage.", result);

        var harvestRepair = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.HarvestRepair.cs");
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

        var removal = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.CommandApplyRemoval.cs");
        RequireText(removal, "_buildingDeathBuffer.Clear();", "Building death removal must clear reusable death storage.", result);
        RequireText(removal, "_unitDeathBuffer.Clear();", "Unit death removal must clear reusable death storage.", result);
        RequireText(removal, "Units.RemoveAll(IsRemovedUnit)", "Unit removal must use the reusable removed-unit id set.", result);
        ForbidText(removal, ".Select(BuildingDeathInfo)", "Building death removal must not allocate LINQ death projections.", result);
        ForbidText(removal, ".Select(death => death.Id).ToHashSet()", "Death removal must not allocate removed-id sets.", result);
        ForbidText(removal, ".Where(unit => unit.Hp <= 0)", "Unit death removal must not allocate LINQ unit filters.", result);
        ForbidText(removal, "var deadIds = BuildingTargetIds()", "Dead building scan must not allocate BuildingTargetIds snapshots.", result);
    }

    private static void RequireProductionSyncBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<int> _productionActiveProducerIds", "Production sync must reuse active producer storage.", result);
        RequireText(battlefield, "HashSet<int> _productionKnownEntityIds", "Production sync must reuse known entity id storage.", result);
        RequireText(battlefield, "List<UnitBattlefieldProductionQueueSnapshot> _productionQueuedBefore", "Production sync must reuse queued-before snapshots.", result);
        RequireText(battlefield, "List<EntityInstance> _productionNewUnitEntities", "Production sync must reuse new unit entity storage.", result);

        var production = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.ProductionSync.cs");
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

        var commandBridge = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.CommandBridge.cs");
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

        var rally = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.ProductionRally.cs");
        RequireText(battlefield, "CollectSelectedBuildingRallyProducerIds(playerSlotId, _selectedBuildingRallyProducerIds)", "Selected building rally commands must fill reusable producer storage.", result);
        RequireText(battlefield, "result.Sort(CompareBuildingIds)", "Selected building rally producers must sort the reusable buffer in place.", result);
        ForbidText(rally, "var selected = BuildingTargetIds()\n            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)\n            .Where(buildingId => BuildingProjection(buildingId)?.Selected == true)\n            .ToList();", "Selected building rally commands must not allocate selected-building lists.", result);
        ForbidText(rally, "var producers = selected\n            .Where(HasAnyProductionForCore)\n            .OrderBy(buildingId => buildingId)\n            .ToList();", "Selected building rally commands must not allocate producer lists.", result);
    }

}
