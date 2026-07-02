static class UnitBattlefieldProductionAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireProductionEnqueueProducerBuffers(root, result);
        RequireProductionQueueSummaryBuffers(root, result);
    }

    private static void RequireProductionEnqueueProducerBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<int> _productionCandidateProducerIds", "Production enqueue paths must reuse producer candidate storage.", result);

        var rally = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.ProductionRally.cs");
        RequireText(rally, "CollectCandidateProducerIds(productionKind, playerSlotId, _productionCandidateProducerIds)", "Legacy production enqueue must fill reusable producer candidate storage.", result);
        RequireText(rally, "CollectCandidateProducerIds(spec, playerSlotId, _productionCandidateProducerIds)", "UnitDesign production enqueue must fill reusable producer candidate storage.", result);
        RequireText(rally, "LeastQueuedProducerId(_productionCandidateProducerIds)", "Production enqueue must choose the least-queued producer without ordered LINQ.", result);
        ForbidText(rally, "CandidateProducerIds(productionKind, playerSlotId)\n            .OrderBy(buildingId => BuildingProductionQueue(buildingId).Count)", "Legacy production enqueue must not allocate ordered producer candidate chains.", result);
        ForbidText(rally, "CandidateProducerIds(spec, playerSlotId)\n            .OrderBy(buildingId => BuildingProductionQueue(buildingId).Count)", "UnitDesign production enqueue must not allocate ordered producer candidate chains.", result);
        ForbidText(rally, ".ThenBy(buildingId => buildingId)\n            .Select(buildingId => (int?)buildingId)\n            .FirstOrDefault();", "Production enqueue must not allocate ordered producer candidate chains.", result);
    }

    private static void RequireProductionQueueSummaryBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<ProductionQueueSummaryEntry> _productionQueueSummaryBuffer", "Production queue summary must reuse queue entry storage.", result);
        RequireText(battlefield, "HashSet<int> _productionQueueSummarySeenIds", "Production queue summary must reuse building de-duplication storage.", result);

        var summary = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.ProductionQueueSummary.cs");
        RequireText(summary, "CollectQueuedProductionSummary(playerSlotId, _productionQueueSummaryBuffer)", "Production queue summary and cancel paths must fill reusable queue storage.", result);
        RequireText(summary, "_productionQueueSummaryBuffer.Sort(CompareProductionQueueSummaryEntries)", "Production queue summary must sort reusable queue storage in place.", result);
        ForbidText(summary, ".SelectMany(buildingId => BuildingProductionQueue(buildingId).Select(item => new", "Production queue summary must not allocate anonymous queue entries.", result);
        ForbidText(summary, ".OrderBy(entry => entry.Item.Id)\n            .ToList();", "Production queue summary must not allocate ordered queue lists.", result);
        ForbidText(summary, ".Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)", "Production queue summary paths must not allocate LINQ building filters.", result);
        ForbidText(summary, ".Any(buildingId => BuildingProductionQueue(buildingId).Count > 0)", "HasQueuedProduction must use an explicit early-exit scan.", result);
    }
}
