using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Render-ready snapshot for EntityWorld projectiles. Views consume this instead
/// of reaching through to ProjectileComponentState.
/// </summary>
public readonly record struct ProjectilePresentationProjection(
    EntityId Id,
    Vector2 Position,
    Vector2 GroundPosition,
    Vector2 Velocity,
    Vector2 GroundVelocity,
    string WeaponId,
    string AmmoId,
    ProjectileBehavior Behavior,
    HitRule HitRule,
    float FlightProgress,
    float ArcHeight,
    ProjectileVfxStyle Style,
    Color Accent)
{
    public bool IsSeekerRocket => AmmoId == AmmoIds.SeekerRocket;
    public bool HasGroundShadow => Behavior == ProjectileBehavior.Ballistic && ArcHeight > 0.5f;
    public float CullingRadius => Style.HeadRadius + Style.CullingPadding + ArcHeight;
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
        var relation = world.Relations.Relation(viewer, sourceOwner);
        if (relation is not (PlayerRelation.Self or PlayerRelation.Allied)
            && !world.Visibility.IsVisible(viewer, entity.Id))
        {
            return null;
        }

        var flightProgress = projectile.FlightProgress;
        var arcHeight = projectile.Behavior == ProjectileBehavior.Ballistic
            ? ProjectilePresentationMath.BallisticArcHeight(projectile.Origin, projectile.AimPoint, flightProgress)
            : 0;
        var visualPosition = entity.Transform.Position + Vector2.Up * arcHeight;
        var visualVelocity = projectile.Velocity;
        if (arcHeight > 0)
        {
            visualVelocity += Vector2.Up * ProjectilePresentationMath.BallisticArcVerticalSpeed(
                projectile.Origin,
                projectile.AimPoint,
                flightProgress,
                projectile.FlightDuration);
        }

        return new ProjectilePresentationProjection(
            entity.Id,
            visualPosition,
            entity.Transform.Position,
            visualVelocity,
            projectile.Velocity,
            projectile.WeaponId,
            projectile.AmmoId,
            projectile.Behavior,
            projectile.HitRule,
            flightProgress,
            arcHeight,
            ProjectileVfxMath.StyleFor(ammo),
            AccentFor(world, viewer, sourceOwner, ElementPresentationCatalog.ProjectileAccentFor(ammo.DamageElementId, ammo.Accent)));
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

public static class ProjectilePresentationMath
{
    public static float BallisticArcHeight(Vector2 origin, Vector2 impactPoint, float progress)
    {
        return BallisticArcAmplitude(origin, impactPoint)
            * MathF.Sin(Mathf.Clamp(progress, 0, 1) * MathF.PI);
    }

    public static float BallisticArcVerticalSpeed(
        Vector2 origin,
        Vector2 impactPoint,
        float progress,
        float flightDuration)
    {
        if (flightDuration <= 0)
        {
            return 0;
        }

        return BallisticArcAmplitude(origin, impactPoint)
            * MathF.PI
            / flightDuration
            * MathF.Cos(Mathf.Clamp(progress, 0, 1) * MathF.PI);
    }

    private static float BallisticArcAmplitude(Vector2 origin, Vector2 impactPoint)
    {
        return Mathf.Clamp(origin.DistanceTo(impactPoint) * 0.18f, 24f, 96f);
    }
}
