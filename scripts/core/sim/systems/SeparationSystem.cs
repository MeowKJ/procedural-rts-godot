using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Severe-collision separation (docs/RTS99Design.md weight order: this outranks
/// lateral avoidance). After movement integrates, any two overlapping
/// blocks-movement entities are pushed apart along their contact normal, weighted
/// so lighter/moving units yield to heavier/standing ones. Runs last in the
/// pipeline and iterates stable pairs (lower EntityId first) for determinism.
///
/// A recently-fired unit is never displaced by a moving unit during its short
/// fire-anchor window, so rear units settle around active shooters.
/// </summary>
public sealed class SeparationSystem : ISimSystem
{
    private const float CellSize = 96f;
    private readonly SpatialGrid<EntityInstance> _grid = new(CellSize);

    public void Step(SimContext context)
    {
        var world = context.World;
        var dt = context.FixedDelta;
        var settle = Mathf.Clamp(dt * 16f, 0.15f, 0.9f);

        // Bucket collidable entities by cell for near-neighbor pair checks.
        _grid.Clear();
        foreach (var entity in world.OrderedEntities)
        {
            if (entity.Components.TryGet<CollisionComponentState>(out var c) && c.BlocksMovement)
            {
                _grid.Add(entity.Transform.Position, entity);
            }
        }

        // For each entity, resolve against neighbors with a higher EntityId so each
        // pair is handled exactly once, in a deterministic order.
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<CollisionComponentState>(out var selfCollision) || !selfCollision.BlocksMovement)
            {
                continue;
            }

            foreach (var other in _grid.Neighbors(entity.Transform.Position))
            {
                if (other.Id.Value <= entity.Id.Value)
                {
                    continue;
                }

                ResolvePair(context.World, entity, selfCollision, other, settle);
            }
        }
    }

    private static void ResolvePair(EntityWorld world, EntityInstance first, CollisionComponentState firstCollision, EntityInstance second, float settle)
    {
        if (!second.Components.TryGet<CollisionComponentState>(out var secondCollision) || !secondCollision.BlocksMovement)
        {
            return;
        }

        var delta = second.Transform.Position - first.Transform.Position;
        var distance = delta.Length();
        var minDistance = firstCollision.Radius + secondCollision.Radius;
        if (distance >= minDistance || minDistance <= 0)
        {
            return;
        }

        // Deterministic fallback normal when exactly coincident.
        var normal = distance <= 0.001f
            ? new Vector2(MathF.Cos(first.Id.Value), MathF.Sin(first.Id.Value)).Normalized()
            : delta / distance;
        var overlap = minDistance - distance;

        var firstWeight = ResolveWeight(first, firstCollision);
        var secondWeight = ResolveWeight(second, secondCollision);
        var total = firstWeight + secondWeight;
        var firstShare = total <= 0 ? 0.5f : secondWeight / total;
        var secondShare = total <= 0 ? 0.5f : firstWeight / total;
        var firstAnchor = IsHardAnchor(world, first);
        var secondAnchor = IsHardAnchor(world, second);
        var pairSettle = settle;
        if (firstAnchor && !secondAnchor)
        {
            firstShare = 0;
            secondShare = 1;
            pairSettle = 1;
            world.Metrics.RecordAnchorPushEvent();
        }
        else if (secondAnchor && !firstAnchor)
        {
            firstShare = 1;
            secondShare = 0;
            pairSettle = 1;
            world.Metrics.RecordAnchorPushEvent();
        }

        first.Transform = first.Transform with { Position = first.Transform.Position - (normal * overlap * firstShare * pairSettle) };
        second.Transform = second.Transform with { Position = second.Transform.Position + (normal * overlap * secondShare * pairSettle) };
    }

    private static bool IsHardAnchor(EntityWorld world, EntityInstance entity)
    {
        if (!entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            return true;
        }

        return movement.FireAnchorRemaining > 0 || IsCombatAnchor(world, entity, movement);
    }

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

    private static float ResolveWeight(EntityInstance entity, CollisionComponentState collision)
    {
        // Standing units are heavier; recently-fired units become hard anchors.
        // Mass and push priority add to the weight.
        var moving = entity.Components.TryGet<MovementComponentState>(out var m) && m.MoveTarget is not null;
        var baseWeight = collision.Mass + collision.PushPriority;
        if (m?.FireAnchorRemaining > 0)
        {
            return baseWeight + 8f;
        }

        return moving ? MathF.Max(0.1f, baseWeight) : baseWeight + 4f;
    }
}
