static class EntityStateHashAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var entityWorld = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityWorld.cs");
        RequireText(entityWorld, "StableValuesInto(_stateHashComponentValues)", "DeterministicStateHash must reuse a component ordering buffer.", result);
        RequireText(entityWorld, "List<AbilityCooldownState> _stateHashAbilityCooldownValues", "DeterministicStateHash must reuse ability cooldown ordering storage.", result);
        RequireText(entityWorld, "EntityStateHash.Add(hash, component, _stateHashAbilityCooldownValues)", "DeterministicStateHash must pass ability cooldown scratch storage into EntityStateHash.", result);
        ForbidText(entityWorld, "foreach (var component in entity.Components.StableValues)", "DeterministicStateHash must not allocate StableValues lists per entity.", result);

        var stateHash = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityStateHash.cs");
        RequireText(stateHash, "stackalloc byte[4]", "EntityStateHash string hashing must use stack UTF-8 storage.", result);
        RequireText(stateHash, "SortAbilityCooldownsByKind(ordered)", "EntityStateHash ability cooldown hashing must sort reusable storage in place.", result);
        ForbidText(stateHash, "Encoding.UTF8.GetBytes(value)", "EntityStateHash string hashing must not allocate byte arrays.", result);
        ForbidText(stateHash, "state.Cooldowns.OrderBy(cooldown => cooldown.Kind)", "EntityStateHash ability cooldown hashing must not allocate ordered LINQ queries.", result);
    }
}
