using System.Diagnostics;
using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public const float AllyThreatShareRadius = 330;
    public const float PassiveAllyCallRadius = 115;
    public const int HarvesterCargoCapacity = 700;
    public const float PathCellSize = 96;
    private const float LocalAvoidanceCellSize = 96;
    private const float DynamicBlobCellSize = 128;
    private const int DynamicBlobMinimumUnits = 3;
    private const float DynamicBlobObstaclePadding = 18;
    private const float SlotHoldRadius = 8;
    private const float SlotInvisibleSnapRadius = 1.1f;
    private const float SlotSlowRadius = 86;
    private const float SlotAvoidanceMinimumScale = 0.08f;
    private const float SlotMinimumForwardSteering = 0.35f;
    private const float StuckRepathAfterSeconds = AdvancedPathingPolicy.StuckRepathAfterSeconds;
    private const float RepathCooldownSeconds = AdvancedPathingPolicy.RepathCooldownSeconds;
    private const float RepathProgressEpsilon = AdvancedPathingPolicy.RepathProgressEpsilon;
    private const float FireRangeSlack = 28;
    private const float EngagementRangeScale = 0.92f;
    private const float AttackSlotRepathDistance = 46;
    private const float AttackLostTrailSeconds = 2.2f;
    private const float AttackLostTrailLeadDistance = 190;
    private const float HoldThreatLinkSlack = 72;
    private const float SharedThreatMemorySeconds = 0.9f;
    private const float HarvestRate = 190;
    private const float HarvesterBoxIntentMaxSize = 180;
    private const float HarvesterBoxIntentCenterMargin = 20;
    private const float UnloadRate = 460;
    private const float ProductionRefundRatio = 0.5f;
    private readonly Stopwatch _fogUpdateStopwatch = new();
    private readonly List<PlacementObstacle> _mapObstacles = [];
    private readonly List<PlacementBuildAnchor> _legacyPlacementBuildAnchors = [];
    private readonly List<(ProductionKind Kind, UnitSpec Spec, ProductionSpec Production)> _legacyProductionSpecBuffer = [];
    private readonly List<UnitDeathInfo> _legacyUnitDeathBuffer = [];
    private readonly HashSet<int> _legacyRemovedUnitIds = [];
    private readonly List<int> _legacyRemovedBuildingIds = [];
    private readonly HashSet<int> _legacyRemovedBuildingIdSet = [];
    private readonly List<BuildingModel> _legacyRemovedBuildings = [];

    public Vector2 WorldSize { get; }
    public List<UnitModel> Units { get; } = [];
    public List<BuildingModel> Buildings { get; } = [];
    public List<ResourceFieldModel> ResourceFields { get; } = [];
    public List<ProjectileModel> Projectiles { get; } = [];
    public List<BeamModel> Beams { get; } = [];
    public List<CompletedProductionItem> CompletedProduction { get; } = [];
    public List<SignalNetworkNode> SignalNodes { get; }
    public IReadOnlyList<PlacementObstacle> MapObstacles => _mapObstacles;
    public FogQualityTier FogQuality { get; }
    public FogOfWarMap FogOfWar { get; }
    public OwnerRelationTable OwnerRelations { get; } = new();
    public double LastFogUpdateMs { get; private set; }
    public SkirmishOptions Options { get; }
    public MatchConfig MatchConfig { get; }
    public GameOutcome Outcome { get; private set; } = GameOutcome.InProgress;
    public WorldVisualThemeState VisualTheme { get; private set; } = new(
        WorldVisualTheme.DayCommand,
        WorldVisualTheme.DayCommand,
        1,
        "default");
    public Dictionary<Owner, ResourceInventory> ResourceInventories { get; } = [];
    public event Action<IReadOnlyList<UnitDeathInfo>>? UnitsRemoved;
    public event Action<UnitModel>? UnitAdded;
    public event Action<BuildingModel>? BuildingAdded;
    public event Action<IReadOnlyList<int>>? BuildingsRemoved;
    public event Action<BuildingModel, ProductionQueueItem>? ProductionQueued;
    public event Action<BuildingModel, CompletedProductionItem>? ProductionCompleted;
    public event Action<Owner, ResourceInventory>? ResourceInventoryChanged;
    public event Action<Owner, FactionId, Vector2, string>? EntityAttacked;
    public event Action<GameOutcome>? OutcomeChanged;
    public event Action<WorldVisualThemeState>? VisualThemeChanged;
    public event Action? SignalNetworkChanged;

    private int _nextId = 1;
    private int _nextBuildingId = 1;
    private int _nextProjectileId = 1;
    private int _nextBeamId = 1;
    private int _nextProductionId = 1;
    private int _nextResourceFieldId = 1;
    private float _fogRefreshTimer;

    public static IReadOnlyDictionary<WeaponKind, WeaponDefinition> WeaponDefinitions => WeaponCatalog.Weapons;
    public static IReadOnlyDictionary<AmmoKind, AmmoDefinition> AmmoDefinitions => WeaponCatalog.Ammo;

    public static bool IsHarvesterUnit(UnitModel unit)
    {
        return IsHarvesterSpec(unit.Spec);
    }

    private static bool IsHarvesterSpec(UnitSpec spec)
    {
        return spec.RoleTags.Contains(UnitRoleTag.Economy)
            && spec.Abilities.Any(ability => ability.Kind == AbilityKind.Harvest);
    }

    public GameState()
        : this(MatchConfig.Default)
    {
    }

    public GameState(SkirmishOptions options, FogQualityTier fogQuality = FogQualityTier.Medium)
        : this(options.ToMatchConfig(), fogQuality)
    {
    }

    public GameState(MatchConfig matchConfig, FogQualityTier fogQuality = FogQualityTier.Medium)
    {
        MatchConfig = matchConfig;
        Options = matchConfig.ToSkirmishOptions();
        WorldSize = matchConfig.WorldSize;
        FogQuality = fogQuality;
        FogOfWar = new FogOfWarMap(fogQuality);
        SignalNodes = SignalNetworkMath.CreateDefaultNetwork(WorldSize).ToList();
        ResourceInventories[Owner.Player] = new ResourceInventory { Credits = matchConfig.StartingCredits };
        ResourceInventories[Owner.Enemy] = new ResourceInventory { Credits = matchConfig.StartingCredits };
        Seed();
        if (matchConfig.LaunchMode == LaunchMode.Sandbox)
        {
            ConfigureDeveloperSandbox();
        }

        UpdateFogOfWar();
    }

    public void Update(double delta)
    {
        var dt = (float)Math.Min(delta, 0.05);
        UpdateVisualThemeTransition(dt);
        var localAvoidance = BuildLocalAvoidanceHash();
        foreach (var unit in Units)
        {
            unit.AttackCooldownRemaining = Mathf.Max(0, unit.AttackCooldownRemaining - dt);
            unit.ThreatShareCooldownRemaining = Mathf.Max(0, unit.ThreatShareCooldownRemaining - dt);
            unit.RepathCooldownRemaining = Mathf.Max(0, unit.RepathCooldownRemaining - dt);
            unit.HitPulse = Mathf.Max(0, unit.HitPulse - dt * 3.8f);
            unit.AlertPulse = Mathf.Max(0, unit.AlertPulse - dt * 2.2f);
            unit.HarvestPulse = Mathf.Max(0, unit.HarvestPulse - dt * 2.8f);
            UpdateHarvester(unit, dt);
            AcquireAutoTarget(unit);
            UpdateMovement(unit, dt, localAvoidance);
            unit.CommandPulse = Mathf.Max(0, unit.CommandPulse - dt * 2.6f);
        }

        foreach (var building in Buildings)
        {
            building.AttackCooldownRemaining = Mathf.Max(0, building.AttackCooldownRemaining - dt);
            building.HitPulse = Mathf.Max(0, building.HitPulse - dt * 3.2f);
            building.RallyPulse = Mathf.Max(0, building.RallyPulse - dt * 2.4f);
            building.DeliveryPulse = Mathf.Max(0, building.DeliveryPulse - dt * 2.9f);
            AcquireBuildingAutoTarget(building);
            UpdateBuildingCombat(building);
        }

        foreach (var field in ResourceFields)
        {
            field.Pulse = Mathf.Max(0, field.Pulse - dt * 1.8f);
        }

        UpdateProductionQueues(dt);

        foreach (var unit in Units)
        {
            UpdateCombat(unit);
        }

        UpdateProjectiles(dt);
        UpdateBeams(dt);
        RemoveDeadUnits();
        RemoveDeadBuildings();
        _fogRefreshTimer -= dt;
        if (_fogRefreshTimer <= 0)
        {
            _fogRefreshTimer = FogOfWarVisualPolicy.WorldRedrawIntervalFor(FogQuality);
            UpdateFogOfWar();
        }
    }

    public void UpdateWorldOnly(double delta, IEnumerable<(Vector2 Position, float SightRange)>? unitVisionSources = null)
    {
        var dt = (float)Math.Min(delta, 0.05);
        UpdateVisualThemeTransition(dt);

        foreach (var building in Buildings)
        {
            building.HitPulse = Mathf.Max(0, building.HitPulse - dt * 3.2f);
            building.RallyPulse = Mathf.Max(0, building.RallyPulse - dt * 2.4f);
            building.DeliveryPulse = Mathf.Max(0, building.DeliveryPulse - dt * 2.9f);
        }

        foreach (var field in ResourceFields)
        {
            field.Pulse = Mathf.Max(0, field.Pulse - dt * 1.8f);
        }

        _fogRefreshTimer -= dt;
        if (_fogRefreshTimer <= 0)
        {
            _fogRefreshTimer = FogOfWarVisualPolicy.WorldRedrawIntervalFor(FogQuality);
            UpdateFogOfWar(unitVisionSources, includeLegacyUnitSources: unitVisionSources is null);
        }
    }

}
