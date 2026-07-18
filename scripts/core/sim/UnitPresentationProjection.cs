using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Immutable, render-ready runtime unit state. Presentation reads this projection
/// and static UnitSpec data, never a mutable UnitInstance feedback field.
/// </summary>
public readonly record struct UnitPresentationProjection(
    EntityProjection Entity,
    Vector2 Velocity,
    Vector2? MoveTarget,
    float CommandPulse,
    float AlertPulse,
    float HitPulse,
    float HarvestPulse,
    int Cargo,
    IReadOnlyList<WeaponMountRuntimeState> Mounts)
{
    public bool IsMoving => Velocity.LengthSquared() > 0.01f
        || MoveTarget is { } target && Entity.Position.DistanceSquaredTo(target) > 1f;
}

public static class UnitPresentationProjector
{
    private static readonly IReadOnlyList<WeaponMountRuntimeState> EmptyMounts = Array.Empty<WeaponMountRuntimeState>();

    public static UnitPresentationProjection ProjectOne(EntityWorld world, EntityInstance entity)
    {
        var velocity = Vector2.Zero;
        Vector2? moveTarget = null;
        if (entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            velocity = movement.Velocity;
            moveTarget = movement.MoveTarget;
        }

        var alertPulse = entity.Components.TryGet<SelectableComponentState>(out var selectable)
            ? selectable.AlertPulse
            : 0;
        var commandPulse = 0f;
        var hitPulse = 0f;
        if (entity.Components.TryGet<PresentationPulseComponentState>(out var pulse))
        {
            commandPulse = pulse.CommandPulse;
            alertPulse = MathF.Max(alertPulse, pulse.AlertPulse);
            hitPulse = pulse.HitPulse;
        }

        var harvestPulse = entity.Components.TryGet<HarvesterComponentState>(out var harvester)
            ? harvester.HarvestPulse
            : 0;
        var cargo = entity.Components.TryGet<ResourceCargoComponentState>(out var resourceCargo)
            ? resourceCargo.Cargo
            : 0;
        var mounts = entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
            ? weapon.Mounts
            : EmptyMounts;

        return new UnitPresentationProjection(
            EntityProjector.ProjectOne(world, entity),
            velocity,
            moveTarget,
            commandPulse,
            alertPulse,
            hitPulse,
            harvestPulse,
            cargo,
            mounts);
    }
}
