using Godot;

namespace ProceduralRts.Core;

public sealed class BallisticCannonAmmo : AmmoDesign
{
    public override AmmoKind Kind => AmmoKind.BallisticCannon;

    public override AmmoDefinition ToDefinition()
    {
        return new AmmoDefinition(
            Kind,
            "Ballistic Cannon",
            ProjectileBehavior.Ballistic,
            HitRule.BallisticDeviation,
            720,
            22,
            0,
            0,
            0.62f,
            0,
            new Color("#59f1ff"),
            CombatProfileDesign.DamageProfile(
                weights: new() { [UnitWeightClass.Light] = 1.05f, [UnitWeightClass.Medium] = 1, [UnitWeightClass.Heavy] = 1.15f },
                armor: new() { [ArmorTag.Infantry] = 0.68f, [ArmorTag.Vehicle] = 1.05f, [ArmorTag.Structure] = 1.05f, [ArmorTag.Ship] = 1.05f, [ArmorTag.Aircraft] = 0.36f }),
            SpecialAttackHook.Targeting | SpecialAttackHook.Impact,
            SplashRadius: 42f,
            SplashMinDamageRatio: 0.32f);
    }
}
