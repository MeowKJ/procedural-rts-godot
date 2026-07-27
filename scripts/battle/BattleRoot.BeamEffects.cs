using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void AddBeamIfNeeded(
        Vector2 start,
        Vector2 end,
        string? ammoId,
        UnitFactionId sourceFaction,
        PlayerSlotId sourcePlayerSlot)
    {
        if (ammoId is not { } kind
            || !WeaponCatalog.AmmoDefinitions.TryGetValue(kind, out var ammo)
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

    private static string? AmmoIdForPrimaryWeapon(UnitInstance attacker)
    {
        if (attacker.Spec.Weapons.Count == 0)
        {
            return null;
        }

        return WeaponCatalog.WeaponDefinitions[attacker.Spec.PrimaryWeapon.WeaponId].AmmoId;
    }

    private static string? DamageElementIdForAmmoId(string? ammoId)
    {
        return ElementPresentationCatalog.DamageElementIdFor(ammoId);
    }

    private static float DamageForPrimaryWeapon(UnitInstance attacker, BuildSpec targetSpec)
    {
        if (attacker.Spec.Weapons.Count == 0)
        {
            return 0;
        }

        var weapon = WeaponCatalog.WeaponDefinitions[attacker.Spec.PrimaryWeapon.WeaponId];
        var ammo = WeaponCatalog.AmmoDefinitions[weapon.AmmoId];
        return DamageResolver.Resolve(
            ammo,
            UnitWeightClass.Heavy,
            MovementDomain.Land,
            targetSpec.ArmorTag,
            targetElementDefense: targetSpec.ElementDefense,
            targetTraits: targetSpec.TargetTraits);
    }
}
