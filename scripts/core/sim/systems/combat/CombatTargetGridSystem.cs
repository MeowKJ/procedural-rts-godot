using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private void BuildTargetGrid(EntityWorld world)
    {
        _targetGrid.Clear();

        var maxAcquireRange = 1f;
        foreach (var entity in world.OrderedEntities)
        {
            if (!IsDead(entity) && entity.Components.Has<HealthComponentState>())
            {
                _targetGrid.Add(entity.Transform.Position, entity);
            }

            if (!entity.Components.TryGet<WeaponUserComponentState>(out var weapon) || IsDead(entity))
            {
                continue;
            }

            var autonomy = EffectiveAutonomy(world, entity, weapon);
            if (!autonomy.AllowsAutoAcquire)
            {
                continue;
            }

            var acquireRange = autonomy.AcquireRange;
            if (entity.Components.TryGet<GuardOrderComponentState>(out var guard) && guard.Radius > acquireRange)
            {
                acquireRange = guard.Radius;
            }

            if (acquireRange > maxAcquireRange)
            {
                maxAcquireRange = acquireRange;
            }
        }

        if (maxAcquireRange > _targetGrid.CellSize)
        {
            RebuildTargetGrid(world, maxAcquireRange);
        }
    }

    private void RebuildTargetGrid(EntityWorld world, float cellSize)
    {
        _targetGrid.Reset(cellSize);
        foreach (var entity in world.OrderedEntities)
        {
            if (IsDead(entity) || !entity.Components.Has<HealthComponentState>())
            {
                continue;
            }

            _targetGrid.Add(entity.Transform.Position, entity);
        }
    }
}
