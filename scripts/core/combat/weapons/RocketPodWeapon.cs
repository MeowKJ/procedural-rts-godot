namespace ProceduralRts.Core;

public sealed class RocketPodWeapon : WeaponDesign
{
    public override WeaponKind Kind => WeaponKind.RocketPod;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Kind,
            "Seeker Rocket Pod",
            AmmoKind.SeekerRocket,
            WeaponMountKind.MobileTurret,
            360,
            1.35f,
            0.84f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Land, MovementDomain.Naval, MovementDomain.Amphibious, MovementDomain.Air],
                armor: [ArmorTag.Infantry, ArmorTag.Vehicle, ArmorTag.Structure, ArmorTag.Ship, ArmorTag.Aircraft],
                weights: new() { [UnitWeightClass.Light] = 0.45f, [UnitWeightClass.Medium] = 1.15f, [UnitWeightClass.Heavy] = 1.05f },
                domainPriority: new() { [MovementDomain.Air] = 0.78f },
                armorPriority: new() { [ArmorTag.Vehicle] = 1.22f, [ArmorTag.Structure] = 1.1f, [ArmorTag.Ship] = 1.14f, [ArmorTag.Aircraft] = 0.82f, [ArmorTag.Infantry] = 0.45f }),
            SpecialAttackHook.ProjectileUpdate | SpecialAttackHook.Impact);
    }
}
