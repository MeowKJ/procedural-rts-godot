using Godot;

namespace ProceduralRts.Core;

public sealed partial class MovementSystem
{
    private static bool TryResolveCrowdedArrivalStop(
        int entityId,
        Vector2 target,
        float radius,
        SpatialGrid<LocalAvoidanceBody> grid,
        out Vector2 stopPosition)
    {
        var offset = Vector2.Zero;

        foreach (var other in grid.Neighbors(target.X, target.Y))
        {
            if (other.Id == entityId)
            {
                continue;
            }

            var fromOther = target - new Vector2(other.X, other.Y);
            var distance = fromOther.Length();
            var desired = radius + other.Radius + ArrivalPadding;
            if (distance >= desired)
            {
                continue;
            }

            var normal = distance <= 0.001f
                ? DeterministicNormal(entityId, other.Id)
                : fromOther / distance;
            var bias = other.IsAnchor ? 1.2f : 1f;
            offset += normal * (desired - distance) * bias;
        }

        if (offset.LengthSquared() <= 0.0001f)
        {
            stopPosition = target;
            return false;
        }

        stopPosition = target + offset.LimitLength(ArrivalMaxOffset);
        return true;
    }

    private static Vector2 DeterministicNormal(int firstId, int secondId)
    {
        var angle = ((firstId * 37 + secondId * 17) % 360) * MathF.PI / 180f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)).Normalized();
    }
}
