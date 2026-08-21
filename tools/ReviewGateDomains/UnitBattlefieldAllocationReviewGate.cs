static class UnitBattlefieldAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireQueryDomainModules(root, result);
        RequireResourceDomainModules(root, result);
        RequireConstructionDomainModules(root, result);
        RequireBuildingTargetIdBuffers(root, result);
        RequireSelectionSubjectBuffer(root, result);
        RequireDeathRemovalBuffers(root, result);
        RequireProductionSyncBuffers(root, result);
        RequireConstructionSubjectBuffers(root, result);
        RequireSelectedBuildingRallyBuffers(root, result);
        RequireBuildingProjectionBuffers(root, result);
        RequireUnitResourceProjectionBuffers(root, result);
    }

    private static void RequireConstructionDomainModules(string root, GateResult result)
    {
        foreach (var module in new[]
        {
            "UnitBattlefield.BuildingEntityCreation.cs",
            "UnitBattlefield.BuildingLifecycle.cs",
            "UnitBattlefield.BuildingSell.cs",
            "UnitBattlefield.BuildingState.cs",
            "UnitBattlefield.ConstructionProviderLanes.cs",
            "UnitBattlefield.ConstructionTickets.cs",
        })
        {
            ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "runtime", "battlefield", "construction", module);
            ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "runtime", "battlefield", module);
        }
    }

    private static void RequireResourceDomainModules(string root, GateResult result)
    {
        foreach (var module in new[]
        {
            "UnitBattlefield.ResourceHarvestRuntime.cs",
            "UnitBattlefield.ResourceNodeProjections.cs",
            "UnitBattlefield.ResourceNotificationBuffers.cs",
            "UnitBattlefield.ResourceQueries.cs",
        })
        {
            ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "runtime", "battlefield", "resource", module);
            ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "runtime", "battlefield", module);
        }
    }

    private static void RequireQueryDomainModules(string root, GateResult result)
    {
        foreach (var module in new[]
        {
            "UnitBattlefield.AttackTargetQueries.cs",
            "UnitBattlefield.AttackWaveGeometryQueries.cs",
            "UnitBattlefield.CoreQueries.cs",
            "UnitBattlefield.ObservationView.cs",
            "UnitBattlefield.PickingQueries.cs",
        })
        {
            ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "runtime", "battlefield", "query", module);
            ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "runtime", "battlefield", module);
        }
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
    private static void RequireSelectionSubjectBuffer(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<int> _selectionUnitIdBuffer", "UnitBattlefield selection commands must reuse requested-id storage.", result);

        var commands = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.Commands.cs");
        RequireText(commands, "CollectRequestedSelectionUnits(playerSlotId, unitIds, _selectionUnitBuffer)", "Selection commands must fill reusable unit storage.", result);
        RequireText(commands, "_selectionUnitIdBuffer.Add(unitId)", "Selection commands must reuse requested-id storage.", result);
        ForbidText(commands, "unitIds.ToHashSet()", "Selection commands must not allocate requested-id sets.", result);
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
        RequireText(battlefield, "List<ProductionCompletionCandidate> _productionCompletionCandidates", "Production sync must reuse completion candidate storage.", result);
        RequireText(battlefield, "List<EntityInstance> _productionNewUnitEntities", "Production sync must reuse new unit entity storage.", result);

        var production = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.ProductionSync.cs");
        RequireText(production, "CollectActiveProducerIds(_productionActiveProducerIds)", "Production sync must fill the reusable active producer buffer.", result);
        RequireText(production, "CollectKnownProductionEntityIds(_productionKnownEntityIds)", "Production sync must fill the reusable known entity set.", result);
        RequireText(production, "CollectProductionCompletionCandidates(_productionActiveProducerIds, _productionCompletionCandidates)", "Production sync must fill completion candidates explicitly.", result);
        RequireText(production, "CollectNewProductionUnitEntities(_productionKnownEntityIds, _productionNewUnitEntities)", "Production sync must fill new-unit storage explicitly.", result);
        ForbidText(production, ".ToList()", "Production sync must not allocate materialized LINQ lists.", result);
        ForbidText(production, ".ToHashSet()", "Production sync must not allocate known-entity sets.", result);
        ForbidText(production, "new\n            {", "Production sync must not use anonymous completion candidates.", result);
        ForbidText(production, ".OrderBy(entry => entry.Snapshot.Position", "Production completion matching must not allocate ordered LINQ snapshots.", result);
    }

    private static void RequireConstructionSubjectBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "List<int> _constructionSubjectBuildingIds", "Construction command routing must reuse subject building-id storage.", result);
        RequireText(battlefield, "List<EntityId> _constructionSubjectEntityBuffer", "Construction command routing must reuse subject entity-id storage.", result);

        var commandRouting = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CommandRouting.cs");
        RequireText(commandRouting, "CollectConstructionSubjectEntities(playerSlotId, spec, _constructionSubjectBuildingIds, _constructionSubjectEntityBuffer)", "Construction commands must fill reusable subject buffers.", result);
        RequireText(commandRouting, "buildingIds.Sort(CompareBuildingIds)", "Construction subject building ids must sort the reusable buffer in place.", result);
        ForbidText(commandRouting, ".Select(BuildingSnapshot)\n            .Where(snapshot => snapshot is not null)", "Construction subject routing must not allocate snapshot LINQ chains.", result);
        ForbidText(commandRouting, ".OrderBy(building => building.Id)\n            .Select(building =>", "Construction subject routing must not allocate ordered subject LINQ chains.", result);
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
        RequireText(battlefield, "units.Add(Spawn(designs[index].ToSpec(), playerSlotId, start + spacing * index, facing))", "SpawnRoster must preserve roster order through an indexed loop.", result);
        var core = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "query", "UnitBattlefield.CoreQueries.cs");
        var resourceQueries = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "resource", "UnitBattlefield.ResourceQueries.cs");
        ForbidText(battlefield, ".Select((design, index) => Spawn", "SpawnRoster must not allocate LINQ projection iterators.", result);
        ForbidText(core, ".OrderBy(unit => unit.EntityId.Value)", "UnitProjections must sort reusable storage in place.", result);
        ForbidText(core, ".ToList()", "Unit projection paths must not allocate result lists.", result);
        ForbidText(resourceQueries, ".OrderBy(", "Resource query paths must not allocate ordered LINQ queries.", result);
        ForbidText(resourceQueries, ".ToList()", "Resource query paths must not allocate result lists.", result);
        var visibility = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.VisibilityCombat.cs");
        ForbidText(visibility, ".GroupBy(unit =>", "SelectionSummary must not allocate grouping enumerables.", result);
        ForbidText(visibility, ".ToList()", "Unit minimap and selection summary paths must not allocate result lists.", result);
    }

}
