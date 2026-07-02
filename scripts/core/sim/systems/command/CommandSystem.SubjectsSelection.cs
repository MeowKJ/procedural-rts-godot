namespace ProceduralRts.Core;

public sealed partial class CommandSystem
{
    private static void CollectOwnedSubjects(
        EntityWorld world,
        OwnerId issuer,
        IReadOnlyList<EntityId> subjects,
        List<EntityInstance> result)
    {
        result.Clear();
        foreach (var entityId in subjects)
        {
            if (world.TryGet(entityId, out var entity) && entity.OwnerId.Value == issuer.Value)
            {
                result.Add(entity);
            }
        }
    }

    private void ApplySelection(EntityWorld world, SetSelectionEntityCommand command)
    {
        _selectionSubjectIds.Clear();
        foreach (var id in command.Subjects)
        {
            _selectionSubjectIds.Add(id.Value);
        }

        foreach (var entity in world.OrderedEntities)
        {
            if (entity.OwnerId.Value != command.Issuer.Value
                || !entity.Components.TryGet<SelectableComponentState>(out var selectable))
            {
                continue;
            }

            entity.Components.Set(selectable with { Selected = _selectionSubjectIds.Contains(entity.Id.Value) });
        }

        _selectionSubjectIds.Clear();
    }
}
