using Godot;

namespace ProceduralRts.Core;

public enum ColorRole
{
    Body,
    Ink,
    Owner,
    Effect,
    Warning,
    Shadow
}

public enum EnvironmentResponse
{
    Normal,
    OwnerProtected,
    EffectReactive,
    WarningFixed
}

public sealed record EnvironmentToneRoleProfile(
    Color Tint,
    float TintStrength,
    float AlphaScale = 1);

public sealed record EnvironmentTone(
    string Id,
    EnvironmentToneRoleProfile Body,
    EnvironmentToneRoleProfile Ink,
    EnvironmentToneRoleProfile Shadow,
    EnvironmentToneRoleProfile Owner,
    EnvironmentToneRoleProfile Effect,
    EnvironmentToneRoleProfile Warning,
    float OwnerMinimumChannel = 0.34f)
{
    public static readonly EnvironmentTone Day = new(
        "day",
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.Paper, 1), 0),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.Ink, 1), 0),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.Ink, 1), 0),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.Paper, 1), 0),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.InnerLight, 1), 0),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.Danger, 1), 0));

    public static readonly EnvironmentTone FogMorning = new(
        "fog-morning",
        new EnvironmentToneRoleProfile(new Color("#bcc3b9"), 0.42f, 0.92f),
        new EnvironmentToneRoleProfile(new Color("#626966"), 0.16f, 0.94f),
        new EnvironmentToneRoleProfile(new Color("#4d5552"), 0.20f, 0.88f),
        new EnvironmentToneRoleProfile(new Color("#cfc8bb"), 0.08f, 0.96f),
        new EnvironmentToneRoleProfile(new Color("#d8d0c4"), 0.18f, 0.92f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.FogDanger, 1), 0.08f),
        0.38f);

    public static readonly EnvironmentTone Dusk = new(
        "dusk",
        new EnvironmentToneRoleProfile(new Color("#39403b"), 0.32f, 0.94f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.DuskLine, 1), 0.18f, 0.94f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.DuskPanelSubtle, 1), 0.28f, 0.90f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.DuskTextMuted, 1), 0.10f, 0.96f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.DuskCommand, 1), 0.24f, 0.96f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.DuskDanger, 1), 0.10f),
        0.40f);

    public static readonly EnvironmentTone Night = new(
        "night",
        new EnvironmentToneRoleProfile(new Color("#1b2633"), 0.40f, 0.96f),
        new EnvironmentToneRoleProfile(new Color("#d5c7ad"), 0.22f, 0.98f),
        new EnvironmentToneRoleProfile(new Color("#020a0e"), 0.48f, 0.92f),
        new EnvironmentToneRoleProfile(new Color("#d5c7ad"), 0.12f, 0.98f),
        new EnvironmentToneRoleProfile(new Color("#8fd8ca"), 0.22f, 0.98f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.DuskDanger, 1), 0.10f),
        0.42f);

    public static readonly EnvironmentTone Corruption = new(
        "corruption",
        new EnvironmentToneRoleProfile(new Color("#6f3b72"), 0.30f, 0.98f),
        new EnvironmentToneRoleProfile(new Color("#241924"), 0.18f, 0.98f),
        new EnvironmentToneRoleProfile(new Color("#1a0d1b"), 0.32f, 0.92f),
        new EnvironmentToneRoleProfile(new Color("#e4c0df"), 0.10f, 0.98f),
        new EnvironmentToneRoleProfile(new Color("#c15b6c"), 0.35f, 0.98f),
        new EnvironmentToneRoleProfile(new Color(SoftOldCityPalette.Danger, 1), 0.12f),
        0.42f);

    public Color Apply(Color color, ColorRole role, EnvironmentResponse response = EnvironmentResponse.Normal)
    {
        if (response == EnvironmentResponse.WarningFixed || role == ColorRole.Warning)
        {
            return color;
        }

        var profile = ProfileFor(role, response);
        var tuned = color.Lerp(profile.Tint, profile.TintStrength);

        tuned.A *= profile.AlphaScale;
        return response == EnvironmentResponse.OwnerProtected || role == ColorRole.Owner
            ? PreserveOwnerReadability(tuned)
            : tuned;
    }

    public Color Apply(Color color, EnvironmentResponse response)
    {
        return Apply(color, ColorRole.Body, response);
    }

    public static EnvironmentTone Lerp(EnvironmentTone from, EnvironmentTone to, float amount)
    {
        var t = Mathf.Clamp(amount, 0, 1);
        return new EnvironmentTone(
            $"{from.Id}->{to.Id}",
            Lerp(from.Body, to.Body, t),
            Lerp(from.Ink, to.Ink, t),
            Lerp(from.Shadow, to.Shadow, t),
            Lerp(from.Owner, to.Owner, t),
            Lerp(from.Effect, to.Effect, t),
            Lerp(from.Warning, to.Warning, t),
            Mathf.Lerp(from.OwnerMinimumChannel, to.OwnerMinimumChannel, t));
    }

    private EnvironmentToneRoleProfile ProfileFor(ColorRole role, EnvironmentResponse response)
    {
        return response switch
        {
            EnvironmentResponse.OwnerProtected => Owner,
            EnvironmentResponse.EffectReactive => Effect,
            _ => role switch
            {
                ColorRole.Body => Body,
                ColorRole.Ink => Ink,
                ColorRole.Owner => Owner,
                ColorRole.Effect => Effect,
                ColorRole.Shadow => Shadow,
                ColorRole.Warning => Warning,
                _ => Effect,
            },
        };
    }

    private static EnvironmentToneRoleProfile Lerp(EnvironmentToneRoleProfile from, EnvironmentToneRoleProfile to, float amount)
    {
        return new EnvironmentToneRoleProfile(
            from.Tint.Lerp(to.Tint, amount),
            Mathf.Lerp(from.TintStrength, to.TintStrength, amount),
            Mathf.Lerp(from.AlphaScale, to.AlphaScale, amount));
    }

    private Color PreserveOwnerReadability(Color color)
    {
        var max = Mathf.Max(color.R, Mathf.Max(color.G, color.B));
        if (max >= OwnerMinimumChannel)
        {
            return color;
        }

        var lift = OwnerMinimumChannel - max;
        return new Color(
            Mathf.Clamp(color.R + lift, 0, 1),
            Mathf.Clamp(color.G + lift, 0, 1),
            Mathf.Clamp(color.B + lift, 0, 1),
            color.A);
    }
}

public static class EnvironmentTonePalette
{
    public static EnvironmentTone For(WorldVisualThemeState? state)
    {
        if (state is null)
        {
            return EnvironmentTone.Day;
        }

        var current = For(state.Current);
        var target = For(state.Target);
        var baseTone = state.Current != state.Target && state.TransitionProgress >= 1
            ? target
            : state.IsTransitioning
                ? EnvironmentTone.Lerp(current, target, state.TransitionProgress)
                : current;

        return IsCorruptionDriver(state.Driver)
            ? EnvironmentTone.Lerp(baseTone, EnvironmentTone.Corruption, 0.55f)
            : baseTone;
    }

    public static EnvironmentTone For(WorldVisualTheme theme)
    {
        return theme switch
        {
            WorldVisualTheme.DayCommand => EnvironmentTone.Day,
            WorldVisualTheme.FogMorning => EnvironmentTone.FogMorning,
            WorldVisualTheme.DuskDefense => EnvironmentTone.Dusk,
            WorldVisualTheme.NightRadar => EnvironmentTone.Night,
            _ => EnvironmentTone.Day,
        };
    }

    public static EnvironmentTone For(ResourceAtmosphere atmosphere)
    {
        return atmosphere switch
        {
            ResourceAtmosphere.Day => EnvironmentTone.Day,
            ResourceAtmosphere.Fog => EnvironmentTone.FogMorning,
            ResourceAtmosphere.Night => EnvironmentTone.Night,
            ResourceAtmosphere.Corruption => EnvironmentTone.Corruption,
            _ => EnvironmentTone.Day,
        };
    }

    private static bool IsCorruptionDriver(string driver)
    {
        return driver.Contains("corruption", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record EntityRenderPalette(
    Color Body,
    Color Ink,
    Color Owner,
    Color Effect,
    Color Warning,
    Color Shadow)
{
    public static EntityRenderPalette SoftOldCity(Color ownerColor)
    {
        return SoftOldCity(ownerColor, null);
    }

    public static EntityRenderPalette SoftOldCity(Color ownerColor, Color? roleAccent)
    {
        var accentBody = roleAccent is { } accent
            ? new Color(SoftOldCityPalette.PaperSubtle.Lerp(accent, 0.16f), 0.86f)
            : new Color(SoftOldCityPalette.PaperSubtle, 0.86f);
        var accentEffect = roleAccent is { } effect
            ? new Color(effect, 0.58f)
            : new Color(SoftOldCityPalette.InnerLight, 0.58f);

        return new EntityRenderPalette(
            accentBody,
            new Color(SoftOldCityPalette.Ink, 0.92f),
            new Color(ownerColor, 0.86f),
            accentEffect,
            new Color(SoftOldCityPalette.Danger, 0.90f),
            new Color(SoftOldCityPalette.Ink, 0.24f));
    }

    public Color Resolve(
        ColorRole role,
        EnvironmentTone? tone = null,
        EnvironmentResponse response = EnvironmentResponse.Normal)
    {
        var color = role switch
        {
            ColorRole.Body => Body,
            ColorRole.Ink => Ink,
            ColorRole.Owner => Owner,
            ColorRole.Effect => Effect,
            ColorRole.Warning => Warning,
            ColorRole.Shadow => Shadow,
            _ => Effect,
        };

        return tone is null ? color : tone.Apply(color, role, response);
    }

}
