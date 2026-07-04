using Godot;

namespace ProceduralRts.Core;

public static class DeathVfxMath
{
    public static DeathVfxStyle StyleFor(UnitWeightClass weightClass, MovementDomain movementDomain, AmmoKind? ammoKind, float overkillDamage)
    {
        var weightScale = weightClass switch
        {
            UnitWeightClass.Light => 0.72f,
            UnitWeightClass.Heavy => 1.35f,
            _ => 1f,
        };
        var domainScale = movementDomain switch
        {
            MovementDomain.Air => 1.18f,
            MovementDomain.Naval => 1.12f,
            _ => 1f,
        };
        var overkillScale = Mathf.Clamp(overkillDamage / 80f, 0, 0.75f);
        var fragmentCount = weightClass switch
        {
            UnitWeightClass.Light => 7,
            UnitWeightClass.Heavy => 18,
            _ => 12,
        };
        var smokeCount = weightClass switch
        {
            UnitWeightClass.Light => 3,
            UnitWeightClass.Heavy => 8,
            _ => 5,
        };

        var secondary = ammoKind switch
        {
            AmmoKind.ElectromagneticLance => new Color("#8fffe1", 0.92f),
            AmmoKind.IonBeam => new Color("#d8f7ff", 0.94f),
            AmmoKind.SeekerRocket => new Color("#ffb35c", 0.9f),
            AmmoKind.BallisticCannon => new Color("#f6c55c", 0.88f),
            AmmoKind.NeedleDart => new Color("#ffffff", 0.82f),
            _ => new Color("#d8f7ff", 0.86f),
        };

        return new DeathVfxStyle(
            Lifetime: 0.82f + weightScale * 0.22f + overkillScale * 0.18f,
            BurstScale: weightScale * domainScale + overkillScale,
            FragmentCount: fragmentCount + Mathf.RoundToInt(overkillScale * 8),
            SmokeCount: smokeCount + Mathf.RoundToInt(overkillScale * 5),
            SmokeScale: weightScale * (movementDomain == MovementDomain.Air ? 0.72f : 1f),
            ScorchScale: weightScale * (movementDomain == MovementDomain.Air ? 0.58f : 1f) + overkillScale * 0.46f,
            ScorchAlpha: Mathf.Clamp(0.16f + weightScale * 0.06f + overkillScale * 0.08f, 0.12f, 0.34f),
            RingWidth: 1.8f + weightScale * 1.2f + overkillScale * 1.4f,
            SecondaryColor: secondary,
            EmitsEmbers: ammoKind is AmmoKind.BallisticCannon or AmmoKind.SeekerRocket || overkillDamage > 25,
            EmitsEmpDissolve: ammoKind is AmmoKind.ElectromagneticLance or AmmoKind.IonBeam);
    }
}
