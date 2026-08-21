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

    public readonly record struct MinimapUnit(Vector2 Position, Owner Owner, FactionId FactionId, bool Selected, float AlertPulse);
    public readonly record struct MinimapBuilding(Vector2 Position, Vector2 Size, Owner Owner, FactionId FactionId, bool Selected, float AlertPulse);

    public readonly record struct MinimapResource(Vector2 Position, float Radius, float RemainingRatio);

    public readonly record struct MinimapAlertPing(Vector2 Position, AlertKind Kind, float RemainingRatio);

    public readonly record struct AlertLine(AlertKind Kind, FactionId? FactionId, string Text, float RemainingRatio);

    public readonly record struct AbilityCardState(AbilitySpec Ability, float CooldownRemaining, bool IsActive);

    public readonly record struct SelectionIconItem(FactionId? FactionId, IconGlyph Glyph, string Label, int Count, Color Accent, string? UnitDesignId = null);
}
