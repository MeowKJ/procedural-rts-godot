static class EnemyProductionAiReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireFileBudgets(root, result);
        RequireProductionBuffers(root, result);
        RequireConstructionScans(root, result);
        RequireEconomyScans(root, result);
    }

    private static void RequireFileBudgets(string root, GateResult result)
    {
        foreach (var file in new[]
        {
            "UnitBattlefieldEnemyProductionAi.cs",
            "UnitBattlefieldEnemyProductionAi.Production.cs",
            "UnitBattlefieldEnemyProductionAi.ProductionScans.cs",
            "UnitBattlefieldEnemyProductionAi.ProductionComparison.cs",
            "UnitBattlefieldEnemyProductionAi.Construction.cs",
            "UnitBattlefieldEnemyProductionAi.ConstructionOffsets.cs",
            "UnitBattlefieldEnemyProductionAi.Economy.cs",
        })
        {
            RequireFileUnderLineBudget(root, result, file);
        }
    }

    private static void RequireProductionBuffers(string root, GateResult result)
    {
        var ai = ReviewGateEvidence.ReadSourceWithPartials(ProductionAiPath(root, "UnitBattlefieldEnemyProductionAi.cs"));
        RequireText(ai, "List<ProductionOptionState> _queueableDesignOptions", "Enemy production AI must reuse queueable design option storage.", result);
        RequireText(ai, "List<UnitBattlefieldBuildingSnapshot> _ownedBuildingBuffer", "Enemy production AI must reuse owned-building snapshot storage.", result);
        RequireText(ai, "CollectQueueableDesignOptions(", "Enemy production design choices must fill reusable option storage.", result);
        RequireText(ai, "FirstFallbackCombatOption(", "Enemy production fallback must use an explicit best-option scan.", result);
        RequireText(ai, "QueuedCategoryCount(", "Enemy production queued-category counts must use explicit queue scans.", result);
        RequireText(ai, "QueuedDesignCount(", "Enemy production queued-design counts must use explicit queue scans.", result); RequireText(ai, "LiveUnitDesignCount(", "Runtime enemy production AI must use UnitBattlefield design-count queries.", result); RequireText(ai, "LiveEconomyUnitCount(", "Runtime enemy production AI must use UnitBattlefield economy-unit count queries.", result);

        var production = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyProductionAi.Production.cs");
        var productionScans = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyProductionAi.ProductionScans.cs");
        RequireText(ai, "CommandEnqueueProductionDesign(", "Runtime enemy production AI must submit concrete design ids through UnitBattlefield.", result); ForbidText(ai, "battlefield.EnqueueProductionDesign(", "Runtime enemy production AI must not bypass the authoritative design command path.", result); ForbidText(ai, "CommandEnqueueProduction(", "Runtime enemy production AI must not restore generic production-kind commands.", result);
        ForbidProductionLinq(production, "Enemy production choice", result);
        ForbidProductionLinq(productionScans, "Enemy production scan helpers", result); ForbidText(productionScans, "battlefield.Units", "Runtime enemy production scan helpers must not scan the UnitBattlefield unit list directly.", result);
    }

    private static void RequireConstructionScans(string root, GateResult result)
    {
        var construction = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyProductionAi.Construction.cs");
        var offsets = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyProductionAi.ConstructionOffsets.cs");
        RequireText(construction, "CollectOwnedBuildings(battlefield, enemyPlayerSlotId, _ownedBuildingBuffer", "Enemy construction requirements must reuse owned-building storage.", result);
        RequireText(construction, "battlefield.ConstructBuilding(", "Enemy construction decisions must enter through the UnitBattlefield construction command routing.", result);
        RequireText(construction, "new Vector2(placement.X, placement.Y)", "Enemy construction must submit the shared authority's snapped coordinates.", result);
        RequireText(construction, "ConstructionPlacementIntent.Direct", "Enemy construction must query the shared Direct placement intent.", result);
        RequireText(construction, "CandidateBuildOffsets(next)", "Enemy construction placement must iterate static build offsets.", result); RequireText(construction, "LiveNonEconomyUnitsNear(", "Enemy construction defense decisions must use the UnitBattlefield combat-unit count query.", result);
        RequireText(offsets, "private static readonly Vector2[] PowerPlantBuildOffsets", "Enemy construction offsets must be static data.", result);
        ForbidProductionLinq(construction, "Enemy construction", result);
        ForbidText(construction, "new StartConstructionEntityCommand", "Enemy construction AI must not instantiate construction commands directly; use the UnitBattlefield gateway.", result);
        ForbidText(construction, "IEnumerable<Vector2>", "Enemy construction must not allocate iterator state for candidate positions.", result);
        ForbidText(construction, "yield return", "Enemy construction candidate positions must not use yield iterators.", result);
        ForbidText(construction, "new[] { new Vector2", "Enemy construction must not allocate offset arrays per decision.", result); ForbidText(construction, "battlefield.Units", "Runtime enemy construction choices must not scan the UnitBattlefield unit list directly.", result);
    }

    private static void RequireEconomyScans(string root, GateResult result)
    {
        var ai = ReviewGateEvidence.ReadSourceWithPartials(ProductionAiPath(root, "UnitBattlefieldEnemyProductionAi.cs"));
        RequireText(ai, "List<UnitInstance> _idleHarvesterBuffer", "Enemy economy must reuse idle harvester storage.", result);
        RequireText(ai, "List<int> _idleHarvesterIds", "Enemy economy must reuse harvester command id storage.", result);

        var economy = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyProductionAi.Economy.cs"); var queries = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.ProductionEconomyQueries.cs");
        RequireText(economy, "battlefield.CollectIdleEconomyUnits(", "Enemy economy must fill idle harvester storage through the UnitBattlefield query routing.", result); RequireText(economy, "battlefield.NearestVisibleResourceField(", "Enemy economy must choose resources through the UnitBattlefield visibility query routing.", result);
        RequireText(economy, "CollectUnitIds(_idleHarvesterBuffer, _idleHarvesterIds)", "Enemy economy harvest commands must reuse id storage.", result); RequireText(economy, "battlefield.CollectOwnedBuildings(", "Enemy economy/base helpers must use the UnitBattlefield owned-building query routing.", result);
        RequireText(economy, "SetMissingProducerRallyPoints(", "Enemy economy rally setup must use the UnitBattlefield rally command routing.", result); RequireText(economy, "LiveBuildingCenterOrFallback(", "Enemy base center must use the UnitBattlefield building-center query routing.", result); RequireText(economy, "FirstOwnedBuildingFactionOrDefault(", "Enemy faction lookup must use the UnitBattlefield building-faction query routing.", result);
        RequireText(queries, "CollectIdleEconomyUnits(", "UnitBattlefield must own idle economy-unit collection.", result); RequireText(queries, "NearestVisibleResourceField(", "UnitBattlefield must own visible resource-field selection.", result);
        ForbidProductionLinq(economy, "Enemy economy", result);
        ForbidText(economy, "battlefield.Units", "Enemy economy must not scan UnitBattlefield units directly.", result); ForbidText(economy, "battlefield.BuildingSnapshots()", "Enemy economy must not scan UnitBattlefield buildings directly.", result); ForbidText(economy, "battlefield.ResourceFields", "Enemy economy must not scan resource fields directly.", result); ForbidText(economy, "EntityWorld.Visibility", "Enemy economy must not inspect visibility directly.", result); ForbidText(economy, ".Aggregate(", "Enemy base-center calculation must not allocate aggregate delegates.", result);
        ForbidText(economy, "idleHarvesters.Select", "Enemy harvester commands must not allocate id projections.", result);
    }

    private static void ForbidProductionLinq(string source, string name, GateResult result)
    {
        ForbidText(source, ".Where(", $"{name} must not allocate LINQ filter chains.", result);
        ForbidText(source, ".OrderBy(", $"{name} must not allocate ordered LINQ chains.", result);
        ForbidText(source, ".ThenBy(", $"{name} must not allocate secondary ordered LINQ chains.", result);
        ForbidText(source, ".Select(", $"{name} must not allocate projection chains.", result);
        ForbidText(source, ".SelectMany(", $"{name} must not allocate flattened queue chains.", result);
        ForbidText(source, ".ToList()", $"{name} must not materialize temporary lists.", result);
        ForbidText(source, ".ToHashSet()", $"{name} must not materialize temporary sets.", result);
        ForbidText(source, ".Any(", $"{name} must not allocate LINQ Any queries.", result);
        ForbidText(source, ".Sum(", $"{name} must not allocate LINQ Sum queries.", result);
        ForbidText(source, ".FirstOrDefault()", $"{name} must not use LINQ first queries.", result);
    }

    private static string ProductionAiPath(string root, string file)
    {
        return Path.Combine(root, "scripts", "core", "units", "runtime", file);
    }

    private static void RequireFileUnderLineBudget(string root, GateResult result, string file)
    {
        var path = ProductionAiPath(root, file);
        if (!File.Exists(path))
        {
            result.Error($"Enemy production AI partial is missing: scripts/core/units/runtime/{file}.");
            return;
        }

        var lines = File.ReadAllLines(path).Length;
        if (lines > 200)
        {
            result.Error($"Enemy production AI partial exceeds 200 lines: scripts/core/units/runtime/{file} has {lines} lines.");
        }
    }
}
