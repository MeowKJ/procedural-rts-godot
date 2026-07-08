namespace ProceduralRts.Core;

public sealed partial class AbilitySystem
{
    private static bool TryResolveTargetPoint(
        EntityWorld world,
        AbilityEntityCommand command,
        EntityInstance caster,
        out Godot.Vector2 targetPoint)
    {
        if (command.Target.IsValid && world.TryGet(command.Target, out var target))
        {
            targetPoint = target.Transform.Position;
            return true;
        }

        if (command.TargetPoint is { } point)
        {
            targetPoint = point;
            return true;
        }

        targetPoint = caster.Transform.Position;
        return true;
    }

    private static bool HasValidTarget(
        EntityWorld world,
        AbilityEntityCommand command,
        EntityInstance caster,
        AbilitySpec ability)
    {
        var rule = ResolveTargetRule(ability);
        return rule switch
        {
            AbilityTargetRule.Self => !command.Target.IsValid || command.Target.Value == caster.Id.Value,
            AbilityTargetRule.Point => command.TargetPoint is not null,
            AbilityTargetRule.Entity => command.Target.IsValid && world.TryGet(command.Target, out _),
            AbilityTargetRule.FriendlyEntity => command.Target.IsValid
                && world.TryGet(command.Target, out var friendly)
                && IsFriendly(world, caster, friendly),
            AbilityTargetRule.HostileEntity => command.Target.IsValid
                && world.TryGet(command.Target, out var hostile)
                && world.Relations.CanAttack(caster.OwnerId, hostile.OwnerId),
            AbilityTargetRule.PointOrEntity => command.TargetPoint is not null
                || (command.Target.IsValid && world.TryGet(command.Target, out _)),
            AbilityTargetRule.FriendlyPointOrEntity => command.TargetPoint is not null
                || !command.Target.IsValid
                || (world.TryGet(command.Target, out var friendly)
                    && IsFriendly(world, caster, friendly)),
            AbilityTargetRule.HostilePointOrEntity => command.TargetPoint is not null
                || (command.Target.IsValid
                    && world.TryGet(command.Target, out var hostile)
                    && world.Relations.CanAttack(caster.OwnerId, hostile.OwnerId)),
            _ => false,
        };
    }

    private static AbilityTargetRule ResolveTargetRule(AbilitySpec ability)
    {
        if (ability.TargetRule != AbilityTargetRule.Auto)
        {
            return ability.TargetRule;
        }

        return ability.Kind switch
        {
            AbilityKind.Deploy => AbilityTargetRule.Self,
            AbilityKind.Scan => AbilityTargetRule.Point,
            AbilityKind.RepairField => AbilityTargetRule.FriendlyPointOrEntity,
            AbilityKind.ShieldField => AbilityTargetRule.FriendlyPointOrEntity,
            _ => AbilityTargetRule.PointOrEntity,
        };
    }

    private static bool CanPayCost(EntityWorld world, OwnerId ownerId, AbilitySpec ability)
    {
        return ability.Cost <= 0 || world.ResourceInventory(ownerId).Credits >= ability.Cost;
    }

    private static void PayCost(EntityWorld world, OwnerId ownerId, AbilitySpec ability)
    {
        if (ability.Cost <= 0)
        {
            return;
        }

        var inventory = world.ResourceInventory(ownerId);
        inventory.Credits = Math.Max(0, inventory.Credits - ability.Cost);
    }

    private static bool TryGetAbility(EntitySpec spec, AbilityKind kind, out AbilitySpec ability)
    {
        foreach (var candidate in spec.Abilities)
        {
            if (candidate.Kind == kind)
            {
                ability = candidate;
                return true;
            }
        }

        ability = default!;
        return false;
    }

    private static bool IsOnCooldown(AbilityRuntimeComponentState runtime, AbilityKind kind)
    {
        foreach (var cooldown in runtime.Cooldowns)
        {
            if (cooldown.Kind == kind && cooldown.CooldownRemaining > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDeployed(EntityInstance entity)
    {
        return entity.Components.TryGet<DeployComponentState>(out var deploy) && deploy.IsDeployed;
    }

    private static bool IsFriendly(EntityWorld world, EntityInstance caster, EntityInstance candidate)
    {
        return world.Relations.Relation(caster.OwnerId, candidate.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied;
    }

    private static void SetCooldown(
        EntityInstance caster,
        AbilityRuntimeComponentState runtime,
        AbilityKind kind,
        float seconds)
    {
        for (var index = 0; index < runtime.Cooldowns.Count; index++)
        {
            var cooldown = runtime.Cooldowns[index];
            if (cooldown.Kind == kind)
            {
                var cooldowns = WritableCooldowns(caster, runtime);
                cooldowns[index] = cooldown with { CooldownRemaining = seconds };
                return;
            }
        }

        var expanded = new AbilityCooldownState[runtime.Cooldowns.Count + 1];
        for (var index = 0; index < runtime.Cooldowns.Count; index++)
        {
            expanded[index] = runtime.Cooldowns[index];
        }

        expanded[^1] = new AbilityCooldownState(kind, seconds);
        caster.Components.Set(runtime with { Cooldowns = expanded });
    }

    private static IList<AbilityCooldownState> WritableCooldowns(
        EntityInstance entity,
        AbilityRuntimeComponentState runtime)
    {
        if (runtime.Cooldowns is AbilityCooldownState[] array)
        {
            return array;
        }

        if (runtime.Cooldowns is List<AbilityCooldownState> list)
        {
            return list;
        }

        var copy = new AbilityCooldownState[runtime.Cooldowns.Count];
        for (var index = 0; index < runtime.Cooldowns.Count; index++)
        {
            copy[index] = runtime.Cooldowns[index];
        }

        entity.Components.Set(runtime with { Cooldowns = copy });
        return copy;
    }

    private static bool IsDead(EntityInstance entity)
    {
        return entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0;
    }
}
