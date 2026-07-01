using Godot;

namespace ProceduralRts.Core;

public static class ResourceMiningMath
{
    public static bool TryFindNearestAvailableResourceNode(
        EntityWorld world,
        Vector2 from,
        out EntityInstance resource,
        out ResourceNodeComponentState node)
    {
        EntityInstance? best = null;
        ResourceNodeComponentState? bestNode = null;
        var bestDistanceSq = float.MaxValue;

        foreach (var candidate in world.OrderedEntities)
        {
            if (!candidate.Components.TryGet<ResourceNodeComponentState>(out var candidateNode)
                || candidateNode.Amount <= 0)
            {
                continue;
            }

            var distanceSq = candidate.Transform.Position.DistanceSquaredTo(from);
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                best = candidate;
                bestNode = candidateNode;
            }
        }

        if (best is not null && bestNode is not null)
        {
            resource = best;
            node = bestNode;
            return true;
        }

        resource = null!;
        node = null!;
        return false;
    }
}
