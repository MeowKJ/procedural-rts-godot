static class CommandSystemAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireEntityCommandBufferDrainBuffers(root, result);
        RequireScalarOrderSubjectBuffer(root, result);
        RequireSelectionSubjectSet(root, result);
        RequireEconomyOrderSubjectBuffer(root, result);
        RequireUnitBattlefieldConstructionTicketBuffers(root, result);
        RequireUnitBattlefieldSelectionBuffers(root, result);
    }

    private static void RequireEntityCommandBufferDrainBuffers(string root, GateResult result)
    {
        var buffer = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityCommandBuffer.cs");
        RequireText(buffer, "List<SequencedCommandEnvelope> _snapshotBuffer", "EntityCommandBuffer must reuse its ordered snapshot buffer.", result);
        RequireText(buffer, "List<SequencedCommandEnvelope> _readyBuffer", "EntityCommandBuffer must reuse its ready command buffer.", result);
        RequireText(buffer, "HashSet<long> _readySequences", "EntityCommandBuffer must reuse its removal sequence set.", result);
        RequireText(buffer, "CopyOrderedCommandsInto(_snapshotBuffer)", "DrainUpToTick must sort through the reusable snapshot buffer.", result);
        RequireText(buffer, "_commands.RemoveAll(IsReadySequence)", "DrainUpToTick must remove ready commands through the reusable sequence set.", result);
        ForbidText(buffer, ".Where(item => item.Command.Tick <= tick)", "EntityCommandBuffer drain must not allocate a LINQ ready list.", result);
        ForbidText(buffer, ".Select(item => item.Sequence).ToHashSet()", "EntityCommandBuffer drain must not allocate a sequence HashSet per tick.", result);
        ForbidText(buffer, ".OrderBy(item => item.Command.Tick)", "EntityCommandBuffer must not allocate ordered LINQ snapshots.", result);
    }

    private static void RequireScalarOrderSubjectBuffer(string root, GateResult result)
    {
        var commandSystem = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "sim", "systems", "CommandSystem.cs"));
        RequireText(commandSystem, "List<EntityInstance> _scalarOrderMembers", "CommandSystem must reuse a scalar-order subject buffer.", result);

        var movementOrders = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "sim",
            "systems",
            "command",
            "CommandSystem.MovementOrders.cs");
        RequireText(movementOrders, "CollectOwnedSubjects(world, issuer, subjects, _scalarOrderMembers)", "Move/Stop commands must fill the reusable scalar subject buffer.", result);
        RequireText(movementOrders, "CollectOwnedSubjects(world, patrol.Issuer, patrol.Subjects, _scalarOrderMembers)", "Patrol commands must fill the reusable scalar subject buffer.", result);
        RequireText(movementOrders, "CollectOwnedSubjects(world, guard.Issuer, guard.Subjects, _scalarOrderMembers)", "Guard commands must fill the reusable scalar subject buffer.", result);
        RequireText(movementOrders, "CollectOwnedSubjects(world, command.Issuer, command.Subjects, _scalarOrderMembers)", "Stance commands must fill the reusable scalar subject buffer.", result);
        ForbidText(movementOrders, "OwnedSubjects(world, issuer, subjects)", "Move/Stop commands must not allocate the OwnedSubjects iterator.", result);
        ForbidText(movementOrders, "OwnedSubjects(world, patrol.Issuer, patrol.Subjects)", "Patrol commands must not allocate the OwnedSubjects iterator.", result);
        ForbidText(movementOrders, "OwnedSubjects(world, guard.Issuer, guard.Subjects)", "Guard commands must not allocate the OwnedSubjects iterator.", result);
        ForbidText(movementOrders, "OwnedSubjects(world, command.Issuer, command.Subjects)", "Stance commands must not allocate the OwnedSubjects iterator.", result);

        var combatOrders = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "sim",
            "systems",
            "command",
            "CommandSystem.CombatOrders.cs");
        RequireText(combatOrders, "CollectOwnedSubjects(world, attack.Issuer, attack.Subjects, _scalarOrderMembers)", "Attack commands must fill the reusable scalar subject buffer.", result);
        ForbidText(combatOrders, "OwnedSubjects(world, attack.Issuer, attack.Subjects)", "Attack commands must not allocate the OwnedSubjects iterator.", result);
    }

    private static void RequireSelectionSubjectSet(string root, GateResult result)
    {
        var commandSystem = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "sim", "systems", "CommandSystem.cs"));
        RequireText(commandSystem, "HashSet<int> _selectionSubjectIds", "CommandSystem must reuse a selection subject id set.", result);

        var selection = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "sim",
            "systems",
            "command",
            "CommandSystem.SubjectsSelection.cs");
        RequireText(selection, "_selectionSubjectIds.Clear();", "Selection commands must clear and reuse the selection subject id set.", result);
        RequireText(selection, "_selectionSubjectIds.Add(id.Value)", "Selection commands must fill the reusable subject id set.", result);
        RequireText(selection, "_selectionSubjectIds.Contains(entity.Id.Value)", "Selection commands must read membership from the reusable subject id set.", result);
        ForbidText(selection, ".ToHashSet()", "Selection commands must not allocate a subject HashSet per command.", result);
        ForbidText(selection, ".Select(id => id.Value)", "Selection commands must not allocate a LINQ projection before set construction.", result);
    }

    private static void RequireEconomyOrderSubjectBuffer(string root, GateResult result)
    {
        var commandSystem = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "sim", "systems", "CommandSystem.cs"));
        ForbidText(commandSystem, "IEnumerable<EntityInstance> OwnedSubjects", "CommandSystem must not keep the allocating OwnedSubjects iterator helper.", result);

        var economy = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "sim",
            "systems",
            "command",
            "CommandSystem.EconomyOrders.cs");
        RequireText(economy, "CollectOwnedSubjects(world, command.Issuer, command.Subjects, _scalarOrderMembers)", "Economy orders must fill the reusable scalar subject buffer.", result);
        RequireText(economy, "ApplyHarvestIntent(entity, resource)", "AutoHarvest must apply harvest intent directly without nested command allocation.", result);
        ForbidText(economy, "OwnedSubjects(world, command.Issuer, command.Subjects)", "Economy orders must not allocate the OwnedSubjects iterator.", result);
        ForbidText(economy, "new HarvestEntityCommand(", "AutoHarvest must not allocate a nested harvest command.", result);
        ForbidText(economy, "[entity.Id]", "AutoHarvest must not allocate a single-entity subject array.", result);
    }

    private static void RequireUnitBattlefieldConstructionTicketBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<int> _constructionEntityIdsBefore", "UnitBattlefield construction commands must reuse the before-entity id set.", result);
        RequireText(battlefield, "List<UnitBattlefieldConstructionTicketSnapshot> _constructionTicketBuffer", "UnitBattlefield construction tickets must reuse ticket snapshot storage.", result);

        var tickets = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.ConstructionTickets.cs");
        RequireText(tickets, "CollectEntityIds(_constructionEntityIdsBefore)", "Construction queue/place paths must fill the reusable before-entity id set.", result);
        RequireText(tickets, "LastNewConstructionTicket(playerSlotId, kind, _constructionEntityIdsBefore)", "Queued tickets must be found through the reusable ticket buffer.", result);
        RequireText(tickets, "LastNewConstructedEntity(owner, ticket.Kind, _constructionEntityIdsBefore)", "Placed buildings must be found without LINQ chains.", result);
        RequireText(tickets, "CollectReadyConstructionTickets(playerSlotId, includeQueued: false, _constructionTicketBuffer)", "Ready ticket snapshots must use the reusable ticket buffer.", result);
        ForbidText(tickets, ".ToHashSet()", "Construction ticket bridge must not allocate before-entity HashSets.", result);
        ForbidText(tickets, ".Where(ticket", "Construction ticket bridge must not allocate LINQ ticket filters.", result);
        ForbidText(tickets, ".Where(entity", "Construction ticket bridge must not allocate LINQ entity filters.", result);
    }

    private static void RequireUnitBattlefieldSelectionBuffers(string root, GateResult result)
    {
        var battlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        RequireText(battlefield, "HashSet<EntityId> _selectionEntityBuffer", "UnitBattlefield selection commands must reuse the selection entity buffer.", result);

        var picking = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.SelectionPicking.cs");
        RequireText(picking, "PrepareUnitSelectionBuffer(playerSlotId, additive)", "Unit selection paths must prepare the reusable selection buffer.", result);
        RequireText(picking, "PrepareBuildingSelectionBuffer(playerSlotId, additive)", "Building selection paths must prepare the reusable selection buffer.", result);
        RequireText(picking, "SubmitSelectionBuffer(playerSlotId)", "Selection paths must submit and clear the reusable selection buffer.", result);
        RequireText(picking, "_selectionEntityBuffer.Clear();", "Selection buffer helpers must clear reusable storage.", result);
        ForbidText(picking, ".ToHashSet()", "UnitBattlefield selection picking must not allocate HashSets per selection command.", result);
        ForbidText(picking, "new HashSet<EntityId>()", "UnitBattlefield selection picking must reuse the selection entity buffer.", result);
    }
}
