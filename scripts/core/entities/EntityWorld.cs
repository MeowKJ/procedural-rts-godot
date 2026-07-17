using System.Diagnostics;

namespace ProceduralRts.Core;

public sealed partial class EntityWorld
{
    private readonly SortedDictionary<int, EntityInstance> _entities = [];
    private readonly List<EntityInstance> _orderedEntities = [];
    private readonly SortedDictionary<string, EntitySpec> _specs = [];
    private readonly List<ISimSystem> _systems = [];
    private readonly List<PendingSpawn> _pendingSpawns = [];
    private readonly SortedSet<int> _pendingRemovals = [];
    private readonly List<EntityComponentState> _stateHashComponentValues = [];
    private readonly List<AbilityCooldownState> _stateHashAbilityCooldownValues = [];
    private readonly List<WeaponMountRuntimeState> _stateHashWeaponMountValues = [];
    private readonly List<UnitProductionQueueItem> _stateHashProductionQueueItems = [];
    private readonly List<EntityCommand> _stateHashCommandQueueItems = [];
    private readonly List<EntityId> _stateHashCommandSubjectIds = [];
    private readonly Stopwatch _systemStopwatch = new();
    private int _nextEntityId = 1;
    private int _nextProductionItemId = 1;

    public EntityWorld(ulong seed = 0)
    {
        Rng = new DeterministicRng(seed);
        RegisterCombatDefinitions(WeaponCatalog.WeaponDefinitions.Values, WeaponCatalog.AmmoDefinitions.Values);
    }

    /// <summary>Self-owned deterministic random source for gameplay systems.</summary>
    public DeterministicRng Rng { get; }

    /// <summary>Runtime hostility lookup; the only authority for "can A target B".</summary>
    public OwnerRelationTable Relations { get; } = new();

    /// <summary>Per-tick simulation events for presentation to drain.</summary>
    public SimEventSink Events { get; } = new();

    /// <summary>World bounds used by formation/clamp math. Set by the driver.</summary>
    public float WorldWidth { get; set; } = 3600;
    public float WorldHeight { get; set; } = 2400;

    /// <summary>Per-owner gameplay visibility, recomputed each tick by VisionSystem.</summary>
    public VisibilityIndex Visibility { get; } = new();

    /// <summary>Read-only quality metrics, fed from the event stream by the driver.</summary>
    public SimMetrics Metrics { get; } = new();

    /// <summary>Data-only economy knobs read by pure simulation systems.</summary>
    public EconomyTuningConfig EconomyTuning { get; set; } = EconomyTuningConfig.Default;

    /// <summary>Pure economy atmosphere hook for scenario/day-night tuning.</summary>
    public ResourceAtmosphere ResourceAtmosphere { get; set; } = ResourceAtmosphere.Day;

    /// <summary>Match outcome from the owner's perspective, set by OutcomeSystem.</summary>
    public GameOutcome Outcome { get; set; } = GameOutcome.InProgress;

    /// <summary>Banked economic resources owned by the deterministic simulation.</summary>
    public SortedDictionary<int, ResourceInventory> ResourceInventories { get; } = [];

    /// <summary>Per-owner match-time upgrades. Specs stay immutable; systems read derived values through UpgradeResolver.</summary>
    public SortedDictionary<int, UpgradeState> UpgradeStates { get; } = [];

    /// <summary>
    /// Debug-only system timing. Kept off by default so deterministic sim hot paths
    /// do not pay stopwatch cost unless a profiling run opts in.
    /// </summary>
    public bool SystemTimingEnabled { get; set; } = System.Environment.GetEnvironmentVariable("PROCEDURAL_RTS_SIM_TIMING") == "1";

    /// <summary>
    /// Optional debug guard for malformed sim state. Off by default; enable with
    /// PROCEDURAL_RTS_SIM_INVARIANTS=1 in engine/debug runs or set directly in tests.
    /// </summary>
    public bool SimInvariantsEnabled { get; set; } = System.Environment.GetEnvironmentVariable(SimInvariants.EnvironmentToggle) == "1";

    public IReadOnlyCollection<EntityInstance> StableEntities => _orderedEntities;
    public IReadOnlyCollection<EntitySpec> StableSpecs => _specs.Values;
    public int Count => _entities.Count;

    /// <summary>
    /// Entities in stable <see cref="EntityId"/> order without allocating a copy.
    /// Systems iterate this. Safe to read while mutating component state or
    /// transforms in place; do not add or remove entities during iteration.
    /// </summary>
    public IReadOnlyList<EntityInstance> OrderedEntities => _orderedEntities;

    /// <summary>Registered systems, run in registration order each tick.</summary>
    public void AddSystem(ISimSystem system)
    {
        _systems.Add(system);
    }

    /// <summary>
    /// Advance the authoritative world by one fixed tick: apply the commands due
    /// this tick, then run every registered system in order. The driver is
    /// responsible for draining commands from the command buffer in stable order.
    /// </summary>
    public void Step(int tick, float fixedDelta, IReadOnlyList<SequencedCommandEnvelope> commands)
    {
        var context = new SimContext(this, tick, fixedDelta, commands);
        foreach (var system in _systems)
        {
            if (!SystemTimingEnabled)
            {
                system.Step(context);
                FlushQueuedSpawns();
                continue;
            }

            _systemStopwatch.Restart();
            system.Step(context);
            _systemStopwatch.Stop();
            FlushQueuedSpawns();
            Metrics.RecordSystemStep(system.GetType().Name, _systemStopwatch.Elapsed.TotalMilliseconds);
        }

        FlushQueuedRemovals();

        if (SimInvariantsEnabled)
        {
            SimInvariants.AssertValid(this, tick);
        }
    }

    /// <summary>
    /// Queue an entity for removal after the current tick's systems finish, so a
    /// system never mutates the entity collection while another iterates it.
    /// Removal order is stable (ascending EntityId).
    /// </summary>
    public void QueueRemoval(EntityId id)
    {
        _pendingRemovals.Add(id.Value);
    }

    public void FlushQueuedSpawns()
    {
        if (_pendingSpawns.Count == 0)
        {
            return;
        }

        foreach (var pending in _pendingSpawns)
        {
            SpawnNow(pending.Spec, pending.OwnerId, pending.Transform, pending.Components);
        }

        _pendingSpawns.Clear();
    }

    public void FlushQueuedRemovals()
    {
        if (_pendingRemovals.Count == 0)
        {
            return;
        }

        foreach (var id in _pendingRemovals)
        {
            RemoveEntityNow(id);
        }

        _pendingRemovals.Clear();
    }

    public void RegisterSpec(EntitySpec spec)
    {
        _specs[spec.Id] = spec;
    }

    public bool TryGetSpec(string specId, out EntitySpec spec)
    {
        return _specs.TryGetValue(specId, out spec!);
    }

    public EntityInstance Spawn(
        EntitySpec spec,
        OwnerId ownerId,
        EntityTransform transform,
        IEnumerable<EntityComponentState>? initialComponents = null)
    {
        RegisterSpec(spec);
        return SpawnNow(spec, ownerId, transform, initialComponents);
    }

    public void QueueSpawn(
        EntitySpec spec,
        OwnerId ownerId,
        EntityTransform transform,
        IEnumerable<EntityComponentState>? initialComponents = null)
    {
        RegisterSpec(spec);
        _pendingSpawns.Add(new PendingSpawn(
            spec,
            ownerId,
            transform,
            initialComponents?.ToArray() ?? []));
    }

    private EntityInstance SpawnNow(
        EntitySpec spec,
        OwnerId ownerId,
        EntityTransform transform,
        IEnumerable<EntityComponentState>? initialComponents)
    {
        var entity = new EntityInstance
        {
            Id = new EntityId(_nextEntityId++),
            SpecId = spec.Id,
            OwnerId = ownerId,
            Transform = transform,
        };

        if (_orderedEntities.Count > 0 && _orderedEntities[^1].Id.Value >= entity.Id.Value)
        {
            throw new InvalidOperationException("Entity IDs must remain strictly increasing in the ordered membership index.");
        }

        if (initialComponents is not null)
        {
            foreach (var component in initialComponents)
            {
                entity.Components.Set(component);
            }
        }

        _entities.Add(entity.Id.Value, entity);
        _orderedEntities.Add(entity);
        return entity;
    }

    public bool TryGet(EntityId id, out EntityInstance entity)
    {
        return _entities.TryGetValue(id.Value, out entity!);
    }

    public bool Remove(EntityId id)
    {
        return RemoveEntityNow(id.Value);
    }

    private bool RemoveEntityNow(int id)
    {
        if (!_entities.TryGetValue(id, out var entity))
        {
            return false;
        }

        var low = 0;
        var high = _orderedEntities.Count - 1;
        var foundIndex = -1;
        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var middleId = _orderedEntities[middle].Id.Value;
            if (middleId == id)
            {
                foundIndex = middle;
                break;
            }

            if (middleId < id)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        if (foundIndex < 0 || !ReferenceEquals(_orderedEntities[foundIndex], entity))
        {
            throw new InvalidOperationException($"Entity {id} is missing or divergent in the ordered membership index.");
        }

        if (!_entities.Remove(id))
        {
            throw new InvalidOperationException($"Entity {id} disappeared during ordered membership removal.");
        }

        _orderedEntities.RemoveAt(foundIndex);
        return true;
    }

    public bool ChangeOwner(EntityId id, OwnerId ownerId)
    {
        if (!_entities.TryGetValue(id.Value, out var entity))
        {
            return false;
        }

        entity.OwnerId = ownerId;
        return true;
    }

    public ResourceInventory ResourceInventory(OwnerId ownerId)
    {
        if (!ResourceInventories.TryGetValue(ownerId.Value, out var inventory))
        {
            inventory = new ResourceInventory { Credits = 0 };
            ResourceInventories[ownerId.Value] = inventory;
        }

        return inventory;
    }

    public UpgradeState Upgrades(OwnerId ownerId)
    {
        if (!UpgradeStates.TryGetValue(ownerId.Value, out var state))
        {
            state = new UpgradeState();
            UpgradeStates[ownerId.Value] = state;
        }

        return state;
    }

    public int AllocateProductionItemId()
    {
        return _nextProductionItemId++;
    }

    public ulong DeterministicStateHash()
    {
        var hash = EntityStateHash.Begin();

        // Fold RNG state in first so any divergence in random consumption is
        // caught immediately, not just when it later affects an entity.
        hash = EntityStateHash.Add(hash, Rng.State);
        hash = EntityStateHash.Add(hash, _nextEntityId);
        hash = EntityStateHash.Add(hash, _nextProductionItemId);
        hash = EntityStateHash.Add(hash, WorldWidth);
        hash = EntityStateHash.Add(hash, WorldHeight);
        hash = EntityStateHash.Add(hash, MapEnvironment.WorldSize.Width);
        hash = EntityStateHash.Add(hash, MapEnvironment.WorldSize.Height);
        hash = EntityStateHash.Add(hash, MapEnvironment.TerrainCells.Count);
        for (var terrainIndex = 0; terrainIndex < MapEnvironment.TerrainCells.Count; terrainIndex++)
        {
            var terrain = MapEnvironment.TerrainCells[terrainIndex];
            hash = EntityStateHash.Add(hash, terrain.Id);
            hash = EntityStateHash.Add(hash, terrain.Bounds.X);
            hash = EntityStateHash.Add(hash, terrain.Bounds.Y);
            hash = EntityStateHash.Add(hash, terrain.Bounds.Width);
            hash = EntityStateHash.Add(hash, terrain.Bounds.Height);
            hash = EntityStateHash.Add(hash, terrain.TerrainId);
            hash = EntityStateHash.Add(hash, terrain.MovementCost);
            hash = EntityStateHash.Add(hash, terrain.BlocksLand ? 1 : 0);
        }

        hash = EntityStateHash.Add(hash, MapEnvironment.StaticObstacles.Count);
        for (var obstacleIndex = 0; obstacleIndex < MapEnvironment.StaticObstacles.Count; obstacleIndex++)
        {
            var obstacle = MapEnvironment.StaticObstacles[obstacleIndex];
            hash = EntityStateHash.Add(hash, obstacle.Id);
            hash = EntityStateHash.Add(hash, obstacle.Bounds.X);
            hash = EntityStateHash.Add(hash, obstacle.Bounds.Y);
            hash = EntityStateHash.Add(hash, obstacle.Bounds.Width);
            hash = EntityStateHash.Add(hash, obstacle.Bounds.Height);
        }

        hash = EntityStateHash.Add(hash, MapEnvironment.OwnerStarts.Count);
        for (var startIndex = 0; startIndex < MapEnvironment.OwnerStarts.Count; startIndex++)
        {
            var start = MapEnvironment.OwnerStarts[startIndex];
            hash = EntityStateHash.Add(hash, start.OwnerId.Value);
            hash = EntityStateHash.Add(hash, (int)start.Faction);
            hash = EntityStateHash.Add(hash, start.Position.X);
            hash = EntityStateHash.Add(hash, start.Position.Y);
            hash = EntityStateHash.Add(hash, start.Facing);
            hash = EntityStateHash.Add(hash, start.StartingCredits);
        }

        hash = EntityStateHash.Add(hash, MapEnvironment.Triggers.Count);
        for (var triggerIndex = 0; triggerIndex < MapEnvironment.Triggers.Count; triggerIndex++)
        {
            var trigger = MapEnvironment.Triggers[triggerIndex];
            hash = EntityStateHash.Add(hash, trigger.Id);
            hash = EntityStateHash.Add(hash, trigger.Bounds.X);
            hash = EntityStateHash.Add(hash, trigger.Bounds.Y);
            hash = EntityStateHash.Add(hash, trigger.Bounds.Width);
            hash = EntityStateHash.Add(hash, trigger.Bounds.Height);
            hash = EntityStateHash.Add(hash, trigger.EventKey);
        }

        hash = EntityStateHash.Add(hash, MapEnvironment.Objectives.Count);
        for (var objectiveIndex = 0; objectiveIndex < MapEnvironment.Objectives.Count; objectiveIndex++)
        {
            var objective = MapEnvironment.Objectives[objectiveIndex];
            hash = EntityStateHash.Add(hash, objective.Id);
            hash = EntityStateHash.Add(hash, objective.Position.X);
            hash = EntityStateHash.Add(hash, objective.Position.Y);
            hash = EntityStateHash.Add(hash, objective.ObjectiveKey);
            hash = EntityStateHash.Add(hash, objective.Primary ? 1 : 0);
        }

        hash = EntityStateHash.Add(hash, MapEnvironment.NarrativeNodes.Count);
        for (var narrativeIndex = 0; narrativeIndex < MapEnvironment.NarrativeNodes.Count; narrativeIndex++)
        {
            var narrative = MapEnvironment.NarrativeNodes[narrativeIndex];
            hash = EntityStateHash.Add(hash, narrative.Id);
            hash = EntityStateHash.Add(hash, narrative.Position.X);
            hash = EntityStateHash.Add(hash, narrative.Position.Y);
            hash = EntityStateHash.Add(hash, narrative.TextKey);
            hash = EntityStateHash.AddNullableString(hash, narrative.TriggerId);
        }

        hash = EntityStateHash.Add(hash, EconomyTuning.GatherDistance);
        hash = EntityStateHash.Add(hash, EconomyTuning.DockDistance);
        hash = EntityStateHash.Add(hash, EconomyTuning.GatherRate);
        hash = EntityStateHash.Add(hash, EconomyTuning.UnloadRate);
        hash = EntityStateHash.Add(hash, EconomyTuning.RegenerationRate);
        hash = EntityStateHash.Add(hash, EconomyTuning.RegenerationCapRatio);
        hash = EntityStateHash.Add(hash, EconomyTuning.CleanRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, EconomyTuning.TaintedRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, EconomyTuning.HostileRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, EconomyTuning.SafeAuraRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, EconomyTuning.DayRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, EconomyTuning.FogRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, EconomyTuning.NightRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, EconomyTuning.CorruptionRegenerationMultiplier);
        hash = EntityStateHash.Add(hash, (int)ResourceAtmosphere);

        foreach (var spec in _specs)
        {
            hash = EntityStateHash.Add(hash, spec.Key);
            hash = EntityStateHash.Add(hash, (int)spec.Value.Kind);
            hash = EntityStateHash.Add(hash, spec.Value.Display.Label);
        }

        foreach (var inventory in ResourceInventories)
        {
            hash = EntityStateHash.Add(hash, inventory.Key);
            hash = EntityStateHash.Add(hash, inventory.Value.Credits);
        }

        foreach (var upgrades in UpgradeStates)
        {
            hash = EntityStateHash.Add(hash, upgrades.Key);
            foreach (var id in upgrades.Value.CompletedIds)
            {
                hash = EntityStateHash.Add(hash, id);
            }
        }

        for (var entityIndex = 0; entityIndex < _orderedEntities.Count; entityIndex++)
        {
            var entity = _orderedEntities[entityIndex];
            hash = EntityStateHash.Add(hash, entity.Id.Value);
            hash = EntityStateHash.Add(hash, entity.SpecId);
            hash = EntityStateHash.Add(hash, entity.OwnerId.Value);
            hash = EntityStateHash.Add(hash, entity.Transform.Position);
            hash = EntityStateHash.Add(hash, entity.Transform.Facing);

            entity.Components.StableValuesInto(_stateHashComponentValues);
            foreach (var component in _stateHashComponentValues)
            {
                hash = EntityStateHash.Add(hash, component.GetType().Name);
                hash = EntityStateHash.Add(
                    hash,
                    component,
                    _stateHashAbilityCooldownValues,
                    _stateHashWeaponMountValues,
                    _stateHashProductionQueueItems,
                    _stateHashCommandQueueItems,
                    _stateHashCommandSubjectIds);
            }
        }

        _stateHashComponentValues.Clear();
        _stateHashAbilityCooldownValues.Clear();
        _stateHashWeaponMountValues.Clear();
        _stateHashProductionQueueItems.Clear();
        _stateHashCommandQueueItems.Clear();
        _stateHashCommandSubjectIds.Clear();
        return hash;
    }

    private sealed record PendingSpawn(
        EntitySpec Spec,
        OwnerId OwnerId,
        EntityTransform Transform,
        IReadOnlyList<EntityComponentState> Components);
}
