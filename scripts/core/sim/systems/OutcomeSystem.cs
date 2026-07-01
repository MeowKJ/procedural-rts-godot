namespace ProceduralRts.Core;

/// <summary>
/// Resolves match outcome generically from a perspective owner: that owner is
/// defeated when it has lost all its victory-critical entities; it wins when
/// every hostile owner has lost all of theirs. "Victory-critical" is data on the
/// entity (<see cref="ObjectiveComponentState.IsVictoryCritical"/>), so this never
/// hard-codes "destroy the HQ" — a campaign can mark any entity as critical.
///
/// Only meaningful once at least one critical entity has existed, so an empty
/// early world is not instantly a victory.
/// </summary>
public sealed class OutcomeSystem : ISimSystem
{
    private readonly OwnerId _perspective;

    public OutcomeSystem(OwnerId perspective)
    {
        _perspective = perspective;
    }

    public void Step(SimContext context)
    {
        var world = context.World;
        if (world.Outcome != GameOutcome.InProgress)
        {
            return;
        }

        var selfCriticalAlive = 0;
        var hostileCriticalAlive = 0;
        var sawAnyCritical = false;

        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<ObjectiveComponentState>(out var objective) || !objective.IsVictoryCritical)
            {
                continue;
            }

            var alive = !entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0;
            if (!alive)
            {
                continue;
            }

            sawAnyCritical = true;
            var relation = world.Relations.Relation(_perspective, entity.OwnerId);
            if (relation == PlayerRelation.Self)
            {
                selfCriticalAlive++;
            }
            else if (relation == PlayerRelation.Hostile)
            {
                hostileCriticalAlive++;
            }
        }

        if (!sawAnyCritical)
        {
            return;
        }

        if (selfCriticalAlive == 0)
        {
            world.Outcome = GameOutcome.Defeat;
        }
        else if (hostileCriticalAlive == 0)
        {
            world.Outcome = GameOutcome.Victory;
        }
    }
}
