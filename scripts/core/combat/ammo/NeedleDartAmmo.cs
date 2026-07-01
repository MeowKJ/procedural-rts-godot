using Godot;

namespace ProceduralRts.Core;

public sealed class NeedleDartAmmo : AmmoDesign
{
    public override AmmoKind Kind => AmmoKind.NeedleDart;

    public override AmmoDefinition ToDefinition()
    {
        return new AmmoDefinition(
            Kind,
            "Needle Dart",
            ProjectileBehavior.Direct,
            HitRule.Guaranteed,
            980,
            7,
            0,
            0,
            1,
            0,
            new Color("#8fffe1"),
            CombatProfileDesign.DamageProfile(
                weights: new() { [UnitWeightClass.Light] = 1.05f, [UnitWeightClass.Medium] = 0.85f, [UnitWeightClass.Heavy] = 0.68f },
                armor: new() { [ArmorTag.Infantry] = 1.18f, [ArmorTag.Vehicle] = 0.86f, [ArmorTag.Structure] = 0.55f, [ArmorTag.Ship] = 0.82f, [ArmorTag.Aircraft] = 0.9f }),
            SpecialAttackHook.ProjectileUpdate | SpecialAttackHook.Impact);
    }
}
