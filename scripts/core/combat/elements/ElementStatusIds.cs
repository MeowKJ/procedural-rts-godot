namespace ProceduralRts.Core;

public static class ElementStatusIds
{
    public const string EnergyCharge = "element.status.energy_charge";
    public const string EntropyDecay = "element.status.entropy_decay";
    public const string KineticStress = "element.status.kinetic_stress";
    public const string MoonshadowMark = "element.status.moonshadow_mark";
    public const string ResonanceTone = "element.status.resonance_tone";
    public const string ThermalHeat = "element.status.thermal_heat";

    private static readonly string[] AllIds =
    [
        EnergyCharge,
        EntropyDecay,
        KineticStress,
        MoonshadowMark,
        ResonanceTone,
        ThermalHeat,
    ];

    public static IReadOnlyList<string> All => AllIds;
}
