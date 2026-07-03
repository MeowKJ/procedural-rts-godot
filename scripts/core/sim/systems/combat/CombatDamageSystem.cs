using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    internal static void ApplyResolvedDamage(
        SimContext context,
        EntityInstance target,
        EntityInstance attacker,
        float incomingDamage)
    {
        ApplyResolvedDamage(context, target, attacker, incomingDamage, recordRetaliation: true);
    }

    internal static void ApplyResolvedDamage(
        SimContext context,
        EntityInstance target,
        EntityInstance attacker,
        float incomingDamage,
        bool recordRetaliation)
    {
        if (!target.Components.TryGet<HealthComponentState>(out var health) || health.Hp <= 0)
        {
            return;
        }

        var damage = ApplyShieldAbsorption(target, incomingDamage);
        var newHp = health.Hp - damage;
        target.Components.Set(health with { Hp = newHp });

        context.World.Events.Raise(new EntityDamagedEvent(
            context.Tick, target.Id, attacker.Id, damage, target.Transform.Position));

        if (newHp <= 0)
        {
            if (recordRetaliation)
            {
                VeterancySystem.AwardKill(context.World, attacker, target);
            }

            context.World.Events.Raise(new EntityDestroyedEvent(
                context.Tick, target.Id, target.OwnerId, target.Transform.Position));
            context.World.QueueRemoval(target.Id);
        }
        else if (recordRetaliation)
        {
            RecordRetaliationThreat(context.World, target, attacker, context.Tick);
            RecordHarvesterThreat(context.World, target, attacker);
        }
    }

    private static float ApplyShieldAbsorption(EntityInstance target, float incomingDamage)
    {
        if (incomingDamage <= 0
            || !target.Components.TryGet<ShieldComponentState>(out var shield)
            || shield.AbsorbRemaining <= 0
            || shield.DurationRemaining <= 0)
        {
            return incomingDamage;
        }

        var absorbed = MathF.Min(incomingDamage, shield.AbsorbRemaining);
        var remainingAbsorb = shield.AbsorbRemaining - absorbed;
        if (remainingAbsorb <= 0)
        {
            target.Components.Remove<ShieldComponentState>();
        }
        else
        {
            target.Components.Set(shield with { AbsorbRemaining = remainingAbsorb });
        }

        return incomingDamage - absorbed;
    }

    private static void RecordRetaliationThreat(EntityWorld world, EntityInstance victim, EntityInstance attacker, int tick)
    {
        if (victim.Id.Value == attacker.Id.Value
            || !IsPassiveRetaliate(victim)
            || !victim.Components.TryGet<WeaponUserComponentState>(out var weapon)
            || weapon.AttackTargetIsManual
            || weapon.Mounts.Count == 0
            || WeaponMath.EffectiveRange(world, victim, weapon) <= 0
            || !world.Relations.CanAttack(victim.OwnerId, attacker.OwnerId)
            || !IsVisibleToOwner(world, victim.OwnerId, attacker)
            || TargetPriority(world, weapon, attacker) <= 0)
        {
            return;
        }

        victim.Components.Set(new RetaliationComponentState(attacker.Id, tick));
    }

    private static void RecordHarvesterThreat(EntityWorld world, EntityInstance victim, EntityInstance attacker)
    {
        if (victim.Id.Value == attacker.Id.Value
            || !victim.Components.TryGet<HarvesterComponentState>(out var harvester)
            || harvester.Mode == HarvesterMode.Idle
            || harvester.Retreating
            || !world.Relations.CanAttack(victim.OwnerId, attacker.OwnerId))
        {
            return;
        }

        victim.Components.Set(harvester with { Retreating = true });
    }

    private static bool IsPassiveRetaliate(EntityInstance entity)
    {
        return entity.Components.TryGet<StanceComponentState>(out var stance)
            && stance.Stance == UnitStance.PassiveRetaliate;
    }

}
