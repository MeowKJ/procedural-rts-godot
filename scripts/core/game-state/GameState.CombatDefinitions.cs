using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public WeaponDefinition Weapon(UnitModel unit)
    {
        return WeaponDefinitions[unit.RuntimeDescriptor.WeaponKind];
    }

    public WeaponDefinition? Weapon(BuildingModel building)
    {
        return BuildSpecCatalog.For(building.Kind).WeaponKind is { } weaponKind ? WeaponDefinitions[weaponKind] : null;
    }

    public AmmoDefinition Ammo(UnitModel unit)
    {
        return AmmoDefinitions[Weapon(unit).AmmoKind];
    }

    public static float EffectiveDamageAgainst(AmmoKind ammoKind, UnitSpecRuntimeDescriptor targetDescriptor)
    {
        var ammo = AmmoDefinitions[ammoKind];
        return DamageResolver.Resolve(
            ammo,
            targetDescriptor.WeightClass,
            targetDescriptor.MovementDomain,
            targetDescriptor.ArmorTag);
    }

    public static float EffectiveDamageAgainst(AmmoKind ammoKind, BuildSpec targetSpec)
    {
        var ammo = AmmoDefinitions[ammoKind];
        return DamageResolver.Resolve(
            ammo,
            UnitWeightClass.Heavy,
            MovementDomain.Land,
            targetSpec.ArmorTag);
    }

    public static bool WeaponCanTarget(WeaponDefinition weapon, UnitSpecRuntimeDescriptor targetDescriptor)
    {
        return weapon.TargetProfile.CanTarget(targetDescriptor);
    }

    public static bool WeaponCanTarget(WeaponDefinition weapon, BuildSpec targetSpec)
    {
        return weapon.TargetProfile.CanTarget(targetSpec);
    }

    public static float WeaponTargetPriority(WeaponDefinition weapon, UnitSpecRuntimeDescriptor targetDescriptor)
    {
        return weapon.TargetProfile.Priority(targetDescriptor);
    }

    public static float WeaponTargetPriority(WeaponDefinition weapon, BuildSpec targetSpec)
    {
        return weapon.TargetProfile.Priority(targetSpec);
    }
}
