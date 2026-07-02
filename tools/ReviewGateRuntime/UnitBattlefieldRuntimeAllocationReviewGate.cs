static class UnitBattlefieldRuntimeAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireConstructBuildingAdoptionBuffers(root, result);
        RequireOwnerRelationSyncBuffers(root, result);
        RequireResourceHarvestSyncBuffers(root, result);
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

        var sync = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.SyncRuntime.cs");
        RequireText(sync, "CollectResourceCreditsBefore(_resourceCreditsBefore)", "Harvester update must fill the reusable credits-before snapshot.", result);
        RequireText(sync, "SyncAllCreditsFromEntityWorld(_resourceCreditsBefore)", "Harvester update must reuse the credits-before snapshot for notifications.", result);
        ForbidText(sync, "ResourceInventories.ToDictionary", "Harvester update must not allocate credits-before dictionaries.", result);
        ForbidText(sync, "Units.Where(IsHarvester)", "Harvester sync must not allocate harvester filter enumerables.", result);
        ForbidText(sync, "BuildingTargetIds()\n            .Where(buildingId => BuildingIdentity(buildingId)?.Kind == BuildingDesignIds.Refinery)", "Dock sync must not allocate refinery filter enumerables.", result);

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
}
