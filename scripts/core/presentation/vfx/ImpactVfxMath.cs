using Godot;

namespace ProceduralRts.Core;

public static class ImpactVfxMath
{
    public static ImpactVfxStyle StyleFor(
        UnitWeightClass weightClass,
        MovementDomain movementDomain,
        AmmoDefinition ammo,
        float damage)
    {
        return StyleFor(weightClass, movementDomain, ammo.Id, damage, ammo.DamageElementId);
    }

    public static ImpactVfxStyle StyleFor(
        UnitWeightClass weightClass,
        MovementDomain movementDomain,
        string? ammoId,
        float damage)
    {
        return StyleFor(weightClass, movementDomain, ammoId, damage, ElementPresentationCatalog.DamageElementIdFor(ammoId));
    }

    public static ImpactVfxStyle StyleFor(
        UnitWeightClass weightClass,
        MovementDomain movementDomain,
        string? ammoId,
        float damage,
        string? damageElementId)
    {
        var weightScale = weightClass switch
        {
            UnitWeightClass.Light => 0.78f,
            UnitWeightClass.Heavy => 1.32f,
            _ => 1f,
        };
        var domainScale = movementDomain switch
        {
            MovementDomain.Air => 1.16f,
            MovementDomain.Naval => 1.1f,
            _ => 1f,
        };
        var ammoScale = ammoId switch
        {
            AmmoIds.SeekerRocket or AmmoIds.BallisticCannon => 1.28f,
            AmmoIds.ElectromagneticLance or AmmoIds.IonBeam => 1.12f,
            AmmoIds.NeedleDart => 0.82f,
            _ => 1f,
        };
        var damageScale = Mathf.Clamp(damage / 75f, 0, 0.7f);
        var sparkCount = ammoId switch
        {
            AmmoIds.NeedleDart => 3,
            AmmoIds.SeekerRocket or AmmoIds.BallisticCannon => 7,
            AmmoIds.ElectromagneticLance or AmmoIds.IonBeam => 6,
            _ => 5,
        };
        var shakeBase = ammoId switch
        {
            AmmoIds.SeekerRocket => 2.8f,
            AmmoIds.BallisticCannon => 2.4f,
            _ => 0,
        };
        var damageCanShake = shakeBase > 0 || ammoId is null;
        var shakeDamage = damageCanShake && ammoId != AmmoIds.NeedleDart ? Mathf.Clamp((damage - 55f) / 55f, 0, 1) * 2.2f : 0;
        var shakeWeight = weightClass == UnitWeightClass.Heavy && shakeBase > 0 ? 1.1f : 0;
        var shakeDomain = movementDomain == MovementDomain.Air ? 0.72f : 1f;
        var shakeAmplitude = Mathf.Clamp((shakeBase + shakeDamage + shakeWeight) * shakeDomain, 0, 6.5f);
        var shakeRadius = shakeAmplitude <= 0 ? 0 : Mathf.Clamp(420f + damage * 2.4f + shakeAmplitude * 42f, 420, 860);
        var secondary = ElementPresentationCatalog.TryFor(damageElementId, out var element)
            ? element.ImpactColor
            : ammoId switch
        {
            AmmoIds.ElectromagneticLance => new Color("#8fffe1", 0.92f),
            AmmoIds.IonBeam => new Color("#d8f7ff", 0.94f),
            AmmoIds.SeekerRocket => new Color("#ffb35c", 0.9f),
            AmmoIds.BallisticCannon => new Color("#f6c55c", 0.88f),
            AmmoIds.NeedleDart => new Color("#ffffff", 0.82f),
            _ => new Color("#d8f7ff", 0.86f),
        };

        return new ImpactVfxStyle(
            Expansion: Mathf.Clamp(18f * weightScale * domainScale * ammoScale + damage * 0.22f, 14, 58),
            LineWidth: Mathf.Clamp(1.4f + weightScale * 0.7f + damageScale * 1.4f, 1.7f, 4.8f),
            SparkScale: Mathf.Clamp(weightScale * domainScale * ammoScale, 0.7f, 1.9f),
            SparkCount: sparkCount + Mathf.RoundToInt(damageScale * 3),
            SecondaryColor: secondary,
            ShakeAmplitude: shakeAmplitude,
            ShakeRadius: shakeRadius,
            EmitsEmbers: element?.EmitsEmbers == true || ammoId is AmmoIds.BallisticCannon or AmmoIds.SeekerRocket || damage > 55,
            EmitsEmpDissolve: element?.EmitsEmpDissolve == true || ammoId is AmmoIds.ElectromagneticLance or AmmoIds.IonBeam);
    }
}
