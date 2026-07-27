namespace ProceduralRts.Core;

public sealed class IonEmitterWeapon : WeaponDesign
{
    public override string Id => WeaponIds.IonEmitter;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Id,
            "Ion Emitter",
            AmmoIds.IonBeam,
            WeaponMountKind.StaticTurret,
            260,
            0.9f,
            0.72f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Land, MovementDomain.Naval, MovementDomain.Amphibious, MovementDomain.Air],
                armor: [ArmorTag.Infantry, ArmorTag.Vehicle, ArmorTag.Structure, ArmorTag.Ship, ArmorTag.Aircraft],
                weights: new() { [UnitWeightClass.Light] = 1.48f, [UnitWeightClass.Medium] = 0.88f, [UnitWeightClass.Heavy] = 0.72f },
                armorPriority: new() { [ArmorTag.Infantry] = 1.42f, [ArmorTag.Aircraft] = 1.0f, [ArmorTag.Vehicle] = 0.82f, [ArmorTag.Structure] = 0.7f }),
            SpecialAttackHook.Beam | SpecialAttackHook.Impact);
    }
}
