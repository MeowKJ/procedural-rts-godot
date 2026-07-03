using Godot;

namespace ProceduralRts.Ui;

public enum UiFontRole
{
    Display,
    Title,
    Body,
    Compact,
    Numeric,
}

public static class UiFontProfile
{
    public const string ProfileName = "GodotSystemLatinCjkFallback";

    public static readonly string[] FallbackOrder =
    [
        "Noto Sans CJK SC",
        "Noto Sans SC",
        "Source Han Sans SC",
        "PingFang SC",
        "Microsoft YaHei UI",
        "Inter",
        "Segoe UI",
        "SF Pro Display",
        "Noto Sans",
        "Arial Unicode MS",
        "sans-serif",
    ];

    public static readonly string EnglishCoverageSample = "NEXUS COMMAND SETTINGS QUEUE Victory 0123456789";
    public static readonly string ChineseCoverageSample = "枢纽指挥 设置 队列 胜利 资金 简体中文";

    private static readonly Lazy<Font> Display = new(() => MakeFont(weight: 700));
    private static readonly Lazy<Font> Title = new(() => MakeFont(weight: 650));
    private static readonly Lazy<Font> Body = new(() => MakeFont(weight: 500));
    private static readonly Lazy<Font> Compact = new(() => MakeFont(weight: 500));
    private static readonly Lazy<Font> Numeric = new(() => MakeFont(weight: 600));

    public static Font FontFor(UiFontRole role)
    {
        return role switch
        {
            UiFontRole.Display => Display.Value,
            UiFontRole.Title => Title.Value,
            UiFontRole.Compact => Compact.Value,
            UiFontRole.Numeric => Numeric.Value,
            _ => Body.Value,
        };
    }

    public static Font DrawFont(UiFontRole role)
    {
        return FontFor(role);
    }

    public static UiFontRole RoleForSize(int fontSize)
    {
        return fontSize >= 30
            ? UiFontRole.Display
            : fontSize >= 18 ? UiFontRole.Title : fontSize <= 11 ? UiFontRole.Compact : UiFontRole.Body;
    }

    public static LabelSettings MakeLabelSettings(
        UiFontRole role,
        int fontSize,
        Color fontColor,
        Color outlineColor,
        int outlineSize)
    {
        return new LabelSettings
        {
            Font = FontFor(role),
            FontSize = fontSize,
            FontColor = fontColor,
            OutlineColor = outlineColor,
            OutlineSize = outlineSize,
        };
    }

    public static void ApplyToLabelSettings(LabelSettings settings, UiFontRole role, int? fontSize = null)
    {
        settings.Font = FontFor(role);
        if (fontSize is { } size)
        {
            settings.FontSize = size;
        }
    }

    public static void ApplyToControl(Control control, UiFontRole role, int fontSize)
    {
        control.AddThemeFontOverride("font", FontFor(role));
        control.AddThemeFontSizeOverride("font_size", fontSize);
    }

    private static SystemFont MakeFont(int weight)
    {
        return new SystemFont
        {
            FontNames = FallbackOrder,
            FontWeight = weight,
            AllowSystemFallback = true,
            DisableEmbeddedBitmaps = true,
            MultichannelSignedDistanceField = true,
            MsdfPixelRange = 8,
            MsdfSize = 48,
        };
    }
}
