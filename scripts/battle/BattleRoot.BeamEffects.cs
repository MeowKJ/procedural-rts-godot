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
            || !GameState.AmmoDefinitions.TryGetValue(kind, out var ammo)
            || ammo.Behavior != ProjectileBehavior.Beam)
        {
            return;
        }

        _combatEffects.AddBeam(
            start,
            end,
            ammo.BeamDuration,
            ammo.BeamWidth,
            UnitFactionAccent(sourceFaction, sourcePlayerSlot).Lerp(ammo.Accent, 0.44f));
    }

    private static AmmoKind? AmmoKindForPrimaryWeapon(UnitInstance attacker)
    {
        if (attacker.Spec.Weapons.Count == 0)
        {
            return null;
        }

        return WeaponCatalog.Weapons[attacker.Spec.PrimaryWeapon.WeaponKind].AmmoKind;
    }
}
