namespace ProceduralRts.Core;

static class WeaponEngagementState
{
    public static bool HasCoolingMount(WeaponUserComponentState weapon)
    {
        foreach (var mount in weapon.Mounts)
        {
            if (WeaponSystem.IsRecovering(mount))
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
            var next = WeaponSystem.TickIdle(mount, dt);
            if (copy is null)
            {
                if (next == mount)
                {
                    continue;
                }

                copy = new WeaponMountRuntimeState[mounts.Count];
                for (var previous = 0; previous < index; previous++)
                {
                    copy[previous] = mounts[previous];
                }
            }

            copy[index] = next;
        }

        return copy ?? mounts;
    }

    public static void CoolMountsInPlace(EntityInstance entity, WeaponUserComponentState weapon, float dt)
    {
        var mounts = WritableMounts(entity, weapon);
        for (var index = 0; index < mounts.Count; index++)
        {
            var mount = mounts[index];
            mounts[index] = WeaponSystem.TickIdle(mount, dt);
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
