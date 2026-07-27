using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void AddBeamIfNeeded(
        Vector2 start,
        Vector2 end,
        AmmoKind? ammoKind,
        UnitFactionId sourceFaction,
        PlayerSlotId sourcePlayerSlot)
    {
        if (ammoKind is not { } kind
            || !WeaponCatalog.Ammo.TryGetValue(kind, out var ammo)
            || ammo.Behavior != ProjectileBehavior.Beam)
        {
            return;
        }

        _combatEffects.AddBeam(
            start,
            end,
            ammo.BeamDuration,
            ammo.BeamWidth * ElementPresentationCatalog.BeamWidthMultiplierFor(ammo.DamageElementId),
            UnitFactionAccent(sourceFaction, sourcePlayerSlot).Lerp(
                ElementPresentationCatalog.BeamAccentFor(ammo.DamageElementId, ammo.Accent),
                0.44f));
    }

    private static AmmoKind? AmmoKindForPrimaryWeapon(UnitInstance attacker)
    {
        if (attacker.Spec.Weapons.Count == 0)
        {
            return null;
        }

        return WeaponCatalog.Weapons[attacker.Spec.PrimaryWeapon.WeaponKind].AmmoKind;
    }

    private static string? DamageElementIdForAmmoKind(AmmoKind? ammoKind)
    {
        return ElementPresentationCatalog.DamageElementIdFor(ammoKind);
    }

    private static float DamageForPrimaryWeapon(UnitInstance attacker, BuildSpec targetSpec)
    {
        if (attacker.Spec.Weapons.Count == 0)
        {
            return 0;
        }

        var weapon = WeaponCatalog.Weapons[attacker.Spec.PrimaryWeapon.WeaponKind];
        var ammo = WeaponCatalog.Ammo[weapon.AmmoKind];
        return DamageResolver.Resolve(
            ammo,
            UnitWeightClass.Heavy,
            MovementDomain.Land,
            targetSpec.ArmorTag,
            targetElementDefense: targetSpec.ElementDefense,
            targetTraits: targetSpec.TargetTraits);
    }
}
