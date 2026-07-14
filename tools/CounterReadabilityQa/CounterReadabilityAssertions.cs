using Godot;
using ProceduralRts.Core;

internal static class CounterReadabilityAssertions
{
    public static void CheckDataRules(List<string> failures)
    {
        var catBasic = UnitDesignCatalog.Spec("cat.basic");
        var dogInfantry = UnitDesignCatalog.Spec("dog.infantry");
        var genericInfantry = UnitDesignCatalog.Spec("generic.infantry");
        var dogTank = UnitDesignCatalog.Spec("dog.guard_tank");
        var catTank = UnitDesignCatalog.Spec("cat.tank");
        var dogRocket = UnitDesignCatalog.Spec("dog.rocket");
        var catAircraft = UnitDesignCatalog.Spec("cat.scout_aircraft");
        var powerPlant = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var antiAirTurret = BuildSpecCatalog.For(BuildingDesignIds.AntiAirTurret);

        var vectorCannon = WeaponCatalog.Weapons[WeaponKind.VectorCannon];
        var rocketPod = WeaponCatalog.Weapons[WeaponKind.RocketPod];
        var skySpear = WeaponCatalog.Weapons[WeaponKind.SkySpear];
        var needleRifle = WeaponCatalog.Weapons[WeaponKind.NeedleRifle];

        Require(catBasic.Stats.WeightClass == UnitWeightClass.Light, "cat.basic must be UnitWeightClass.Light.", failures);
        Require(catBasic.Stats.ArmorTag == ArmorTag.Infantry, "cat.basic must use ArmorTag.Infantry.", failures);
        Require(catBasic.Movement.Domain == MovementDomain.Land, "cat.basic must move on MovementDomain.Land.", failures);
        Require(catBasic.Stats.Cost < genericInfantry.Stats.Cost, "cat.basic should be cheaper than generic infantry.", failures);
        Require(catBasic.Movement.Speed > genericInfantry.Movement.Speed, "cat.basic should be faster than generic infantry.", failures);
        Require(catBasic.Stats.Cost < dogTank.Stats.Cost, "light infantry should be much cheaper than a tank.", failures);
        Require(catBasic.Movement.Speed > dogTank.Movement.Speed, "light infantry should be faster than a tank.", failures);

        Require(dogInfantry.Stats.WeightClass == UnitWeightClass.Light, "dog.infantry must remain a light assault unit.", failures);
        Require(dogInfantry.Movement.Speed > genericInfantry.Movement.Speed, "dog.infantry should be faster than generic infantry.", failures);
        Require(dogInfantry.RoleTags.Contains(UnitRoleTag.Assault), "dog.infantry should carry the assault role tag.", failures);

        Require(dogTank.Stats.ArmorTag == ArmorTag.Vehicle, "dog.guard_tank must use ArmorTag.Vehicle.", failures);
        Require(dogTank.Movement.Domain == MovementDomain.Land, "dog.guard_tank must stay on MovementDomain.Land.", failures);
        Require(dogTank.Weapons.Any(mount => mount.WeaponKind == WeaponKind.VectorCannon), "dog.guard_tank must mount VectorCannon.", failures);
        Require(CanWeaponTargetUnit(vectorCannon, catTank), "VectorCannon should target vehicle armor.", failures);
        Require(CanWeaponTargetBuilding(vectorCannon, powerPlant), "VectorCannon should target structures.", failures);
        Require(!CanWeaponTargetUnit(vectorCannon, catAircraft), "VectorCannon must not target aircraft.", failures);
        Require(PriorityFor(vectorCannon.TargetProfile.ArmorPriority, ArmorTag.Vehicle) > PriorityFor(vectorCannon.TargetProfile.ArmorPriority, ArmorTag.Infantry),
            "VectorCannon target priority should read as anti-vehicle.", failures);
        Require(PriorityFor(vectorCannon.TargetProfile.ArmorPriority, ArmorTag.Structure) > PriorityFor(vectorCannon.TargetProfile.ArmorPriority, ArmorTag.Infantry),
            "VectorCannon target priority should read as structure pressure.", failures);

        Require(dogRocket.RoleTags.Contains(UnitRoleTag.AntiAir), "dog.rocket should advertise AntiAir.", failures);
        Require(dogRocket.Weapons.Any(mount => mount.WeaponKind == WeaponKind.RocketPod), "dog.rocket must mount RocketPod.", failures);
        Require(CanWeaponTargetUnit(rocketPod, catTank), "RocketPod should target vehicles.", failures);
        Require(CanWeaponTargetUnit(rocketPod, catAircraft), "RocketPod should target aircraft.", failures);
        Require(PriorityFor(rocketPod.TargetProfile.ArmorPriority, ArmorTag.Vehicle) > PriorityFor(rocketPod.TargetProfile.ArmorPriority, ArmorTag.Infantry),
            "RocketPod priority should explain anti-vehicle pressure.", failures);

        Require(catAircraft.Movement.Domain == MovementDomain.Air, "cat.scout_aircraft must use MovementDomain.Air.", failures);
        Require(catAircraft.Stats.ArmorTag == ArmorTag.Aircraft, "cat.scout_aircraft must use ArmorTag.Aircraft.", failures);
        Require(CanWeaponTargetUnit(needleRifle, dogTank), "aircraft NeedleRifle should target ground vehicles.", failures);

        Require(antiAirTurret.WeaponKind == WeaponKind.SkySpear, "AntiAirTurret must mount SkySpear.", failures);
        Require(antiAirTurret.ToEntitySpec().Kind == EntityKind.Turret, "armed fixed defenses must enter EntityWorld as EntityKind.Turret.", failures);
        Require(CanWeaponTargetUnit(skySpear, catAircraft), "SkySpear should target aircraft.", failures);
        Require(!CanWeaponTargetUnit(skySpear, dogTank), "SkySpear should not target ground tanks.", failures);
        Require(skySpear.TargetProfile.AllowedDomains.Count == 1 && skySpear.TargetProfile.AllowedDomains.Contains(MovementDomain.Air),
            "SkySpear WeaponTargetProfile should be air-only.", failures);
        Require(skySpear.TargetProfile.AllowedArmorTags.Count == 1 && skySpear.TargetProfile.AllowedArmorTags.Contains(ArmorTag.Aircraft),
            "SkySpear WeaponTargetProfile should be aircraft-only.", failures);

        Console.WriteLine("CHECK [data] WeaponTargetProfile / MovementDomain / ArmorTag / cost / speed rules are explicit.");
        CheckCombatChemistryProfiles(failures);
        CheckElementPresentationStyles(failures);
    }

    public static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }

    private static bool CanWeaponTargetUnit(WeaponDefinition weapon, UnitSpec target)
    {
        return weapon.TargetProfile.AllowedDomains.Contains(target.Movement.Domain)
            && weapon.TargetProfile.AllowedArmorTags.Contains(target.Stats.ArmorTag);
    }

    private static bool CanWeaponTargetBuilding(WeaponDefinition weapon, BuildSpec target)
    {
        return weapon.TargetProfile.AllowedDomains.Contains(MovementDomain.Land)
            && weapon.TargetProfile.AllowedArmorTags.Contains(target.ArmorTag);
    }

    private static float PriorityFor<T>(IReadOnlyDictionary<T, float> priorities, T key)
        where T : notnull
    {
        return priorities.TryGetValue(key, out var value) ? value : 1f;
    }

    private static void CheckCombatChemistryProfiles(List<string> failures)
    {
        var ammo = WeaponCatalog.Ammo[AmmoKind.NeedleDart];
        var shieldedVehicle = TargetTraitProfile.FromRoleTags(new HashSet<UnitRoleTag>
        {
            UnitRoleTag.Vehicle,
            UnitRoleTag.Shield,
        });
        var counterAmmo = ammo with
        {
            CounterRules = CombatProfileDesign.CounterRules(
                new CounterRule(1.35f, Trait: TargetTrait.Shielded),
                new CounterRule(1.1f, Role: UnitRoleTag.Vehicle)),
        };

        var neutral = DamageResolver.Resolve(ammo, UnitWeightClass.Medium, MovementDomain.Land, ArmorTag.Vehicle, targetTraits: shieldedVehicle);
        var countered = DamageResolver.Resolve(counterAmmo, UnitWeightClass.Medium, MovementDomain.Land, ArmorTag.Vehicle, targetTraits: shieldedVehicle);
        Require(Nearly(countered, neutral * 1.35f * 1.1f), "Trait and role counter rules must stack deterministically without design ids.", failures);

        var resistant = DamageResolver.Resolve(
            ammo,
            UnitWeightClass.Medium,
            MovementDomain.Land,
            ArmorTag.Vehicle,
            targetElementDefense: CombatProfileDesign.ElementDefense(new() { [ammo.DamageElementId] = 0.75f }));
        Require(Nearly(resistant, neutral * 0.75f), "ElementDefenseProfile must apply sparse element overrides through DamageResolver.", failures);
        Console.WriteLine("CHECK [combat chemistry] ElementDefenseProfile and TargetTrait counter rules resolve without unit design ids.");
    }

    private static void CheckElementPresentationStyles(List<string> failures)
    {
        Require(ElementPresentationCatalog.Definitions.Count == DamageElementIds.All.Count, "ElementPresentationCatalog must define every damage element.", failures);
        Require(DamageElementIds.All.SequenceEqual(ElementPresentationCatalog.Definitions.Keys), "ElementPresentationCatalog must enumerate damage elements in stable order.", failures);

        var shortCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in DamageElementIds.All)
        {
            var style = ElementPresentationCatalog.For(id);
            Require(style.DamageElementId == id, $"{id} presentation style id must match catalog key.", failures);
            Require(!string.IsNullOrWhiteSpace(style.Label), $"{id} presentation label must be present.", failures);
            Require(shortCodes.Add(style.ShortCode), $"{id} presentation short code must be unique.", failures);
            Require(style.Badge.DamageElementId == id, $"{id} UI badge must point back to its element id.", failures);
            Require(style.Badge.ShortCode == style.ShortCode, $"{id} UI badge must reuse presentation short code.", failures);
            Require(style.Projectile.TrailWidth >= ProjectileVfxMath.MinimumTrailWidth, $"{id} projectile trail must stay readable.", failures);
            Require(style.Projectile.CoreWidth >= ProjectileVfxMath.MinimumCoreWidth, $"{id} projectile core must stay readable.", failures);
            Require(style.Projectile.HeadRadius >= ProjectileVfxMath.MinimumHeadRadius, $"{id} projectile head must stay readable.", failures);
            Require(style.Projectile.TrailAlpha >= ProjectileVfxMath.MinimumTrailAlpha, $"{id} projectile trail alpha must stay readable.", failures);
            Require(style.BeamWidthMultiplier > 0, $"{id} beam width multiplier must be positive.", failures);
        }

        for (var outer = 0; outer < DamageElementIds.All.Count; outer++)
        {
            for (var inner = outer + 1; inner < DamageElementIds.All.Count; inner++)
            {
                var left = ElementPresentationCatalog.For(DamageElementIds.All[outer]);
                var right = ElementPresentationCatalog.For(DamageElementIds.All[inner]);
                Require(ColorDistance(left.Accent, right.Accent) >= 0.18f, $"{left.Label} and {right.Label} accents must be visually distinct.", failures);
            }
        }

        var kineticAmmo = WeaponCatalog.Ammo[AmmoKind.NeedleDart];
        var explosiveAmmo = WeaponCatalog.Ammo[AmmoKind.SeekerRocket];
        var energyAmmo = WeaponCatalog.Ammo[AmmoKind.IonBeam];
        var kineticProjectileStyle = ProjectileVfxMath.StyleFor(kineticAmmo);
        var kineticElementStyle = ElementPresentationCatalog.For(DamageElementIds.Kinetic).Projectile;
        var explosiveProjectileStyle = ProjectileVfxMath.StyleFor(explosiveAmmo);
        var explosiveElementStyle = ElementPresentationCatalog.For(DamageElementIds.Explosive).Projectile;
        Require(kineticProjectileStyle with { MinimumVisibleSeconds = kineticElementStyle.MinimumVisibleSeconds } == kineticElementStyle,
            "ProjectileVfxMath must prefer kinetic element visuals while preserving ammo flight readability.", failures);
        Require(explosiveProjectileStyle with { MinimumVisibleSeconds = explosiveElementStyle.MinimumVisibleSeconds } == explosiveElementStyle,
            "ProjectileVfxMath must prefer explosive element visuals while preserving ammo flight readability.", failures);
        Require(kineticProjectileStyle.MinimumVisibleSeconds >= ProjectileVfxMath.MinimumVisibleSeconds
            && explosiveProjectileStyle.MinimumVisibleSeconds >= ProjectileVfxMath.MinimumVisibleSeconds,
            "Non-beam projectile styles must keep a readable minimum flight time.", failures);
        Require(ImpactVfxMath.StyleFor(UnitWeightClass.Medium, MovementDomain.Air, energyAmmo, 34).EmitsEmpDissolve, "Energy impact style must expose EMP dissolve through element presentation.", failures);
        Require(DeathVfxMath.StyleFor(UnitWeightClass.Heavy, MovementDomain.Land, explosiveAmmo, 90).EmitsEmbers, "Explosive death style must expose embers through element presentation.", failures);
        Require(ElementPresentationCatalog.BadgeFor(DamageElementIds.Moonshadow).ShortCode == "MSH", "Moonshadow badge must be available as presentation data.", failures);
        Console.WriteLine("CHECK [combat chemistry] ElementPresentationCatalog resolves seven distinct VFX and UI badge styles.");
    }

    private static float ColorDistance(Color left, Color right)
    {
        var red = left.R - right.R;
        var green = left.G - right.G;
        var blue = left.B - right.B;
        return MathF.Sqrt(red * red + green * green + blue * blue);
    }

    private static bool Nearly(float actual, float expected)
    {
        return MathF.Abs(actual - expected) < 0.001f;
    }
}
