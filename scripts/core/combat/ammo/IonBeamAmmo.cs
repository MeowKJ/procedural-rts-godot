using Godot;

namespace ProceduralRts.Core;

public sealed class IonBeamAmmo : AmmoDesign
{
    public override AmmoKind Kind => AmmoKind.IonBeam;

    public override AmmoDefinition ToDefinition()
    {
        return new AmmoDefinition(
            Kind,
            "Ion Beam",
            ProjectileBehavior.Beam,
            HitRule.Guaranteed,
            0,
            14,
            0.18f,
            4.9f,
            1,
            0,
            new Color("#d98cff"),
            CombatProfileDesign.DamageProfile(
                weights: new() { [UnitWeightClass.Light] = 1.78f, [UnitWeightClass.Medium] = 0.92f, [UnitWeightClass.Heavy] = 0.82f },
                armor: new() { [ArmorTag.Infantry] = 1.2f, [ArmorTag.Vehicle] = 0.92f, [ArmorTag.Structure] = 0.78f, [ArmorTag.Ship] = 0.88f, [ArmorTag.Aircraft] = 0.94f }),
            SpecialAttackHook.Beam | SpecialAttackHook.Impact,
            DamageElementId: DamageElementIds.Energy);
    }
}
