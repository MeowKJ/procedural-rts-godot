using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Render-ready snapshot for EntityWorld projectiles. Views consume this instead
/// of reaching through to ProjectileComponentState.
/// </summary>
public readonly record struct ProjectilePresentationProjection(
    EntityId Id,
    Vector2 Position,
    Vector2 Velocity,
    string WeaponId,
    string AmmoId,
    ProjectileBehavior Behavior,
    HitRule HitRule,
    AmmoKind? LegacyAmmoKind,
    ProjectileVfxStyle Style,
    Color Accent)
{
    public bool IsSeekerRocket => LegacyAmmoKind == AmmoKind.SeekerRocket;
    public float CullingRadius => Style.HeadRadius + Style.CullingPadding;
    public float TailLength => Style.TailLength;
    public float TrailWidth => Style.TrailWidth;
    public float CoreWidth => Style.CoreWidth;
    public float HeadRadius => Style.HeadRadius;
}

public static class ProjectilePresentationProjector
{
    public static IReadOnlyList<ProjectilePresentationProjection> Project(EntityWorld world, PlayerSlotId viewer)
    {
        var result = new List<ProjectilePresentationProjection>();
        ProjectInto(world, viewer, result);
        return result;
    }

    public static void ProjectInto(EntityWorld world, PlayerSlotId viewer, List<ProjectilePresentationProjection> result)
    {
        result.Clear();
        var viewerOwner = OwnerId.FromPlayerSlot(viewer);
        foreach (var entity in world.OrderedEntities)
        {
            if (ProjectOne(world, entity, viewerOwner) is { } projection)
            {
                result.Add(projection);
            }
        }
    }

    public static int Count(EntityWorld world)
    {
        var count = 0;
        foreach (var entity in world.OrderedEntities)
        {
            if (entity.Components.TryGet<ProjectileComponentState>(out var projectile)
                && world.TryGetAmmoDefinition(projectile.AmmoId, out _))
            {
                count++;
            }
        }

        return count;
    }

    public static ProjectilePresentationProjection? ProjectOne(EntityWorld world, EntityInstance entity, OwnerId viewer)
    {
        if (!entity.Components.TryGet<ProjectileComponentState>(out var projectile)
            || !world.TryGetAmmoDefinition(projectile.AmmoId, out var ammo))
        {
            return null;
        }

        var sourceOwner = world.TryGet(projectile.Source, out var source)
            ? source.OwnerId
            : entity.OwnerId;

        return new ProjectilePresentationProjection(
            entity.Id,
            entity.Transform.Position,
            projectile.Velocity,
            projectile.WeaponId,
            projectile.AmmoId,
            ammo.Behavior,
            ammo.HitRule,
            ammo.LegacyKind,
            ProjectileVfxMath.StyleFor(ammo.LegacyKind),
            AccentFor(world, viewer, sourceOwner, ammo.Accent));
    }

    private static Color AccentFor(EntityWorld world, OwnerId viewer, OwnerId sourceOwner, Color ammoAccent)
    {
        return world.Relations.Relation(viewer, sourceOwner) switch
        {
            PlayerRelation.Hostile => FactionVisualPolicy.HostileOverlay,
            PlayerRelation.Allied => ammoAccent.Lerp(FactionVisualPolicy.AlliedOverlay, 0.32f),
            PlayerRelation.Neutral => ammoAccent.Lerp(FactionVisualPolicy.NeutralOverlay, 0.36f),
            _ => ammoAccent.Lerp(PlayerSlotAccent(sourceOwner.ToPlayerSlot()), 0.36f),
        };
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
