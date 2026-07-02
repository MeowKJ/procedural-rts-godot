static class CommandSystemAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireScalarOrderSubjectBuffer(root, result);
        RequireSelectionSubjectSet(root, result);
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
}
