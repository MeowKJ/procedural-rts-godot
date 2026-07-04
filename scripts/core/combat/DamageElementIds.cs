namespace ProceduralRts.Core;

public static class DamageElementIds
{
    public const string Kinetic = "kinetic";
    public const string Explosive = "explosive";
    public const string Thermal = "thermal";
    public const string Energy = "energy";
    public const string Moonshadow = "moonshadow";
    public const string Entropy = "entropy";
    public const string Resonance = "resonance";

    private static readonly string[] AllIds =
    [
        Energy,
        Entropy,
        Explosive,
        Kinetic,
        Moonshadow,
        Resonance,
        Thermal,
    ];

    public static IReadOnlyList<string> All => AllIds;
}
