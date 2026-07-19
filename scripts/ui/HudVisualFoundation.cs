using Godot;

namespace ProceduralRts.Ui;

public enum HudVisualPrimitive
{
    ModeStrip,
    CommandCard,
    QueueRow,
    StatusBadge,
}

[Flags]
public enum HudVisualState
{
    Normal = 0,
    Hover = 1 << 0,
    Focused = 1 << 1,
    Pressed = 1 << 2,
    Selected = 1 << 3,
    Disabled = 1 << 4,
    Warning = 1 << 5,
}

public enum HudStatusBadgeRole
{
    Neutral,
    Warning,
}

public readonly record struct HudVisualStyle(Color Fill, Color Border, Color Text, Color Accent, int BorderWidth);

public readonly record struct HudVisualMetrics(
    int CornerRadius,
    float ContentPadding,
    float ItemSpacing,
    UiFontRole FontRole,
    int FontSize,
    UiFontRole DetailFontRole,
    int DetailFontSize);

public static class HudVisualFoundation
{
    public static HudVisualMetrics MetricsFor(HudVisualPrimitive primitive)
    {
        return primitive switch
        {
            HudVisualPrimitive.ModeStrip => new(3, 4, 2, UiFontRole.Compact, 11, UiFontRole.Compact, 11),
            HudVisualPrimitive.CommandCard => new(3, 7, 3, UiFontRole.Body, 13, UiFontRole.Compact, 9),
            HudVisualPrimitive.QueueRow => new(3, 3, 2, UiFontRole.Compact, 11, UiFontRole.Compact, 11),
            HudVisualPrimitive.StatusBadge => new(2, 3, 2, UiFontRole.Compact, 8, UiFontRole.Compact, 8),
            _ => throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null),
        };
    }

    public static HudVisualState StateFor(HudStatusBadgeRole role)
    {
        return role switch
        {
            HudStatusBadgeRole.Neutral => HudVisualState.Normal,
            HudStatusBadgeRole.Warning => HudVisualState.Warning,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
    }

    public static HudVisualStyle For(
        SoftOldCityHudPalette palette,
        HudVisualPrimitive primitive,
        HudVisualState state,
        Color accent)
    {
        var fill = primitive switch
        {
            HudVisualPrimitive.CommandCard => palette.PanelFill,
            HudVisualPrimitive.StatusBadge => new Color(accent, 0.14f),
            _ => palette.PanelSubtleFill,
        };
        var border = new Color(accent, primitive == HudVisualPrimitive.StatusBadge ? 0.68f : 0.28f);
        var text = palette.Text;
        var borderWidth = 1;

        if (state.HasFlag(HudVisualState.Warning))
        {
            accent = palette.Danger;
            fill = new Color(accent, primitive == HudVisualPrimitive.StatusBadge ? 0.18f : 0.14f);
            border = new Color(accent, 0.72f);
        }

        if (state.HasFlag(HudVisualState.Selected))
        {
            fill = new Color(accent, primitive == HudVisualPrimitive.StatusBadge ? 0.24f : 0.18f);
            border = new Color(accent, 0.86f);
            borderWidth = 2;
        }

        if (state.HasFlag(HudVisualState.Hover))
        {
            fill = new Color(fill.Lightened(0.08f), fill.A);
            border = new Color(accent, MathF.Max(border.A, 0.68f));
        }

        if (state.HasFlag(HudVisualState.Pressed))
        {
            fill = new Color(fill.Lightened(0.14f), fill.A);
            border = new Color(palette.Text, MathF.Max(border.A, 0.48f));
        }

        if (state.HasFlag(HudVisualState.Focused))
        {
            if (!state.HasFlag(HudVisualState.Selected))
            {
                fill = new Color(accent, 0.12f);
            }

            border = new Color(accent, 0.95f);
            borderWidth = Math.Max(borderWidth, 2);
        }

        if (state.HasFlag(HudVisualState.Disabled))
        {
            fill = new Color(palette.PanelSubtleFill, 0.68f);
            border = new Color(palette.TextDim, primitive == HudVisualPrimitive.StatusBadge ? 0.36f : 0.20f);
            text = new Color(palette.TextDim, 0.72f);
            borderWidth = 1;
        }

        return new HudVisualStyle(fill, border, text, accent, borderWidth);
    }
}
