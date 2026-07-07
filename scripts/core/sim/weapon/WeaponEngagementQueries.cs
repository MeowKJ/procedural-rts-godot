namespace ProceduralRts.Core;

static class WeaponEngagementQueries
{
    public static CombatTargetKind TargetKind(EntityWorld world, EntityInstance target)
    {
        return world.TryGetSpec(target.SpecId, out var spec)
            && spec.Kind is EntityKind.Building or EntityKind.Turret
            ? CombatTargetKind.Building
            : CombatTargetKind.Unit;
    }

    public static bool CanAnyMountTarget(EntityWorld world, WeaponUserComponentState weapon, EntityInstance target)
    {
        foreach (var mount in weapon.Mounts)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var definition)
                && WeaponMath.CanTarget(world, definition, target))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CanAnyMountAttackGround(EntityWorld world, WeaponUserComponentState weapon)
    {
        foreach (var mount in weapon.Mounts)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var weaponDef)
                && world.TryGetAmmoDefinition(weaponDef.AmmoId, out var ammo)
                && CanAttackGround(weaponDef, ammo))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CanAttackGround(WeaponDefinition weaponDef, AmmoDefinition ammo)
    {
        return ammo.SplashRadius > 0
            && ammo.Behavior != ProjectileBehavior.Tracking
            && !weaponDef.CanInterceptProjectiles;
    }
}
