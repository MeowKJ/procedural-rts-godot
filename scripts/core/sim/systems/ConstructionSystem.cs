namespace ProceduralRts.Core;

/// <summary>
/// Deterministic building construction authority. Build commands spend owner
/// credits up front, spawn an under-construction structure, and fixed ticks
/// advance ConstructionComponentState until existing consumer systems see it as
/// completed/active.
/// </summary>
public sealed partial class ConstructionSystem : ISimSystem
{
    private readonly List<PlacementBuildAnchor> _placementBuildAnchors = new();
    private readonly List<PlacementObstacle> _placementObstacles = new();
    private readonly List<PlacementBuildVisibility> _placementVisibility = new();
    private readonly List<string> _requiredBuildingOrder = new();
    private readonly List<EntityId> _constructionSubjectOrder = new();

    public void Step(SimContext context)
    {
        foreach (var sequenced in context.Commands)
        {
            if (sequenced.Command is StartConstructionEntityCommand build)
            {
                ApplyStartConstruction(context.World, build);
            }
            else if (sequenced.Command is QueueConstructionEntityCommand queue)
            {
                ApplyQueueConstruction(context.World, queue);
            }
            else if (sequenced.Command is CancelConstructionEntityCommand cancel)
            {
                ApplyCancelConstruction(context.World, cancel);
            }
        }

        AdvanceConstruction(context.World, context.Tick, context.FixedDelta);
    }
}
