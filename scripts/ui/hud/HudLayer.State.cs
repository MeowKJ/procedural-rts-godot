using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private const float DrawerWidth = HudLayoutMath.DrawerWidth;
    private const float RailWidth = HudLayoutMath.RailWidth;
    private const int FontTiny = 9;
    private const int FontSmall = 11;
    private const int FontBody = 12;
    private const int FontMeta = 13;
    private const int FontValue = 15;
    private const int FontTitle = 18;

    private static SoftOldCityHudPalette CurrentPalette = SoftOldCityTheme.Day;
    private static Color Ink => CurrentPalette.Text;
    private static Color InkMuted => CurrentPalette.TextMuted;
    private static Color Cyan => CurrentPalette.CatRoute;
    private static Color Mint => CurrentPalette.Repair;
    private static Color Amber => CurrentPalette.DogCommand;
    private static Color Danger => CurrentPalette.Danger;
    private static readonly Dictionary<IconGlyph, Texture2D?> IconTextureCache = [];

    private Label _creditsValue = null!;
    private Label _selectedTitle = null!;
    private Label _selectedMeta = null!;
    private Label _selectedStats = null!;
    private Label _selectedDetail = null!;
    private Label _drawerSelectedTitle = null!;
    private Label _drawerSelectedMeta = null!;
    private Label _drawerSelectedStats = null!;
    private Label _drawerSelectedDetail = null!;
    private PortraitGlyph _drawerPortrait = null!;
    private SelectionIconSummary _drawerIconSummary = null!;
    private Label _statusValue = null!;
    private Label _productionValue = null!;
    private Label _queueValue = null!;
    private Label _alertValue = null!;
    private Label _outcomeTitle = null!;
    private Label _outcomeDetail = null!;
    private Panel _outcomeBanner = null!;
    private Panel _selectionCluster = null!;
    private Panel _commandRibbon = null!;
    private Panel _globalSkillPanel = null!;
    private Panel _sandboxDeveloperPanel = null!;
    private Panel _rightRail = null!;
    private Panel _rightProductionPanel = null!;
    private Panel _rightDetailPanel = null!;
    private Label _sandboxDeveloperStatus = null!;
    private Button _sandboxOwnerButton = null!;
    private Button _sandboxFactionButton = null!;
    private Button _sandboxTeamButton = null!;
    private Button _sandboxRelationButton = null!;
    private Button _sandboxTimeButton = null!;
    private Button _sandboxAtmosphereButton = null!;
    private Button _sandboxOverlayButton = null!;
    private Button _sandboxStressButton = null!;
    private Button _cancelProduction = null!;
    private IconActionButton _settingsButton = null!;
    private PortraitGlyph _portrait = null!;
    private MinimapSurface _minimapSurface = null!;
    private CommandPreviewOverlay _commandPreview = null!;
    private readonly List<AlertRow> _alertRows = [];
    private readonly List<ControlGroupSlot> _controlGroupSlots = [];
    private readonly List<MoveModeButton> _moveModeButtons = [];
    private readonly List<StanceModeButton> _stanceModeButtons = [];
    private readonly Dictionary<string, CommandButton> _commandButtons = [];
    private readonly List<Button> _sandboxDeveloperButtons = [];
    private MoveCommandMode _selectedMoveMode = MoveCommandMode.Direct;
    private UnitStance? _selectedUnitStance;
    private SandboxDeveloperContext _sandboxDeveloperContext = SandboxDeveloperContext.Default;
    private bool _hasSelection;
    private bool _hasBuildingSelection;
    private bool _buildModeActive;
    private bool _manualDrawerOpen;
    private float _productionDrawerProgress;
    private float _detailDrawerProgress;
    private float _drawerInactivity;

    public void SetSandboxDeveloperContext(SandboxDeveloperContext context)
    {
        _sandboxDeveloperContext = context;
        if (_sandboxDeveloperPanel is null)
        {
            return;
        }

        var owner = SandboxDeveloperContextOptions.OwnerOption(context.OwnerId);
        var faction = SandboxDeveloperContextOptions.FactionOption(context.Faction);
        var team = SandboxDeveloperContextOptions.TeamOption(context.TeamId);
        var relation = SandboxDeveloperContextOptions.RelationOption(context.Relation);
        var environment = SandboxDeveloperContextOptions.EnvironmentOption(context.Environment);
        var overlay = context.DebugOverlay.FormatStatus();

        _sandboxDeveloperStatus.Text = CompactText($"{faction.Label} / {SandboxTimeScaleMath.Format(context.TimeScale)}", 36);
        _sandboxOwnerButton.Text = $"Own {owner.OwnerId.Value}";
        _sandboxFactionButton.Text = faction.CanSpawn ? faction.Label : "Locked";
        _sandboxTeamButton.Text = $"Team {team.TeamId}";
        _sandboxRelationButton.Text = relation.Label;
        _sandboxTimeButton.Text = SandboxTimeScaleMath.Format(context.TimeScale).Replace("Sandbox time ", "", StringComparison.Ordinal);
        _sandboxAtmosphereButton.Text = CompactText(environment.Label, 12);
        _sandboxOverlayButton.Text = overlay == "Sandbox overlays: off" ? "Overlay off" : "Overlay on";
        _sandboxStressButton.Text = context.CanSpawnCurrentFaction ? "Stress spawn" : "Locked";
        _sandboxStressButton.Disabled = !context.CanSpawnCurrentFaction;

        foreach (var button in _sandboxDeveloperButtons)
        {
            button.QueueRedraw();
        }
    }

    public void SetSelectedCount(int count)
    {
        SetHudContext(count > 0, hasBuildingSelection: false, _buildModeActive);
        if (count == 0)
        {
            SetSelectionInfo(
                GameText.T("ui.noSelection.title"),
                GameText.T("ui.noSelection.meta"),
                GameText.T("ui.noSelection.stats"),
                GameText.T("ui.noSelection.detail"),
                "none",
                IconGlyph.None);
        }
        else
        {
            SetSelectionInfo(GameText.Format("ui.multi.title", count), GameText.T("ui.status.ready"), GameText.T("ui.multi.mixedSelection"), GameText.T("ui.multi.detail"), "multi", IconGlyph.Group);
        }
    }

    public void SetHudContext(bool hasSelection, bool hasBuildingSelection, bool buildModeActive)
    {
        _hasSelection = hasSelection;
        _hasBuildingSelection = hasBuildingSelection;
        _buildModeActive = buildModeActive;
        if (hasSelection)
        {
            _detailDrawerProgress = 1f;
        }

        if (hasBuildingSelection || buildModeActive)
        {
            _drawerInactivity = 0;
            _productionDrawerProgress = 1f;
        }

        if (_selectionCluster is not null)
        {
            _selectionCluster.Visible = false;
        }

        if (_commandRibbon is not null)
        {
            _commandRibbon.Visible = true;
        }
    }

    public void SetSelectionInfo(string title, string meta, string stats, string detail, string portraitMode, IconGlyph icon = IconGlyph.None)
    {
        SetSelectionInfo(title, meta, stats, detail, portraitMode, icon, [], icon == IconGlyph.None ? InkMuted : Mint, null);
    }

    public void SetSelectionInfo(
        string title,
        string meta,
        string stats,
        string detail,
        string portraitMode,
        IconGlyph icon,
        IReadOnlyList<SelectionIconItem> iconSummary,
        Color iconAccent,
        string? unitDesignId = null)
    {
        _selectedTitle.Text = CompactText(title, 24);
        _selectedMeta.Text = CompactText(meta, 34);
        _selectedStats.Text = CompactText(stats, 34);
        _selectedDetail.Text = CompactText(detail, 42);
        _drawerSelectedTitle.Text = CompactText(title, 24);
        _drawerSelectedMeta.Text = CompactText(meta, 30);
        _drawerSelectedStats.Text = CompactText(stats, 31);
        _drawerSelectedDetail.Text = CompactText(detail, 34);
        _portrait.Mode = portraitMode;
        _portrait.Icon = icon;
        _portrait.UnitDesignId = unitDesignId;
        _portrait.Accent = iconAccent;
        _portrait.QueueRedraw();
        _drawerPortrait.Mode = portraitMode;
        _drawerPortrait.Icon = icon;
        _drawerPortrait.UnitDesignId = unitDesignId;
        _drawerPortrait.Accent = iconAccent;
        _drawerPortrait.QueueRedraw();
        _drawerIconSummary.Items = iconSummary;
        _drawerIconSummary.Visible = iconSummary.Count > 0;
        _drawerPortrait.Visible = iconSummary.Count == 0;
        _drawerIconSummary.QueueRedraw();
    }

    public void SetStatus(string status)
    {
        _statusValue.Text = CompactText(status, 42);
    }

    public void SetProductionStatus(string status)
    {
        _productionValue.Text = CompactText(status, 54);
        if (!string.IsNullOrWhiteSpace(status) && status != GameText.T("ui.status.ready"))
        {
            _drawerInactivity = 0;
            _manualDrawerOpen = true;
            _productionDrawerProgress = 1f;
        }
    }

    public void SetProductionQueueSummary(string summary, bool canCancel)
    {
        _queueValue.Text = CompactMultiline(summary, 28);
        _cancelProduction.Disabled = !canCancel;
        _cancelProduction.TooltipText = canCancel ? GameText.T("ui.cancel.available") : GameText.T("ui.cancel.none");
    }

    public void SetResourceCredits(int credits)
    {
        _creditsValue.Text = credits.ToString("N0");
    }

    public void SetMinimapState(
        Vector2 worldSize,
        Rect2 cameraWorldRect,
        IReadOnlyList<MinimapUnit> units,
        IReadOnlyList<MinimapBuilding> buildings,
        IReadOnlyList<MinimapResource> resources,
        Texture2D? fogMask,
        IReadOnlyList<UnitMinimapPip>? unitDesignPips = null)
    {
        _minimapSurface.WorldSize = worldSize;
        _minimapSurface.ViewerFaction = ViewerFaction;
        _minimapSurface.CameraWorldRect = cameraWorldRect;
        _minimapSurface.Units = units;
        _minimapSurface.UnitDesignPips = unitDesignPips ?? [];
        _minimapSurface.Buildings = buildings;
        _minimapSurface.Resources = resources;
        _minimapSurface.FogMask = fogMask;
        _minimapSurface.QueueRedraw();
    }

    public void SetControlGroups(IReadOnlyList<ControlGroupSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (snapshot.Number < 1 || snapshot.Number > _controlGroupSlots.Count)
            {
                continue;
            }

            _controlGroupSlots[snapshot.Number - 1].SetSnapshot(snapshot);
        }
    }

    public void SetAlerts(IReadOnlyList<AlertLine> alerts)
    {
        _alertValue.Visible = false;
        for (var index = 0; index < _alertRows.Count; index++)
        {
            _alertRows[index].SetAlert(index < alerts.Count ? alerts[index] : null);
        }
    }

    public void SetCommandPreview(CommandPreviewState preview)
    {
        _commandPreview.Preview = preview;
        _commandPreview.QueueRedraw();
    }

    public void SetOutcomeBanner(GameOutcome outcome, string detail)
    {
        if (outcome == GameOutcome.InProgress)
        {
            _outcomeBanner.Visible = false;
            return;
        }

        _outcomeBanner.Visible = true;
        _outcomeTitle.Text = outcome == GameOutcome.Victory ? GameText.T("ui.outcome.victory") : GameText.T("ui.outcome.defeat");
        SetLabelColor(_outcomeTitle, outcome == GameOutcome.Victory ? Mint : Danger);
        _outcomeDetail.Text = CompactText(detail, 54);
    }

    public void SetCommandCardState(IReadOnlyList<ProductionOptionState> states)
    {
        var orderedStates = states.Take(12).ToArray();
        var activeIds = orderedStates.Select(ProductionOptionId).ToHashSet();
        foreach (var stale in _commandButtons.Keys.Where(key => !activeIds.Contains(key)).ToArray())
        {
            _commandButtons[stale].QueueFree();
            _commandButtons.Remove(stale);
        }

        for (var index = 0; index < orderedStates.Length; index++)
        {
            var state = orderedStates[index];
            var optionId = ProductionOptionId(state);
            if (!_commandButtons.TryGetValue(optionId, out var button))
            {
                button = AddCommandButton(_rightProductionPanel, optionId);
            }

            button.Hotkey = ProductionHotkey(index);
            button.Position = ProductionButtonPosition(index);
            button.Kind = state.Kind;
            button.UnitDesignId = state.UnitDesignId;

            var disabledReason = state.DisabledReasonKey == "ui.needCredits"
                ? GameText.Format("ui.needCredits", state.Cost)
                : string.IsNullOrWhiteSpace(state.DisabledReasonKey) ? "" : GameText.T(state.DisabledReasonKey);
            button.SetState(state, disabledReason);
        }

        if (orderedStates.Length == 0)
        {
            foreach (var button in _commandButtons.Values)
            {
                button.SetState(false, 0, 0, GameText.T("ui.producerUnavailable"));
            }
        }
    }

    public void SetMoveCommandMode(MoveCommandMode mode)
    {
        _selectedMoveMode = mode;
        foreach (var button in _moveModeButtons)
        {
            button.SetSelected(button.Mode == mode);
        }
    }

    public void SetSelectedUnitStance(UnitStance? stance)
    {
        _selectedUnitStance = stance;
        foreach (var button in _stanceModeButtons)
        {
            button.SetSelected(stance is not null && button.Stance == stance.Value);
        }
    }

    public readonly record struct MinimapUnit(Vector2 Position, Owner Owner, FactionId FactionId, bool Selected, float AlertPulse);

    public readonly record struct MinimapBuilding(Vector2 Position, Vector2 Size, Owner Owner, FactionId FactionId, bool Selected, float AlertPulse);

    public readonly record struct MinimapResource(Vector2 Position, float Radius, float RemainingRatio);

    public readonly record struct AlertLine(AlertKind Kind, FactionId? FactionId, string Text, float RemainingRatio);

    public readonly record struct SelectionIconItem(FactionId? FactionId, IconGlyph Glyph, string Label, int Count, Color Accent, string? UnitDesignId = null);
}
