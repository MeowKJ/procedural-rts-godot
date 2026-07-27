namespace ProceduralRts.Core;

public static partial class EntityStateHash
{
    private static ulong AddWeaponUser(
        ulong hash,
        WeaponUserComponentState state,
        List<WeaponMountRuntimeState>? weaponMountOrder)
    {
        hash = Add(hash, state.Mounts.Count);
        var ordered = weaponMountOrder ?? new List<WeaponMountRuntimeState>(state.Mounts.Count);
        ordered.Clear();
        foreach (var mount in state.Mounts)
        {
            ordered.Add(mount);
        }

        SortWeaponMountsByMountId(ordered);
        foreach (var mount in ordered)
        {
            hash = Add(hash, mount.MountId);
            hash = Add(hash, mount.WeaponId);
            hash = Add(hash, mount.Facing);
            hash = Add(hash, mount.CooldownRemaining);
            hash = Add(hash, (int)mount.Phase);
            hash = Add(hash, mount.WarmupRemaining);
            hash = Add(hash, mount.ReloadRemaining);
        }

        ordered.Clear();
        hash = Add(hash, state.AttackTarget.Value);
        hash = Add(hash, (int)state.AttackTargetKind);
        hash = Add(hash, state.AttackTargetIsManual ? 1 : 0);
        hash = Add(hash, state.AutoReacquireCooldownRemaining);
        hash = AddNullableVector(hash, state.LastKnownTargetPosition);
        return Add(hash, state.LastKnownTargetRemaining);
    }

    private static ulong AddProduction(
        ulong hash,
        ProductionQueueComponentState state,
        List<UnitProductionQueueItem>? productionQueueOrder)
    {
        hash = Add(hash, state.Items.Count);
        hash = Add(hash, (int)state.PauseReason);
        hash = AddNullableString(hash, state.RepeatOutputSpecId);
        var ordered = productionQueueOrder ?? new List<UnitProductionQueueItem>(state.Items.Count);
        ordered.Clear();
        foreach (var item in state.Items)
        {
            ordered.Add(item);
        }

        SortProductionQueueItemsById(ordered);
        foreach (var item in ordered)
        {
            hash = Add(hash, item.Id);
            hash = Add(hash, item.DesignId);
            hash = Add(hash, item.Progress);
            hash = Add(hash, (int)item.Faction);
        }

        ordered.Clear();
        return hash;
    }

    private static ulong AddAbilityRuntime(
        ulong hash,
        AbilityRuntimeComponentState state,
        List<AbilityCooldownState>? abilityCooldownOrder)
    {
        hash = Add(hash, state.Cooldowns.Count);
        var ordered = abilityCooldownOrder ?? new List<AbilityCooldownState>(state.Cooldowns.Count);
        ordered.Clear();
        foreach (var cooldown in state.Cooldowns)
        {
            ordered.Add(cooldown);
        }

        SortAbilityCooldownsByKind(ordered);
        foreach (var cooldown in ordered)
        {
            hash = Add(hash, (int)cooldown.Kind);
            hash = Add(hash, cooldown.CooldownRemaining);
        }

        ordered.Clear();
        return hash;
    }

    private static ulong AddCommandQueue(
        ulong hash,
        CommandQueueComponentState state,
        List<EntityCommand>? commandQueueOrder,
        List<EntityId>? commandSubjectOrder)
    {
        hash = Add(hash, state.Items.Count);
        var ordered = commandQueueOrder ?? new List<EntityCommand>(state.Items.Count);
        ordered.Clear();
        foreach (var item in state.Items)
        {
            ordered.Add(item);
        }

        SortCommandQueueItems(ordered);
        foreach (var item in ordered)
        {
            hash = Add(hash, (int)item.Kind);
            hash = Add(hash, item.Issuer.Value);
            hash = Add(hash, item.Tick);
            hash = Add(hash, item.Subjects.Count);
            hash = AddCommandSubjects(hash, item, commandSubjectOrder);
        }

        ordered.Clear();
        return hash;
    }

    private static ulong AddCommandSubjects(ulong hash, EntityCommand item, List<EntityId>? commandSubjectOrder)
    {
        var ordered = commandSubjectOrder ?? new List<EntityId>(item.Subjects.Count);
        ordered.Clear();
        foreach (var subject in item.Subjects)
        {
            ordered.Add(subject);
        }

        SortEntityIdsByValue(ordered);
        foreach (var subject in ordered)
        {
            hash = Add(hash, subject.Value);
        }

        ordered.Clear();
        return hash;
    }

    private static void SortWeaponMountsByMountId(List<WeaponMountRuntimeState> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            var current = values[index];
            var previous = index - 1;
            while (previous >= 0 && StringComparer.Ordinal.Compare(values[previous].MountId, current.MountId) > 0)
            {
                values[previous + 1] = values[previous];
                previous--;
            }

            values[previous + 1] = current;
        }
    }

    private static void SortProductionQueueItemsById(List<UnitProductionQueueItem> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            var current = values[index];
            var previous = index - 1;
            while (previous >= 0 && values[previous].Id > current.Id)
            {
                values[previous + 1] = values[previous];
                previous--;
            }

            values[previous + 1] = current;
        }
    }

    private static void SortAbilityCooldownsByKind(List<AbilityCooldownState> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            var current = values[index];
            var previous = index - 1;
            while (previous >= 0 && values[previous].Kind > current.Kind)
            {
                values[previous + 1] = values[previous];
                previous--;
            }

            values[previous + 1] = current;
        }
    }

    private static void SortCommandQueueItems(List<EntityCommand> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            var current = values[index];
            var previous = index - 1;
            while (previous >= 0 && CompareCommandQueueItems(values[previous], current) > 0)
            {
                values[previous + 1] = values[previous];
                previous--;
            }

            values[previous + 1] = current;
        }
    }

    private static int CompareCommandQueueItems(EntityCommand left, EntityCommand right)
    {
        var tick = left.Tick.CompareTo(right.Tick);
        return tick != 0 ? tick : left.Kind.CompareTo(right.Kind);
    }

    private static void SortEntityIdsByValue(List<EntityId> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            var current = values[index];
            var previous = index - 1;
            while (previous >= 0 && values[previous].Value > current.Value)
            {
                values[previous + 1] = values[previous];
                previous--;
            }

            values[previous + 1] = current;
        }
    }
}
