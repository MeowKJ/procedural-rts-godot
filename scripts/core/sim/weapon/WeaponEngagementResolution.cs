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

        context.World.Events.Raise(new WeaponFiredEvent(
            context.Tick,
            attacker.Id,
            mount.MountId,
            mount.WeaponId,
            attacker.Transform.Position,
            target.Transform.Position));

        if (ShouldSpawnProjectile(context.World, weaponDef))
        {
            SpawnProjectile(context.World, attacker, target, weaponDef, damage);
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

    public static bool ShouldSpawnProjectile(EntityWorld world, WeaponDefinition weaponDef)
    {
        return world.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo)
            && ammo.Behavior == ProjectileBehavior.Tracking;
    }

    public static void ApplyProjectileImpact(
        SimContext context,
        EntityInstance target,
        EntityInstance? source,
        EntityInstance projectile,
        ProjectileComponentState state)
    {
        if (!context.World.TryGetWeaponDefinition(state.WeaponId, out var weaponDef))
        {
            return;
        }

        var attacker = source ?? projectile;
        ApplyWeaponImpact(
            context,
            target,
            attacker,
            projectile.OwnerId,
            source,
            weaponDef,
            state.Damage,
            target.Transform.Position,
            recordRetaliation: source is not null);
    }

    private static void SpawnProjectile(
        EntityWorld world,
        EntityInstance attacker,
        EntityInstance target,
        WeaponDefinition weaponDef,
        float damage)
    {
        if (!world.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo)
            || ammo.Speed <= 0
            || damage <= 0)
        {
            return;
        }

        var toTarget = target.Transform.Position - attacker.Transform.Position;
        var distance = MathF.Max(1f, toTarget.Length());
        var direction = distance <= 0.001f ? Vector2.Right : toTarget / distance;
        var targetRadius = target.Components.TryGet<CollisionComponentState>(out var collision) ? collision.Radius : 0f;
        var hitRadius = MathF.Max(4f, targetRadius * MathF.Max(0.2f, ammo.AccuracyRadiusMultiplier));
        var lifetime = MathF.Max(0.35f, distance / ammo.Speed * 2.4f);
        var spec = ProjectileSpec(weaponDef, ammo);

        world.QueueSpawn(spec, attacker.OwnerId, EntityTransform.At(attacker.Transform.Position, direction.Angle()), new EntityComponentState[]
        {
            new ProjectileComponentState(
                attacker.Id,
                target.Id,
                weaponDef.Id,
                ammo.Id,
                damage,
                direction * ammo.Speed,
                ammo.Speed,
                ammo.TrackingStrength,
                hitRadius,
                lifetime,
                ammo.Interceptable),
        });
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
