using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IReadOnlyList<UnitProductionQueueItem> BuildingProductionQueue(int buildingId)
    {
        return BuildingProductionQueueCore(buildingId);
    }

    public string? BuildingProductionRepeatOutputSpecId(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<ProductionQueueComponentState>(out var queue))
        {
            return null;
        }

        return queue.RepeatOutputSpecId;
    }

    private IReadOnlyList<UnitProductionQueueItem> BuildingProductionQueueCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<ProductionQueueComponentState>(out var queue))
        {
            return [];
        }

        return queue.Items;
    }

    public Vector2? BuildingRallyPoint(int buildingId)
    {
        return BuildingRallyPointCore(buildingId);
    }

    private Vector2? BuildingRallyPointCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<RallyPointComponentState>(out var rally))
        {
            return null;
        }

        return rally.Target;
    }

    public float BuildingRallyPulse(int buildingId)
    {
        return BuildingRallyPulseCore(buildingId);
    }

    private float BuildingRallyPulseCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<PresentationPulseComponentState>(out var pulse))
        {
            return 0;
        }

        return pulse.CommandPulse;
    }

    private float BuildingHitPulseCore(int buildingId)
    {
        return BuildingPresentationPulseCore(buildingId).HitPulse;
    }

    public int? BuildingAttackTargetId(int buildingId)
    {
        return BuildingAttackTargetIdCore(buildingId);
    }

    private int? BuildingAttackTargetIdCore(int buildingId)
    {
        return BuildingWeaponStateCore(buildingId) is { } weapon
            ? TargetIdForEntity(weapon.AttackTarget, weapon.AttackTargetKind)
            : null;
    }

    public CombatTargetKind BuildingAttackTargetKind(int buildingId)
    {
        return BuildingAttackTargetKindCore(buildingId);
    }

    private CombatTargetKind BuildingAttackTargetKindCore(int buildingId)
    {
        return BuildingWeaponStateCore(buildingId)?.AttackTargetKind ?? CombatTargetKind.Unit;
    }

    public float BuildingAttackCooldownRemaining(int buildingId)
    {
        return BuildingAttackCooldownRemainingCore(buildingId);
    }

    private float BuildingAttackCooldownRemainingCore(int buildingId)
    {
        var weapon = BuildingWeaponStateCore(buildingId);
        return weapon is null || weapon.Mounts.Count == 0
            ? 0
            : weapon.Mounts[0].CooldownRemaining;
    }

    private PresentationPulseComponentState BuildingPresentationPulseCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<PresentationPulseComponentState>(out var pulse))
        {
            return new PresentationPulseComponentState();
        }

        return pulse;
    }

    private WeaponUserComponentState? BuildingWeaponStateCore(int buildingId)
    {
        return _buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            && _entityWorld.TryGet(entityId, out var entity)
            && entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
                ? weapon
                : null;
    }

    public bool BuildingPowered(int buildingId)
    {
        return BuildingPoweredCore(buildingId);
    }

    private bool BuildingPoweredCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<PowerComponentState>(out var power))
        {
            return true;
        }

        return power.Powered;
    }

    public float BuildingBuildProgress(int buildingId)
    {
        return BuildingBuildProgressCore(buildingId);
    }

    private float BuildingBuildProgressCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<ConstructionComponentState>(out var construction))
        {
            return 1;
        }

        return Mathf.Clamp(construction.Progress, 0, 1);
    }

    public int? BuildingDockReservedByHarvesterId(int buildingId)
    {
        return BuildingDockReservedByHarvesterIdCore(buildingId);
    }

    private int? BuildingDockReservedByHarvesterIdCore(int buildingId)
    {
        return BuildingDockStateCore(buildingId).ReservedByEntityId is { } entityId
            ? UnitIdForEntity(entityId)
            : null;
    }

    public int? BuildingDockedHarvesterId(int buildingId)
    {
        return BuildingDockedHarvesterIdCore(buildingId);
    }

    private int? BuildingDockedHarvesterIdCore(int buildingId)
    {
        return BuildingDockStateCore(buildingId).DockedEntityId is { } entityId
            ? UnitIdForEntity(entityId)
            : null;
    }

    private DockComponentState BuildingDockStateCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<DockComponentState>(out var dock))
        {
            return new DockComponentState();
        }

        return dock;
    }

    public void SetBuildingHitPulse(int buildingId, float value)
    {
        SetBuildingHitPulseCore(buildingId, value);
    }

    private void SetBuildingHitPulseCore(int buildingId, float value)
    {
        SetBuildingPresentationPulseCore(buildingId, hitPulse: value);
    }

    private void SetBuildingDeliveryPulseCore(int buildingId, float value)
    {
        SetBuildingPresentationPulseCore(buildingId, alertPulse: value);
    }

    private void SetBuildingRallyPulseCore(int buildingId, float value)
    {
        SetBuildingPresentationPulseCore(buildingId, commandPulse: value);
    }

    private void SetBuildingPresentationPulseCore(
        int buildingId,
        float? commandPulse = null,
        float? alertPulse = null,
        float? hitPulse = null)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity))
        {
            return;
        }

        var current = entity.Components.TryGet<PresentationPulseComponentState>(out var pulse)
            ? pulse
            : new PresentationPulseComponentState();
        entity.Components.Set(current with
        {
            CommandPulse = commandPulse ?? current.CommandPulse,
            AlertPulse = alertPulse ?? current.AlertPulse,
            HitPulse = hitPulse ?? current.HitPulse,
        });
    }

    private void DecayBuildingPresentationPulses(int buildingId, float dt)
    {
        var current = BuildingPresentationPulseCore(buildingId);
        SetBuildingPresentationPulseCore(
            buildingId,
            Mathf.Max(0, current.CommandPulse - dt * CommandPulseDecay),
            Mathf.Max(0, current.AlertPulse - dt * 2.9f),
            Mathf.Max(0, current.HitPulse - dt * 3.2f));
    }

    private void RemoveBuildingEntity(int buildingId)
    {
        if (RemoveBuildingTargetEntityId(buildingId, out var entityId))
        {
            _entityWorld.Remove(entityId);
        }
    }

}
