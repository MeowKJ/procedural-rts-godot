using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private static WeaponUserComponentState TickAutoReacquireCooldown(
        EntityInstance entity,
        WeaponUserComponentState weapon,
        float dt)
    {
        if (weapon.AttackTargetIsManual)
        {
            if (weapon.AutoReacquireCooldownRemaining <= 0)
            {
                return weapon;
            }

            var manualNext = weapon with { AutoReacquireCooldownRemaining = 0 };
            entity.Components.Set(manualNext);
            return manualNext;
        }

        if (weapon.AutoReacquireCooldownRemaining <= 0)
        {
            return weapon;
        }

        var remaining = MathF.Max(0, weapon.AutoReacquireCooldownRemaining - dt);
        var next = weapon with { AutoReacquireCooldownRemaining = remaining };
        entity.Components.Set(next);
        return next;
    }

    private static WeaponUserComponentState TickLastKnownTargetMemory(
        EntityInstance entity,
        WeaponUserComponentState weapon,
        float dt)
    {
        if (weapon.LastKnownTargetRemaining <= 0)
        {
            if (weapon.LastKnownTargetPosition is null && Mathf.IsEqualApprox(weapon.LastKnownTargetRemaining, 0))
            {
                return weapon;
            }

            var cleared = weapon with
            {
                LastKnownTargetPosition = null,
                LastKnownTargetRemaining = 0,
            };
            entity.Components.Set(cleared);
            return cleared;
        }

        var remaining = MathF.Max(0, weapon.LastKnownTargetRemaining - dt);
        var next = remaining <= 0
            ? weapon with
            {
                LastKnownTargetPosition = null,
                LastKnownTargetRemaining = 0,
            }
            : weapon with { LastKnownTargetRemaining = remaining };
        entity.Components.Set(next);
        return next;
    }
}
