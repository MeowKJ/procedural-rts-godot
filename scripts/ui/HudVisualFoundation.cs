using Godot;

namespace ProceduralRts.Ui;

public enum HudVisualPrimitive
{
    ModeStrip,
    CommandCard,
    QueueRow,
    StatusBadge,
}

public enum HudVisualState
{
    Normal,
    Hover,
    Pressed,
    Selected,
    Disabled,
    Warning,
}

public readonly record struct HudVisualStyle(Color Fill, Color Border, Color Text, Color Accent, int BorderWidth);

public static class HudVisualFoundation
{
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

        switch (state)
        {
            case HudVisualState.Hover:
                fill = new Color(fill.Lightened(0.08f), fill.A);
                border = new Color(accent, 0.68f);
                break;
            case HudVisualState.Pressed:
                fill = new Color(fill.Lightened(0.14f), fill.A);
                border = new Color(palette.Text, 0.48f);
                break;
            case HudVisualState.Selected:
                fill = new Color(accent, primitive == HudVisualPrimitive.StatusBadge ? 0.24f : 0.18f);
                border = new Color(accent, 0.86f);
                borderWidth = 2;
                break;
            case HudVisualState.Disabled:
                fill = new Color(palette.PanelSubtleFill, 0.68f);
                border = new Color(palette.TextDim, primitive == HudVisualPrimitive.StatusBadge ? 0.36f : 0.20f);
                text = new Color(palette.TextDim, 0.72f);
                break;
            case HudVisualState.Warning:
                fill = new Color(palette.Danger, primitive == HudVisualPrimitive.StatusBadge ? 0.18f : 0.14f);
                border = new Color(palette.Danger, 0.72f);
                accent = palette.Danger;
                break;
        }

        return new HudVisualStyle(fill, border, text, accent, borderWidth);
    }
}
