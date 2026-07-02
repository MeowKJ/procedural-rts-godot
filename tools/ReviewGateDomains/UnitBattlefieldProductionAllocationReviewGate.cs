static class UnitBattlefieldProductionAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireProductionEnqueueProducerBuffers(root, result);
        RequireProductionQueueSummaryBuffers(root, result);
        RequireProductionOptionStateBuffers(root, result);
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

    private static void RequireProductionOptionStateBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<UnitSpec> _productionDesignSpecBuffer", "Production option states must reuse design spec storage.", result);

        var options = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.ProductionOptions.cs");
        RequireText(options, "CollectCandidateProducerIds(kind, playerSlotId, _productionCandidateProducerIds)", "Legacy production option states must reuse producer candidate storage.", result);
        RequireText(options, "CollectCandidateProducerIds(spec, playerSlotId, _productionCandidateProducerIds)", "UnitDesign production option states must reuse producer candidate storage.", result);
        RequireText(options, "CollectProductionDesignSpecs(playerSlotId, _productionDesignSpecBuffer)", "UnitDesign production option states must reuse design spec storage.", result);
        RequireText(options, "ProductionKindQueueMetrics(kind, spec)", "Legacy production option states must compute queue metrics with explicit loops.", result);
        RequireText(options, "ProductionDesignQueueMetrics(spec)", "UnitDesign production option states must compute queue metrics with explicit loops.", result);
        ForbidText(options, "CandidateProducerIds(kind, playerSlotId).ToList()", "Production option states must not materialize producer candidates.", result);
        ForbidText(options, "CandidateProducerIds(spec, playerSlotId).ToList()", "Production option states must not materialize producer candidates.", result);
        ForbidText(options, ".Sum(buildingId => BuildingProductionQueue(buildingId).Count", "Production option states must not allocate LINQ queue counts.", result);
        ForbidText(options, ".Select(buildingId => BuildingProductionQueue(buildingId).FirstOrDefault())", "Production option states must not allocate LINQ first-item scans.", result);
        ForbidText(options, ".DefaultIfEmpty(0)\n                    .Max();", "Production option states must compute max progress in explicit loops.", result);

        var systems = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.EntityWorldSystems.cs");
        ForbidText(systems, "private IEnumerable<int> CandidateProducerIds", "UnitBattlefield production candidates must use caller-owned buffers, not allocating enumerable helpers.", result);
        ForbidText(systems, "private IEnumerable<UnitSpec> ProductionDesignSpecs", "Production design option states must use caller-owned spec buffers, not allocating enumerable helpers.", result);
        RequireText(systems, "foreach (var designId in UnitDesignFactionRosterCatalog.For(identity.Faction).PlayableDesignIds)", "UnitBattlefield production availability must scan playable design ids explicitly.", result);
        ForbidText(systems, ".PlayableDesignIds\n            .Select(UnitDesignCatalog.Spec)\n            .Any(spec =>", "UnitBattlefield production availability must not allocate roster projection/predicate chains.", result);
    }
}
