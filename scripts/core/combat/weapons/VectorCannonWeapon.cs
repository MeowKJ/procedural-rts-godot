namespace ProceduralRts.Core;

public sealed class VectorCannonWeapon : WeaponDesign
{
    public override WeaponKind Kind => WeaponKind.VectorCannon;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Kind,
            "Vector Cannon",
            AmmoKind.BallisticCannon,
            WeaponMountKind.MobileTurret,
            310,
            1.05f,
            0.42f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Land, MovementDomain.Naval, MovementDomain.Amphibious],
                armor: [ArmorTag.Infantry, ArmorTag.Vehicle, ArmorTag.Structure, ArmorTag.Ship],
                weights: new() { [UnitWeightClass.Light] = 0.48f, [UnitWeightClass.Medium] = 1.12f, [UnitWeightClass.Heavy] = 1.24f },
                armorPriority: new() { [ArmorTag.Vehicle] = 1.2f, [ArmorTag.Structure] = 1.05f, [ArmorTag.Ship] = 1.12f, [ArmorTag.Infantry] = 0.42f }),
            SpecialAttackHook.Targeting | SpecialAttackHook.Impact);
    }
}
