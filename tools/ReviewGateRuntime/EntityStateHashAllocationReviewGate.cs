static class EntityStateHashAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var entityWorld = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityWorld.cs");
        RequireText(entityWorld, "StableValuesInto(_stateHashComponentValues)", "DeterministicStateHash must reuse a component ordering buffer.", result);
        RequireText(entityWorld, "List<AbilityCooldownState> _stateHashAbilityCooldownValues", "DeterministicStateHash must reuse ability cooldown ordering storage.", result);
        RequireText(entityWorld, "List<WeaponMountRuntimeState> _stateHashWeaponMountValues", "DeterministicStateHash must reuse weapon mount ordering storage.", result);
        RequireText(entityWorld, "List<UnitProductionQueueItem> _stateHashProductionQueueItems", "DeterministicStateHash must reuse production queue ordering storage.", result);
        RequireText(entityWorld, "List<EntityCommand> _stateHashCommandQueueItems", "DeterministicStateHash must reuse command queue ordering storage.", result);
        RequireText(entityWorld, "List<EntityId> _stateHashCommandSubjectIds", "DeterministicStateHash must reuse command subject ordering storage.", result);
        RequireText(entityWorld, "_stateHashWeaponMountValues", "DeterministicStateHash must pass weapon mount scratch storage into EntityStateHash.", result);
        RequireText(entityWorld, "_stateHashProductionQueueItems", "DeterministicStateHash must pass production queue scratch storage into EntityStateHash.", result);
        RequireText(entityWorld, "_stateHashCommandQueueItems", "DeterministicStateHash must pass command queue scratch storage into EntityStateHash.", result);
        RequireText(entityWorld, "_stateHashCommandSubjectIds", "DeterministicStateHash must pass command subject scratch storage into EntityStateHash.", result);
        ForbidText(entityWorld, "foreach (var component in entity.Components.StableValues)", "DeterministicStateHash must not allocate StableValues lists per entity.", result);

        ReviewGateSource.RequireFile(root, result, "scripts", "core", "entities", "EntityStateHash.Ordering.cs");
        var stateHash = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "entities", "EntityStateHash.cs"));
        RequireText(stateHash, "stackalloc byte[4]", "EntityStateHash string hashing must use stack UTF-8 storage.", result);
        RequireText(stateHash, "SortAbilityCooldownsByKind(ordered)", "EntityStateHash ability cooldown hashing must sort reusable storage in place.", result);
        RequireText(stateHash, "SortWeaponMountsByMountId(ordered)", "EntityStateHash weapon mount hashing must sort reusable storage in place.", result);
        RequireText(stateHash, "SortProductionQueueItemsById(ordered)", "EntityStateHash production queue hashing must sort reusable storage in place.", result);
        RequireText(stateHash, "SortCommandQueueItems(ordered)", "EntityStateHash command queue hashing must sort reusable storage in place.", result);
        RequireText(stateHash, "SortEntityIdsByValue(ordered)", "EntityStateHash command subject hashing must sort reusable storage in place.", result);
        ForbidText(stateHash, "Encoding.UTF8.GetBytes(value)", "EntityStateHash string hashing must not allocate byte arrays.", result);
        ForbidText(stateHash, "state.Cooldowns.OrderBy(cooldown => cooldown.Kind)", "EntityStateHash ability cooldown hashing must not allocate ordered LINQ queries.", result);
        ForbidText(stateHash, "state.Mounts.OrderBy(mount => mount.MountId, StringComparer.Ordinal)", "EntityStateHash weapon mount hashing must not allocate ordered LINQ queries.", result);
        ForbidText(stateHash, "state.Items.OrderBy(item => item.Id)", "EntityStateHash production queue hashing must not allocate ordered LINQ queries.", result);
        ForbidText(stateHash, "state.Items.OrderBy(item => item.Tick).ThenBy(item => item.Kind)", "EntityStateHash command queue hashing must not allocate ordered LINQ queries.", result);
        ForbidText(stateHash, "item.Subjects.OrderBy(subject => subject.Value)", "EntityStateHash command subject hashing must not allocate ordered LINQ queries.", result);
    }
}
