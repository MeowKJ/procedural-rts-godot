namespace ProceduralRts.Core;

public static class AmmoIds
{
    public const string NeedleDart = "ammo.needledart";
    public const string BallisticCannon = "ammo.ballisticcannon";
    public const string ElectromagneticLance = "ammo.electromagneticlance";
    public const string IonBeam = "ammo.ionbeam";
    public const string SeekerRocket = "ammo.seekerrocket";

    public static IReadOnlyList<string> All { get; } =
    [
        NeedleDart,
        BallisticCannon,
        ElectromagneticLance,
        IonBeam,
        SeekerRocket,
    ];
}
