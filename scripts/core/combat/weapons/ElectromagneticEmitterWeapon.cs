namespace ProceduralRts.Core;

public sealed class ElectromagneticEmitterWeapon : WeaponDesign
{
    public override string Id => WeaponIds.ElectromagneticEmitter;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Id,
            "Electromagnetic Emitter",
            AmmoIds.ElectromagneticLance,
            WeaponMountKind.MobileTurret,
            125,
            1.6f,
            0.72f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Land, MovementDomain.Naval, MovementDomain.Amphibious, MovementDomain.Air],
                armor: [ArmorTag.Infantry, ArmorTag.Vehicle, ArmorTag.Structure, ArmorTag.Ship, ArmorTag.Aircraft],
                weights: new() { [UnitWeightClass.Light] = 0.34f, [UnitWeightClass.Medium] = 1.2f, [UnitWeightClass.Heavy] = 1.55f },
                armorPriority: new() { [ArmorTag.Vehicle] = 1.25f, [ArmorTag.Structure] = 1.08f, [ArmorTag.Ship] = 1.08f, [ArmorTag.Aircraft] = 0.62f, [ArmorTag.Infantry] = 0.34f }),
            SpecialAttackHook.Beam | SpecialAttackHook.Impact);
    }
}
