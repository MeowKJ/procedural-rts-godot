static class AbilitySystemAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var abilitySystem = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "sim", "systems", "AbilitySystem.cs"));
        RequireText(abilitySystem, "private static IList<AbilityCooldownState> WritableCooldowns(", "AbilitySystem must expose owned cooldown storage writes.", result);
        RequireText(abilitySystem, "cooldowns[index] = cooldown with { CooldownRemaining = next }", "AbilitySystem cooldown tick must update owned storage.", result); RequireText(abilitySystem, "expanded[^1] = new AbilityCooldownState(kind, seconds)", "AbilitySystem may allocate only when adding a missing cooldown slot.", result);
        ForbidText(abilitySystem, "List<AbilityCooldownState> _cooldownScratch", "AbilitySystem must not keep per-write scratch snapshots.", result); ForbidText(abilitySystem, "_cooldownScratch.ToArray()", "AbilitySystem must not allocate scratch snapshots.", result);
        ForbidText(abilitySystem, "runtime.Cooldowns.ToArray()", "AbilitySystem must not copy cooldowns before mutation.", result);
    }
}
