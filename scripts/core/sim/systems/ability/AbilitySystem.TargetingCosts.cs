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
        return runtime.Cooldowns.Any(cooldown => cooldown.Kind == kind && cooldown.CooldownRemaining > 0);
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
        var cooldowns = runtime.Cooldowns.ToArray();
        for (var index = 0; index < cooldowns.Length; index++)
        {
            if (cooldowns[index].Kind == kind)
            {
                cooldowns[index] = cooldowns[index] with { CooldownRemaining = seconds };
                caster.Components.Set(runtime with { Cooldowns = cooldowns });
                return;
            }
        }

        caster.Components.Set(runtime with
        {
            Cooldowns = cooldowns.Append(new AbilityCooldownState(kind, seconds)).ToArray(),
        });
    }

    private static bool IsDead(EntityInstance entity)
    {
        return entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0;
    }
}
