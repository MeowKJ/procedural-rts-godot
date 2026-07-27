using Godot;

namespace ProceduralRts.Core;

public enum BuildingDamageReadabilityLevel
{
    None = 0,
    Light = 1,
    Moderate = 2,
    Heavy = 3,
    Critical = 4,
}

/// <summary>
/// A building-specific view snapshot assembled only from EntityWorld components.
/// It keeps generic entity projection small while moving building UI state away
/// from parallel mutable presentation state one field at a time.
/// </summary>
public readonly record struct BuildingPresentationProjection(
    EntityProjection Entity,
    float TurretFacing,
    Vector2 Footprint,
    float Radius,
    bool Powered,
    float BuildProgress,
    bool ConstructionPaused,
    ConstructionPauseReason PauseReason,
    Vector2? RallyPoint,
    float RallyPulse,
    float HitPulse,
    float DeliveryPulse,
    bool DockOccupied,
    IReadOnlyList<UnitProductionQueueItem> ProductionQueue)
{
    public bool IsUnderConstruction => BuildProgress < 1;
    public bool IsConstructionPaused => IsUnderConstruction && ConstructionPaused;
    public bool HasReadableOfflineState => !Powered || IsConstructionPaused;
    public bool IsProducing => ProductionQueue.Count > 0;
    public float HealthFraction => Entity.HealthFraction;
    public float MissingHealthFraction => 1f - HealthFraction;
    public BuildingDamageReadabilityLevel DamageSeverity => DamageSeverityFor(HealthFraction, Entity.IsAlive);
    public bool HasReadableDamageState => DamageSeverity != BuildingDamageReadabilityLevel.None;

    public static BuildingDamageReadabilityLevel DamageSeverityFor(float healthFraction, bool isAlive)
    {
        if (!isAlive)
        {
            return BuildingDamageReadabilityLevel.None;
        }

        var clamped = Mathf.Clamp(healthFraction, 0, 1);
        if (clamped <= 0.22f)
        {
            return BuildingDamageReadabilityLevel.Critical;
        }

        if (clamped <= 0.45f)
        {
            return BuildingDamageReadabilityLevel.Heavy;
        }

        if (clamped <= 0.70f)
        {
            return BuildingDamageReadabilityLevel.Moderate;
        }

        return clamped < 0.98f
            ? BuildingDamageReadabilityLevel.Light
            : BuildingDamageReadabilityLevel.None;
    }
}

public static partial class BuildingPresentationProjector
{
    public static BuildingPresentationProjection ProjectOne(EntityWorld world, EntityInstance entity)
    {
        var footprint = entity.Components.TryGet<FootprintComponentState>(out var footprintState)
            ? footprintState.Size
            : Vector2.Zero;
        var turretFacing = entity.Components.TryGet<WeaponUserComponentState>(out var weaponUser)
            && weaponUser.Mounts.Count > 0
            ? weaponUser.Mounts[0].Facing
            : entity.Transform.Facing;
        var radius = entity.Components.TryGet<CollisionComponentState>(out var collision)
            ? collision.Radius
            : Mathf.Max(footprint.X, footprint.Y) * 0.5f;
        var powered = !entity.Components.TryGet<PowerComponentState>(out var power) || power.Powered;
        var hasConstruction = entity.Components.TryGet<ConstructionComponentState>(out var construction);
        var buildProgress = hasConstruction
            ? Mathf.Clamp(construction.Progress, 0, 1)
            : 1;
        var constructionPaused = hasConstruction && construction.Paused;
        var pauseReason = hasConstruction
            ? construction.PauseReason
            : ConstructionPauseReason.None;
        var rallyPoint = entity.Components.TryGet<RallyPointComponentState>(out var rally)
            ? rally.Target
            : null;
        var rallyPulse = entity.Components.TryGet<PresentationPulseComponentState>(out var pulse)
            ? pulse.CommandPulse
            : 0;
        var hitPulse = entity.Components.TryGet<PresentationPulseComponentState>(out pulse)
            ? pulse.HitPulse
            : 0;
        var deliveryPulse = entity.Components.TryGet<PresentationPulseComponentState>(out pulse)
            ? pulse.AlertPulse
            : 0;
        var dockOccupied = entity.Components.TryGet<DockComponentState>(out var dock)
            && (dock.ReservedByEntityId is not null || dock.DockedEntityId is not null);
        var productionQueue = entity.Components.TryGet<ProductionQueueComponentState>(out var production)
            ? CloneProductionQueue(production.Items)
            : [];

        return new BuildingPresentationProjection(
            EntityProjector.ProjectOne(world, entity),
            turretFacing,
            footprint,
            radius,
            powered,
            buildProgress,
            constructionPaused,
            pauseReason,
            rallyPoint,
            rallyPulse,
            hitPulse,
            deliveryPulse,
            dockOccupied,
            productionQueue);
    }
}

public readonly record struct BuildingRallyProjection(
    int Id,
    Vector2 Position,
    Vector2 RallyPoint,
    float RallyPulse);

public readonly record struct BuildingSelectionProjection(
    int Id,
    string Kind,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    string Label,
    float Hp,
    float MaxHp,
    float SightRange,
    bool HasRallyPoint,
    IReadOnlyList<UnitProductionQueueItem> ProductionQueue,
    IconGlyph Icon,
    string ShortCode,
    Color Accent);

public readonly record struct BuildingViewProjection(
    int Id,
    string Kind,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    BuildingPresentationProjection Presentation);

public readonly record struct BuildingHoverProjection(
    int Id,
    string Kind,
    PlayerSlotId PlayerSlotId,
    Vector2 Position,
    float Radius,
    PlayerRelation Relation);

public readonly record struct BuildingHitPulseProjection(
    int Id,
    Vector2 Position,
    float Radius,
    float HitPulse,
    Color Accent);

public readonly record struct BuildingMinimapProjection(
    int Id,
    Vector2 Position,
    Vector2 Footprint,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    bool Selected,
    float AlertPulse);

public readonly record struct UnitBattlefieldPowerStatusProjection(
    PlayerSlotId PlayerSlotId,
    int Provided,
    int Used,
    bool HasProvider,
    bool IsStable)
{
    public bool IsOffline => !IsStable;
}
