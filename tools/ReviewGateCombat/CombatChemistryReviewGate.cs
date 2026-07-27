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
        var upgradeDefinition = ReviewGateSource.Read(root, "scripts", "core", "progression", "UpgradeDefinition.cs");
        var upgradeCatalog = ReviewGateSource.Read(root, "scripts", "core", "progression", "UpgradeCatalog.cs");
        var upgradeResolver = ReviewGateSource.Read(root, "scripts", "core", "progression", "UpgradeResolver.cs");
        var weaponMath = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponMath.cs");
        var balanceReport = ReviewGateSource.Read(root, "tools", "BalanceReport", "Program.cs");
        var statusDefinition = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementStatusDefinition.cs");
        var reactionDefinition = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementReactionDefinition.cs");
        var statusCatalog = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementStatusCatalog.cs");
        var reactionCatalog = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementReactionCatalog.cs");
        var reactionResolver = ReviewGateSource.Read(root, "scripts", "core", "combat", "elements", "ElementReactionResolver.cs");
        var elementPresentation =
            ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "ElementPresentationCatalog.cs")
            + ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "ElementPresentationCatalog.Definitions.cs");
        var elementPresentationStyle = ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "ElementPresentationStyle.cs");
        var elementBadge = ReviewGateSource.Read(root, "scripts", "core", "presentation", "ui", "ElementBadgePresentation.cs");
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
        RequireText(elementPresentation, "ElementPresentationCatalog", "Element visuals must be routed through a presentation catalog.", result);
        RequireText(elementPresentationStyle, "public sealed record ElementPresentationStyle", "Element presentation must use data definitions.", result);
        RequireText(elementPresentationStyle, "ElementProjectileTrailStyle", "Element presentation must author projectile trail style.", result);
        RequireText(elementPresentationStyle, "ElementBeamStyle", "Element presentation must author beam style.", result);
        RequireText(elementBadge, "public readonly record struct ElementBadgePresentation", "Element UI badges must be presentation read models.", result);
        RequireText(catalog, "DamageElementIds.Moonshadow", "DamageElementCatalog must include the moonshadow element.", result);
        RequireText(catalog, "DamageElementIds.Resonance", "DamageElementCatalog must include the resonance element.", result);
        RequireText(reactionCatalog, "ElementReactionIds.Overload", "ElementReactionCatalog must include Energy + Explosive -> Overload.", result);
        RequireText(reactionCatalog, "ElementStatusIds.EnergyCharge", "ElementReactionCatalog must define Overload as status-driven data.", result);
        RequireText(reactionCatalog, "DamageElementIds.Explosive", "ElementReactionCatalog must define Overload's explosive trigger as data.", result);
        RequireText(elementPresentation, "DamageElementIds.Moonshadow", "ElementPresentationCatalog must include Moonshadow styling.", result);
        RequireText(elementPresentation, "DamageElementIds.Resonance", "ElementPresentationCatalog must include Resonance styling.", result);
        RequireText(elementPresentation, "BadgeFor(string damageElementId)", "ElementPresentationCatalog must expose UI badge data.", result);
        RequireText(resolver, "targetElementDefense", "DamageResolver must accept target-side element defense.", result);
        RequireText(resolver, "targetIncomingElementDamageMultiplier", "DamageResolver must accept resolver-composed incoming element modifiers.", result);
        RequireText(resolver, "CounterRules.MultiplierFor", "DamageResolver must apply counter rules through the resolver spine.", result);
        RequireText(upgradeDefinition, "OutgoingElementDamageMultipliers", "UpgradeModifier must expose element-specific outgoing damage data.", result);
        RequireText(upgradeDefinition, "IncomingElementDamageMultipliers", "UpgradeModifier must expose element-specific incoming damage data.", result);
        RequireText(upgradeDefinition, "VisualDeltaIds", "UpgradeModifier must expose visual delta ids for future presentation.", result);
        RequireText(upgradeDefinition, "OutgoingElementDamageMultiplierFor(string damageElementId)", "UpgradeModifier must resolve outgoing element modifiers by stable element id.", result);
        RequireText(upgradeDefinition, "IncomingElementDamageMultiplierFor(string damageElementId)", "UpgradeModifier must resolve incoming element modifiers by stable element id.", result);
        RequireText(upgradeCatalog, "UpgradeIds.EnergyCapacitors", "UpgradeCatalog must include one authored element-upgrade probe.", result);
        RequireText(upgradeCatalog, "DamageElementIds.Energy", "Element upgrade probe must target a real authored damage element.", result);
        RequireText(upgradeCatalog, "visual.delta.energy_capacitors", "Element upgrade probe must expose a visual delta id.", result);
        RequireText(upgradeResolver, "Damage(EntityWorld world, OwnerId owner, string damageElementId, float baseDamage)", "UpgradeResolver must compose owner-scoped outgoing element damage.", result);
        RequireText(upgradeResolver, "IncomingElementDamageMultiplier(EntityWorld world, OwnerId owner, string damageElementId)", "UpgradeResolver must compose owner-scoped incoming element damage.", result);
        RequireText(upgradeResolver, "VisualDeltaIds(EntityWorld world, OwnerId owner)", "UpgradeResolver must expose completed visual delta ids.", result);
        RequireText(weaponMath, "UpgradeResolver.Damage(world, attackerOwner, ammo.DamageElementId, 1f)", "Owner-based weapon damage must use element-aware upgrade composition.", result);
        RequireText(weaponMath, "UpgradeResolver.Damage(world, attacker, ammo.DamageElementId, 1f)", "Entity-based weapon damage must use element-aware upgrade composition.", result);
        RequireText(weaponMath, "UpgradeResolver.IncomingElementDamageMultiplier(world, target, ammo.DamageElementId)", "Weapon damage must compose target-side element upgrade modifiers.", result);
        RequireText(balanceReport, "ValidateElementUpgradeModifiers", "BalanceReport must pin element upgrade modifier behavior.", result);
        RequireText(balanceReport, "otherOwnerEnergy", "BalanceReport must pin owner-scoped element upgrade behavior.", result);
        RequireText(balanceReport, "visual.delta.energy_capacitors", "BalanceReport must pin visual delta id exposure.", result);

        var ammo = ReviewGateSource.Read(root, "scripts", "core", "combat", "AmmoDefinition.cs");
        RequireText(ammo, "public string DamageElementId", "AmmoDefinition must carry a data-driven damage element id.", result);
        RequireText(ammo, "public CounterRuleProfile CounterRules", "AmmoDefinition must carry data-driven counter rules.", result);
        RequireText(ammo, "DamageElementCatalog.For(this.DamageElementId)", "AmmoDefinition must validate its damage element id through the catalog.", result);
        RequireAmmoElementMappings(root, result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "units", "UnitSpec.cs"), "TargetTraitProfile? TargetTraits", "StatsSpec must carry target trait profiles.", result);
        RequireText(weaponMath, "DamageResolver.Resolve(", "Generic weapon damage must route through DamageResolver.", result);
        RequireText(ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs")), "DamageResolver.Resolve(", "UnitBattlefield damage must route through DamageResolver.", result);
        ReviewGateSource.RequireAnyText(root, result, "RunElementReactionScenario", "tools/SimReplay");
        ReviewGateSource.RequireAnyText(root, result, "ValidateElementReactionCatalog", "tools/ContentAuthoringQa");
        ReviewGateSource.RequireAnyText(root, result, "ValidateElementPresentationCatalog", "tools/ContentAuthoringQa");
        ReviewGateSource.RequireAnyText(root, result, "Combat chemistry coverage", "tools/BalanceReport");
        ReviewGateSource.RequireAnyText(root, result, "Counter rule probe", "tools/BalanceReport");
        ReviewGateSource.RequireAnyText(root, result, "Element defense probe", "tools/BalanceReport");
        ReviewGateSource.RequireAnyText(root, result, "CheckElementPresentationStyles", "tools/CounterReadabilityQa");
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "ProjectileVfxMath.cs"), "ElementPresentationCatalog.DamageElementIdFor", "Projectile VFX must prefer element style while preserving string fallback.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "ImpactVfxMath.cs"), "ElementPresentationCatalog.DamageElementIdFor", "Impact VFX must prefer element style while preserving string fallback.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "DeathVfxMath.cs"), "ElementPresentationCatalog.DamageElementIdFor", "Death VFX must prefer element style while preserving string fallback.", result);
        RequireText(ReviewGateSource.Read(root, "scripts", "core", "sim", "ProjectilePresentationProjection.cs"), "ProjectileVfxMath.StyleFor(ammo)", "ECS projectile projections must use ammo element presentation style.", result);
        ReviewGateSource.ForbidTextInSources(root, result, "DamageResolver", "scripts/core/presentation/ui", "scripts/core/presentation/vfx");
        ReviewGateSource.ForbidTextInSources(root, result, "ElementReactionResolver", "scripts/core/presentation/ui", "scripts/core/presentation/vfx");

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
