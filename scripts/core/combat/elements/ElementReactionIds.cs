namespace ProceduralRts.Core;

public static class ElementReactionIds
{
    public const string DecayBurst = "element.reaction.decay_burst";
    public const string EclipseDecay = "element.reaction.eclipse_decay";
    public const string Fracture = "element.reaction.fracture";
    public const string HarmonicOverload = "element.reaction.harmonic_overload";
    public const string Meltdown = "element.reaction.meltdown";
    public const string Moonbreak = "element.reaction.moonbreak";
    public const string Overload = "element.reaction.overload";

    private static readonly string[] AllIds =
    [
        DecayBurst,
        EclipseDecay,
        Fracture,
        HarmonicOverload,
        Meltdown,
        Moonbreak,
        Overload,
    ];

    public static IReadOnlyList<string> All => AllIds;
}
