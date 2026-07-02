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
            CombatSystem.ApplyResolvedDamage(context, target, attacker, damage);
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
        float incomingDamage)
    {
        CombatSystem.ApplyResolvedDamage(
            context,
            target,
            source ?? projectile,
            incomingDamage,
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
                lifetime),
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
}
