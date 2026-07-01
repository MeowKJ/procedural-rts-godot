using Godot;

namespace ProceduralRts.Core;

public static partial class SimInvariants
{
    private static void ValidateDock(
        EntityWorld world,
        EntityInstance entity,
        DockComponentState dock,
        List<SimInvariantViolation> violations,
        Dictionary<int, EntityId> dockReservations)
    {
        CheckEntityReference(world, entity, "Dock.ReservedByEntityId", dock.ReservedByEntityId, violations);
        CheckEntityReference(world, entity, "Dock.DockedEntityId", dock.DockedEntityId, violations);

        if (dock.ReservedByEntityId is not { } reserved)
        {
            return;
        }

        if (dockReservations.TryGetValue(reserved, out var existingDock))
        {
            Add(entity, "Dock", $"entity {reserved} is reserved by docks {existingDock.Value} and {entity.Id.Value}", violations);
            return;
        }

        dockReservations[reserved] = entity.Id;
    }

    private static void CheckEntityReference(
        EntityWorld world,
        EntityInstance entity,
        string component,
        int? targetId,
        List<SimInvariantViolation> violations)
    {
        if (targetId is null || targetId.Value <= 0)
        {
            return;
        }

        if (!world.TryGet(new EntityId(targetId.Value), out _))
        {
            Add(entity, component, $"referenced entity {targetId.Value} does not exist", violations);
        }
    }

    private static void CheckFinite(EntityInstance entity, string component, Vector2? value, List<SimInvariantViolation> violations)
    {
        if (value.HasValue)
        {
            CheckFinite(entity, component, value.Value, violations);
        }
    }

    private static void CheckFinite(EntityInstance entity, string component, Vector2 value, List<SimInvariantViolation> violations)
    {
        if (!IsFinite(value.X) || !IsFinite(value.Y))
        {
            Add(entity, component, $"vector must be finite, got {value}", violations);
        }
    }

    private static void CheckFinite(EntityInstance entity, string component, float value, List<SimInvariantViolation> violations)
    {
        if (!IsFinite(value))
        {
            Add(entity, component, $"value must be finite, got {value}", violations);
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void Add(EntityInstance entity, string component, string message, List<SimInvariantViolation> violations)
    {
        violations.Add(new SimInvariantViolation(entity.Id, component, message));
    }
}
