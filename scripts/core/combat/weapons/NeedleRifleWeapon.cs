namespace ProceduralRts.Core;

public sealed class NeedleRifleWeapon : WeaponDesign
{
    public override string Id => WeaponIds.NeedleRifle;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Id,
            "Needle Rifle",
            AmmoIds.NeedleDart,
            WeaponMountKind.FixedForward,
            190,
            0.55f,
            0.62f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Land, MovementDomain.Amphibious, MovementDomain.Air],
                armor: [ArmorTag.Infantry, ArmorTag.Vehicle, ArmorTag.Aircraft, ArmorTag.Structure],
                weights: new() { [UnitWeightClass.Light] = 1.35f, [UnitWeightClass.Medium] = 0.72f, [UnitWeightClass.Heavy] = 0.38f },
                domainPriority: new() { [MovementDomain.Air] = 0.72f },
                armorPriority: new() { [ArmorTag.Infantry] = 1.35f, [ArmorTag.Aircraft] = 0.7f, [ArmorTag.Vehicle] = 0.42f, [ArmorTag.Structure] = 0.14f }),
            SpecialAttackHook.FireAuthorization);
    }
}
