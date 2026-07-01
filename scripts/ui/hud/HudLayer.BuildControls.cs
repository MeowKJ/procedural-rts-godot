using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private static IconActionButton AddIconActionButton(
        Control parent,
        IconGlyph glyph,
        string tooltip,
        Vector2 position,
        Vector2 size,
        Color accent)
    {
        var button = new IconActionButton
        {
            Glyph = glyph,
            Accent = accent,
            Position = position,
            CustomMinimumSize = size,
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = tooltip,
        };
        button.Size = size;
        UiFactory.ApplyHudActionButtonTheme(button, CurrentPalette, accent, FontTiny);
        parent.AddChild(button);
        return button;
    }

    private Button AddSandboxDeveloperButton(
        string name,
        string tooltip,
        Vector2 position,
        Vector2 size,
        Color accent,
        Action pressed)
    {
        var button = new Button
        {
            Name = name,
            Position = position,
            CustomMinimumSize = size,
            Size = size,
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = tooltip,
        };
        UiFactory.ApplyHudActionButtonTheme(button, CurrentPalette, accent, FontTiny);
        button.Pressed += pressed;
        _sandboxDeveloperButtons.Add(button);
        _sandboxDeveloperPanel.AddChild(button);
        return button;
    }

    private OwnerId NextSandboxOwner()
    {
        return NextOption(
            SandboxDeveloperContextOptions.Owners,
            option => option.OwnerId == _sandboxDeveloperContext.OwnerId).OwnerId;
    }

    private UnitFactionId NextSandboxFaction()
    {
        return NextOption(
            SandboxDeveloperContextOptions.Factions,
            option => option.Faction == _sandboxDeveloperContext.Faction).Faction;
    }

    private int NextSandboxTeam()
    {
        return NextOption(
            SandboxDeveloperContextOptions.Teams,
            option => option.TeamId == _sandboxDeveloperContext.TeamId).TeamId;
    }

    private PlayerRelation NextSandboxRelation()
    {
        return NextOption(
            SandboxDeveloperContextOptions.Relations,
            option => option.Relation == _sandboxDeveloperContext.Relation).Relation;
    }

    private float NextSandboxTimeScale()
    {
        return NextOption(
            SandboxDeveloperContextOptions.TimeScales,
            option => MathF.Abs(option.Scale - _sandboxDeveloperContext.TimeScale) < 0.0001f).Scale;
    }

    private SandboxAtmospherePreset NextSandboxEnvironment()
    {
        return NextOption(
            SandboxDeveloperContextOptions.Environments,
            option => option.Preset == _sandboxDeveloperContext.Environment).Preset;
    }

    private SandboxDebugOverlayPreset NextSandboxOverlayPreset()
    {
        var current = _sandboxDeveloperContext.DebugOverlay.EnabledFlags;
        return NextOption(
            SandboxDebugOverlayState.Presets,
            option => option.Flags == current);
    }

    private static T NextOption<T>(IReadOnlyList<T> options, Func<T, bool> isCurrent)
    {
        if (options.Count == 0)
        {
            throw new InvalidOperationException("Sandbox developer option list must not be empty.");
        }

        for (var index = 0; index < options.Count; index++)
        {
            if (isCurrent(options[index]))
            {
                return options[(index + 1) % options.Count];
            }
        }

        return options[0];
    }

    private static void AddProductionTab(Control parent, IconGlyph glyph, string tooltip, Vector2 position, bool active)
    {
        var tab = new ProductionTab
        {
            Glyph = glyph,
            Active = active,
            Selected = glyph == IconGlyph.Building,
            Position = position,
            CustomMinimumSize = new Vector2(38, 32),
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = tooltip,
        };
        tab.Size = tab.CustomMinimumSize;
        parent.AddChild(tab);
    }

    private static void AddPlaceholderBuildSlot(Control parent, string label, Vector2 position)
    {
        var slot = new PlaceholderBuildSlot
        {
            SlotLabel = label,
            Position = position,
            CustomMinimumSize = new Vector2(80, 70),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        slot.Size = slot.CustomMinimumSize;
        parent.AddChild(slot);
    }

    private CommandButton AddCommandButton(Control parent, string optionId)
    {
        var button = new CommandButton
        {
            OptionId = optionId,
            Hotkey = "",
            ShortLabel = "",
            Glyph = IconGlyph.None,
            Accent = Mint,
            Cost = 0,
            CustomMinimumSize = new Vector2(82, 58),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        button.Size = button.CustomMinimumSize;
        UiFactory.ApplyHudCommandButtonTheme(button, CurrentPalette, FontBody);
        button.Pressed += () =>
        {
            if (!string.IsNullOrWhiteSpace(button.UnitDesignId))
            {
                ProductionDesignRequested?.Invoke(button.UnitDesignId);
                return;
            }

            ProductionRequested?.Invoke(button.Kind);
        };
        _commandButtons[optionId] = button;
        parent.AddChild(button);
        return button;
    }

    private static string ProductionOptionId(ProductionOptionState state)
    {
        return string.IsNullOrWhiteSpace(state.UnitDesignId)
            ? $"legacy.{state.Kind}"
            : $"design.{state.UnitDesignId}";
    }

    private static string ProductionHotkey(int index)
    {
        var hotkeys = new[] { "Q", "W", "E", "A", "S", "D", "Z", "X", "C", "R", "F", "V" };
        return index >= 0 && index < hotkeys.Length ? hotkeys[index] : (index + 1).ToString();
    }

    private static Vector2 ProductionButtonPosition(int index)
    {
        var column = index % 3;
        var row = index / 3;
        return new Vector2(14 + column * 94, 76 + row * 58);
    }

    private void AddMoveModeButton(Control parent, MoveCommandMode mode, IconGlyph glyph, string tooltip, Vector2 position)
    {
        var button = new MoveModeButton
        {
            Mode = mode,
            Glyph = glyph,
            Position = position,
            CustomMinimumSize = new Vector2(36, 34),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = tooltip,
        };
        button.Size = button.CustomMinimumSize;
        UiFactory.ApplyHudMoveModeButtonTheme(button, CurrentPalette, mode, FontTiny);
        button.Pressed += () =>
        {
            SetMoveCommandMode(mode);
            MoveModeRequested?.Invoke(mode);
        };
        button.SetSelected(mode == _selectedMoveMode);
        _moveModeButtons.Add(button);
        parent.AddChild(button);
    }

    private void AddStanceModeButton(Control parent, UnitStance stance, IconGlyph glyph, string tooltip, Vector2 position)
    {
        var button = new StanceModeButton
        {
            Stance = stance,
            Glyph = glyph,
            Position = position,
            CustomMinimumSize = new Vector2(36, 34),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = tooltip,
        };
        button.Size = button.CustomMinimumSize;
        UiFactory.ApplyHudStanceButtonTheme(button, CurrentPalette, stance, FontTiny);
        button.Pressed += () =>
        {
            SetSelectedUnitStance(stance);
            UnitStanceRequested?.Invoke(stance);
        };
        button.SetSelected(_selectedUnitStance is not null && stance == _selectedUnitStance.Value);
        _stanceModeButtons.Add(button);
        parent.AddChild(button);
    }
}
