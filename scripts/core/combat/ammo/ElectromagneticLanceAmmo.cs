using Godot;

namespace ProceduralRts.Core;

public sealed class ElectromagneticLanceAmmo : AmmoDesign
{
    public override string Id => AmmoIds.ElectromagneticLance;

    public override AmmoDefinition ToDefinition()
    {
        return new AmmoDefinition(
            Id,
            "Electromagnetic Lance",
            ProjectileBehavior.Beam,
            HitRule.Guaranteed,
            0,
            16,
            0.16f,
            5.8f,
            1,
            0,
            new Color("#b5f8ff"),
            CombatProfileDesign.DamageProfile(
                weights: new() { [UnitWeightClass.Light] = 0.38f, [UnitWeightClass.Medium] = 1.45f, [UnitWeightClass.Heavy] = 1.72f },
                armor: new() { [ArmorTag.Infantry] = 0.55f, [ArmorTag.Vehicle] = 1.18f, [ArmorTag.Structure] = 1.18f, [ArmorTag.Ship] = 1.12f, [ArmorTag.Aircraft] = 0.72f }),
            SpecialAttackHook.Beam | SpecialAttackHook.Impact,
            DamageElementId: DamageElementIds.Energy);
    }
}
