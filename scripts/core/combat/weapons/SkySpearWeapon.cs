namespace ProceduralRts.Core;

public sealed class SkySpearWeapon : WeaponDesign
{
    public override WeaponKind Kind => WeaponKind.SkySpear;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Kind,
            "Sky Spear Battery",
            AmmoKind.SeekerRocket,
            WeaponMountKind.StaticTurret,
            390,
            1.05f,
            0.78f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Air],
                armor: [ArmorTag.Aircraft],
                weights: new() { [UnitWeightClass.Light] = 1.25f, [UnitWeightClass.Medium] = 1.1f, [UnitWeightClass.Heavy] = 0.95f },
                domainPriority: new() { [MovementDomain.Air] = 1.45f },
                armorPriority: new() { [ArmorTag.Aircraft] = 1.6f }),
            SpecialAttackHook.ProjectileUpdate | SpecialAttackHook.Impact);
    }
}
