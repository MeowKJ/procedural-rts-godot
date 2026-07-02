using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private static AutonomyModel EffectiveAutonomy(
        EntityWorld world,
        EntityInstance entity,
        WeaponUserComponentState weapon)
    {
        var stance = entity.Components.TryGet<StanceComponentState>(out var stanceState)
            ? stanceState.Stance
            : UnitStance.Aggressive;
        var weaponRange = WeaponMath.EffectiveRange(world, entity, weapon);
        var acquireRange = entity.Components.TryGet<AutonomyComponentState>(out var autonomy)
            ? autonomy.AcquireRange
            : entity.Components.TryGet<VisionComponentState>(out var vision)
                ? vision.SightRange
                : weaponRange;
        var leashRange = autonomy?.LeashRange ?? MathF.Max(acquireRange, weaponRange);
        var anchor = autonomy?.AnchorPosition ?? stanceState?.AnchorPosition;

        return stance switch
        {
            UnitStance.Hold => new AutonomyModel(
                AcquireRange: MathF.Min(acquireRange > 0 ? acquireRange : weaponRange, weaponRange),
                LeashRange: weaponRange,
                AnchorPosition: anchor,
                AllowsAutoAcquire: true,
                AllowsChase: false,
                ShouldReturnToAnchor: false),
            UnitStance.ReturnGuard => new AutonomyModel(
                AcquireRange: acquireRange,
                LeashRange: leashRange,
                AnchorPosition: anchor,
                AllowsAutoAcquire: true,
                AllowsChase: true,
                ShouldReturnToAnchor: true),
            UnitStance.PassiveRetaliate => new AutonomyModel(
                AcquireRange: 0,
                LeashRange: leashRange,
                AnchorPosition: anchor,
                AllowsAutoAcquire: false,
                AllowsChase: false,
                ShouldReturnToAnchor: false),
            UnitStance.Ignore => new AutonomyModel(
                AcquireRange: 0,
                LeashRange: 0,
                AnchorPosition: anchor,
                AllowsAutoAcquire: false,
                AllowsChase: false,
                ShouldReturnToAnchor: false),
            _ => new AutonomyModel(
                AcquireRange: acquireRange,
                LeashRange: leashRange,
                AnchorPosition: anchor,
                AllowsAutoAcquire: true,
                AllowsChase: true,
                ShouldReturnToAnchor: false),
        };
    }

    private static bool IsOutsideLeash(EntityInstance attacker, EntityInstance target, AutonomyModel autonomy)
    {
        if (!autonomy.ShouldReturnToAnchor || autonomy.AnchorPosition is not { } anchor || autonomy.LeashRange <= 0)
        {
            return false;
        }

        var leashSq = autonomy.LeashRange * autonomy.LeashRange;
        return target.Transform.Position.DistanceSquaredTo(anchor) > leashSq
            || attacker.Transform.Position.DistanceSquaredTo(anchor) > leashSq;
    }

    private static bool IsOutsideRetaliationLeash(EntityInstance attacker, EntityInstance target, AutonomyModel autonomy)
    {
        if (autonomy.AnchorPosition is not { } anchor || autonomy.LeashRange <= 0)
        {
            return false;
        }

        var leashSq = autonomy.LeashRange * autonomy.LeashRange;
        return target.Transform.Position.DistanceSquaredTo(anchor) > leashSq
            || attacker.Transform.Position.DistanceSquaredTo(anchor) > leashSq;
    }

    private static void ReturnToAnchor(EntityInstance entity, AutonomyModel autonomy)
    {
        if (!autonomy.ShouldReturnToAnchor
            || autonomy.AnchorPosition is not { } anchor
            || !entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            return;
        }

        if (entity.Transform.Position.DistanceSquaredTo(anchor) <= 4f || !entity.Components.Has<MovementProfileComponentState>())
        {
            if (movement.MoveTarget is not null || movement.Velocity != Vector2.Zero)
            {
                entity.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null, FormationSlot = null });
            }

            return;
        }

        entity.Components.Set(movement with { MoveTarget = anchor, FormationSlot = null });
    }

    private readonly record struct AutonomyModel(
        float AcquireRange,
        float LeashRange,
        Vector2? AnchorPosition,
        bool AllowsAutoAcquire,
        bool AllowsChase,
        bool ShouldReturnToAnchor);
}
