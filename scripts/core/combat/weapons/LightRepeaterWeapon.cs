namespace ProceduralRts.Core;

public sealed class LightRepeaterWeapon : WeaponDesign
{
    public override WeaponKind Kind => WeaponKind.LightRepeater;

    public override WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition(
            Kind,
            "Light Repeater",
            AmmoKind.NeedleDart,
            WeaponMountKind.MobileTurret,
            235,
            0.42f,
            0.9f,
            true,
            CombatProfileDesign.TargetProfile(
                domains: [MovementDomain.Land, MovementDomain.Amphibious, MovementDomain.Air],
                armor: [ArmorTag.Infantry, ArmorTag.Vehicle, ArmorTag.Aircraft, ArmorTag.Structure],
                weights: new() { [UnitWeightClass.Light] = 1.65f, [UnitWeightClass.Medium] = 0.58f, [UnitWeightClass.Heavy] = 0.24f },
                domainPriority: new() { [MovementDomain.Air] = 0.55f },
                armorPriority: new() { [ArmorTag.Infantry] = 1.55f, [ArmorTag.Aircraft] = 0.58f, [ArmorTag.Vehicle] = 0.32f, [ArmorTag.Structure] = 0.08f }),
            SpecialAttackHook.FireAuthorization | SpecialAttackHook.ProjectileUpdate,
            MinRange: 118);
    }
}
