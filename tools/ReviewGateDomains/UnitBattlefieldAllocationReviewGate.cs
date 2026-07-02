static class UnitBattlefieldAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireHarvestRepairCommandBuffers(root, result);
        RequireDeathRemovalBuffers(root, result);
        RequireProductionSyncBuffers(root, result);
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
}
