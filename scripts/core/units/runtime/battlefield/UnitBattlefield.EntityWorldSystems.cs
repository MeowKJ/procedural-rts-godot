using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void UpdateCombatFromEntityWorld(float dt)
    {
        SyncOwnerRelations();
        _combatSystem.Step(new SimContext(_entityWorld, _inputCommandTick, dt, []));
    }

    private void UpdateProjectilesFromEntityWorld(float dt)
    {
        var context = new SimContext(_entityWorld, _inputCommandTick, dt, []);
        _projectileSystem.Step(context);
        _entityWorld.FlushQueuedSpawns();
        _entityWorld.Events.DrainInto(_simEventDrainBuffer);

        foreach (var simEvent in _simEventDrainBuffer)
        {
            if (simEvent is WeaponFiredEvent fired)
            {
                WeaponFired?.Invoke(fired);
            }
            else if (simEvent is ProjectileImpactEvent impact)
            {
                ProjectileImpacted?.Invoke(impact);
            }
        }

        ApplyUnitCombatEvents(_simEventDrainBuffer);
        ApplyBuildingDamageEvents(_simEventDrainBuffer);
        ApplyUnitDamageEventsFromBuildings(_simEventDrainBuffer);
        _simEventDrainBuffer.Clear();
    }

    private void ApplyUnitCombatEvents(IReadOnlyList<SimEvent> events)
    {
        foreach (var simEvent in events)
        {
            if (simEvent is not EntityDamagedEvent damaged
                || UnitByEntityId(damaged.Target) is not { } target)
            {
                continue;
            }

            if (!_entityWorld.TryGet(target.EntityId, out var targetEntity)
                || !targetEntity.Components.Has<HealthComponentState>())
            {
                continue;
            }

            var attacker = UnitByEntityId(damaged.Attacker);
            var ammoId = attacker is not null
                ? PrimaryWeapon(attacker).AmmoId
                : AmmoIdForProjectileEntity(damaged.Attacker);
            ApplyUnitDamageProjection(target, targetEntity, damaged.Damage, ammoId);
            if (attacker is not null)
            {
                UnitAttacked?.Invoke(target, attacker);
            }
        }
    }

    private string? AmmoIdForProjectileEntity(EntityId entityId)
    {
        return _entityWorld.TryGet(entityId, out var entity)
            && entity.Components.TryGet<ProjectileComponentState>(out var projectile)
                ? projectile.AmmoId
                : null;
    }

    private void UpdateConstructionFromEntityWorld(float dt)
    {
        if (!HasActiveConstructionWork())
        {
            return;
        }

        SyncOwnerRelations();
        _constructionSystem.Step(new SimContext(
            _entityWorld,
            NextInputCommandTick(),
            dt,
            Array.Empty<SequencedCommandEnvelope>()));
        AdoptUnmappedConstructedBuildings();
    }

    private void UpdateAbilitiesFromEntityWorld(float dt)
    {
        if (!HasAbilityRuntimeWork())
        {
            return;
        }

        SyncOwnerRelations();
        var context = new SimContext(_entityWorld, _inputCommandTick, dt, []);
        _abilitySystem.Step(context);
        _entityWorld.FlushQueuedSpawns();
        _entityWorld.FlushQueuedRemovals();
        RefreshUnitProjections();
    }

    private bool HasAbilityRuntimeWork()
    {
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (entity.Components.Has<ShieldComponentState>()
                || entity.Components.Has<ScanRevealComponentState>())
            {
                return true;
            }

            if (entity.Components.TryGet<DeployComponentState>(out var deploy)
                && deploy is { IsDeployed: true, SetupRemaining: > 0 })
            {
                return true;
            }

            if (!entity.Components.TryGet<AbilityRuntimeComponentState>(out var runtime))
            {
                continue;
            }

            foreach (var cooldown in runtime.Cooldowns)
            {
                if (cooldown.CooldownRemaining > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasActiveConstructionWork()
    {
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (entity.Components.TryGet<ConstructionComponentState>(out var construction)
                && construction.Progress < 1
                && construction.Phase is ConstructionPhase.Building or ConstructionPhase.Queued)
            {
                return true;
            }
        }

        return false;
    }

    private void CollectCandidateProducerIds(UnitSpec spec, PlayerSlotId playerSlotId, List<int> result)
    {
        result.Clear();
        if (spec.Production is null)
        {
            return;
        }

        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building
                || building.PlayerSlotId != playerSlotId
                || building.Faction != spec.Faction
                || building.Hp <= 0
                || !BuildingPowered(building.Id)
                || BuildingBuildProgress(building.Id) < 1
                || building.Kind != spec.Production.ProducerKind
                || ProducerTechTier(building.Kind) < spec.Stats.TechTier)
            {
                continue;
            }

            result.Add(building.Id);
        }
    }

    private bool HasAnyProductionForCore(int buildingId)
    {
        var identity = BuildingIdentity(buildingId);
        if (identity is null)
        {
            return false;
        }

        var producerTechTier = ProducerTechTier(identity.Kind);
        foreach (var designId in UnitDesignFactionRosterCatalog.For(identity.Faction).PlayableDesignIds)
        {
            var spec = UnitDesignCatalog.Spec(designId);
            if (spec.Production?.ProducerKind == identity.Kind
                && producerTechTier >= spec.Stats.TechTier)
            {
                return true;
            }
        }

        return false;
    }

    private static int ProducerTechTier(string kind)
    {
        return kind switch
        {
            BuildingDesignIds.Barracks => 3,
            BuildingDesignIds.VehicleFactory => 3,
            BuildingDesignIds.Airfield => 3,
            _ => 1,
        };
    }

    private float BuildingTargetRadiusCore(int buildingId)
    {
        var identity = BuildingIdentity(buildingId);
        return BuildingTargetRadiusCore(buildingId, identity?.Kind);
    }

    private float BuildingTargetRadiusCore(int buildingId, string? fallbackKind)
    {
        var projectedRadius = BuildingPresentationProjection(buildingId)?.Radius;
        if (projectedRadius is float radius)
        {
            return radius;
        }

        return fallbackKind is null
            ? 0
            : BuildSpecRadius(fallbackKind);
    }

    private static float BuildSpecRadius(string kind)
    {
        var footprint = BuildSpecCatalog.For(kind).Footprint;
        return Mathf.Max(footprint.X, footprint.Y) * 0.5f;
    }

    private static string ProducerLabelFor(UnitSpec? spec)
    {
        var producerKind = spec?.Production?.ProducerKind ?? BuildingDesignIds.Barracks;
        return BuildSpecCatalog.For(producerKind).Label;
    }

    private Vector2 ClampInsideWorld(Vector2 point, float margin)
    {
        return new Vector2(
            Mathf.Clamp(point.X, margin, Mathf.Max(margin, WorldSize.X - margin)),
            Mathf.Clamp(point.Y, margin, Mathf.Max(margin, WorldSize.Y - margin)));
    }

    private static Color PlayerSlotAccent(PlayerSlotId playerSlotId)
    {
        return playerSlotId.Value switch
        {
            1 => new Color("#68a6c8"),
            2 => new Color("#c86c68"),
            3 => new Color("#8abf74"),
            4 => new Color("#c5a45d"),
            _ => new Color("#b7ad9c"),
        };
    }
}
