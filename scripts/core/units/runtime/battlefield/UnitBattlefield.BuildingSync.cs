using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private bool SyncBuildingTargetEntity(
        int buildingId,
        Vector2? seedRallyPoint = null,
        bool? seedPowered = null,
        float? seedBuildProgress = null,
        float? seedHpOverride = null,
        BuildingEntitySeed? seedOverride = null)
    {
        var seedTarget = seedOverride;
        var existingEntity = BuildingEntityByTargetId(buildingId);
        var existingIdentity = existingEntity?.Components.TryGet<BuildingIdentityComponentState>(out var identity) == true
            ? identity
            : null;
        if (seedTarget is null && existingIdentity is null)
        {
            return false;
        }

        var spec = BuildSpecCatalog.For(seedTarget?.Kind ?? existingIdentity!.Kind);
        var seed = seedTarget ?? SeedForExistingBuildingEntity(buildingId, existingEntity!, existingIdentity!, spec, seedHpOverride);

        if (existingEntity is { } existing)
        {
            var pulseState = existing.Components.TryGet<PresentationPulseComponentState>(out var pulse)
                ? pulse
                : new PresentationPulseComponentState();
            var selectableState = existing.Components.TryGet<SelectableComponentState>(out var selectable)
                ? selectable
                : new SelectableComponentState(AlertPulse: pulseState.HitPulse);
            var queueItems = existing.Components.TryGet<ProductionQueueComponentState>(out var queue)
                ? queue.Items
                : [];
            var existingRally = existing.Components.TryGet<RallyPointComponentState>(out var rally)
                ? rally
                : null;
            var rallyPoint = seedRallyPoint ?? existingRally?.Target;
            var rallyTargetEntityId = seedRallyPoint is null ? existingRally?.TargetEntityId : null;
            var powered = seedPowered
                ?? (existing.Components.TryGet<PowerComponentState>(out var power)
                    ? power.Powered
                    : true);
            var buildProgress = seedBuildProgress
                ?? (existing.Components.TryGet<ConstructionComponentState>(out var construction)
                    ? construction.Progress
                    : 1);
            var dockState = existing.Components.TryGet<DockComponentState>(out var dock)
                ? dock
                : new DockComponentState();
            var weaponState = existing.Components.TryGet<WeaponUserComponentState>(out var weapon)
                ? weapon
                : null;
            var healthState = existing.Components.TryGet<HealthComponentState>(out var health)
                ? health
                : null;
            var runtimeSeed = seedHpOverride is not null
                ? seed with { Hp = seedHpOverride.Value }
                : healthState is null
                    ? seed
                    : seed with { Hp = healthState.Hp };
            existing.Transform = EntityTransform.At(seed.Position, seed.Facing);
            existing.Components.Clear();
            foreach (var component in runtimeSeed.ToEntityComponents(
                spec,
                selectableState.Selected,
                selectableState.AlertPulse,
                queueItems,
                rallyPoint,
                pulseState.CommandPulse,
                pulseState.HitPulse,
                pulseState.AlertPulse,
                powered,
                buildProgress,
                dockState.ReservedByEntityId,
                dockState.DockedEntityId,
                weaponState))
            {
                existing.Components.Set(component);
            }

            EnsureProductionQueueComponent(seed.Id, existing);
            if (rallyPoint is not null || rallyTargetEntityId is not null)
            {
                existing.Components.Set(new RallyPointComponentState(rallyPoint, rallyTargetEntityId));
            }

            return true;
        }

        var entity = _entityWorld.SpawnBuildingTarget(
            seed,
            spec,
            seedRallyPoint,
            powered: seedPowered ?? true,
            buildProgress: seedBuildProgress ?? 1);
        EnsureProductionQueueComponent(seed.Id, entity);
        SetBuildingTargetEntityId(seed.Id, entity.Id);
        return true;
    }

    private static BuildingEntitySeed SeedForExistingBuildingEntity(
        int buildingId,
        EntityInstance entity,
        BuildingIdentityComponentState identity,
        BuildSpec spec,
        float? hpOverride)
    {
        var hp = hpOverride
            ?? (entity.Components.TryGet<HealthComponentState>(out var health) ? health.Hp : spec.MaxHp);
        return new BuildingEntitySeed(
            buildingId,
            identity.Kind,
            identity.PlayerSlotId,
            identity.Faction,
            entity.Transform.Position,
            entity.Transform.Facing,
            hp);
    }

    private void EnsureProductionQueueComponent(int buildingId, EntityInstance entity)
    {
        if (!HasAnyProductionForCore(buildingId) || entity.Components.Has<ProductionQueueComponentState>())
        {
            return;
        }

        entity.Components.Set(new ProductionQueueComponentState([]));
    }

    private void SyncBuildingTargetEntities()
    {
        foreach (var buildingId in BuildingTargetIds())
        {
            SyncBuildingTargetEntity(buildingId);
        }
    }

}
