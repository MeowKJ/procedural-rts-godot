using Godot;

namespace ProceduralRts;

public partial class StyleTestRoot
{
    private static StyleSpec[] CreateStyleSpecs()
    {
        return new[]
        {
            new StyleSpec(
                "A",
                "Soft map + filled tokens",
                "Best for a light low-fatigue direction",
                new Color("#f4ecdf"),
                new Color("#cdbfae", 0.34f),
                new Color("#a89277", 0.36f),
                new Color("#28313a"),
                new Color("#c89235"),
                new Color("#7569b9"),
                new Color("#a13f64"),
                FilledTokens: true,
                Dark: false,
                CorruptionHeavy: false),
            new StyleSpec(
                "B",
                "Warm gray line board",
                "Safest if units stay mostly vector lines",
                new Color("#cfc9bf"),
                new Color("#7f817c", 0.26f),
                new Color("#4f5a5e", 0.33f),
                new Color("#172026"),
                new Color("#d9a441"),
                new Color("#715bc1"),
                new Color("#b7375e"),
                FilledTokens: false,
                Dark: false,
                CorruptionHeavy: false),
            new StyleSpec(
                "C",
                "Dusk defense phase",
                "Use for pressure, not as the whole game",
                new Color("#202b30"),
                new Color("#e3d7ba", 0.10f),
                new Color("#f4c76a", 0.17f),
                new Color("#ecedf0"),
                new Color("#ffcc65"),
                new Color("#9d8cff"),
                new Color("#ff5a7d"),
                FilledTokens: false,
                Dark: true,
                CorruptionHeavy: false),
            new StyleSpec(
                "D",
                "Old city map + AI rewrite",
                "Narrative battlefield: repaired lights vs corrupted routes",
                new Color("#e7dccb"),
                new Color("#9d8d78", 0.30f),
                new Color("#6f665e", 0.34f),
                new Color("#2b2b2a"),
                new Color("#d99a2e"),
                new Color("#6f61b8"),
                new Color("#be315d"),
                FilledTokens: true,
                Dark: false,
                CorruptionHeavy: true),
        };
    }

    private sealed record StyleSpec(
        string Tag,
        string Title,
        string Verdict,
        Color Background,
        Color Grid,
        Color Major,
        Color Ink,
        Color Dog,
        Color Cat,
        Color Ai,
        bool FilledTokens,
        bool Dark,
        bool CorruptionHeavy);
}
