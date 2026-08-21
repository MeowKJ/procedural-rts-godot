using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private const float SelectionHarvesterIntentMaxSize = 160f;
    private const float SelectionEconomyIntentCenterMargin = 20f;
    private const float CommandPulseDecay = 2.4f;
    private const float AlertPulseDecay = 1.2f;
    private const int HarvesterCargoCapacity = 700;

    private int _nextUnitId = 1;
    private readonly EntityWorld _entityWorld;
    private readonly EntityCommandBuffer _inputCommands = new();
    private readonly CommandSystem _inputCommandSystem = new();
    private readonly AbilitySystem _abilitySystem = new();
    private readonly CombatSystem _combatSystem = new();
    private readonly ResourceSystem _resourceSystem = new();
    private readonly ProductionSystem _productionSystem = new();
    private readonly ConstructionSystem _constructionSystem = new();
    private readonly ProjectileSystem _projectileSystem = new();
    private readonly PathfindingSystem _pathfindingSystem = new();
    private readonly MovementSystem _movementSystem = new();
    private readonly SeparationSystem _separationSystem = new();
    private readonly VisionSystem _visionSystem = new();
    private readonly SimClock _simulationClock = new();
    private readonly Dictionary<int, EntityId> _buildingTargetEntityIds = [];
    private readonly Dictionary<EntityId, int> _buildingTargetIdsByEntityId = [];
    private readonly List<UnitBattlefieldResourceNodeProjection> _resourceNodeProjectionBuffer = [];
    private readonly Dictionary<int, int?> _lastDockedHarvesterIds = [];
    private readonly HashSet<int> _constructionEntityIdsBefore = [];
    private readonly List<UnitBattlefieldConstructionTicketSnapshot> _constructionTicketBuffer = [];
    private readonly List<int> _constructionSubjectBuildingIds = [];
    private readonly List<EntityId> _constructionSubjectEntityBuffer = [];
    private readonly List<PlayerSlotId> _ownerRelationSlots = [];
    private readonly Dictionary<PlayerSlotId, int> _resourceCreditsBefore = [];
    private readonly List<int> _resourceCreditOwnerIds = [];
    private readonly HashSet<EntityId> _selectionEntityBuffer = [];
    private readonly HashSet<EntityId> _selectionRectCandidateBuffer = [];
    private readonly List<UnitInstance> _selectionRectEconomyUnits = [];
    private readonly List<UnitInstance> _selectionRectCombatUnits = [];
    private readonly List<EntityId> _selectionCommandEntityBuffer = [];
    private readonly List<UnitInstance> _selectionUnitBuffer = [];
    private readonly List<int> _selectedBuildingRallyProducerIds = [];
    private readonly List<int> _buildingTargetIdBuffer = [];
    private readonly List<int> _buildingTargetIdSecondaryBuffer = [];
    private readonly List<int> _buildingProjectionTargetIdBuffer = [];
    private readonly List<int> _buildingVisibilityViewerIdBuffer = [];
    private readonly List<int> _buildingVisibilityTargetIdBuffer = [];
    private readonly List<UnitBattlefieldBuildingSnapshot> _buildingSnapshotBuffer = [];
    private readonly List<BuildingRallyProjection> _buildingRallyProjectionBuffer = [];
    private readonly List<BuildingSelectionProjection> _buildingSelectionProjectionBuffer = [];
    private readonly List<EntityId> _selectedBuildingEntityIdBuffer = [];
    private readonly List<BuildingHitPulseProjection> _buildingHitPulseProjectionBuffer = [];
    private readonly List<BuildingMinimapProjection> _buildingMinimapProjectionBuffer = [];
    private readonly List<BuildingMinimapProjection> _buildingMinimapProjectionSecondaryBuffer = [];
    private readonly List<EntityProjection> _unitProjectionBuffer = [];
    private readonly List<UnitBattlefieldVisionSource> _visionSourceBuffer = [];
    private readonly List<UnitBattlefieldResourcePip> _resourcePipBuffer = [];
    private readonly List<UnitBattlefieldResourcePip> _resourcePipSecondaryBuffer = [];
    private readonly List<RepairOrderProjection> _repairOrderProjectionBuffer = [];
    private readonly List<UnitMinimapPip> _unitMinimapPipBuffer = [];
    private readonly List<UnitMinimapPip> _unitMinimapPipSecondaryBuffer = [];
    private readonly List<UnitSelectionSummaryItem> _selectionSummaryBuffer = [];
    private readonly List<int> _productionCandidateProducerIds = [];
    private readonly HashSet<int> _selectionUnitIdBuffer = [];
    private readonly List<UnitInstanceDeathInfo> _unitDeathBuffer = [];
    private readonly List<UnitBattlefieldBuildingDeathInfo> _buildingDeathBuffer = [];
    private readonly List<int> _deadBuildingIdBuffer = [];
    private readonly HashSet<int> _combatDamagedBuildingIds = [];
    private readonly HashSet<int> _combatDestroyedBuildingIds = [];
    private readonly HashSet<int> _combatDeadBuildingIds = [];
    private readonly List<SimEvent> _simEventDrainBuffer = [];
    private readonly HashSet<int> _removedUnitIdBuffer = [];
    private readonly HashSet<int> _removedBuildingIdBuffer = [];
    private readonly List<int> _productionActiveProducerIds = [];
    private readonly HashSet<int> _productionBuildingIdSeen = [];
    private readonly HashSet<int> _productionKnownEntityIds = [];
    private readonly List<ProductionCompletionCandidate> _productionCompletionCandidates = [];
    private readonly List<EntityInstance> _productionNewUnitEntities = [];
    private readonly HashSet<int> _productionQueueSummarySeenIds = [];
    private readonly List<ProductionQueueSummaryEntry> _productionQueueSummaryBuffer = [];
    private readonly List<UnitSpec> _productionDesignSpecBuffer = [];
    private readonly List<ProductionOptionState> _designProductionOptionStateBuffer = [];
    private readonly List<BuildOptionSnapshot> _buildOptionSnapshotBuffer = [];
    private readonly HashSet<string> _readyBuildingKinds = [];
    private readonly List<ProductionProviderLaneState> _productionProviderLaneStateBuffer = [];
    private readonly List<ProductionProviderLaneState> _specificProductionProviderLaneBuffer = [];
    private readonly Dictionary<string, int> _productionProviderLaneKindCounts = [];
    private readonly List<ProductionProviderLaneState> _constructionProviderLaneStateBuffer = [];
    private readonly List<ProductionProviderLaneState> _specificConstructionProviderLaneBuffer = [];
    private readonly Dictionary<string, int> _constructionProviderLaneKindCounts = [];
    private readonly HashSet<string> _constructionProviderKinds = [];
    private readonly List<int> _selectedProductionProducerIdBuffer = [];
    private readonly List<UnitInstance> _units = [];
    private Vector2 _worldSize = new(3600, 2400);
    private int _inputCommandTick;
    private int _nextBuildingTargetId = 1;
    private bool _useSecondaryBuildingMinimapProjectionBuffer;
    private bool _useSecondaryResourcePipBuffer;
    private bool _useSecondaryUnitMinimapPipBuffer;

    public IReadOnlyList<UnitInstance> Units => _units;
    public PlayerRelationTable Relations { get; } = new();
    public EntityWorld EntityWorld => _entityWorld;
    public int SimulationTick => _simulationClock.CurrentTick;
    public int LastDroppedSimulationTicks => _simulationClock.LastDroppedBacklogTicks;
    public double LastDroppedSimulationSeconds => _simulationClock.LastDroppedBacklogSeconds;
    public int AppliedInputCommandCount { get; private set; }
    public Vector2 WorldSize
    {
        get => _worldSize;
        set
        {
            _worldSize = value;
            _entityWorld.WorldWidth = value.X;
            _entityWorld.WorldHeight = value.Y;
        }
    }
    public PlayerSlotId OutcomeViewer { get; set; } = PlayerSlotId.One;
    public GameOutcome Outcome { get; private set; } = GameOutcome.InProgress;
    public event Action<IReadOnlyList<UnitInstanceDeathInfo>>? UnitsRemoved;
    public event Action<WeaponFiredEvent>? WeaponFired;
    public event Action<ProjectileImpactEvent>? ProjectileImpacted;
    public event Action<UnitInstance, UnitInstance>? UnitAttacked;
    public event Action<UnitInstance, UnitBattlefieldBuildingSnapshot>? UnitAttackedByBuilding;
    public event Action<UnitBattlefieldBuildingSnapshot, UnitInstance>? BuildingAttacked;
    public event Action<IReadOnlyList<UnitBattlefieldBuildingDeathInfo>>? BuildingsRemoved;
    public event Action<GameOutcome>? OutcomeChanged;
    public event Action<PlayerSlotId, ResourceInventory>? ResourceInventoryChanged;
    public event Action<UnitBattlefieldBuildingSnapshot, UnitProductionQueueItem>? ProductionQueued;
    public event Action<UnitBattlefieldBuildingSnapshot, UnitProductionQueueItem, UnitInstance>? ProductionCompleted;

    public UnitBattlefield()
        : this(new EntityWorld())
    {
    }

    private UnitBattlefield(EntityWorld entityWorld)
    {
        _entityWorld = entityWorld ?? throw new ArgumentNullException(nameof(entityWorld));
        _worldSize = new Vector2(entityWorld.WorldWidth, entityWorld.WorldHeight);
    }

    public UnitInstance Spawn<TDesign>(PlayerSlotId playerSlotId, Vector2 position, float facing = 0)
        where TDesign : UnitDesign, new()
    {
        return Spawn(UnitDesignCatalog.Spec<TDesign>(), playerSlotId, position, facing);
    }

    public UnitInstance Spawn(string designId, PlayerSlotId playerSlotId, Vector2 position, float facing = 0)
    {
        return Spawn(UnitDesignCatalog.Spec(designId), playerSlotId, position, facing);
    }

    public UnitInstance Spawn(UnitSpec spec, PlayerSlotId playerSlotId, Vector2 position, float facing = 0)
    {
        var entity = _entityWorld.SpawnUnit(spec, OwnerId.FromPlayerSlot(playerSlotId), position, facing);
        return AdoptUnitEntity(entity);
    }

    public IReadOnlyList<UnitInstance> SpawnRoster(UnitRosterProfile roster, PlayerSlotId playerSlotId, Vector2 start, Vector2 spacing, float facing = 0)
    {
        var designs = UnitDesignCatalog.ForRoster(roster);
        var units = new List<UnitInstance>(designs.Count);
        for (var index = 0; index < designs.Count; index++)
        {
            units.Add(Spawn(designs[index].ToSpec(), playerSlotId, start + spacing * index, facing));
        }

        return units;
    }

    public void Update(double delta)
    {
        StepSimulation((float)delta);
    }

    /// <summary>
    /// Advances the live battlefield from a render/frame delta using the
    /// battlefield-owned fixed clock. The authoritative EntityWorld is the
    /// only runtime world stepped by normal gameplay.
    /// </summary>
    public int AdvanceSimulation(double realDelta)
    {
        var ticks = _simulationClock.Advance(realDelta);
        for (var index = 0; index < ticks; index++)
        {
            StepSimulation(_simulationClock.FixedDelta);
        }

        return ticks;
    }

    private void StepSimulation(float dt)
    {
        DecayUnitPresentationPulses(dt);

        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            DecayBuildingPresentationPulses(buildingId, dt);
        }

        UpdateConstructionFromEntityWorld(dt);
        UpdateAbilitiesFromEntityWorld(dt);
        RebuildVisibilityIndex();
        UpdateCombatFromEntityWorld(dt);
        UpdateProjectilesFromEntityWorld(dt);
        UpdateResourceHarvestersFromEntityWorld(dt);
        UpdateProductionQueues(dt);
        UpdateUnitRuntimeMotionFromEntityWorld(dt);
        RemoveDeadBuildingTargetsFromEntities();
        RemoveDeadUnits();
        _entityWorld.FlushQueuedRemovals();
        RefreshUnitProjections();
    }

}
