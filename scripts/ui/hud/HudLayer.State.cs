using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private const float DrawerWidth = HudLayoutMath.DrawerWidth;
    private const float RailWidth = HudLayoutMath.RailWidth;
    private const int FontTiny = HudLayoutMath.MinimumCompactFontSize;
    private const int FontSmall = HudLayoutMath.MinimumBodyFontSize;
    private const int FontBody = 13;
    private const int FontMeta = 14;
    private const int FontValue = 15;
    private const int FontTitle = 18;
    private const int MaxProductionProviderLaneButtons = 8;

    private static SoftOldCityHudPalette CurrentPalette = SoftOldCityTheme.Day;
    private static Color Ink => CurrentPalette.Text;
    private static Color InkMuted => CurrentPalette.TextMuted;
    private static Color Cyan => CurrentPalette.CatRoute;
    private static Color Mint => CurrentPalette.Repair;
    private static Color Amber => CurrentPalette.DogCommand;
    private static Color Danger => CurrentPalette.Danger;
    private Label _creditsValue = null!;
    private Label _drawerSelectedTitle = null!;
    private Label _drawerSelectedMeta = null!;
    private Label _drawerSelectedStats = null!;
    private Label _drawerSelectedDetail = null!;
    private PortraitGlyph _drawerPortrait = null!;
    private SelectionIconSummary _drawerIconSummary = null!;
    private Label _catalogSurfaceLabel = null!;
    private Label _catalogOverviewValue = null!;
    private Label _statusValue = null!;
    private Label _commandRibbonContextValue = null!;
    private Label _productionValue = null!;
    private Label _queueValue = null!;
    private Label _repeatProductionStateValue = null!;
    private ColorRect _productionFooterDivider = null!;
    private Label _alertValue = null!;
    private Label _outcomeTitle = null!;
    private Label _outcomeDetail = null!;
    private Panel _outcomeBanner = null!;
    private Panel _commandRibbon = null!;
    private UnitStanceStrip _unitStanceStrip = null!;
    private Panel _sandboxDeveloperPanel = null!;
    private Panel _rightRail = null!;
    private Panel _rightProductionPanel = null!;
    private Panel _rightDetailPanel = null!;
    private Label _sandboxDeveloperStatus = null!;
    private Label _sandboxStateHashValue = null!;
    private Button _sandboxOwnerButton = null!;
    private Button _sandboxFactionButton = null!;
    private Button _sandboxTeamButton = null!;
    private Button _sandboxRelationButton = null!;
    private Button _sandboxTimeButton = null!;
    private Button _sandboxAtmosphereButton = null!;
    private Button _sandboxOverlayButton = null!;
    private Button _sandboxStressButton = null!;
    private IconActionButton _deckToggle = null!;
    private QueueMiniStack _queueMiniStack = null!;
    private IconActionButton _cancelProduction = null!;
    private IconActionButton _repeatProduction = null!;
    private IconActionButton _settingsButton = null!;
    private MinimapSurface _minimapSurface = null!;
    private CommandPreviewOverlay _commandPreview = null!;
    private readonly List<AlertRow> _alertRows = [];
    private readonly List<ControlGroupSlot> _controlGroupSlots = [];
    private readonly List<MoveModeButton> _moveModeButtons = [];
    private readonly Dictionary<string, CommandButton> _commandButtons = [];
    private readonly List<BuildOptionSnapshot> _buildCardStates = [];
    private readonly List<BuildOptionSnapshot> _visibleBuildCardStates = [];
    private readonly List<ProductionOptionState> _commandCardStates = [];
    private readonly List<ProductionOptionState> _visibleCommandCardStates = [];
    private readonly List<ProductionProviderLaneState> _productionProviderLaneStates = [];
    private readonly List<ProductionProviderLaneState> _constructionProviderLaneStates = [];
    private readonly List<ProductionProviderLaneButton> _productionProviderLaneButtons = [];
    private readonly Dictionary<string, int> _allProductionProviderCursorByKind = [];
    private readonly Dictionary<string, int> _allConstructionProviderCursorByKind = [];
    private readonly HashSet<string> _commandCardActiveIds = [];
    private readonly List<string> _commandCardStaleIds = [];
    private readonly List<Button> _sandboxDeveloperButtons = [];
    private MoveCommandMode _selectedMoveMode = MoveCommandMode.Direct;
    private UnitStance? _selectedUnitStance;
    private int _selectedUnitCount;
    private ProductionProviderLaneScope _selectedProductionProviderLaneScope = ProductionProviderLaneScope.Auto;
    private int _selectedProductionProviderId;
    private ProductionProviderLaneScope _selectedConstructionProviderLaneScope = ProductionProviderLaneScope.Auto;
    private int _selectedConstructionProviderId;
    private SandboxDeveloperContext _sandboxDeveloperContext = SandboxDeveloperContext.Default;
    private bool _hasSelection;
    private bool _hasBuildingSelection;
    private bool _buildModeActive;
    private bool _manualDrawerOpen;
    private float _productionDrawerProgress;
    private float _detailDrawerProgress;
    private float _productionStatusPulse;
    private float _queueStatusPulse;
    private bool _commandFailureVisible;
    private bool _productionCommandFailureVisible;
    private string _lastProductionStatus = "";
    private string _lastQueueSummary = "";
    private string _focusedRepeatProductionDesignId = "";
    private string _focusedRepeatProductionLabel = "";
    private string _focusedRepeatProductionProducerKind = "";
    private string _focusedRepeatProductionProducerLabel = "";
    private string _lastRepeatProductionRefreshKey = "";
    private bool _repeatProductionStateCached;
    private bool _lastCanCancelProduction;

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
        if (!context.DebugOverlay.IsEnabled(SandboxDebugOverlayFlag.StateHash))
        {
            SetSandboxStateHash(null);
        }

        foreach (var button in _sandboxDeveloperButtons)
        {
            button.QueueRedraw();
        }
    }

    public void SetSandboxStateHash(ulong? hash)
    {
        if (_sandboxStateHashValue is null)
        {
            return;
        }

        _sandboxStateHashValue.Visible = hash is not null;
        _sandboxStateHashValue.Text = hash is null ? "" : $"HASH {hash.Value:X16}";
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
        var wasShowingNoSelectionCommandHint = ShouldShowNoSelectionCommandHint();
        _hasSelection = hasSelection;
        _hasBuildingSelection = hasBuildingSelection;
        _buildModeActive = buildModeActive;
        if (hasSelection)
        {
            _detailDrawerProgress = 1f;
        }

        if (hasBuildingSelection || buildModeActive)
        {
            _productionDrawerProgress = 1f;
        }

        if (_commandRibbon is not null)
        {
            _commandRibbon.Visible = true;
        }

        if (wasShowingNoSelectionCommandHint != ShouldShowNoSelectionCommandHint())
        {
            SetCatalogInspectorDefault(DefaultCatalogInspectorText());
        }

        if (IsInsideTree()) LayoutDynamicHud(GetViewport().GetVisibleRect().Size);
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
        _drawerSelectedTitle.Text = CompactText(title, 24);
        _drawerSelectedMeta.Text = CompactText(meta, 30);
        _drawerSelectedStats.Text = CompactText(stats, 31);
        _drawerSelectedDetail.Text = CompactText(detail, 34);
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
        _commandFailureVisible = CommandFailurePresentation.IsFailureStatus(status);
        _statusValue.Text = CompactText(CommandFailurePresentation.InlineText(status), 42);
    }

    public void SetProductionStatus(string status)
    {
        _productionCommandFailureVisible = CommandFailurePresentation.IsFailureStatus(status);
        if (!string.Equals(_lastProductionStatus, status, StringComparison.Ordinal))
        {
            _productionStatusPulse = 1f;
            _lastProductionStatus = status;
        }

        if (_selectedCatalogMode != CatalogModeKind.Abilities)
        {
            SetCommandPanelResult(status);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != GameText.T("ui.status.ready"))
        {
            SetCommandDeckOpen(true);
        }
    }

    public void ClearCommandFailureFeedback()
    {
        if (_commandFailureVisible)
        {
            _commandFailureVisible = false;
            _statusValue.Text = CompactText(GameText.T("ui.status.ready"), 42);
        }

        if (!_productionCommandFailureVisible)
        {
            return;
        }

        _productionCommandFailureVisible = false;
        _lastProductionStatus = "";
        if (_selectedCatalogMode != CatalogModeKind.Abilities)
        {
            ClearCatalogInspectorCommandFeedback();
            SetCatalogInspectorDefault(DefaultCatalogInspectorText());
        }
    }

    public void SetProductionQueueSummary(string summary, bool canCancel)
    {
        if (!string.Equals(_lastQueueSummary, summary, StringComparison.Ordinal)
            || _lastCanCancelProduction != canCancel)
        {
            _queueStatusPulse = 1f;
            _lastQueueSummary = summary;
            _lastCanCancelProduction = canCancel;
            var lineBreak = summary.IndexOf('\n');
            var surfaceSummary = lineBreak >= 0 ? summary[..lineBreak] : summary;
            _queueValue.Text = CompactText(surfaceSummary, 28);
            _cancelProduction.FixedHoverText = canCancel ? summary : GameText.T("ui.cancel.none");
        }

        _cancelProduction.Disabled = !canCancel;
        RefreshProductionProviderLaneSummary();
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
        IReadOnlyList<UnitMinimapPip>? unitDesignPips = null,
        IReadOnlyList<MinimapAlertPing>? alertPings = null)
    {
        _minimapSurface.WorldSize = worldSize;
        _minimapSurface.ViewerFaction = ViewerFaction;
        _minimapSurface.CameraWorldRect = cameraWorldRect;
        _minimapSurface.Units = units;
        _minimapSurface.UnitDesignPips = unitDesignPips ?? [];
        _minimapSurface.Buildings = buildings;
        _minimapSurface.Resources = resources;
        _minimapSurface.AlertPings = alertPings ?? [];
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
        ApplyCommandCursor(preview);
        _commandPreview.QueueRedraw();
        RefreshCommandRibbonContext();
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
        _commandCardStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            var state = states[index];
            _commandCardStates.Add(state);
        }

        ValidateProductionProviderLaneSelection();
        RefreshCommandCards();
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshCatalogOverview();
        RefreshRepeatProductionControl();
    }

    public void SetProductionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)
    {
        _productionProviderLaneStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            _productionProviderLaneStates.Add(states[index]);
        }

        ValidateProductionProviderLaneSelection();
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshCatalogOverview();
        RefreshRepeatProductionControl();
    }

    public void SetConstructionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)
    {
        _constructionProviderLaneStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            _constructionProviderLaneStates.Add(states[index]);
        }

        ValidateConstructionProviderLaneSelection();
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshCatalogOverview();
        RefreshRepeatProductionControl();
    }

    public void SetBuildCardState(IReadOnlyList<BuildOptionSnapshot> states)
    {
        _buildCardStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            _buildCardStates.Add(states[index]);
        }

        RefreshCommandCards();
    }

    private void RefreshCommandCards()
    {
        _visibleBuildCardStates.Clear();
        _visibleCommandCardStates.Clear();
        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            for (var index = 0; index < _buildCardStates.Count; index++)
            {
                var state = _buildCardStates[index];
                if (state.Category != _selectedBuildCategory)
                {
                    continue;
                }

                if (_visibleBuildCardStates.Count >= 12)
                {
                    break;
                }

                _visibleBuildCardStates.Add(state);
            }
        }
        else if (_selectedCatalogMode == CatalogModeKind.Train)
        {
            for (var index = 0; index < _commandCardStates.Count; index++)
            {
                var state = _commandCardStates[index];
                if (state.Category != _selectedProductionCategory)
                {
                    continue;
                }

                if (_visibleCommandCardStates.Count >= 12)
                {
                    break;
                }

                _visibleCommandCardStates.Add(state);
            }
        }

        _commandCardActiveIds.Clear();
        _commandCardStaleIds.Clear();
        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            for (var index = 0; index < _visibleBuildCardStates.Count; index++)
            {
                _commandCardActiveIds.Add(BuildOptionId(_visibleBuildCardStates[index]));
            }
        }
        else if (_selectedCatalogMode == CatalogModeKind.Train)
        {
            for (var index = 0; index < _visibleCommandCardStates.Count; index++)
            {
                _commandCardActiveIds.Add(ProductionOptionId(_visibleCommandCardStates[index]));
            }
        }

        foreach (var key in _commandButtons.Keys)
        {
            if (!_commandCardActiveIds.Contains(key))
            {
                _commandCardStaleIds.Add(key);
            }
        }

        foreach (var stale in _commandCardStaleIds)
        {
            InvalidateCatalogInspectorItem(CommandCardInspectorItemId(stale));
            _commandButtons[stale].QueueFree();
            _commandButtons.Remove(stale);
        }

        ClearRepeatFocusIfHidden();
        RefreshCatalogOverview();

        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            ClearUpgradeProjectCards();
            ClearAbilityCards();
            RefreshProductionProviderLaneSummary();
            RefreshProductionProviderLaneButtons();
            RefreshBuildCards();
            return;
        }

        if (_selectedCatalogMode == CatalogModeKind.Train)
        {
            ClearUpgradeProjectCards();
            ClearAbilityCards();
            RefreshProductionProviderLaneSummary();
            RefreshProductionProviderLaneButtons();
            RefreshProductionCards();
            return;
        }

        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        if (_selectedCatalogMode == CatalogModeKind.Upgrades)
        {
            ClearAbilityCards();
            RefreshRepeatProductionControl();
            RefreshUpgradeProjectCards();
            return;
        }

        ClearUpgradeProjectCards();
        RefreshAbilityCards();
    }

    private string LastProductionCatalogStatusText()
    {
        return string.IsNullOrWhiteSpace(_lastProductionStatus)
            ? GameText.T("ui.status.ready")
            : CommandFailurePresentation.PanelText(_lastProductionStatus);
    }

    private void RefreshBuildCards()
    {
        for (var index = 0; index < _visibleBuildCardStates.Count; index++)
        {
            var state = _visibleBuildCardStates[index];
            var optionId = BuildOptionId(state);
            if (!_commandButtons.TryGetValue(optionId, out var button))
            {
                button = AddCommandButton(_rightProductionPanel, optionId);
            }

            button.Hotkey = ProductionHotkey(index);
            button.Position = ProductionButtonPosition(index);

            var disabledReason = LocalizedDisabledReason(state.DisabledReasonKey, state.Cost);
            button.SetBuildState(state, disabledReason);
            RefreshCatalogInspectorItem(CommandCardInspectorItemId(optionId), button.InspectorText);
        }
    }

    private void RefreshProductionCards()
    {
        for (var index = 0; index < _visibleCommandCardStates.Count; index++)
        {
            var state = _visibleCommandCardStates[index];
            var optionId = ProductionOptionId(state);
            if (!_commandButtons.TryGetValue(optionId, out var button))
            {
                button = AddCommandButton(_rightProductionPanel, optionId);
            }

            button.Hotkey = ProductionHotkey(index);
            button.Position = ProductionButtonPosition(index);
            button.Kind = state.Kind;
            button.UnitDesignId = state.UnitDesignId;

            var disabledReason = LocalizedDisabledReason(state.DisabledReasonKey, state.Cost);
            button.SetState(state, disabledReason);
            RefreshCatalogInspectorItem(CommandCardInspectorItemId(optionId), button.InspectorText);
        }

    }

    private static string LocalizedDisabledReason(string disabledReasonKey, int cost)
    {
        return disabledReasonKey == "ui.needCredits"
            ? GameText.Format("ui.needCredits", cost)
            : string.IsNullOrWhiteSpace(disabledReasonKey) ? "" : GameText.T(disabledReasonKey);
    }

    public readonly record struct MinimapUnit(Vector2 Position, Owner Owner, FactionId FactionId, bool Selected, float AlertPulse);
    public readonly record struct MinimapBuilding(Vector2 Position, Vector2 Size, Owner Owner, FactionId FactionId, bool Selected, float AlertPulse);

    public readonly record struct MinimapResource(Vector2 Position, float Radius, float RemainingRatio);

    public readonly record struct MinimapAlertPing(Vector2 Position, AlertKind Kind, float RemainingRatio);

    public readonly record struct AlertLine(AlertKind Kind, FactionId? FactionId, string Text, float RemainingRatio);

    public readonly record struct AbilityCardState(AbilitySpec Ability, float CooldownRemaining, bool IsActive);

    public readonly record struct SelectionIconItem(FactionId? FactionId, IconGlyph Glyph, string Label, int Count, Color Accent, string? UnitDesignId = null);
}
