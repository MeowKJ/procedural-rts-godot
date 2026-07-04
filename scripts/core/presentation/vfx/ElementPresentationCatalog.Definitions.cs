namespace ProceduralRts.Core;

public static partial class ElementPresentationCatalog
{
    private static SortedDictionary<string, ElementPresentationStyle> CreateElementStyles()
    {
        return new SortedDictionary<string, ElementPresentationStyle>(StringComparer.Ordinal)
        {
            [DamageElementIds.Energy] = Style(
                DamageElementIds.Energy, "Energy", "ENE", "#8fffe1", "#52cfff", "#ffffff", "#b8fff2",
                ElementProjectileTrailStyle.Surge,
                Projectile(TailLength: 32f, TrailWidth: 6.4f, CoreWidth: 2.3f, HeadRadius: 5.2f, Flare: "#aafff0", FlareAlpha: 0.34f),
                ElementBeamStyle.Surging, "#8fffe1", BeamWidthMultiplier: 1.08f, Impact: "#8fffe1", Death: "#d8f7ff",
                EmitsEmpDissolve: true),
            [DamageElementIds.Entropy] = Style(
                DamageElementIds.Entropy, "Entropy", "ENT", "#b46a8f", "#6c3a72", "#f1d5e3", "#c77aa4",
                ElementProjectileTrailStyle.Decay,
                Projectile(TailLength: 29f, TrailWidth: 6.9f, CoreWidth: 2.1f, HeadRadius: 5f, Flare: "#d191b8", FlareAlpha: 0.3f),
                ElementBeamStyle.Decaying, "#c779aa", BeamWidthMultiplier: 0.98f, Impact: "#d191b8", Death: "#8f5278"),
            [DamageElementIds.Explosive] = Style(
                DamageElementIds.Explosive, "Explosive", "EXP", "#ffb35c", "#ff7f3f", "#fff0d2", "#ffd27a",
                ElementProjectileTrailStyle.Burst,
                Projectile(TailLength: 34f, TrailWidth: 7.6f, CoreWidth: 2.6f, HeadRadius: 5.8f, Flare: "#ffefad", FlareAlpha: 0.38f),
                ElementBeamStyle.Burning, "#ffb35c", BeamWidthMultiplier: 1.16f, Impact: "#ffb35c", Death: "#f6c55c",
                EmitsEmbers: true),
            [DamageElementIds.Kinetic] = Style(
                DamageElementIds.Kinetic, "Kinetic", "KIN", "#d7d2c4", "#b4afa4", "#ffffff", "#e8e1d1",
                ElementProjectileTrailStyle.Needle,
                Projectile(TailLength: 28f, TrailWidth: ProjectileVfxMath.MinimumTrailWidth, CoreWidth: ProjectileVfxMath.MinimumCoreWidth, HeadRadius: ProjectileVfxMath.MinimumHeadRadius, Flare: "#d8fff7", FlareAlpha: 0.22f),
                ElementBeamStyle.Focused, "#d7d2c4", BeamWidthMultiplier: 0.92f, Impact: "#ffffff", Death: "#d7d2c4"),
            [DamageElementIds.Moonshadow] = Style(
                DamageElementIds.Moonshadow, "Moonshadow", "MSH", "#9f9cff", "#625ad9", "#f4f2ff", "#c1bfff",
                ElementProjectileTrailStyle.Veil,
                Projectile(TailLength: 31f, TrailWidth: 5.8f, CoreWidth: 2.2f, HeadRadius: 4.9f, Flare: "#c1bfff", FlareAlpha: 0.32f),
                ElementBeamStyle.Veiled, "#9f9cff", BeamWidthMultiplier: 1f, Impact: "#b8b6ff", Death: "#7770d8"),
            [DamageElementIds.Resonance] = Style(
                DamageElementIds.Resonance, "Resonance", "RES", "#c9f26b", "#8fd35a", "#faffc8", "#ddff8c",
                ElementProjectileTrailStyle.Pulse,
                Projectile(TailLength: 30f, TrailWidth: 6.2f, CoreWidth: 2.4f, HeadRadius: 5.1f, Flare: "#e8ff9a", FlareAlpha: 0.34f),
                ElementBeamStyle.Harmonic, "#c9f26b", BeamWidthMultiplier: 1.04f, Impact: "#d8f76f", Death: "#8faf48"),
            [DamageElementIds.Thermal] = Style(
                DamageElementIds.Thermal, "Thermal", "THM", "#ff7763", "#ff493f", "#ffe1d8", "#ff9d87",
                ElementProjectileTrailStyle.Heat,
                Projectile(TailLength: 33f, TrailWidth: 7.1f, CoreWidth: 2.5f, HeadRadius: 5.5f, Flare: "#ffb5a4", FlareAlpha: 0.36f),
                ElementBeamStyle.Burning, "#ff7763", BeamWidthMultiplier: 1.12f, Impact: "#ff8a6c", Death: "#d95745",
                EmitsEmbers: true),
        };
    }
}
