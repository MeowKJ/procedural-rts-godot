using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public readonly record struct SoftOldCityHudPalette(
    Color PanelFill,
    Color PanelStrongFill,
    Color PanelSubtleFill,
    Color PanelBorder,
    Color PanelBorderStrong,
    Color Text,
    Color TextMuted,
    Color TextDim,
    Color DogCommand,
    Color CatRoute,
    Color Repair,
    Color Danger,
    Color Shadow,
    bool Dark);

public static class SoftOldCityTheme
{
    public static readonly SoftOldCityHudPalette Day = new(
        PanelFill: new Color(new Color("#111820"), 0.92f),
        PanelStrongFill: new Color(new Color("#1B2530"), 0.97f),
        PanelSubtleFill: new Color(new Color("#151E27"), 0.88f),
        PanelBorder: new Color(new Color("#C99A52"), 0.38f),
        PanelBorderStrong: new Color(new Color("#C99A52"), 0.76f),
        Text: new Color("#E9E1D1"),
        TextMuted: new Color("#94A0A8"),
        TextDim: new Color("#74808A"),
        DogCommand: new Color("#C99A52"),
        CatRoute: new Color("#62C9C4"),
        Repair: new Color("#62C9C4"),
        Danger: new Color("#D75B5B"),
        Shadow: new Color("#02070D", 0.72f),
        Dark: true);

    public static readonly SoftOldCityHudPalette Fog = Day with
    {
        PanelFill = new Color(new Color("#141C24"), 0.93f),
        PanelStrongFill = new Color(new Color("#202A34"), 0.97f),
        PanelSubtleFill = new Color(new Color("#18222B"), 0.89f),
        PanelBorder = new Color(new Color("#B99359"), 0.36f),
        PanelBorderStrong = new Color(new Color("#C5A066"), 0.68f),
        DogCommand = new Color("#C5A066"),
        CatRoute = new Color("#76C4C0"),
        Repair = new Color("#76C4C0"),
    };

    public static readonly SoftOldCityHudPalette Dusk = Day with
    {
        PanelFill = new Color(new Color("#16171D"), 0.94f),
        PanelStrongFill = new Color(new Color("#25232A"), 0.98f),
        PanelSubtleFill = new Color(new Color("#1B1B22"), 0.90f),
        PanelBorder = new Color(new Color("#C99A52"), 0.42f),
        PanelBorderStrong = new Color(new Color("#D0A05B"), 0.80f),
        DogCommand = new Color("#D0A05B"),
        CatRoute = new Color("#64C6C0"),
        Repair = new Color("#64C6C0"),
        Shadow = new Color("#02040A", 0.82f),
    };

    public static SoftOldCityHudPalette For(WorldVisualThemeState state)
    {
        var current = For(state.Current);
        if (!state.IsTransitioning)
        {
            return current;
        }

        return Lerp(current, For(state.Target), Mathf.Clamp(state.TransitionProgress, 0, 1));
    }

    public static SoftOldCityHudPalette For(WorldVisualTheme theme)
    {
        return theme switch
        {
            WorldVisualTheme.FogMorning => Fog,
            WorldVisualTheme.DuskDefense or WorldVisualTheme.NightRadar => Dusk,
            _ => Day,
        };
    }

    public static StyleBoxFlat Panel(Color fill, Color border, int borderWidth = 1)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            AntiAliasing = false,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0,
            ShadowColor = new Color("#02070D", 0.46f),
            ShadowSize = borderWidth > 1 ? 4 : 2,
            ShadowOffset = new Vector2(0, 2),
        };
    }

    public static Color AccentForAlert(AlertKind kind, SoftOldCityHudPalette palette)
    {
        return kind switch
        {
            AlertKind.Combat => palette.Danger,
            AlertKind.Production => palette.Repair,
            AlertKind.Economy => palette.DogCommand,
            AlertKind.Harvester => palette.Text,
            AlertKind.Building => palette.CatRoute,
            AlertKind.Power => palette.DogCommand,
            _ => palette.Text,
        };
    }

    private static SoftOldCityHudPalette Lerp(SoftOldCityHudPalette from, SoftOldCityHudPalette to, float amount)
    {
        return new SoftOldCityHudPalette(
            from.PanelFill.Lerp(to.PanelFill, amount),
            from.PanelStrongFill.Lerp(to.PanelStrongFill, amount),
            from.PanelSubtleFill.Lerp(to.PanelSubtleFill, amount),
            from.PanelBorder.Lerp(to.PanelBorder, amount),
            from.PanelBorderStrong.Lerp(to.PanelBorderStrong, amount),
            from.Text.Lerp(to.Text, amount),
            from.TextMuted.Lerp(to.TextMuted, amount),
            from.TextDim.Lerp(to.TextDim, amount),
            from.DogCommand.Lerp(to.DogCommand, amount),
            from.CatRoute.Lerp(to.CatRoute, amount),
            from.Repair.Lerp(to.Repair, amount),
            from.Danger.Lerp(to.Danger, amount),
            from.Shadow.Lerp(to.Shadow, amount),
            amount < 0.5f ? from.Dark : to.Dark);
    }
}
