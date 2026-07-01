namespace ProceduralRts.Core;

static class WeaponEngagementState
{
    public static bool HasCoolingMount(WeaponUserComponentState weapon)
    {
        foreach (var mount in weapon.Mounts)
        {
            if (mount.CooldownRemaining > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<WeaponMountRuntimeState> CoolMountsCopy(
        IReadOnlyList<WeaponMountRuntimeState> mounts,
        float dt)
    {
        WeaponMountRuntimeState[]? copy = null;
        for (var index = 0; index < mounts.Count; index++)
        {
            var mount = mounts[index];
            var cooldown = WeaponEngagementMath.TickCooldown(mount.CooldownRemaining, dt);
            if (copy is null)
            {
                if (MathF.Abs(cooldown - mount.CooldownRemaining) <= 0.0001f)
                {
                    continue;
                }

                copy = new WeaponMountRuntimeState[mounts.Count];
                for (var previous = 0; previous < index; previous++)
                {
                    copy[previous] = mounts[previous];
                }
            }

            copy[index] = mount with { CooldownRemaining = cooldown };
        }

        return copy ?? mounts;
    }

    public static void CoolMountsInPlace(EntityInstance entity, WeaponUserComponentState weapon, float dt)
    {
        var mounts = WritableMounts(entity, weapon);
        for (var index = 0; index < mounts.Count; index++)
        {
            var mount = mounts[index];
            mounts[index] = mount with { CooldownRemaining = WeaponEngagementMath.TickCooldown(mount.CooldownRemaining, dt) };
        }
    }

    public static IList<WeaponMountRuntimeState> WritableMounts(EntityInstance entity, WeaponUserComponentState weapon)
    {
        if (weapon.Mounts is WeaponMountRuntimeState[] array)
        {
            return array;
        }

        if (weapon.Mounts is List<WeaponMountRuntimeState> list)
        {
            return list;
        }

        var copy = weapon.Mounts.ToArray();
        entity.Components.Set(weapon with { Mounts = copy });
        return copy;
    }

    public static IReadOnlyList<WeaponMountRuntimeState> ReadOnlyMounts(IList<WeaponMountRuntimeState> mounts)
    {
        return mounts as IReadOnlyList<WeaponMountRuntimeState> ?? mounts.ToArray();
    }
}
