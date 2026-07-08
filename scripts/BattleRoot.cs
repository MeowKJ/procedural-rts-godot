using System.Diagnostics;
using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public readonly record struct ActiveBattlePerformanceDebugSnapshot(
    int LiveUnitCount,
    int VisibleUnitCount,
    int PlayerBuildingCount,
    int EnemyBuildingCount,
    int PlayerCommandedUnitCount,
    int EnemyCommandedUnitCount,
    int FogTextureUploads,
    double LastFogUpdateMs);

public partial class BattleRoot : Node2D
{
    private const float AlertLifetime = 8.5f;
    private const float CombatAlertCooldown = 2.6f;
    private const float IdleHarvesterAlertCooldown = 11f;
    private const float ProductionAlertCooldown = 1.1f;
    private const float InsufficientCreditsAlertCooldown = 1.2f;
    internal const int ShiftProductionBatchCount = 5;
    private const float MinimapRefreshInterval = 0.2f;
    private const float ViewCullingInterval = 0.05f;
    private const float ViewCullingMargin = 320f;
    private static readonly Vector2 SandboxLaunchFocus = new(980, 1180);
    private static readonly Color HudMint = new("#8fffe1");
    private static readonly string[] DogActiveBattlePerfDesigns =
    [
        "dog.infantry",
        "dog.rocket",
        "dog.guard_tank",
        "dog.patrol_vehicle",
        "dog.assault_tank",
        "dog.sky_patrol_aircraft",
        "dog.siege_artillery",
    ];
    private static readonly string[] CatActiveBattlePerfDesigns =
    [
        "cat.basic",
        "cat.rocket",
        "cat.scout_car",
        "cat.tank",
        "cat.sniper",
        "cat.scout_aircraft",
        "cat.crescent_artillery",
    ];

    private readonly GameState _state = new(SkirmishSetupState.PendingOptions);
    private readonly UnitBattlefield _unitBattlefield = new();
    // Fixed-tick EntityWorld core for deterministic gameplay systems and live
    // presentation projections.
    private readonly SimClock _simClock = new();
    private readonly EntityWorld _entityWorld = new();
    private readonly EntityCommandBuffer _entityCommands = new();
    private readonly SimEventSink _presentationEvents = new();
    private readonly List<SimEvent> _simEventDrainBuffer = [];
    private readonly List<UnitInstance> _selectedUnitInstanceBuffer = [];
    private readonly List<UnitModel> _selectedLegacyUnitBuffer = [];
    private readonly List<BuildingModel> _selectedLegacyBuildingBuffer = [];
    private readonly List<HudLayer.AbilityCardState> _selectedAbilityCardBuffer = [];
    private readonly List<int> _selectedProductionBuildingIdBuffer = [];
    private readonly List<UnitInstance> _sandboxLaunchUnitBuffer = [];
    private readonly List<int> _sandboxLaunchUnitIdBuffer = [];
    private readonly List<int> _debugPlayerAttackerIds = [];
    private readonly List<int> _debugEnemyAttackerIds = [];
    private readonly PresentationMetrics _presentationMetrics = new();
    private readonly Stopwatch _processStopwatch = new();
    private readonly Stopwatch _simStepStopwatch = new();
    private readonly Dictionary<int, BuildingView> _buildingViews = [];
    private readonly Dictionary<int, UnitView> _unitViews = [];
    private readonly Dictionary<int, UnitInstanceView> _unitInstanceViews = [];
    private readonly Dictionary<int, ResourceFieldView> _resourceViews = [];
    private readonly List<AlertEntry> _alerts = [];
    private readonly Dictionary<string, float> _alertCooldowns = [];
    private GridLayer _grid = null!;
    private Node2D _buildingRoot = null!;
    private CameraController _camera = null!;
    private BuildPlacementController _buildPlacement = null!;
    private SelectionController _selection = null!;
    private UnitBodyBatchLayer _unitBodyBatchLayer = null!;
    private Node2D _unitInstanceRoot = null!;
    private CombatEffectsLayer _combatEffects = null!;
    private CommandAcknowledgementLayer _commandAcknowledgements = null!;
    private PathDebugLayer _pathDebug = null!;
    private FootprintLayer _footprints = null!;
    private FogOfWarLayer _fogOfWar = null!;
    private ControlGroupController _controlGroups = null!;
    private HudLayer _hud = null!;
    private PerfHudLayer _perfHud = null!;
    private PauseMenuLayer _pauseMenu = null!;
    private OutcomeScreenLayer _outcomeScreen = null!;
    private TacticalAudioLayer _audio = null!;
    private float _elapsed;
    private float _minimapRefreshTimer;
    private float _viewCullingTimer;
    private float _idleHarvesterAlertAt = -IdleHarvesterAlertCooldown;
    private float _sandboxTimeScale = SandboxTimeScaleMath.DefaultScale;
    private SandboxDeveloperContext _sandboxContext = SandboxDeveloperContext.Default;
    private int _sandboxStressRunIndex;
    private bool _powerStable = true;
    private bool _syncingResourceInventories;
    private bool _debugActiveBattlePerfScenarioConfigured;
    private int _debugPlayerCommandedUnitCount;
    private int _debugEnemyCommandedUnitCount;
    private GameOutcome _displayedOutcome = GameOutcome.InProgress;
    private static bool RunEntityWorldShadow => System.Environment.GetEnvironmentVariable("PROCEDURAL_RTS_DISABLE_ENTITY_SHADOW") != "1"
        && System.Environment.GetEnvironmentVariable("PROCEDURAL_RTS_ENTITY_SHADOW") != "0";
    private static bool UseUnitDesignRuntime => true;
    public GameState State => _state;
    public PresentationMetrics PresentationMetrics => _presentationMetrics;
    public int DebugSimClockTick => _simClock.CurrentTick;

    public void DebugClearPresentationMetrics()
    {
        _presentationMetrics.Clear();
    }

    public PerfHudCounts DebugPerfHudCounts()
    {
        return PerfHudCounts();
    }

    public ActiveBattlePerformanceDebugSnapshot DebugConfigureActiveBattlePerformanceScenario()
    {
        if (!_debugActiveBattlePerfScenarioConfigured)
        {
            var focus = new Vector2(_state.WorldSize.X * 0.5f, _state.WorldSize.Y * 0.48f);
            var playerFaction = ToUnitFaction(_state.Options.PlayerFaction);
            var enemyFaction = ToUnitFaction(_state.Options.AiFaction);
            var playerWave = DebugSpawnActiveBattlePerfUnits(
                PlayerSlotId.One,
                playerFaction,
                focus + new Vector2(-150, -20),
                facing: 0,
                count: 24);
            var enemyWave = DebugSpawnActiveBattlePerfUnits(
                PlayerSlotId.Two,
                enemyFaction,
                focus + new Vector2(150, 20),
                facing: Mathf.Pi,
                count: 24);

            CollectActiveBattlePerfAttackers(PlayerSlotId.One, _debugPlayerAttackerIds);
            CollectActiveBattlePerfAttackers(PlayerSlotId.Two, _debugEnemyAttackerIds);
            var playerTarget = enemyWave.FirstOrDefault(unit => unit.Hp > 0)
                ?? _unitBattlefield.Units.First(unit => unit.PlayerSlotId == PlayerSlotId.Two && unit.Hp > 0);
            var enemyTarget = playerWave.FirstOrDefault(unit => unit.Hp > 0)
                ?? _unitBattlefield.Units.First(unit => unit.PlayerSlotId == PlayerSlotId.One && unit.Hp > 0);

            _debugPlayerCommandedUnitCount = _unitBattlefield.CommandAttackUnits(PlayerSlotId.One, _debugPlayerAttackerIds, playerTarget);
            _debugEnemyCommandedUnitCount = _unitBattlefield.CommandAttackUnits(PlayerSlotId.Two, _debugEnemyAttackerIds, enemyTarget);
            _camera.InputEnabled = false;
            _camera.SnapToWorldPoint(focus);
            _debugActiveBattlePerfScenarioConfigured = true;
            RefreshViewCulling();
            RefreshMinimap();
        }

        return DebugActiveBattlePerformanceSnapshot();
    }

    public ActiveBattlePerformanceDebugSnapshot DebugActiveBattlePerformanceSnapshot()
    {
        var counts = PerfHudCounts();
        return new ActiveBattlePerformanceDebugSnapshot(
            counts.LiveUnitCount,
            counts.VisibleUnitCount,
            _unitBattlefield.LiveBuildingCount(PlayerSlotId.One),
            _unitBattlefield.LiveBuildingCount(PlayerSlotId.Two),
            _debugPlayerCommandedUnitCount,
            _debugEnemyCommandedUnitCount,
            counts.FogTextureUploads,
            counts.LastFogUpdateMs);
    }

    private void CollectActiveBattlePerfAttackers(PlayerSlotId playerSlotId, List<int> result)
    {
        result.Clear();
        foreach (var unit in _unitBattlefield.Units)
        {
            if (unit.PlayerSlotId == playerSlotId
                && unit.Hp > 0
                && unit.WeaponMounts.Count > 0)
            {
                result.Add(unit.Id);
            }
        }
    }

    public IReadOnlyList<string> DebugUnitBattlefieldDesignIds(PlayerSlotId playerSlotId)
    {
        var count = 0;
        foreach (var unit in _unitBattlefield.Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Hp > 0)
            {
                count++;
            }
        }

        var designIds = new string[count];
        var index = 0;
        foreach (var unit in _unitBattlefield.Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Hp > 0)
            {
                designIds[index++] = unit.Spec.Id;
            }
        }

        return designIds;
    }

}
