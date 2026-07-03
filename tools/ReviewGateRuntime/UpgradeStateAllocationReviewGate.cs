static class UpgradeStateAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var state = ReviewGateSource.Read(root, "scripts", "core", "progression", "UpgradeState.cs");
        RequireText(state, "public CompletedUpgradeIds CompletedIds => new(_completed);", "UpgradeState completed-id readout must not allocate a snapshot.", result);
        RequireText(state, "public readonly struct CompletedUpgradeIds", "UpgradeState must expose completed ids through a lightweight enumerable wrapper.", result);
        RequireText(state, "SortedSet<string>.Enumerator GetEnumerator()", "UpgradeState completed-id enumeration must use the sorted set enumerator.", result);
        ForbidText(state, "_completed.ToArray()", "UpgradeState completed-id readout must not allocate arrays.", result);
        ForbidText(state, "IReadOnlyList<string> CompletedIds", "UpgradeState completed-id readout must not expose an allocating list snapshot.", result);

        var resolver = ReviewGateSource.Read(root, "scripts", "core", "progression", "UpgradeResolver.cs");
        RequireText(resolver, "foreach (var id in state.CompletedIds)", "UpgradeResolver must iterate completed ids through the reusable readout.", result);
        var world = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityWorld.cs");
        RequireText(world, "foreach (var id in upgrades.Value.CompletedIds)", "EntityWorld state hash must iterate completed ids through the reusable readout.", result);
    }
}
