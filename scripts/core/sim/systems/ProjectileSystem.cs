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
        if (!world.TryGet(state.Target, out var target)
            || !target.Components.TryGet<HealthComponentState>(out var health)
            || health.Hp <= 0
            || state.LifetimeRemaining <= 0
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
        var toTarget = target.Transform.Position - projectile.Transform.Position;
        var distance = toTarget.Length();
        var desired = toTarget / MathF.Max(distance, 0.001f) * state.Speed;
        var blend = Math.Clamp(state.TrackingStrength * context.FixedDelta, 0, 1);
        var velocity = state.Velocity.Lerp(desired, blend);
        if (velocity.LengthSquared() <= 0.001f)
        {
            velocity = desired;
        }

        var currentPosition = projectile.Transform.Position;
        var nextPosition = currentPosition + velocity * context.FixedDelta;
        if (HitsTarget(currentPosition, nextPosition, target.Transform.Position, state.HitRadius))
        {
            WeaponEngagementResolution.ApplyProjectileImpact(context, target, source, projectile, state);
            world.QueueRemoval(projectile.Id);
            return;
        }

        projectile.Transform = EntityTransform.At(nextPosition, velocity.Angle());
        projectile.Components.Set(state with
        {
            Velocity = velocity,
            LifetimeRemaining = MathF.Max(0, state.LifetimeRemaining - context.FixedDelta),
        });
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
                    interceptor.Transform.Position,
                    projectile.Transform.Position));
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
