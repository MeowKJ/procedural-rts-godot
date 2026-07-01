namespace ProceduralRts.Core;

public sealed partial class MovementSystem
{
    private static bool IsCombatAnchor(EntityWorld world, EntityInstance entity, MovementComponentState movement)
    {
        if (movement.MoveTarget is not null
            || !entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
            || weapon.AttackTarget.Value <= 0
            || !world.TryGet(weapon.AttackTarget, out var target))
        {
            return false;
        }

        var (baseRange, coolingDown) = WeaponMath.MaxRangeAndCooling(world, weapon);
        var range = UpgradeResolver.WeaponRange(world, entity, baseRange);

        if (range <= 0 || !coolingDown)
        {
            return false;
        }

        return entity.Transform.Position.DistanceTo(target.Transform.Position) <= range;
    }
}
