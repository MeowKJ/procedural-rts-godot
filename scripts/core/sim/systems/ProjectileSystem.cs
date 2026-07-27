using Godot;

namespace ProceduralRts.Core;

public sealed class ProjectileSystem : ISimSystem
{
    public void Step(SimContext context)
    {
        foreach (var projectile in context.World.OrderedEntities)
        {
            if (!projectile.Components.TryGet<ProjectileComponentState>(out var state))
            {
                continue;
            }

            StepProjectile(context, projectile, state);
        }
    }

    private static void StepProjectile(SimContext context, EntityInstance projectile, ProjectileComponentState state)
    {
        var world = context.World;
        if (state.LifetimeRemaining <= 0
            || state.Speed <= 0)
        {
            world.QueueRemoval(projectile.Id);
            return;
        }

        if (TryIntercept(context, projectile, state))
        {
            return;
        }

        var source = world.TryGet(state.Source, out var liveSource) ? liveSource : null;
        var target = TryLiveTarget(world, state.Target);
        var aimPoint = state.AimPoint;
        var velocity = state.Velocity;
        if (state.Behavior == ProjectileBehavior.Tracking)
        {
            if (target is not null)
            {
                aimPoint = target.Transform.Position;
                var desired = DirectionTo(projectile.Transform.Position, aimPoint) * state.Speed;
                var blend = Math.Clamp(state.TrackingStrength * context.FixedDelta, 0, 1);
                velocity = state.Velocity.Lerp(desired, blend);
                velocity = velocity.LengthSquared() <= 0.001f
                    ? desired
                    : velocity.Normalized() * state.Speed;
            }
            else
            {
                velocity = DirectionTo(projectile.Transform.Position, aimPoint) * state.Speed;
            }
        }

        var currentPosition = projectile.Transform.Position;
        var nextPosition = currentPosition + velocity * context.FixedDelta;
        var reachedImpact = state.Behavior == ProjectileBehavior.Tracking && target is not null
            ? HitsTarget(currentPosition, nextPosition, aimPoint, state.HitRadius)
            : HitsTarget(currentPosition, nextPosition, aimPoint, 1f);
        if (reachedImpact && state.Age <= 0)
        {
            projectile.Transform = EntityTransform.At(aimPoint, velocity.Angle());
            projectile.Components.Set(state with
            {
                AimPoint = aimPoint,
                Velocity = velocity,
                Age = state.Age + context.FixedDelta,
                LifetimeRemaining = MathF.Max(0, state.LifetimeRemaining - context.FixedDelta),
            });
            return;
        }

        if (reachedImpact)
        {
            WeaponEngagementResolution.ApplyProjectileImpact(
                context,
                target,
                source,
                projectile,
                state with { AimPoint = aimPoint, Velocity = velocity },
                aimPoint);
            world.QueueRemoval(projectile.Id);
            return;
        }

        projectile.Transform = EntityTransform.At(nextPosition, velocity.Angle());
        projectile.Components.Set(state with
        {
            AimPoint = aimPoint,
            Velocity = velocity,
            Age = state.Age + context.FixedDelta,
            LifetimeRemaining = MathF.Max(0, state.LifetimeRemaining - context.FixedDelta),
        });
    }

    private static EntityInstance? TryLiveTarget(EntityWorld world, EntityId targetId)
    {
        return targetId.IsValid
            && world.TryGet(targetId, out var target)
            && target.Components.TryGet<HealthComponentState>(out var health)
            && health.Hp > 0
                ? target
                : null;
    }

    private static Vector2 DirectionTo(Vector2 from, Vector2 to)
    {
        var delta = to - from;
        return delta.LengthSquared() <= 0.001f ? Vector2.Right : delta.Normalized();
    }

    private static bool HitsTarget(Vector2 start, Vector2 end, Vector2 target, float radius)
    {
        var radiusSq = radius * radius;
        if (start.DistanceSquaredTo(target) <= radiusSq || end.DistanceSquaredTo(target) <= radiusSq)
        {
            return true;
        }

        var segment = end - start;
        var segmentLengthSq = segment.LengthSquared();
        if (segmentLengthSq <= 0.001f)
        {
            return false;
        }

        var t = Math.Clamp((target - start).Dot(segment) / segmentLengthSq, 0, 1);
        var closest = start + segment * t;
        return closest.DistanceSquaredTo(target) <= radiusSq;
    }

    private static bool TryIntercept(SimContext context, EntityInstance projectile, ProjectileComponentState state)
    {
        if (!state.Interceptable)
        {
            return false;
        }

        foreach (var interceptor in context.World.OrderedEntities)
        {
            if (interceptor.Id.Value == projectile.Id.Value
                || !context.World.Relations.CanAttack(interceptor.OwnerId, projectile.OwnerId)
                || !CanAct(interceptor)
                || !interceptor.Components.TryGet<WeaponUserComponentState>(out var weapon))
            {
                continue;
            }

            var mounts = WeaponEngagementState.WritableMounts(interceptor, weapon);
            for (var index = 0; index < mounts.Count; index++)
            {
                var mount = mounts[index];
                if (WeaponSystem.IsRecovering(mount)
                    || !context.World.TryGetWeaponDefinition(mount.WeaponId, out var definition)
                    || !definition.CanInterceptProjectiles
                    || !IsInInterceptRange(interceptor, projectile, definition))
                {
                    continue;
                }

                mounts[index] = WeaponSystem.BeginRecovery(mount, definition);
                context.World.Events.Raise(new WeaponFiredEvent(
                    context.Tick,
                    interceptor.Id,
                    mount.MountId,
                    mount.WeaponId,
                    WeaponEngagementResolution.MuzzlePosition(context.World, interceptor, mount),
                    projectile.Transform.Position,
                    mount.WeaponKindAlias));
                WeaponEngagementResolution.SpawnInterceptionRound(
                    context.World,
                    interceptor,
                    projectile,
                    mount,
                    definition);
                context.World.QueueRemoval(projectile.Id);
                return true;
            }
        }

        return false;
    }

    private static bool CanAct(EntityInstance entity)
    {
        return !entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0;
    }

    private static bool IsInInterceptRange(EntityInstance interceptor, EntityInstance projectile, WeaponDefinition definition)
    {
        var range = MathF.Max(0, definition.Range);
        return range > 0
            && interceptor.Transform.Position.DistanceSquaredTo(projectile.Transform.Position) <= range * range;
    }
}
