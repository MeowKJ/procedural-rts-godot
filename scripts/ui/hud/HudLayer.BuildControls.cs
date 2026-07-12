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

    private void AddProductionTab(Control parent, IconGlyph glyph, string tooltip, Vector2 position, BuildCategory category, bool active)
    {
        var tab = new ProductionTab
        {
            Glyph = glyph,
            Category = category,
            Active = active,
            Position = position,
            CustomMinimumSize = new Vector2(31, 32),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = tooltip,
            Disabled = !active,
        };
        tab.Size = tab.CustomMinimumSize;
        RegisterProductionTab(tab);
        tab.Pressed += () =>
        {
            SelectProductionTab(category);
        };
        parent.AddChild(tab);
    }

    private void AddCatalogModeButton(Control parent, CatalogModeKind mode, string label, string detail, string helpText, Vector2 position)
    {
        var button = new CatalogModeButton
        {
            Name = mode switch
            {
                CatalogModeKind.Build => "CatalogModeBuild",
                CatalogModeKind.Train => "CatalogModeTrain",
                CatalogModeKind.Upgrades => "CatalogModeUpgrades",
                CatalogModeKind.Abilities => "CatalogModeAbilities",
                _ => "CatalogMode",
            },
            Mode = mode,
            Label = label,
            Detail = detail,
            HelpText = helpText,
            Position = position,
            CustomMinimumSize = new Vector2(66, 28),
            Size = new Vector2(66, 28),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = helpText,
        };
        RegisterCatalogModeButton(button);
        var inspectorItemId = CatalogModeInspectorItemId(mode);
        button.Pressed += () => SelectCatalogMode(mode);
        button.Pressed += () => ShowCatalogInspectorHover(inspectorItemId, CatalogModePageSelectedText(button));
        button.MouseEntered += () => ShowCatalogInspectorHover(inspectorItemId, button.HelpText);
        button.MouseExited += () => ClearCatalogInspectorHover(inspectorItemId);
        button.FocusEntered += () => ShowCatalogInspectorHover(inspectorItemId, CatalogModeFocusText(button));
        button.FocusExited += () => ClearCatalogInspectorHover(inspectorItemId);
        parent.AddChild(button);
    }

    private void AddTrainCategoryTab(Control parent, IconGlyph glyph, string tooltip, Vector2 position, ProductionCategory category, bool active)
    {
        var tab = new ProductionCategoryTab
        {
            Glyph = glyph,
            Category = category,
            Active = active,
            Position = position,
            CustomMinimumSize = new Vector2(31, 32),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = tooltip,
            Disabled = !active,
        };
        tab.Size = tab.CustomMinimumSize;
        RegisterTrainCategoryTab(tab);
        tab.Pressed += () => SelectProductionCategory(category);
        parent.AddChild(tab);
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
        var inspectorItemId = CommandCardInspectorItemId(optionId);
        button.MouseEntered += () => ShowCatalogInspectorHover(inspectorItemId, button.InspectorText);
        button.MouseEntered += () => FocusRepeatProductionDesign(button.UnitDesignId);
        button.MouseExited += () => ClearCatalogInspectorHover(inspectorItemId);
        button.FocusEntered += () => ShowCatalogInspectorHover(inspectorItemId, button.InspectorText);
        button.FocusEntered += () => FocusRepeatProductionDesign(button.UnitDesignId);
        button.FocusExited += () => ClearCatalogInspectorHover(inspectorItemId);
        button.Pressed += () =>
        {
            PinCatalogInspectorItem(inspectorItemId, button.InspectorText);
            if (!string.IsNullOrWhiteSpace(button.BuildKind))
            {
                BuildKindRequested?.Invoke(button.BuildKind, SelectedConstructionProviderId(button.BuildKind));
                return;
            }

            if (!string.IsNullOrWhiteSpace(button.UnitDesignId))
            {
                FocusRepeatProductionDesign(button.UnitDesignId);
                ProductionDesignRequested?.Invoke(button.UnitDesignId, () => SelectedProductionProviderId(button.UnitDesignId), ProductionRequestCount());
                return;
            }

            ProductionRequested?.Invoke(button.Kind, ProductionRequestCount());
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

    private static string BuildOptionId(BuildOptionSnapshot state)
    {
        return $"build.{state.Kind}";
    }

    private static string ProductionHotkey(int index)
    {
        var hotkeys = new[] { "Q", "W", "E", "A", "S", "D", "Z", "X", "C", "R", "F", "V" };
        return index >= 0 && index < hotkeys.Length ? hotkeys[index] : (index + 1).ToString();
    }

    private static int ProductionRequestCount()
    {
        return Input.IsKeyPressed(Key.Shift) ? ShiftProductionBatchCount : 1;
    }

    private void AddProductionProviderLaneButton(Control parent, int index)
    {
        var button = new ProductionProviderLaneButton
        {
            Name = $"ProductionProviderLane{index}",
            Index = index,
            Position = new Vector2(4, 52 + index * 28),
            CustomMinimumSize = new Vector2(40, 24),
            Size = new Vector2(40, 24),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false,
        };
        UiFactory.ApplyHudActionButtonTheme(button, CurrentPalette, Cyan, FontTiny);
        button.Pressed += () =>
        {
            SelectProviderLane(button.State);
        };
        _productionProviderLaneButtons.Add(button);
        parent.AddChild(button);
    }

    private static Vector2 ProductionButtonPosition(int index)
    {
        var column = index % 3;
        var row = index / 3;
        return new Vector2(14 + column * 94, 96 + row * 58);
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

    private void AddStanceModeButton(Control parent, UnitStancePresentation presentation, Vector2 position)
    {
        var button = new StanceModeButton
        {
            Presentation = presentation,
            Position = position,
            CustomMinimumSize = new Vector2(36, 34),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = presentation.Tooltip,
        };
        button.Size = button.CustomMinimumSize;
        UiFactory.ApplyHudStanceButtonTheme(button, CurrentPalette, presentation, FontTiny);
        button.Pressed += () =>
        {
            SetSelectedUnitStance(presentation.Stance, _selectedUnitCount);
            UnitStanceRequested?.Invoke(presentation.Stance);
        };
        button.SetSelected(_selectedUnitStance is not null && presentation.Stance == _selectedUnitStance.Value);
        _stanceModeButtons.Add(button);
        parent.AddChild(button);
    }
}
