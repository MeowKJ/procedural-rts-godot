using Godot;

namespace ProceduralRts.Core;

public sealed partial class ResourceSystem
{
    private static bool TryGetResourceNode(
        EntityWorld world,
        int? id,
        out EntityInstance entity,
        out ResourceNodeComponentState node)
    {
        if (id is int entityId
            && world.TryGet(new EntityId(entityId), out entity!)
            && entity.Components.TryGet(out node!))
        {
            return true;
        }

        entity = null!;
        node = null!;
        return false;
    }

    private static void ResetHarvester(EntityInstance harvester, HarvesterComponentState state)
    {
        StopMoving(harvester);
        harvester.Components.Set(state with
        {
            Mode = HarvesterMode.Idle,
            FieldId = null,
            RefineryId = null,
        });
    }

    private static void SetMoveTarget(EntityInstance entity, Vector2 target)
    {
        if (!entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            return;
        }

        entity.Components.Set(movement with { MoveTarget = target });
    }

    private static void StopMoving(EntityInstance entity)
    {
        if (!entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            return;
        }

        entity.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null });
    }
}
