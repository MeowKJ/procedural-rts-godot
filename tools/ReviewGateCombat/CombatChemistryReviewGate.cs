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
        var statusDefinition = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementStatusDefinition.cs");
        var reactionDefinition = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementReactionDefinition.cs");
        var statusCatalog = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementStatusCatalog.cs");
        var reactionCatalog = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementReactionCatalog.cs");
        var reactionResolver = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementReactionResolver.cs");
        RequireText(ids, "public static class DamageElementIds", "Damage elements must expose stable string ids.", result);
        RequireText(catalog, "public static class DamageElementCatalog", "Damage elements must be routed through a catalog.", result);
        RequireText(resolver, "public static class DamageResolver", "Damage calculation must route through DamageResolver.", result);
        RequireText(defenses, "public sealed record ElementDefenseProfile", "Target-side element defense must use a sparse profile.", result);
        RequireText(traits, "public enum TargetTrait", "Target traits must be named data, not unit ids.", result);
        RequireText(counters, "public sealed record CounterRule", "Counter rules must be data-driven.", result);
        RequireText(statusDefinition, "public sealed record ElementStatusDefinition", "Element statuses must use data definitions.", result);
        RequireText(statusDefinition, "ElementStatusStackingMode", "Element statuses must author stacking or refresh behavior.", result);
        RequireText(statusDefinition, "ElementStatusVisibility", "Element statuses must author presentation visibility.", result);
        RequireText(reactionDefinition, "public sealed record ElementReactionDefinition", "Element reactions must use data definitions.", result);
        RequireText(reactionDefinition, "ElementReactionEffectPayload", "Element reactions must carry an effect payload.", result);
        RequireText(reactionDefinition, "ElementReactionPresentationStyle", "Element reactions must carry presentation style data.", result);
        RequireText(statusCatalog, "public static class ElementStatusCatalog", "Element statuses must be routed through a catalog.", result);
        RequireText(reactionCatalog, "public static class ElementReactionCatalog", "Element reactions must be routed through a catalog.", result);
        RequireText(reactionResolver, "public static class ElementReactionResolver", "Element reactions must resolve through a single resolver.", result);
        RequireText(reactionResolver, "ElementReactionCatalog.Match", "ElementReactionResolver must be the reaction calculation entry point over catalog data.", result);
        RequireText(catalog, "DamageElementIds.Moonshadow", "DamageElementCatalog must include the moonshadow element.", result);
        RequireText(catalog, "DamageElementIds.Resonance", "DamageElementCatalog must include the resonance element.", result);
        RequireText(reactionCatalog, "ElementReactionIds.Overload", "ElementReactionCatalog must include Energy + Explosive -> Overload.", result);
        RequireText(reactionCatalog, "ElementStatusIds.EnergyCharge", "ElementReactionCatalog must define Overload as status-driven data.", result);
        RequireText(reactionCatalog, "DamageElementIds.Explosive", "ElementReactionCatalog must define Overload's explosive trigger as data.", result);
        RequireText(resolver, "targetElementDefense", "DamageResolver must accept target-side element defense.", result);
        RequireText(resolver, "CounterRules.MultiplierFor", "DamageResolver must apply counter rules through the resolver spine.", result);

        var ammo = ReviewGateSource.Read(root, "scripts", "core", "combat", "AmmoDefinition.cs");
        RequireText(ammo, "public string DamageElementId", "AmmoDefinition must carry a data-driven damage element id.", result);
        RequireText(ammo, "public CounterRuleProfile CounterRules", "AmmoDefinition must carry data-driven counter rules.", result);
        RequireText(ammo, "DamageElementCatalog.For(this.DamageElementId)", "AmmoDefinition must validate its damage element id through the catalog.", result);
        RequireAmmoElementMappings(root, result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "units", "UnitSpec.cs"), "TargetTraitProfile? TargetTraits", "StatsSpec must carry target trait profiles.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponMath.cs"), "DamageResolver.Resolve(ammo", "Generic weapon damage must route through DamageResolver.", result);
        RequireText(ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "GameState.cs")), "DamageResolver.Resolve(", "Legacy GameState damage must route through DamageResolver.", result);
        RequireText(ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs")), "DamageResolver.Resolve(", "UnitBattlefield legacy damage must route through DamageResolver.", result);
        ReviewGateSource.RequireAnyText(root, result, "RunElementReactionScenario", "tools/SimReplay");
        ReviewGateSource.RequireAnyText(root, result, "ValidateElementReactionCatalog", "tools/ContentAuthoringQa");

        var systemsRoot = Path.Combine(root, "scripts", "core", "sim", "systems");
        foreach (var path in Directory.EnumerateFiles(systemsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            ForbidText(File.ReadAllText(path), "DamageElementIds.", $"{relative} must not branch on damage element ids directly; route damage policy through DamageResolver.", result);
            ForbidText(File.ReadAllText(path), "TargetTrait.", $"{relative} must not branch on target traits directly; route counter policy through DamageResolver.", result);
            ForbidText(File.ReadAllText(path), "ElementReactionCatalog", $"{relative} must not calculate element reactions directly; route through ElementReactionResolver.", result);
            ForbidText(File.ReadAllText(path), "ElementReactionDefinition", $"{relative} must not embed element reaction definitions.", result);
            ForbidText(File.ReadAllText(path), "ElementReactionIds.", $"{relative} must not branch on reaction ids directly; route through ElementReactionResolver.", result);
            ForbidText(File.ReadAllText(path), "ElementStatusCatalog", $"{relative} must not calculate element statuses directly; route through ElementReactionResolver.", result);
            ForbidText(File.ReadAllText(path), "ElementStatusDefinition", $"{relative} must not embed element status definitions.", result);
            ForbidText(File.ReadAllText(path), "ElementStatusIds.", $"{relative} must not branch on status ids directly; route through ElementReactionResolver.", result);
        }

        ReviewGateSource.ForbidTextInSources(root, result, "ElementReaction", "scripts/core/combat/ammo", "scripts/core/combat/weapons");
    }

    private static void RequireAmmoElementMappings(string root, GateResult result)
    {
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "NeedleDartAmmo.cs"), "DamageElementId: DamageElementIds.Kinetic", "NeedleDart must explicitly map to Kinetic.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "BallisticCannonAmmo.cs"), "DamageElementId: DamageElementIds.Explosive", "BallisticCannon must explicitly map to Explosive.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "SeekerRocketAmmo.cs"), "DamageElementId: DamageElementIds.Explosive", "SeekerRocket must explicitly map to Explosive.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "IonBeamAmmo.cs"), "DamageElementId: DamageElementIds.Energy", "IonBeam must explicitly map to Energy.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "ElectromagneticLanceAmmo.cs"), "DamageElementId: DamageElementIds.Energy", "ElectromagneticLance must explicitly map to Energy.", result);
    }
}
