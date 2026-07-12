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
        SetLabelColor(_providerLaneSummaryValue, InkMuted);
        SetLabelColor(_productionValue, Ink);
        SetLabelColor(_queueValue, InkMuted);
        SetLabelColor(_outcomeDetail, Ink);
        if (_outcomeTitle is not null)
        {
            SetLabelColor(_outcomeTitle, Mint);
        }

        if (_cancelProduction is not null)
        {
            UiFactory.ApplyHudCancelButtonTheme(_cancelProduction, CurrentPalette, FontSmall);
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

        foreach (var button in _stanceModeButtons)
        {
            UiFactory.ApplyHudStanceButtonTheme(button, CurrentPalette, button.Presentation, FontTiny);
            button.QueueRedraw();
        }

        foreach (var button in _commandButtons.Values)
        {
            UiFactory.ApplyHudCommandButtonTheme(button, CurrentPalette, FontBody);
            button.QueueRedraw();
        }

        foreach (var button in _productionProviderLaneButtons)
        {
            UiFactory.ApplyHudActionButtonTheme(button, CurrentPalette, Cyan, FontTiny);
            button.QueueRedraw();
        }

        foreach (var button in _abilityCards.Values)
        {
            UiFactory.ApplyHudCommandButtonTheme(button, CurrentPalette, FontBody);
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

    private static Control MakeBlock(string title, string value, Vector2 position, Vector2 size)
    {
        var panelColors = UiFactory.HudPanelColorsFor(CurrentPalette, subtle: true);
        var block = MakePanel(title.Replace(" ", ""), panelColors.Fill, panelColors.Border);
        block.Position = position;
        block.CustomMinimumSize = size;
        block.AddChild(MakeLabel(title, new Vector2(10, 5), 10, InkMuted));
        var valueLabel = MakeLabel(value, new Vector2(10, 20), 15, Ink);
        valueLabel.Name = "Value";
        valueLabel.CustomMinimumSize = size - new Vector2(18, 20);
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
        if (text.Length <= maxChars)
        {
            return text;
        }

        return maxChars <= 3 ? text[..maxChars] : text[..(maxChars - 3)] + "...";
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
}
