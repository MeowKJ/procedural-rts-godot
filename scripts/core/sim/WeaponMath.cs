using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Shared weapon math, extracted from the combat systems (CombatSystem,
/// TurretCombatSystem, BuildingTargetCombatSystem, CommandSystem) which each had
/// their own copy of range/damage resolution (M9 - Elegance &amp; Decoupling).
///
/// The cores here are intentionally COMPOSABLE, not monolithic: callers had
/// genuinely different behavior (some apply a Deploy range multiplier, only the
/// mobile CombatSystem applies seeded damage jitter). Each caller keeps its exact
/// semantics by opting into the layers it needs, so this is a pure refactor:
/// SimReplay state hashes must remain byte-identical.
/// </summary>
public static class WeaponMath
{
    /// <summary>
    /// Largest mount range on the weapon (base, before any deploy bonus). Shared
    /// by every caller. Returns 0 if the weapon has no usable mounts.
    /// </summary>
    public static float MaxMountRange(EntityWorld world, WeaponUserComponentState weapon)
    {
        var range = 0f;
        foreach (var mount in weapon.Mounts)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var def) && def.Range > range)
            {
                range = def.Range;
            }
        }

        return range;
    }

    /// <summary>
    /// Single-pass max mount range plus whether any mount is on cooldown. Used by
    /// the "am I a firing anchor / should I hold" checks in MovementSystem and
    /// SeparationSystem, which need both facts and should not loop mounts twice on
    /// a hot per-entity-per-tick path.
    /// </summary>
    public static (float Range, bool AnyCooling) MaxRangeAndCooling(EntityWorld world, WeaponUserComponentState weapon)
    {
        var range = 0f;
        var cooling = false;
        foreach (var mount in weapon.Mounts)
        {
            cooling |= mount.CooldownRemaining > 0;
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var def) && def.Range > range)
            {
                range = def.Range;
            }
        }

        return (range, cooling);
    }

    public static float MaxMountMinRange(EntityWorld world, WeaponUserComponentState weapon)
    {
        var minRange = 0f;
        foreach (var mount in weapon.Mounts)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var def) && def.MinRange > minRange)
            {
                minRange = def.MinRange;
            }
        }

        return minRange;
    }

    public static float EffectiveTargetDistance(float centerDistance, float targetRadius)
    {
        return MathF.Max(0, centerDistance - MathF.Max(0, targetRadius));
    }

    public static bool InsideMinRange(WeaponDefinition weapon, float centerDistance, float targetRadius)
    {
        return weapon.MinRange > 0
            && EffectiveTargetDistance(centerDistance, targetRadius) < weapon.MinRange;
    }

    /// <summary>
    /// Base range scaled by a finished deploy's range multiplier, if present.
    /// Matches the behavior CombatSystem and BuildingTargetCombatSystem had;
    /// callers that must ignore deploy (CommandSystem, TurretCombatSystem) call
    /// <see cref="MaxMountRange"/> directly instead.
    /// </summary>
    public static float EffectiveRange(EntityWorld world, EntityInstance attacker, WeaponUserComponentState weapon)
    {
        var range = MaxMountRange(world, weapon);
        if (attacker.Components.TryGet<DeployComponentState>(out var deploy)
            && deploy.IsDeployed
            && deploy.SetupRemaining <= 0
            && deploy.RangeMultiplier > 0)
        {
            range *= deploy.RangeMultiplier;
        }

        return UpgradeResolver.WeaponRange(world, attacker, range);
    }

    /// <summary>
    /// Base mount range scaled by upgrades, without deploy's positional range
    /// multiplier. Used by turrets and group attack-slot planning.
    /// </summary>
    public static float BaseRange(EntityWorld world, EntityInstance attacker, WeaponUserComponentState weapon)
    {
        return UpgradeResolver.WeaponRange(world, attacker, MaxMountRange(world, weapon));
    }

    public static float BaseRange(EntityWorld world, EntityInstance attacker)
    {
        return attacker.Components.TryGet<WeaponUserComponentState>(out var weapon)
            ? BaseRange(world, attacker, weapon)
            : 0f;
    }

    /// <summary>
    /// Resolves a target's (weight, armor, domain) from its spec, with the same
    /// defaults the combat systems used when the spec or a sub-spec is missing.
    /// </summary>
    public static (UnitWeightClass Weight, ArmorTag Armor, MovementDomain Domain) ResolveTargetProfile(
        EntityWorld world,
        EntityInstance target)
    {
        return ResolveTargetProfile(world, target, useStructureDefaults: false);
    }

    public static (UnitWeightClass Weight, ArmorTag Armor, MovementDomain Domain) ResolveTargetProfile(
        EntityWorld world,
        EntityInstance target,
        bool useStructureDefaults)
    {
        var weight = UnitWeightClass.Medium;
        var armor = ArmorTag.Vehicle;
        var domain = MovementDomain.Land;
        if (world.TryGetSpec(target.SpecId, out var spec))
        {
            var structure = useStructureDefaults && spec.Kind is EntityKind.Building or EntityKind.Turret;
            weight = spec.Stats?.WeightClass ?? (structure ? UnitWeightClass.Heavy : weight);
            armor = spec.Stats?.ArmorTag ?? (structure ? ArmorTag.Structure : armor);
            domain = spec.Movement?.Domain ?? domain;
        }

        return (weight, armor, domain);
    }

    public static bool CanTarget(
        EntityWorld world,
        WeaponDefinition weaponDef,
        EntityInstance target,
        bool useStructureDefaults = false)
    {
        var (_, armor, domain) = ResolveTargetProfile(world, target, useStructureDefaults);
        return weaponDef.TargetProfile.AllowedDomains.Contains(domain)
            && weaponDef.TargetProfile.AllowedArmorTags.Contains(armor);
    }

    public static float TargetPriority(
        EntityWorld world,
        WeaponDefinition weaponDef,
        EntityInstance target,
        bool useStructureDefaults = false)
    {
        var (weight, armor, domain) = ResolveTargetProfile(world, target, useStructureDefaults);
        var profile = weaponDef.TargetProfile;
        if (!profile.AllowedDomains.Contains(domain) || !profile.AllowedArmorTags.Contains(armor))
        {
            return 0;
        }

        return PriorityFor(profile.WeightPriority, weight)
            * PriorityFor(profile.DomainPriority, domain)
            * PriorityFor(profile.ArmorPriority, armor);
    }

    /// <summary>
    /// Deterministic base damage: ammo base damage times the weight/domain/armor
    /// multiplier. No RNG - matches TurretCombatSystem and BuildingTargetCombatSystem.
    /// Returns 0 if the weapon's ammo is unknown.
    /// </summary>
    public static float BaseDamage(EntityWorld world, OwnerId attackerOwner, WeaponDefinition weaponDef, EntityInstance target)
    {
        if (!world.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo))
        {
            return 0;
        }

        var (weight, armor, domain) = ResolveTargetProfile(world, target);
        var baseDamage = ammo.BaseDamage * ammo.DamageProfile.Multiplier(weight, domain, armor);
        return UpgradeResolver.Damage(world, attackerOwner, baseDamage);
    }

    public static float BaseDamage(EntityWorld world, EntityInstance attacker, WeaponDefinition weaponDef, EntityInstance target)
    {
        if (!world.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo))
        {
            return 0;
        }

        var (weight, armor, domain) = ResolveTargetProfile(world, target);
        var baseDamage = ammo.BaseDamage * ammo.DamageProfile.Multiplier(weight, domain, armor);
        return UpgradeResolver.Damage(world, attacker, baseDamage);
    }

    private static float PriorityFor<T>(IReadOnlyDictionary<T, float> values, T key)
        where T : notnull
    {
        return values.TryGetValue(key, out var value) ? value : 1f;
    }
}
