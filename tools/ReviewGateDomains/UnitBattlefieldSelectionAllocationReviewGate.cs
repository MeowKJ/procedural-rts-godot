static class UnitBattlefieldSelectionAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireUnitBattlefieldSelectionBuffers(root, result);
        RequireUnitBattlefieldCursorPickLoops(root, result);
    }
    private static void RequireUnitBattlefieldSelectionBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<EntityId> _selectionEntityBuffer", "UnitBattlefield selection commands must reuse the selection entity buffer.", result);
        RequireText(battlefield, "HashSet<EntityId> _selectionRectCandidateBuffer", "Rect selection preview and commit must share reusable candidate ids.", result);
        RequireText(battlefield, "List<UnitInstance> _selectionRectEconomyUnits", "Rect selection must reuse economy candidate storage.", result);
        RequireText(battlefield, "List<UnitInstance> _selectionRectCombatUnits", "Rect selection must reuse combat candidate storage.", result);
        RequireText(battlefield, "List<EntityId> _selectionCommandEntityBuffer", "UnitBattlefield selection commands must reuse sorted command subject storage.", result);
        RequireText(battlefield, "List<UnitInstance> _selectionUnitBuffer", "UnitBattlefield selection commands must reuse selected-unit storage.", result);
        RequireText(battlefield, "PrepareUnitSelectionBuffer(playerSlotId, additive)", "Unit selection paths must prepare the reusable selection buffer.", result);
        RequireText(battlefield, "PrepareBuildingSelectionBuffer(playerSlotId, additive)", "Building selection paths must prepare the reusable selection buffer.", result);
        RequireText(battlefield, "SubmitSelectionBuffer(playerSlotId)", "Selection paths must submit and clear the reusable selection buffer.", result);
        RequireText(battlefield, "_selectionEntityBuffer.Clear();", "Selection buffer helpers must clear reusable storage.", result);
        ForbidText(battlefield, ".ToHashSet()", "UnitBattlefield selection picking must not allocate HashSets per selection command.", result);
        ForbidText(battlefield, "new HashSet<EntityId>()", "UnitBattlefield selection picking must reuse the selection entity buffer.", result);
        var commandRouting = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CommandRouting.cs");
        RequireText(commandRouting, "CollectSelectionCommandEntityIds(selectedEntityIds, _selectionCommandEntityBuffer)", "Selection commands must fill the reusable sorted subject buffer.", result);
        RequireText(commandRouting, "result.Sort(CompareEntityIds)", "Selection command subjects must sort the reusable buffer in place.", result);
        ForbidText(commandRouting, "selectedEntityIds\n                .Where(id => id.IsValid)", "Selection commands must not allocate LINQ-filtered subject lists.", result);
        ForbidText(commandRouting, ".Distinct()\n                .OrderBy(id => id.Value)", "Selection commands must not allocate distinct ordered LINQ subject lists.", result);
        var commands = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.Commands.cs");
        RequireText(commands, "CollectSelectionRectCandidates(playerSlotId, worldRect)", "Rect selection commit must consume the shared candidate collector.", result);
        RequireText(commands, "public int CountSelectionRectCandidates(PlayerSlotId playerSlotId, Rect2 worldRect)", "Rect selection preview must expose the shared candidate count.", result);
        RequireText(commands, "private IReadOnlyCollection<EntityId> CollectSelectionRectCandidates", "Rect selection eligibility must have one reusable candidate collector.", result);
        RequireText(commands, "ShouldIncludeEconomyInSelectionRect(\n                normalizedRect,", "Rect selection collector must apply economy intent to its reusable unit classes.", result);
        RequireText(commands, "PrepareUnitSelectionBuffer(playerSlotId, additive)", "Rect selection must reuse the existing selection entity buffer.", result);
        RequireText(commands, "CollectRequestedSelectionUnits(playerSlotId, unitIds, _selectionUnitBuffer)", "Id selection must fill the reusable selected-unit buffer.", result);
        RequireText(commands, "public int SelectArmy(PlayerSlotId playerSlotId)", "Select-all-army must stay on the UnitBattlefield selection command path.", result);
        RequireText(commands, "public UnitInstance? SelectNextIdleHarvester(PlayerSlotId playerSlotId)", "Idle-harvester cycle must stay on the UnitBattlefield selection command path.", result);
        RequireText(commands, "private static bool IsIdleHarvester(PlayerSlotId playerSlotId, UnitInstance unit)", "Idle-harvester cycle must use an explicit idle harvester predicate.", result);
        RequireText(commands, "return _selectionUnitBuffer;", "Id selection must return the reusable selected-unit buffer used by current callers.", result);
        ForbidText(commands, "var unitsInRect = Units", "Rect selection must not allocate a units-in-rect list.", result);
        ForbidText(commands, "var economyUnits = unitsInRect", "Rect selection must not allocate an economy-unit list.", result);
        ForbidText(commands, "var nonEconomyUnits = unitsInRect", "Rect selection must not allocate a combat-unit list.", result);
        ForbidText(commands, "SelectedUnits(playerSlotId).Select(unit => unit.EntityId).ToHashSet()", "Rect selection must not allocate additive selected-id sets.", result);
        ForbidText(commands, "private bool ShouldIncludeEconomyInSelectionRect(PlayerSlotId playerSlotId", "Rect selection must not restore the duplicate player-slot scan helper.", result);
        ForbidText(commands, "return SelectedUnits(playerSlotId).ToList();", "Id selection must not allocate a selected-unit return list.", result);
    }
    private static void RequireUnitBattlefieldCursorPickLoops(string root, GateResult result)
    {
        var picking = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.PickingQueries.cs");
        RequireText(picking, "NearestOwnedUnit", "UnitBattlefield cursor pick queries must use explicit nearest-unit scans.", result);
        RequireText(picking, "NearestBuildingTargetId", "UnitBattlefield cursor pick queries must use an explicit nearest-building scan.", result);
        RequireText(picking, "NearestResourceNode", "UnitBattlefield resource pick queries must use an explicit nearest-node scan.", result);
        ForbidText(picking, ".OrderBy(", "UnitBattlefield cursor pick helpers must not allocate ordered LINQ queries.", result);
        ForbidText(picking, ".Where(", "UnitBattlefield cursor pick helpers must not allocate LINQ filters.", result);
        ForbidText(picking, "Mathf.Pow", "UnitBattlefield cursor pick helpers must square radii directly.", result);
        var selectionPicking = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.SelectionPicking.cs");
        ForbidText(selectionPicking, ".OrderBy(unit => unit.Position.DistanceSquaredTo(worldPoint))", "Unit pick methods must not allocate sorting chains.", result);
        ForbidText(selectionPicking, ".Select(BuildingSnapshot)", "Building pick methods must not allocate snapshot projection chains.", result);
        ForbidText(selectionPicking, "SelectedUnits(playerSlotId).Count()", "Runtime selected-count queries must scan units explicitly.", result);
        var coreQueries = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CoreQueries.cs");
        ForbidText(coreQueries, ".OrderBy(field => field.Position.DistanceSquaredTo(worldPoint))", "Resource pick must not allocate sorting chains.", result);
    }
}
