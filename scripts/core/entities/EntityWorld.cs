using System.Diagnostics;

namespace ProceduralRts.Core;

public sealed partial class EntityWorld
{
    private readonly SortedDictionary<int, EntityInstance> _entities = [];
    private readonly SortedDictionary<string, EntitySpec> _specs = [];
    private readonly List<ISimSystem> _systems = [];
    private readonly List<PendingSpawn> _pendingSpawns = [];
    private readonly SortedSet<int> _pendingRemovals = [];
    private readonly List<EntityComponentState> _stateHashComponentValues = [];
    private readonly List<AbilityCooldownState> _stateHashAbilityCooldownValues = [];
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

    public IReadOnlyList<EntityInstance> StableEntities => _entities.Values.ToList();
    public IReadOnlyList<EntitySpec> StableSpecs => _specs.Values.ToList();
    public int Count => _entities.Count;

    /// <summary>
    /// Entities in stable <see cref="EntityId"/> order without allocating a copy.
    /// Systems iterate this. Safe to read while mutating component state or
    /// transforms in place; do not add or remove entities during iteration.
    /// </summary>
    public IEnumerable<EntityInstance> OrderedEntities => _entities.Values;

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
            _entities.Remove(id);
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

        if (initialComponents is not null)
        {
            foreach (var component in initialComponents)
            {
                entity.Components.Set(component);
            }
        }

        _entities[entity.Id.Value] = entity;
        return entity;
    }

    public bool TryGet(EntityId id, out EntityInstance entity)
    {
        return _entities.TryGetValue(id.Value, out entity!);
    }

    public bool Remove(EntityId id)
    {
        return _entities.Remove(id.Value);
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

        foreach (var entity in _entities.Values)
        {
            hash = EntityStateHash.Add(hash, entity.Id.Value);
            hash = EntityStateHash.Add(hash, entity.SpecId);
            hash = EntityStateHash.Add(hash, entity.OwnerId.Value);
            hash = EntityStateHash.Add(hash, entity.Transform.Position);
            hash = EntityStateHash.Add(hash, entity.Transform.Facing);

            entity.Components.StableValuesInto(_stateHashComponentValues);
            foreach (var component in _stateHashComponentValues)
            {
                hash = EntityStateHash.Add(hash, component.GetType().Name);
                hash = EntityStateHash.Add(hash, component, _stateHashAbilityCooldownValues);
            }
        }

        _stateHashComponentValues.Clear();
        _stateHashAbilityCooldownValues.Clear();
        return hash;
    }

    private sealed record PendingSpawn(
        EntitySpec Spec,
        OwnerId OwnerId,
        EntityTransform Transform,
        IReadOnlyList<EntityComponentState> Components);
}
