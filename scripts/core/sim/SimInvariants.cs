using Godot;

namespace ProceduralRts.Core;

public static partial class SimInvariants
{
    public const string EnvironmentToggle = "PROCEDURAL_RTS_SIM_INVARIANTS";
    public const int MaxCommandQueueItems = 32;
    public const int MaxProductionQueueItems = 32;

    public static IReadOnlyList<SimInvariantViolation> Validate(EntityWorld world)
    {
        var violations = new List<SimInvariantViolation>();
        var dockReservations = new Dictionary<int, EntityId>();

        foreach (var entity in world.OrderedEntities)
        {
            ValidateEntity(world, entity, violations, dockReservations);
        }

        return violations;
    }

    public static void AssertValid(EntityWorld world, int tick)
    {
        var violations = Validate(world);
        if (violations.Count == 0)
        {
            return;
        }

        var details = string.Join(System.Environment.NewLine, violations.Select(v => "- " + v));
        throw new InvalidOperationException($"Sim invariant failure at tick {tick}:{System.Environment.NewLine}{details}");
    }
}
