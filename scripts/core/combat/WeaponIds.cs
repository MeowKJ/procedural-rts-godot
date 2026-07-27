namespace ProceduralRts.Core;

public static class WeaponIds
{
    public const string NeedleRifle = "weapon.needlerifle";
    public const string LightRepeater = "weapon.lightrepeater";
    public const string VectorCannon = "weapon.vectorcannon";
    public const string ElectromagneticEmitter = "weapon.electromagneticemitter";
    public const string IonEmitter = "weapon.ionemitter";
    public const string RocketPod = "weapon.rocketpod";
    public const string SkySpear = "weapon.skyspear";

    public static IReadOnlyList<string> All { get; } =
    [
        NeedleRifle,
        LightRepeater,
        VectorCannon,
        ElectromagneticEmitter,
        IonEmitter,
        RocketPod,
        SkySpear,
    ];
}
