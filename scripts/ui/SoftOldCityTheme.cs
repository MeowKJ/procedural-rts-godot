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
        PanelFill: new Color(SoftOldCityPalette.Paper, 0.68f),
        PanelStrongFill: new Color(SoftOldCityPalette.PaperStrong, 0.82f),
        PanelSubtleFill: new Color(SoftOldCityPalette.PaperSubtle, 0.44f),
        PanelBorder: new Color(SoftOldCityPalette.Border, 0.24f),
        PanelBorderStrong: new Color(SoftOldCityPalette.WarmCommand, 0.46f),
        Text: new Color(SoftOldCityPalette.Text, 0.96f),
        TextMuted: new Color(SoftOldCityPalette.InkMuted, 0.82f),
        TextDim: new Color(SoftOldCityPalette.TextDim, 0.66f),
        DogCommand: SoftOldCityPalette.WarmCommand,
        CatRoute: SoftOldCityPalette.Route,
        Repair: SoftOldCityPalette.Repair,
        Danger: SoftOldCityPalette.HudDanger,
        Shadow: new Color(SoftOldCityPalette.Ink, 0.16f),
        Dark: false);

    public static readonly SoftOldCityHudPalette Fog = Day with
    {
        PanelFill = new Color(SoftOldCityPalette.FogPaper, 0.70f),
        PanelStrongFill = new Color(SoftOldCityPalette.FogPaperStrong, 0.84f),
        PanelSubtleFill = new Color(SoftOldCityPalette.FogPaperSubtle, 0.46f),
        PanelBorder = new Color(SoftOldCityPalette.FogBorder, 0.24f),
        PanelBorderStrong = new Color(SoftOldCityPalette.FogCommand, 0.42f),
        Text = new Color(SoftOldCityPalette.FogText, 0.96f),
        TextMuted = new Color(SoftOldCityPalette.FogMuted, 0.80f),
        TextDim = new Color(SoftOldCityPalette.FogDim, 0.64f),
        DogCommand = SoftOldCityPalette.FogCommand,
        CatRoute = SoftOldCityPalette.FogRoute,
        Danger = SoftOldCityPalette.FogDanger,
    };

    public static readonly SoftOldCityHudPalette Dusk = new(
        PanelFill: new Color(SoftOldCityPalette.DuskPanel, 0.72f),
        PanelStrongFill: new Color(SoftOldCityPalette.DuskPanelStrong, 0.88f),
        PanelSubtleFill: new Color(SoftOldCityPalette.DuskPanelSubtle, 0.56f),
        PanelBorder: new Color(SoftOldCityPalette.DuskLine, 0.20f),
        PanelBorderStrong: new Color(SoftOldCityPalette.DuskCommand, 0.58f),
        Text: new Color(SoftOldCityPalette.DuskText, 0.94f),
        TextMuted: new Color(SoftOldCityPalette.DuskTextMuted, 0.76f),
        TextDim: new Color(SoftOldCityPalette.DuskTextDim, 0.62f),
        DogCommand: SoftOldCityPalette.DuskCommand,
        CatRoute: SoftOldCityPalette.DuskRoute,
        Repair: SoftOldCityPalette.DuskRepair,
        Danger: SoftOldCityPalette.DuskDanger,
        Shadow: new Color(SoftOldCityPalette.NightBackground, 0.48f),
        Dark: true);

    public static readonly SoftOldCityHudPalette NightRadar = new(
        PanelFill: new Color(SoftOldCityPalette.NightBackground, 0.78f),
        PanelStrongFill: new Color(SoftOldCityPalette.NightGround, 0.92f),
        PanelSubtleFill: new Color(SoftOldCityPalette.NightWater, 0.42f),
        PanelBorder: new Color(SoftOldCityPalette.NightRadarSoft, 0.22f),
        PanelBorderStrong: new Color(SoftOldCityPalette.NightRadar, 0.64f),
        Text: new Color(SoftOldCityPalette.NightRadarSoft, 0.96f),
        TextMuted: new Color(SoftOldCityPalette.NightMuted, 0.80f),
        TextDim: new Color(SoftOldCityPalette.NightMuted, 0.58f),
        DogCommand: SoftOldCityPalette.NightRadar,
        CatRoute: SoftOldCityPalette.DuskRoute,
        Repair: SoftOldCityPalette.DuskRepair,
        Danger: SoftOldCityPalette.DuskDanger,
        Shadow: new Color(SoftOldCityPalette.NightBackground, 0.56f),
        Dark: true);

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
            WorldVisualTheme.DuskDefense => Dusk,
            WorldVisualTheme.NightRadar => NightRadar,
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
