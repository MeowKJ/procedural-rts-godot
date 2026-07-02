using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private const float SelectionHarvesterIntentMaxSize = 160f;
    private const float SelectionEconomyIntentCenterMargin = 20f;
    private const float CommandPulseDecay = 2.4f;
    private const float AlertPulseDecay = 1.2f;
    private const float DefaultAccelerationMultiplier = 4.8f;
    private const float AutoAcquireRangeMultiplier = 0.92f;
    private const float ManualAttackRangeMultiplier = 0.82f;
    private const int HarvesterCargoCapacity = 700;

    private int _nextUnitId = 1;
    private readonly EntityWorld _entityWorld = new();
    private readonly EntityCommandBuffer _inputCommands = new();
    private readonly CommandSystem _inputCommandSystem = new();
    private readonly ResourceSystem _resourceSystem = new();
    private readonly ProductionSystem _productionSystem = new();
    private readonly ConstructionSystem _constructionSystem = new();
    private readonly BuildingTargetCombatSystem _buildingTargetCombatSystem = new();
    private readonly TurretCombatSystem _turretCombatSystem = new();
    private readonly ProjectileSystem _projectileSystem = new();
    private readonly PathfindingSystem _pathfindingSystem = new();
    private readonly MovementSystem _movementSystem = new();
    private readonly SeparationSystem _separationSystem = new();
    private readonly VisionSystem _visionSystem = new();
    private readonly Dictionary<int, EntityId> _buildingTargetEntityIds = [];
    private readonly Dictionary<EntityId, int> _buildingTargetIdsByEntityId = [];
    private readonly Dictionary<int, EntityId> _resourceFieldEntityIds = [];
    private readonly Dictionary<int, int?> _lastDockedHarvesterIds = [];
    private readonly HashSet<int> _constructionEntityIdsBefore = [];
    private readonly List<UnitBattlefieldConstructionTicketSnapshot> _constructionTicketBuffer = [];
    private readonly HashSet<EntityId> _selectionEntityBuffer = [];
    private int _inputCommandTick;
    private int _nextBuildingTargetId = 1;

    public List<UnitInstance> Units { get; } = [];
    public List<ResourceFieldModel> ResourceFields { get; } = [];
    public Dictionary<PlayerSlotId, ResourceInventory> ResourceInventories { get; } = [];
    public PlayerRelationTable Relations { get; } = new();
    public EntityWorld EntityWorld => _entityWorld;
    public int AppliedInputCommandCount { get; private set; }
    public Vector2 WorldSize { get; set; } = new(3600, 2400);
    public PlayerSlotId OutcomeViewer { get; set; } = PlayerSlotId.One;
    public GameOutcome Outcome { get; private set; } = GameOutcome.InProgress;
    public event Action<IReadOnlyList<UnitInstanceDeathInfo>>? UnitsRemoved;
    public event Action<UnitInstance, UnitInstance>? UnitAttacked;
    public event Action<UnitInstance, UnitBattlefieldBuildingSnapshot>? UnitAttackedByBuilding;
    public event Action<UnitBattlefieldBuildingSnapshot, UnitInstance>? BuildingAttacked;
    public event Action<IReadOnlyList<UnitBattlefieldBuildingDeathInfo>>? BuildingsRemoved;
    public event Action<GameOutcome>? OutcomeChanged;
    public event Action<PlayerSlotId, ResourceInventory>? ResourceInventoryChanged;
    public event Action<UnitBattlefieldBuildingSnapshot, UnitProductionQueueItem>? ProductionQueued;
    public event Action<UnitBattlefieldBuildingSnapshot, UnitProductionQueueItem, UnitInstance>? ProductionCompleted;

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
        var instance = new UnitInstance
        {
            Id = _nextUnitId++,
            EntityId = entity.Id,
            Spec = spec,
            PlayerSlotId = playerSlotId,
            Position = position,
            Facing = facing,
            Velocity = Vector2.Zero,
            Hp = spec.Stats.MaxHp,
            Stance = spec.Weapons.Count > 0 ? UnitStance.Aggressive : UnitStance.Ignore,
            WeaponMounts = spec.Weapons
                .Select(mount => new WeaponMountRuntimeState(mount.MountId, mount.WeaponId, facing, 0, mount.LegacyWeaponKind))
                .ToList(),
        };

        Units.Add(instance);
        SyncUnitEntity(instance);
        return instance;
    }

    public IReadOnlyList<UnitInstance> SpawnRoster(UnitRosterProfile roster, PlayerSlotId playerSlotId, Vector2 start, Vector2 spacing)
    {
        return UnitDesignCatalog.ForRoster(roster)
            .Select((design, index) => Spawn(design.ToSpec(), playerSlotId, start + spacing * index))
            .ToList();
    }

    public void Update(double delta)
    {
        var dt = (float)delta;
        foreach (var unit in Units)
        {
            unit.CommandPulse = Mathf.Max(0, unit.CommandPulse - dt * CommandPulseDecay);
            unit.AlertPulse = Mathf.Max(0, unit.AlertPulse - dt * AlertPulseDecay);
            unit.HitPulse = Mathf.Max(0, unit.HitPulse - dt * 3.6f);
            unit.HarvestPulse = Mathf.Max(0, unit.HarvestPulse - dt * 2.8f);
            unit.AttackCooldownRemaining = Mathf.Max(0, unit.AttackCooldownRemaining - dt);
            AcquireAutoTarget(unit);
            UpdateCombat(unit, dt);
        }

        foreach (var buildingId in BuildingTargetIds())
        {
            DecayBuildingPresentationPulses(buildingId, dt);
        }

        UpdateConstructionFromEntityWorld(dt);
        UpdateBuildingTargetCombatFromEntityWorld(dt);
        UpdateBuildingCombatFromEntityWorld(dt);
        UpdateResourceHarvestersFromEntityWorld(dt);
        UpdateProductionQueues(dt);
        UpdateUnitRuntimeMotionFromEntityWorld(dt);
        RemoveDeadBuildingTargetsFromEntities();
        RemoveDeadUnits();
        _entityWorld.FlushQueuedRemovals();
        SyncBuildingTargetEntities();
    }

}
