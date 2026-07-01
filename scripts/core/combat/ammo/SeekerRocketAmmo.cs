using Godot;

namespace ProceduralRts.Core;

public sealed class SeekerRocketAmmo : AmmoDesign
{
    public override AmmoKind Kind => AmmoKind.SeekerRocket;

    public override AmmoDefinition ToDefinition()
    {
        return new AmmoDefinition(
            Kind,
            "Seeker Rocket",
            ProjectileBehavior.Tracking,
            HitRule.Guaranteed,
            460,
            18,
            0,
            0,
            1,
            6.4f,
            new Color("#ffb35c"),
            CombatProfileDesign.DamageProfile(
                weights: new() { [UnitWeightClass.Light] = 0.92f, [UnitWeightClass.Medium] = 1.05f, [UnitWeightClass.Heavy] = 1 },
                domain: new() { [MovementDomain.Air] = 1.18f, [MovementDomain.Naval] = 1.08f },
                armor: new() { [ArmorTag.Infantry] = 0.86f, [ArmorTag.Vehicle] = 1.35f, [ArmorTag.Structure] = 0.9f, [ArmorTag.Ship] = 1.08f, [ArmorTag.Aircraft] = 1.18f }),
            SpecialAttackHook.ProjectileUpdate | SpecialAttackHook.Impact);
    }
}
