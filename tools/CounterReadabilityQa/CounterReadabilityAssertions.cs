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

    private static bool Nearly(float actual, float expected)
    {
        return MathF.Abs(actual - expected) < 0.001f;
    }
}
