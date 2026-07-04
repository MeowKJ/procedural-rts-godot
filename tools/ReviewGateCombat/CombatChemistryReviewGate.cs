static class CombatChemistryReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var ids = ReviewGateSource.Read(root, "scripts", "core", "combat", "DamageElementIds.cs");
        var catalog = ReviewGateSource.Read(root, "scripts", "core", "combat", "DamageElementCatalog.cs");
        var resolver = ReviewGateSource.Read(root, "scripts", "core", "combat", "DamageResolver.cs");
        var defenses = ReviewGateSource.Read(root, "scripts", "core", "combat", "ElementDefenseProfile.cs");
        var traits = ReviewGateSource.Read(root, "scripts", "core", "combat", "TargetTraitProfile.cs");
        var counters = ReviewGateSource.Read(root, "scripts", "core", "combat", "CounterRuleProfile.cs");
        RequireText(ids, "public static class DamageElementIds", "Damage elements must expose stable string ids.", result);
        RequireText(catalog, "public static class DamageElementCatalog", "Damage elements must be routed through a catalog.", result);
        RequireText(resolver, "public static class DamageResolver", "Damage calculation must route through DamageResolver.", result);
        RequireText(defenses, "public sealed record ElementDefenseProfile", "Target-side element defense must use a sparse profile.", result);
        RequireText(traits, "public enum TargetTrait", "Target traits must be named data, not unit ids.", result);
        RequireText(counters, "public sealed record CounterRule", "Counter rules must be data-driven.", result);
        RequireText(catalog, "DamageElementIds.Moonshadow", "DamageElementCatalog must include the moonshadow element.", result);
        RequireText(catalog, "DamageElementIds.Resonance", "DamageElementCatalog must include the resonance element.", result);
        RequireText(resolver, "targetElementDefense", "DamageResolver must accept target-side element defense.", result);
        RequireText(resolver, "CounterRules.MultiplierFor", "DamageResolver must apply counter rules through the resolver spine.", result);

        var ammo = ReviewGateSource.Read(root, "scripts", "core", "combat", "AmmoDefinition.cs");
        RequireText(ammo, "public string DamageElementId", "AmmoDefinition must carry a data-driven damage element id.", result);
        RequireText(ammo, "public CounterRuleProfile CounterRules", "AmmoDefinition must carry data-driven counter rules.", result);
        RequireText(ammo, "DamageElementCatalog.For(this.DamageElementId)", "AmmoDefinition must validate its damage element id through the catalog.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "units", "UnitSpec.cs"), "TargetTraitProfile? TargetTraits", "StatsSpec must carry target trait profiles.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponMath.cs"), "DamageResolver.Resolve(ammo", "Generic weapon damage must route through DamageResolver.", result);
        RequireText(ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "GameState.cs")), "DamageResolver.Resolve(", "Legacy GameState damage must route through DamageResolver.", result);
        RequireText(ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs")), "DamageResolver.Resolve(", "UnitBattlefield legacy damage must route through DamageResolver.", result);

        var systemsRoot = Path.Combine(root, "scripts", "core", "sim", "systems");
        foreach (var path in Directory.EnumerateFiles(systemsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            ForbidText(File.ReadAllText(path), "DamageElementIds.", $"{relative} must not branch on damage element ids directly; route damage policy through DamageResolver.", result);
            ForbidText(File.ReadAllText(path), "TargetTrait.", $"{relative} must not branch on target traits directly; route counter policy through DamageResolver.", result);
        }
    }
}
