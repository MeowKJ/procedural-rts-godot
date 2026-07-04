using Godot;

namespace ProceduralRts.Core;

public sealed partial class PathfindingSystem
{
    private const float ArcTurnCornerLookaheadMin = 8f;
    private const float ArcTurnCornerLookaheadCellFraction = 0.75f;
    private const float ArcTurnCornerAngleThreshold = 0.45f;

    private int SelectNextWaypointIndex(EntityInstance entity, PathfindingComponentState path)
    {
        var waypointIndex = path.NextWaypointIndex;
        return waypointIndex + 1 < path.Waypoints.Count
            && ShouldAdvanceArcTurnCorner(entity, path.Waypoints[waypointIndex], path.Waypoints[waypointIndex + 1])
            ? waypointIndex + 1
            : waypointIndex;
    }

    private void AdvanceArcTurnCornerIfNeeded(
        EntityInstance entity,
        MovementComponentState movement,
        PathfindingComponentState path)
    {
        var currentWaypointIndex = path.NextWaypointIndex - 1;
        var nextWaypointIndex = path.NextWaypointIndex;
        if (currentWaypointIndex < 0
            || nextWaypointIndex >= path.Waypoints.Count
            || !ShouldAdvanceArcTurnCorner(entity, path.Waypoints[currentWaypointIndex], path.Waypoints[nextWaypointIndex]))
        {
            return;
        }

        var next = path.Waypoints[nextWaypointIndex];
        entity.Components.Set(path with { NextWaypointIndex = nextWaypointIndex + 1 });
        entity.Components.Set(movement with { MoveTarget = new Vector2(next.X, next.Y) });
    }

    private bool ShouldAdvanceArcTurnCorner(EntityInstance entity, PathPoint currentWaypoint, PathPoint nextWaypoint)
    {
        if (!entity.Components.TryGet<MovementProfileComponentState>(out var profile)
            || profile.TurnMode != TurnMode.ArcTurn)
        {
            return false;
        }

        var current = new Vector2(currentWaypoint.X, currentWaypoint.Y);
        var next = new Vector2(nextWaypoint.X, nextWaypoint.Y);
        var toCurrent = current - entity.Transform.Position;
        var toNext = next - current;
        if (toNext.LengthSquared() <= SamePointDistanceSquared)
        {
            return false;
        }

        var turnRadius = profile.TurnRate > 0 ? profile.MaxSpeed / profile.TurnRate : 0;
        var lookahead = Mathf.Clamp(
            turnRadius + profile.ArriveRadius * 2f,
            ArcTurnCornerLookaheadMin,
            _cellSize * ArcTurnCornerLookaheadCellFraction);
        if (toCurrent.LengthSquared() > lookahead * lookahead)
        {
            return false;
        }

        if (toCurrent.LengthSquared() <= SamePointDistanceSquared)
        {
            return true;
        }

        var cornerAngle = MathF.Abs(Mathf.AngleDifference(toCurrent.Angle(), toNext.Angle()));
        return cornerAngle >= ArcTurnCornerAngleThreshold;
    }
}
