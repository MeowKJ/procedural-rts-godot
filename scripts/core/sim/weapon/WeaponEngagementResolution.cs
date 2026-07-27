using Godot;

namespace ProceduralRts.Core;

static class WeaponEngagementResolution
{
    public static void Fire(
        SimContext context,
        EntityInstance attacker,
        EntityInstance target,
        WeaponMountRuntimeState mount,
        WeaponDefinition weaponDef,
        float damage)
    {
        if (!target.Components.TryGet<HealthComponentState>(out _))
        {
            return;
        }

        var muzzle = MuzzlePosition(context.World, attacker, mount);
        context.World.Events.Raise(new WeaponFiredEvent(
            context.Tick,
            attacker.Id,
            mount.MountId,
            mount.WeaponId,
            muzzle,
            target.Transform.Position));

        if (ShouldSpawnProjectile(context.World, weaponDef))
        {
            SpawnProjectile(context.World, attacker, target, weaponDef, muzzle, target.Transform.Position, damage);
        }
        else
        {
            ApplyWeaponImpact(
                context,
                target,
                attacker,
                attacker.OwnerId,
                attacker,
                weaponDef,
                damage,
                target.Transform.Position,
                recordRetaliation: true);
        }
    }

    public static void FireAtGround(
        SimContext context,
        EntityInstance attacker,
        Vector2 targetPoint,
        WeaponMountRuntimeState mount,
        WeaponDefinition weaponDef)
    {
        if (!context.World.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo)
            || !WeaponEngagementQueries.CanAttackGround(weaponDef, ammo))
        {
            return;
        }

        var muzzle = MuzzlePosition(context.World, attacker, mount);
        context.World.Events.Raise(new WeaponFiredEvent(
            context.Tick,
            attacker.Id,
            mount.MountId,
            mount.WeaponId,
            muzzle,
            targetPoint));

        if (ShouldSpawnProjectile(context.World, weaponDef))
        {
            SpawnProjectile(context.World, attacker, null, weaponDef, muzzle, targetPoint, damage: 0);
            return;
        }

        ApplySplashDamage(
            context,
            EntityId.None,
            attacker,
            attacker.OwnerId,
            weaponDef,
            ammo,
            targetPoint,
            recordRetaliation: true);
    }

    public static Vector2 MuzzlePosition(EntityWorld world, EntityInstance source, WeaponMountRuntimeState mount)
    {
        if (!world.TryGetSpec(source.SpecId, out var spec))
        {
            return source.Transform.Position;
        }

        foreach (var mountSpec in spec.Weapons)
        {
            if (mountSpec.MountId != mount.MountId)
            {
                continue;
            }

            return source.Transform.Position
                + mountSpec.Anchor.Rotated(source.Transform.Facing)
                + mountSpec.MuzzleOffset.Rotated(mount.Facing);
        }

        return source.Transform.Position;
    }

    public static bool ShouldSpawnProjectile(EntityWorld world, WeaponDefinition weaponDef)
    {
        return world.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo)
            && ammo.Behavior != ProjectileBehavior.Beam;
    }

    public static void SpawnInterceptionRound(
        EntityWorld world,
        EntityInstance interceptor,
        EntityInstance interceptedProjectile,
        WeaponMountRuntimeState mount,
        WeaponDefinition weaponDef)
    {
        if (!ShouldSpawnProjectile(world, weaponDef))
        {
            return;
        }

        SpawnProjectile(
            world,
            interceptor,
            interceptedProjectile,
            weaponDef,
            MuzzlePosition(world, interceptor, mount),
            interceptedProjectile.Transform.Position,
            damage: 0,
            interceptableOverride: false);
    }

    public static void ApplyProjectileImpact(
        SimContext context,
        EntityInstance? target,
        EntityInstance? source,
        EntityInstance projectile,
        ProjectileComponentState state,
        Vector2 impactPosition)
    {
        if (!context.World.TryGetWeaponDefinition(state.WeaponId, out var weaponDef)
            || !context.World.TryGetAmmoDefinition(state.AmmoId, out var ammo))
        {
            return;
        }

        var attacker = source ?? projectile;
        var recordRetaliation = source is not null;
        var hitPrimary = target is not null && ProjectileHitsPrimary(state, target, impactPosition);
        context.World.Events.Raise(new ProjectileImpactEvent(
            context.Tick,
            projectile.Id,
            state.Source,
            state.AmmoId,
            impactPosition,
            hitPrimary));
        if (target is not null && hitPrimary)
        {
            ApplyWeaponImpact(
                context,
                target,
                attacker,
                projectile.OwnerId,
                source,
                weaponDef,
                state.Damage,
                impactPosition,
                recordRetaliation);
            return;
        }

        if (ammo.SplashRadius > 0)
        {
            ApplySplashDamage(
                context,
                EntityId.None,
                attacker,
                projectile.OwnerId,
                weaponDef,
                ammo,
                impactPosition,
                recordRetaliation);
        }
    }

    private static void SpawnProjectile(
        EntityWorld world,
        EntityInstance attacker,
        EntityInstance? target,
        WeaponDefinition weaponDef,
        Vector2 muzzle,
        Vector2 targetPoint,
        float damage,
        bool? interceptableOverride = null)
    {
        if (!world.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo)
            || ammo.Speed <= 0
            || ammo.Behavior == ProjectileBehavior.Beam
            || damage < 0)
        {
            return;
        }

        var aimPoint = ammo.HitRule == HitRule.BallisticDeviation && target is not null
            ? targetPoint + BallisticDeviation(world, target)
            : targetPoint;
        var toTarget = aimPoint - muzzle;
        var rawDistance = toTarget.Length();
        var distance = MathF.Max(1f, rawDistance);
        var direction = rawDistance <= 0.001f ? Vector2.Right : toTarget / rawDistance;
        var targetRadius = target is not null && target.Components.TryGet<CollisionComponentState>(out var collision)
            ? collision.Radius
            : 0f;
        var hitRadius = MathF.Max(4f, targetRadius * MathF.Max(0.2f, ammo.AccuracyRadiusMultiplier));
        var authoredFlightDuration = distance / ammo.Speed;
        var flightDuration = MathF.Max(authoredFlightDuration, ProjectileVfxMath.StyleFor(ammo).MinimumVisibleSeconds);
        var speed = distance / flightDuration;
        var lifetime = MathF.Max(0.35f, flightDuration * 2.4f);
        var spec = ProjectileSpec(weaponDef, ammo);

        world.QueueSpawn(spec, attacker.OwnerId, EntityTransform.At(muzzle, direction.Angle()), new EntityComponentState[]
        {
            new ProjectileComponentState(
                attacker.Id,
                target?.Id ?? EntityId.None,
                weaponDef.Id,
                ammo.Id,
                ammo.Behavior,
                ammo.HitRule,
                muzzle,
                aimPoint,
                damage,
                direction * speed,
                speed,
                ammo.TrackingStrength,
                hitRadius,
                0,
                flightDuration,
                lifetime,
                interceptableOverride ?? ammo.Interceptable),
        });
    }

    private static bool ProjectileHitsPrimary(
        ProjectileComponentState state,
        EntityInstance target,
        Vector2 impactPosition)
    {
        return state.HitRule == HitRule.Guaranteed
            || impactPosition.DistanceSquaredTo(target.Transform.Position) <= state.HitRadius * state.HitRadius;
    }

    private static Vector2 BallisticDeviation(EntityWorld world, EntityInstance target)
    {
        var radius = target.Components.TryGet<CollisionComponentState>(out var collision)
            ? collision.Radius
            : 0f;
        var weight = WeaponMath.ResolveTargetProfile(world, target).Weight;
        var distance = weight switch
        {
            UnitWeightClass.Light => radius * 2.6f + 18,
            UnitWeightClass.Medium => radius * 0.2f,
            UnitWeightClass.Heavy => radius * 0.14f,
            _ => radius * 0.25f,
        };
        return Vector2.FromAngle(world.Rng.NextRange(0, Mathf.Tau)) * distance;
    }

    private static EntitySpec ProjectileSpec(WeaponDefinition weaponDef, AmmoDefinition ammo)
    {
        return new EntitySpec
        {
            Id = $"projectile.{ammo.Id}",
            Kind = EntityKind.Projectile,
            Display = new EntityDisplaySpec(ammo.Label, $"projectile.{ammo.Id}.name", $"weapon.{weaponDef.Id}.role", "PRJ", IconGlyph.AttackMove),
        };
    }

    private static void ApplyWeaponImpact(
        SimContext context,
        EntityInstance target,
        EntityInstance attacker,
        OwnerId attackerOwner,
        EntityInstance? liveSource,
        WeaponDefinition weaponDef,
        float primaryDamage,
        Vector2 impactPosition,
        bool recordRetaliation)
    {
        CombatSystem.ApplyResolvedDamage(context, target, attacker, primaryDamage, recordRetaliation);
        if (!context.World.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo)
            || ammo.SplashRadius <= 0)
        {
            return;
        }

        ApplySplashDamage(context, target.Id, liveSource ?? attacker, attackerOwner, weaponDef, ammo, impactPosition, recordRetaliation);
    }

    private static void ApplySplashDamage(
        SimContext context,
        EntityId primaryTargetId,
        EntityInstance attacker,
        OwnerId attackerOwner,
        WeaponDefinition weaponDef,
        AmmoDefinition ammo,
        Vector2 impactPosition,
        bool recordRetaliation)
    {
        foreach (var candidate in context.World.OrderedEntities)
        {
            if (candidate.Id == primaryTargetId
                || candidate.Id == attacker.Id
                || !context.World.Relations.CanAttack(attackerOwner, candidate.OwnerId)
                || !candidate.Components.TryGet<HealthComponentState>(out var health)
                || health.Hp <= 0)
            {
                continue;
            }

            var distance = SplashDistance(impactPosition, candidate);
            if (distance > ammo.SplashRadius)
            {
                continue;
            }

            var ratio = SplashDamageRatio(distance, ammo.SplashRadius, ammo.SplashMinDamageRatio);
            var damage = WeaponMath.BaseDamage(context.World, attackerOwner, weaponDef, candidate) * ratio;
            if (damage <= 0)
            {
                continue;
            }

            CombatSystem.ApplyResolvedDamage(context, candidate, attacker, damage, recordRetaliation);
        }
    }

    private static float SplashDistance(Vector2 impactPosition, EntityInstance candidate)
    {
        var distance = impactPosition.DistanceTo(candidate.Transform.Position);
        if (candidate.Components.TryGet<CollisionComponentState>(out var collision))
        {
            distance = MathF.Max(0, distance - collision.Radius);
        }

        return distance;
    }

    private static float SplashDamageRatio(float distance, float radius, float minimumRatio)
    {
        if (radius <= 0)
        {
            return 0;
        }

        var t = Math.Clamp(distance / radius, 0, 1);
        return Math.Clamp(minimumRatio, 0, 1) + (1f - Math.Clamp(minimumRatio, 0, 1)) * (1f - t);
    }
}
