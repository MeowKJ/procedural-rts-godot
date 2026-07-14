using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public static class UiFactory
{
    public static readonly Color Ink = new("#E9E1D1");

    public readonly record struct HudPanelColors(Color Fill, Color Border);

    public readonly record struct HudIconButtonDrawStyle(Color Fill, Color Border, float BorderWidth, Color Icon);

    public readonly record struct HudModeButtonDrawStyle(Color Fill, Color Border, float BorderWidth, Color Icon);

    public readonly record struct HudProductionTabDrawStyle(Color Fill, Color AccentFill, Color AccentBorder, float BorderWidth, Color Icon);

    public readonly record struct HudControlGroupSlotStyle(
        Color Fill,
        Color Border,
        Color Number,
        Color Count,
        Color Contents,
        Color FeedbackPulse);

    public readonly record struct HudCommandButtonOverlayStyle(
        Color ShortLabel,
        Color Progress,
        Color QueueBadge,
        Color QueueBadgeCutout,
        Color DisabledFill,
        Color DisabledStrike);

    public static Panel MakePanel(string name, Color fill, Color stroke)
    {
        var panel = new Panel { Name = name };
        panel.AddThemeStyleboxOverride("panel", PanelStyle(fill, stroke));
        return panel;
    }

    public static Label MakeLabel(string text, int fontSize, Color color, float outlineAlpha = 0.78f)
    {
        return new Label
        {
            Text = text,
            ClipText = true,
            LabelSettings = UiFontProfile.MakeLabelSettings(
                UiFontProfile.RoleForSize(fontSize),
                fontSize,
                color,
                new Color("#02060a", outlineAlpha),
                outlineSize: 1),
        };
    }

    public static Button MakeButton(string text, Color accent)
    {
        var button = new Button
        {
            Text = text,
            FocusMode = Control.FocusModeEnum.All,
            MouseDefaultCursorShape = BattleCursorGodotShapes.ToControlShape(BattleCursorCatalog.DefinitionFor(BattleCursorState.UiHover).Shape),
        };
        StyleButton(button, accent);
        return button;
    }

    public static void StyleButton(BaseButton button, Color accent)
    {
        UiFontProfile.ApplyToControl(button, UiFontRole.Body, 14);
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", new Color("#ffffff"));
        button.AddThemeColorOverride("font_pressed_color", new Color("#ffffff"));
        button.AddThemeStyleboxOverride("normal", ButtonStyle(new Color("#081725", 0.96f), new Color(accent, 0.58f)));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(new Color("#0d2434", 0.98f), new Color(accent, 0.96f)));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(new Color(accent, 0.30f), new Color("#ffffff", 0.94f)));
        button.AddThemeStyleboxOverride("focus", ButtonStyle(new Color("#0d2434", 0.76f), new Color("#ffffff", 0.80f), 2));
    }

    public static StyleBoxFlat ButtonStyle(Color fill, Color stroke, int border = 1)
    {
        var style = PanelStyle(fill, stroke, border);
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }

    public static StyleBoxFlat HudPanelStyle(Color fill, Color stroke, int border = 1)
    {
        return SoftOldCityTheme.Panel(fill, stroke, border);
    }

    public static HudPanelColors HudPanelColorsFor(SoftOldCityHudPalette palette, bool strong = false, bool subtle = false)
    {
        if (strong)
        {
            return new HudPanelColors(palette.PanelStrongFill, palette.PanelBorderStrong);
        }

        if (subtle)
        {
            return new HudPanelColors(palette.PanelSubtleFill, palette.PanelBorder);
        }

        return new HudPanelColors(palette.PanelFill, palette.PanelBorder);
    }

    public static HudPanelColors HudPanelColorsForName(string name, SoftOldCityHudPalette palette)
    {
        var strong = name is "ResourceStrip" or "MinimapCluster" or "ProductionPanel" or "UnitDetailPanel" or "OutcomeBanner";
        return HudPanelColorsFor(palette, strong);
    }

    public static Panel MakeHudPanel(string name, Color fill, Color stroke)
    {
        var panel = new Panel { Name = name };
        ApplyHudPanelTheme(panel, fill, stroke);
        return panel;
    }

    public static Panel MakeHudPanel(string name, SoftOldCityHudPalette palette, bool strong = false, bool subtle = false)
    {
        var colors = HudPanelColorsFor(palette, strong, subtle);
        return MakeHudPanel(name, colors.Fill, colors.Border);
    }

    public static void ApplyHudPanelTheme(Panel panel, Color fill, Color stroke, int border = 1)
    {
        panel.AddThemeStyleboxOverride("panel", HudPanelStyle(fill, stroke, border));
    }

    public static void ApplyNamedHudPanelTheme(Panel panel, SoftOldCityHudPalette palette)
    {
        var colors = HudPanelColorsForName(panel.Name.ToString(), palette);
        var strong = panel.Name.ToString() is "ResourceStrip" or "MinimapCluster" or "ProductionPanel" or "UnitDetailPanel" or "OutcomeBanner";
        ApplyHudPanelTheme(panel, colors.Fill, colors.Border, strong ? 2 : 1);
    }

    public static Label MakeHudLabel(
        string text,
        Vector2 position,
        int fontSize,
        Color color,
        SoftOldCityHudPalette palette)
    {
        var label = new Label
        {
            Text = text,
            Position = position,
            ClipText = true,
            LabelSettings = UiFontProfile.MakeLabelSettings(
                fontSize <= 11 ? UiFontRole.Compact : UiFontRole.Body,
                fontSize,
                color,
                new Color("#020403", 0.0f),
                outlineSize: 0),
        };
        ApplyHudLabelStyle(label, palette, color);
        return label;
    }

    public static Label MakeHudSizedLabel(
        string text,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color,
        SoftOldCityHudPalette palette)
    {
        var label = MakeHudLabel(text, position, fontSize, color, palette);
        label.CustomMinimumSize = size;
        label.Size = size;
        return label;
    }

    public static void ApplyHudLabelStyle(Label label, SoftOldCityHudPalette palette, Color color, float? shadowAlpha = null)
    {
        var settings = label.LabelSettings ?? new LabelSettings();
        UiFontProfile.ApplyToLabelSettings(settings, UiFontProfile.RoleForSize(settings.FontSize));
        settings.FontColor = color;
        settings.OutlineSize = palette.Dark ? 1 : 0;
        settings.OutlineColor = palette.Dark ? new Color("#020403", 0.86f) : new Color(palette.PanelStrongFill, 0.0f);
        label.LabelSettings = settings;
        ApplyHudLabelShadow(label, palette, shadowAlpha ?? HudLabelShadowAlpha(palette));
    }

    public static void ApplyHudLabelShadow(Label label, SoftOldCityHudPalette palette, float? shadowAlpha = null)
    {
        label.AddThemeColorOverride("font_shadow_color", shadowAlpha is { } alpha ? new Color(palette.Shadow, alpha) : palette.Shadow);
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 0);
    }

    public static float HudLabelShadowAlpha(SoftOldCityHudPalette palette)
    {
        return palette.Dark ? 0.32f : 0.12f;
    }

    public static void ApplyHudButtonTheme(
        Button button,
        SoftOldCityHudPalette palette,
        Color fill,
        Color border,
        Color accent,
        int fontSize)
    {
        button.MouseDefaultCursorShape = BattleCursorGodotShapes.ToControlShape(BattleCursorCatalog.DefinitionFor(BattleCursorState.UiHover).Shape);
        button.AddThemeStyleboxOverride("normal", HudPanelStyle(fill, border));
        button.AddThemeStyleboxOverride("hover", HudPanelStyle(new Color(fill.Lightened(0.08f), fill.A), new Color(accent, 0.68f)));
        button.AddThemeStyleboxOverride("pressed", HudPanelStyle(new Color(fill.Lightened(0.14f), fill.A), new Color(palette.Text, 0.48f)));
        button.AddThemeStyleboxOverride("disabled", HudPanelStyle(new Color(palette.PanelSubtleFill, 0.68f), new Color(palette.TextDim, 0.36f)));
        button.AddThemeStyleboxOverride("focus", HudPanelStyle(new Color("#000000", 0), new Color(accent, 0.82f)));
        UiFontProfile.ApplyToControl(button, fontSize <= 11 ? UiFontRole.Compact : UiFontRole.Body, fontSize);
        button.AddThemeColorOverride("font_color", palette.Text);
        button.AddThemeColorOverride("font_hover_color", palette.Text);
        button.AddThemeColorOverride("font_pressed_color", palette.Repair);
        button.AddThemeColorOverride("font_disabled_color", new Color(palette.TextDim, 0.72f));
    }

    public static void ApplyHudActionButtonTheme(Button button, SoftOldCityHudPalette palette, Color accent, int fontSize)
    {
        ApplyHudButtonTheme(button, palette, palette.PanelSubtleFill, new Color(accent, 0.24f), accent, fontSize);
    }

    public static void ApplyHudCancelButtonTheme(Button button, SoftOldCityHudPalette palette, int fontSize)
    {
        ApplyHudButtonTheme(button, palette, palette.PanelSubtleFill, new Color(palette.Danger, 0.34f), palette.Danger, fontSize);
    }

    public static void ApplyHudCommandButtonTheme(Button button, SoftOldCityHudPalette palette, int fontSize)
    {
        ApplyHudButtonTheme(button, palette, palette.PanelFill, new Color(palette.CatRoute, 0.34f), palette.Repair, fontSize);
    }

    public static void ApplyHudMoveModeButtonTheme(Button button, SoftOldCityHudPalette palette, MoveCommandMode mode, int fontSize)
    {
        ApplyHudButtonTheme(button, palette, palette.PanelSubtleFill, new Color(palette.CatRoute, 0.24f), HudMoveModeAccent(mode, palette), fontSize);
    }

    public static void ApplyHudStanceButtonTheme(Button button, SoftOldCityHudPalette palette, UnitStancePresentation presentation, int fontSize)
    {
        var accent = HudStanceAccent(presentation.AccentRole, palette);
        ApplyHudButtonTheme(button, palette, palette.PanelSubtleFill, new Color(accent, 0.22f), accent, fontSize);
    }

    public static Color HudActionAccent(IconGlyph glyph, SoftOldCityHudPalette palette)
    {
        return glyph switch
        {
            IconGlyph.Cancel => palette.Danger,
            IconGlyph.AttackMove or IconGlyph.Building => palette.DogCommand,
            IconGlyph.Settings or IconGlyph.Tank or IconGlyph.Naval => palette.CatRoute,
            _ => palette.Repair,
        };
    }

    public static Color HudMoveModeAccent(MoveCommandMode mode, SoftOldCityHudPalette palette)
    {
        return mode switch
        {
            MoveCommandMode.Attack => palette.DogCommand,
            MoveCommandMode.Ignore => palette.Danger,
            _ => palette.Repair,
        };
    }

    public static Color HudStanceAccent(UnitStanceAccentRole role, SoftOldCityHudPalette palette)
    {
        return role switch
        {
            UnitStanceAccentRole.CatRoute => palette.CatRoute,
            UnitStanceAccentRole.DogCommand => palette.DogCommand,
            UnitStanceAccentRole.Repair => palette.Repair,
            UnitStanceAccentRole.Text => palette.Text,
            UnitStanceAccentRole.Danger => palette.Danger,
            _ => palette.Repair,
        };
    }

    public static Color HudProductionTabAccent(IconGlyph glyph, SoftOldCityHudPalette palette)
    {
        return glyph switch
        {
            IconGlyph.Turret => palette.DogCommand,
            IconGlyph.Building or IconGlyph.Credits => palette.DogCommand,
            IconGlyph.Air or IconGlyph.Naval or IconGlyph.Tank => palette.CatRoute,
            IconGlyph.Infantry or IconGlyph.Harvester => palette.Repair,
            _ => palette.Repair,
        };
    }

    public static Color HudTabLabelColor(bool enabled, SoftOldCityHudPalette palette)
    {
        return enabled ? palette.Repair : palette.TextDim;
    }

    public static HudIconButtonDrawStyle GetHudIconButtonDrawStyle(Color accent, bool pressed, bool focused, bool disabled)
    {
        return new HudIconButtonDrawStyle(
            new Color(accent, pressed ? 0.18f : 0.04f),
            new Color(accent, focused ? 0.76f : 0.28f),
            focused ? 1.7f : 1.1f,
            new Color(accent, disabled ? 0.36f : 0.88f));
    }

    public static HudModeButtonDrawStyle GetHudModeButtonDrawStyle(Color accent, bool selected)
    {
        return new HudModeButtonDrawStyle(
            new Color(accent, selected ? 0.18f : 0.04f),
            new Color(accent, selected ? 0.86f : 0.28f),
            selected ? 1.8f : 1.1f,
            new Color(accent, selected ? 1f : 0.72f));
    }

    public static HudProductionTabDrawStyle GetHudProductionTabDrawStyle(
        IconGlyph glyph,
        bool active,
        bool selected,
        SoftOldCityHudPalette palette)
    {
        var accent = HudProductionTabAccent(glyph, palette);
        var alpha = active ? 1f : 0.44f;
        return new HudProductionTabDrawStyle(
            active ? palette.PanelFill : palette.PanelSubtleFill,
            new Color(accent, selected ? 0.18f : 0.05f),
            new Color(accent, selected ? 0.82f : 0.3f * alpha),
            selected ? 1.8f : 1.1f,
            new Color(accent, active ? 0.92f : 0.38f));
    }

    public static HudControlGroupSlotStyle GetHudControlGroupSlotStyle(ControlGroupSnapshot snapshot, SoftOldCityHudPalette palette)
    {
        var empty = snapshot.TotalCount == 0;
        var fill = snapshot.Active
            ? new Color(palette.CatRoute, 0.18f)
            : empty ? new Color(palette.PanelSubtleFill, 0.74f) : new Color(palette.PanelFill, 0.92f);
        var border = snapshot.Active
            ? new Color(palette.Text, 0.78f)
            : snapshot.FeedbackPulse > 0 ? new Color(palette.DogCommand, 0.54f + snapshot.FeedbackPulse * 0.34f) : new Color(palette.CatRoute, empty ? 0.16f : 0.36f);

        return new HudControlGroupSlotStyle(
            fill,
            border,
            snapshot.Active ? palette.Text : palette.Repair,
            empty ? palette.TextDim : palette.Text,
            empty ? new Color(palette.TextDim, 0.72f) : palette.DogCommand,
            new Color(palette.DogCommand, 0.22f * snapshot.FeedbackPulse));
    }

    public static HudCommandButtonOverlayStyle GetHudCommandButtonOverlayStyle(bool disabled, SoftOldCityHudPalette palette)
    {
        return new HudCommandButtonOverlayStyle(
            disabled ? new Color(palette.TextDim, 0.72f) : new Color(palette.Repair, 0.72f),
            new Color(palette.Repair, 0.72f),
            new Color(palette.DogCommand, 0.92f),
            new Color(palette.PanelStrongFill, 0.8f),
            new Color(palette.PanelSubtleFill, 0.58f),
            new Color(palette.Danger, 0.62f));
    }

    private static StyleBoxFlat PanelStyle(Color fill, Color stroke, int border = 1)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = stroke,
            BorderWidthLeft = border,
            BorderWidthTop = border,
            BorderWidthRight = border,
            BorderWidthBottom = border,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
        };
    }
}
