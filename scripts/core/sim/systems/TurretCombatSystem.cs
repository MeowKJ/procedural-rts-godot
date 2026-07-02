using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Transitional authoritative combat for fixed defenses. It processes only
/// EntityKind.Turret entities so UnitBattlefield can migrate building weapons to
/// EntityWorld without double-stepping mobile unit combat during M1.
/// </summary>
public sealed class TurretCombatSystem : ISimSystem
{
    private readonly SpatialGrid<EntityInstance> _targetGrid = new(1f);
    private float _targetGridMaxTargetRadius;

    public void Step(SimContext context)
    {
        var world = context.World;
        var dt = context.FixedDelta;
        BuildTargetGrid(world);

        foreach (var turret in world.OrderedEntities)
        {
            if (!world.TryGetSpec(turret.SpecId, out var spec) || spec.Kind != EntityKind.Turret)
            {
                continue;
            }

            if (!turret.Components.TryGet<WeaponUserComponentState>(out var weapon))
            {
                continue;
            }

            if (IsInactive(turret))
            {
                SetWeaponState(turret, weapon with
                {
                    Mounts = WeaponEngagementState.CoolMountsCopy(weapon.Mounts, dt),
                    AttackTarget = EntityId.None,
                    AttackTargetKind = CombatTargetKind.Unit,
                    AttackTargetIsManual = false,
                });
                continue;
            }

            var target = ResolveTarget(world, turret, weapon);
            if (target is null)
            {
                SetWeaponState(turret, weapon with
                {
                    Mounts = WeaponEngagementState.CoolMountsCopy(weapon.Mounts, dt),
                    AttackTarget = EntityId.None,
                    AttackTargetKind = CombatTargetKind.Unit,
                    AttackTargetIsManual = false,
                });
                continue;
            }

            Engage(context, turret, weapon, target, dt);
        }
    }

    private static bool IsInactive(EntityInstance entity)
    {
        if (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return true;
        }

        if (entity.Components.TryGet<PowerComponentState>(out var power) && !power.Powered)
        {
            return true;
        }

        return entity.Components.TryGet<ConstructionComponentState>(out var construction) && construction.Progress < 1;
    }

    private void BuildTargetGrid(EntityWorld world)
    {
        var hasActiveTurret = false;
        var maxRange = 1f;
        foreach (var entity in world.OrderedEntities)
        {
            if (!world.TryGetSpec(entity.SpecId, out var spec)
                || spec.Kind != EntityKind.Turret
                || !entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
                || IsInactive(entity))
            {
                continue;
            }

            hasActiveTurret = true;
            maxRange = MathF.Max(maxRange, WeaponMath.BaseRange(world, entity, weapon));
        }

        if (!hasActiveTurret)
        {
            _targetGrid.Reset(1f);
            _targetGridMaxTargetRadius = 0f;
            return;
        }

        _targetGrid.Reset(maxRange);
        _targetGridMaxTargetRadius = 0f;
        foreach (var entity in world.OrderedEntities)
        {
            if (IsDead(entity) || !entity.Components.Has<HealthComponentState>())
            {
                continue;
            }

            if (entity.Components.TryGet<CollisionComponentState>(out var collision))
            {
                _targetGridMaxTargetRadius = MathF.Max(_targetGridMaxTargetRadius, collision.Radius);
            }

            _targetGrid.Add(entity.Transform.Position, entity);
        }
    }

    private EntityInstance? ResolveTarget(EntityWorld world, EntityInstance turret, WeaponUserComponentState weapon)
    {
        var range = WeaponMath.BaseRange(world, turret, weapon);
        if (range <= 0)
        {
            return null;
        }

        if (weapon.AttackTarget.IsValid
            && world.TryGet(weapon.AttackTarget, out var manual)
            && IsTargetable(world, turret, weapon, manual, out _)
            && IsInRange(world, turret, range, manual))
        {
            return manual;
        }

        EntityInstance? best = null;
        var bestPriority = 0f;
        var bestDistance = 0f;
        var cellRadius = _targetGrid.CellRadiusFor(range + _targetGridMaxTargetRadius);
        foreach (var candidate in _targetGrid.Neighbors(turret.Transform.Position, cellRadius))
        {
            if (candidate.Id == turret.Id
                || !IsTargetable(world, turret, weapon, candidate, out _)
                || !IsInRange(world, turret, range, candidate))
            {
                continue;
            }

            var priority = TargetPriority(world, weapon, candidate);
            if (priority <= 0)
            {
                continue;
            }

            var distance = turret.Transform.Position.DistanceSquaredTo(candidate.Transform.Position);
            if (best is null
                || priority > bestPriority
                || (Mathf.IsEqualApprox(priority, bestPriority) && distance < bestDistance)
                || (Mathf.IsEqualApprox(priority, bestPriority)
                    && Mathf.IsEqualApprox(distance, bestDistance)
                    && candidate.Id.Value < best.Id.Value))
            {
                best = candidate;
                bestPriority = priority;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool IsTargetable(EntityWorld world, EntityInstance turret, WeaponUserComponentState weapon, EntityInstance candidate, out EntitySpec? candidateSpec)
    {
        candidateSpec = null;
        if (!world.Relations.CanAttack(turret.OwnerId, candidate.OwnerId)
            || !candidate.Components.TryGet<HealthComponentState>(out var health)
            || health.Hp <= 0
            || !world.TryGetSpec(candidate.SpecId, out var spec))
        {
            return false;
        }

        candidateSpec = spec;
        foreach (var mount in weapon.Mounts)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var definition)
                && WeaponMath.CanTarget(world, definition, candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDead(EntityInstance entity)
    {
        return entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0;
    }

    private static bool IsInRange(EntityWorld world, EntityInstance turret, float range, EntityInstance target)
    {
        var targetRadius = target.Components.TryGet<CollisionComponentState>(out var collision) ? collision.Radius : 0;
        return turret.Transform.Position.DistanceTo(target.Transform.Position) <= range + targetRadius;
    }

    private static void Engage(SimContext context, EntityInstance turret, WeaponUserComponentState weapon, EntityInstance target, float dt)
    {
        var desiredFacing = (target.Transform.Position - turret.Transform.Position).Angle();
        var mounts = WeaponEngagementState.WritableMounts(turret, weapon);
        WeaponEngagementMountLoop.Tick(
            context,
            turret,
            target,
            mounts,
            desiredFacing,
            dt,
            new WeaponEngagementMountLoopOptions(
                InRange: true,
                CenterDistance: turret.Transform.Position.DistanceTo(target.Transform.Position),
                TargetRadius: target.Components.TryGet<CollisionComponentState>(out var targetCollision) ? targetCollision.Radius : 0,
                RespectMinimumRange: false,
                UseStructureTargetDefaults: false,
                RequirePositivePriority: false,
                FireOnlyOneMount: true,
                AnchorMovementOnFire: false,
                DamageVariance: 0));

        SetWeaponState(turret, weapon with
        {
            Mounts = WeaponEngagementState.ReadOnlyMounts(mounts),
            AttackTarget = target.Id,
            AttackTargetKind = WeaponEngagementQueries.TargetKind(context.World, target),
            AttackTargetIsManual = false,
        });
    }

    private static float TargetPriority(EntityWorld world, WeaponUserComponentState weapon, EntityInstance target)
    {
        if (!world.TryGetSpec(target.SpecId, out var spec))
        {
            return 0;
        }

        var best = 0f;
        foreach (var mount in weapon.Mounts)
        {
            if (!world.TryGetWeaponDefinition(mount.WeaponId, out var definition)
                || !WeaponMath.CanTarget(world, definition, target))
            {
                continue;
            }

            best = MathF.Max(best, WeaponMath.TargetPriority(world, definition, target));
        }

        return best;
    }

    private static void SetWeaponState(EntityInstance turret, WeaponUserComponentState weapon)
    {
        turret.Components.Set(weapon);
    }
}
