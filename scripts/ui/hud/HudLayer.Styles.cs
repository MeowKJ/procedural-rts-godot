using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void ApplySoftOldCityPanelStyles()
    {
        foreach (var panel in FindControls<Panel>(this))
        {
            UiFactory.ApplyNamedHudPanelTheme(panel, CurrentPalette);
        }

        foreach (var label in FindControls<Label>(this))
        {
            SetLabelColor(label, label.Name == "Value" ? Ink : InkMuted);
        }

        SetLabelColor(_drawerSelectedTitle, Ink);
        SetLabelColor(_drawerSelectedMeta, Mint);
        SetLabelColor(_drawerSelectedStats, Ink);
        SetLabelColor(_drawerSelectedDetail, InkMuted);
        SetLabelColor(_statusValue, Ink);
        RefreshCommandRibbonContext();
        SetLabelColor(_productionValue, Ink);
        SetLabelColor(_queueValue, InkMuted);
        SetLabelColor(_outcomeDetail, Ink);
        if (_outcomeTitle is not null)
        {
            SetLabelColor(_outcomeTitle, Mint);
        }

        foreach (var button in _sandboxDeveloperButtons)
        {
            var accent = button == _sandboxStressButton
                ? Danger
                : button == _sandboxFactionButton || button == _sandboxAtmosphereButton ? Mint : Cyan;
            UiFactory.ApplyHudActionButtonTheme(button, CurrentPalette, accent, FontTiny);
            button.QueueRedraw();
        }

        foreach (var button in _moveModeButtons)
        {
            UiFactory.ApplyHudMoveModeButtonTheme(button, CurrentPalette, button.Mode, FontTiny);
            button.QueueRedraw();
        }

        _unitStanceStrip?.ApplyTheme(CurrentPalette, FontTiny);

        foreach (var button in _commandButtons.Values)
        {
            UiFactory.ApplyHudCommandButtonTheme(button, CurrentPalette);
            button.QueueRedraw();
        }

        foreach (var button in _productionProviderLaneButtons)
        {
            UiFactory.ApplyHudQueueRowTheme(button, CurrentPalette, button.Accent);
            button.QueueRedraw();
        }

        foreach (var button in _abilityCards.Values)
        {
            UiFactory.ApplyHudCommandButtonTheme(button, CurrentPalette);
            button.QueueRedraw();
        }

        foreach (var button in FindControls<IconActionButton>(this))
        {
            button.Accent = UiFactory.HudActionAccent(button.Glyph, CurrentPalette);
            UiFactory.ApplyHudActionButtonTheme(button, CurrentPalette, button.Accent, FontTiny);
            button.QueueRedraw();
        }

        foreach (var control in FindControls<Control>(this))
        {
            control.QueueRedraw();
        }
    }

    private static void SetLabelColor(Label? label, Color color)
    {
        if (label?.LabelSettings is null)
        {
            return;
        }

        UiFactory.ApplyHudLabelStyle(label, CurrentPalette, color);
    }

    private static IEnumerable<T> FindControls<T>(Node node) where T : Control
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T control)
            {
                yield return control;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static Panel MakePanel(string name, Color fill, Color border)
    {
        var panel = UiFactory.MakeHudPanel(name, fill, border);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        return panel;
    }

    private static Control MakeBlock(string title, string value, Vector2 position, Vector2 size, Color accent)
    {
        var panelColors = UiFactory.HudPanelColorsFor(CurrentPalette, subtle: true);
        var block = MakePanel(title.Replace(" ", ""), panelColors.Fill, panelColors.Border);
        block.Position = position;
        block.CustomMinimumSize = size;
        block.Size = size;
        block.AddChild(new ColorRect
        {
            Position = new Vector2(4, 7),
            CustomMinimumSize = new Vector2(2, size.Y - 14),
            Size = new Vector2(2, size.Y - 14),
            Color = new Color(accent, 0.88f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        block.AddChild(MakeLabel(title, new Vector2(12, 4), 11, InkMuted));
        var valueLabel = MakeLabel(value, new Vector2(12, 19), 15, Ink);
        valueLabel.Name = "Value";
        valueLabel.CustomMinimumSize = size - new Vector2(20, 19);
        block.AddChild(valueLabel);
        return block;
    }

    private static Label MakeTabLabel(string text, Vector2 position, bool enabled)
    {
        var label = MakeLabel(text, position, 11, UiFactory.HudTabLabelColor(enabled, CurrentPalette));
        label.CustomMinimumSize = new Vector2(48, 20);
        return label;
    }

    private static Label MakeLabel(string text, Vector2 position, int fontSize, Color color)
    {
        return UiFactory.MakeHudLabel(text, position, fontSize, color, CurrentPalette);
    }

    private static Label MakeSizedLabel(string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        return UiFactory.MakeHudSizedLabel(text, position, size, fontSize, color, CurrentPalette);
    }

    private static string CompactText(string text, int maxChars)
    {
        return HudLayoutMath.CompactFieldText(text, maxChars);
    }

    private static void SetLabelTextAndResetSizeWhenChanged(Label label, string next)
    {
        if (string.Equals(label.Text, next, StringComparison.Ordinal))
        {
            return;
        }

        label.Text = next;
        label.ResetSize();
    }

    private static string CompactMultiline(string text, int maxCharsPerLine)
    {
        return string.Join("\n", text
            .Split('\n')
            .Select(line => CompactText(line, maxCharsPerLine)));
    }

    private static void AddSeparator(Control parent, Vector2 position)
    {
        var separator = new SeparatorLine
        {
            Position = position,
            CustomMinimumSize = new Vector2(1, 30),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(separator);
    }

    private static ColorRect AddConsoleDivider(Control parent, float y, Color accent)
    {
        var divider = new ColorRect
        {
            Position = new Vector2(8, y),
            CustomMinimumSize = new Vector2(DrawerWidth - 16, 1),
            Size = new Vector2(DrawerWidth - 16, 1),
            Color = new Color(accent, 0.34f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(divider);
        return divider;
    }
}
