using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Converts authoritative movement intents into deterministic waypoint paths
/// before MovementSystem consumes them. It keeps dynamic-unit avoidance local to
/// MovementSystem/SeparationSystem and plans only around static EntityWorld
/// blockers such as buildings, using PathfindingMath's LOS simplification.
/// </summary>
public sealed partial class PathfindingSystem : ISimSystem
{
    private const float SamePointDistanceSquared = 1f;
    private readonly float _cellSize;
    private readonly List<GridObstacle> _obstacles = [];
    private readonly List<GridTerrain> _terrain = [];
    private readonly HashSet<GridObstacle> _seenObstacles = [];
    private readonly HashSet<int> _sharedPlanned = [];
    private readonly Dictionary<SharedMoveKey, List<SharedMoveCandidate>> _sharedGroups = [];
    private readonly List<SharedMoveKey> _sharedGroupKeys = [];
    private readonly List<PathfindingCorridorMember> _sharedMembers = [];
    private readonly List<PathfindingCorridorAssignment> _sharedAssignmentResults = [];
    private readonly Dictionary<int, PathfindingCorridorAssignment> _sharedAssignments = [];
    private readonly PathfindingWorkspace _pathWorkspace = new();

    public PathfindingSystem(float cellSize = PathfindingStaticGrid.RuntimeCellSize)
    {
        _cellSize = cellSize;
    }

    public void Step(SimContext context)
    {
        if (_cellSize <= 0)
        {
            return;
        }

        var world = context.World;
        var sharedPlanned = PlanSharedCorridors(world);
        foreach (var entity in world.OrderedEntities)
        {
            if (sharedPlanned.Contains(entity.Id.Value))
            {
                continue;
            }

            if (!entity.Components.TryGet<MovementComponentState>(out var movement)
                || !entity.Components.TryGet<MovementProfileComponentState>(out _))
            {
                entity.Components.Remove<PathfindingComponentState>();
                continue;
            }

            if (entity.Components.TryGet<DeployComponentState>(out var deploy) && deploy.IsDeployed)
            {
                entity.Components.Remove<PathfindingComponentState>();
                continue;
            }

            if (movement.MoveTarget is not { } moveTarget)
            {
                AdvanceOrClearPath(entity, movement);
                continue;
            }

            if (entity.Components.TryGet<PathfindingComponentState>(out var path)
                && IsFollowingCurrentPath(moveTarget, path))
            {
                AdvanceArcTurnCornerIfNeeded(entity, movement, path);
                continue;
            }

            PlanPath(world, entity, movement, moveTarget);
        }
    }

    private HashSet<int> PlanSharedCorridors(EntityWorld world)
    {
        _sharedPlanned.Clear();
        ClearSharedGroups();
        foreach (var entity in world.OrderedEntities)
        {
            if (!TryGetSharedMoveCandidate(world, entity, out var candidate))
            {
                continue;
            }

            var key = new SharedMoveKey(
                entity.OwnerId.Value,
                candidate.Domain,
                Quantize(candidate.Intent.X),
                Quantize(candidate.Intent.Y),
                candidate.MoveMode);
            if (!_sharedGroups.TryGetValue(key, out var group))
            {
                group = [];
                _sharedGroups[key] = group;
            }

            if (group.Count == 0)
            {
                _sharedGroupKeys.Add(key);
            }

            group.Add(candidate);
        }

        foreach (var key in _sharedGroupKeys)
        {
            var group = _sharedGroups[key];
            if (group.Count <= 1)
            {
                continue;
            }

            BuildStaticBlockers(world, movingEntityId: 0, group[0].Domain);
            _sharedMembers.Clear();
            foreach (var candidate in group)
            {
                _sharedMembers.Add(new PathfindingCorridorMember(
                    candidate.Entity.Id.Value,
                    candidate.Entity.Transform.Position.X,
                    candidate.Entity.Transform.Position.Y,
                    candidate.Slot.X,
                    candidate.Slot.Y));
            }

            var corridor = PathfindingMath.FindSharedCorridor(
                _pathWorkspace,
                _sharedMembers,
                group[0].Intent.X,
                group[0].Intent.Y,
                world.WorldWidth,
                world.WorldHeight,
                _cellSize,
                _obstacles,
                group[0].Domain,
                _terrain,
                _sharedAssignmentResults);
            _sharedAssignments.Clear();
            foreach (var assignment in corridor.Assignments)
            {
                _sharedAssignments[assignment.Id] = assignment;
            }

            foreach (var candidate in group)
            {
                if (!_sharedAssignments.TryGetValue(candidate.Entity.Id.Value, out var assignment))
                {
                    continue;
                }

                var goal = new PathPoint(candidate.Slot.X, candidate.Slot.Y);
                var waypoints = PathOrGoal(assignment.Path, goal);
                var path = new PathfindingComponentState(
                    goal,
                    waypoints,
                    NextWaypointIndex: 0);
                SetNextWaypoint(candidate.Entity, candidate.Movement, path);
                _sharedPlanned.Add(candidate.Entity.Id.Value);
            }
        }

        return _sharedPlanned;
    }

    private void ClearSharedGroups()
    {
        foreach (var key in _sharedGroupKeys)
        {
            _sharedGroups[key].Clear();
        }

        _sharedGroupKeys.Clear();
    }

    private static bool TryGetSharedMoveCandidate(
        EntityWorld world,
        EntityInstance entity,
        out SharedMoveCandidate candidate)
    {
        candidate = default;
        if (!entity.Components.TryGet<MovementComponentState>(out var movement)
            || !entity.Components.TryGet<MovementProfileComponentState>(out _)
            || movement.MoveTarget is null
            || movement.FormationSlot is not { } slot
            || !entity.Components.TryGet<CommandableComponentState>(out var commandable)
            || commandable.PlayerIntentTarget is not { } intent
            || (entity.Components.TryGet<WeaponUserComponentState>(out var weapon) && weapon.AttackTargetIsManual)
            || (entity.Components.TryGet<DeployComponentState>(out var deploy) && deploy.IsDeployed))
        {
            return false;
        }

        if (entity.Components.TryGet<PathfindingComponentState>(out var path)
            && IsFollowingCurrentPath(movement.MoveTarget.Value, path))
        {
            return false;
        }

        var domain = MovementDomain.Land;
        if (world.TryGetSpec(entity.SpecId, out var spec) && spec.Movement is not null)
        {
            domain = spec.Movement.Domain;
        }

        candidate = new SharedMoveCandidate(entity, movement, slot, intent, domain, commandable.MoveMode);
        return true;
    }

    private void AdvanceOrClearPath(EntityInstance entity, MovementComponentState movement)
    {
        if (!entity.Components.TryGet<PathfindingComponentState>(out var path))
        {
            return;
        }

        if (path.NextWaypointIndex >= path.Waypoints.Count)
        {
            entity.Components.Remove<PathfindingComponentState>();
            return;
        }

        if (path.NextWaypointIndex > 0
            && !SamePoint(entity.Transform.Position, path.Waypoints[path.NextWaypointIndex - 1]))
        {
            entity.Components.Remove<PathfindingComponentState>();
            return;
        }

        SetNextWaypoint(entity, movement, path);
    }

    private void PlanPath(
        EntityWorld world,
        EntityInstance entity,
        MovementComponentState movement,
        Vector2 goal)
    {
        var domain = MovementDomain.Land;
        if (world.TryGetSpec(entity.SpecId, out var spec) && spec.Movement is not null)
        {
            domain = spec.Movement.Domain;
        }

        BuildStaticBlockers(world, entity.Id.Value, domain);

        var result = PathfindingMath.FindPathWithDebug(
            _pathWorkspace,
            entity.Transform.Position.X,
            entity.Transform.Position.Y,
            goal.X,
            goal.Y,
            world.WorldWidth,
            world.WorldHeight,
            _cellSize,
            _obstacles,
            domain,
            _terrain);

        var pathGoal = new PathPoint(goal.X, goal.Y);
        var waypoints = PathOrGoal(result.Path, pathGoal);

        var path = new PathfindingComponentState(
            pathGoal,
            waypoints,
            NextWaypointIndex: 0);
        SetNextWaypoint(entity, movement, path);
    }

    private static IReadOnlyList<PathPoint> PathOrGoal(IReadOnlyList<PathPoint> path, PathPoint goal)
    {
        return path.Count == 0 ? [goal] : path;
    }

    private void SetNextWaypoint(
        EntityInstance entity,
        MovementComponentState movement,
        PathfindingComponentState path)
    {
        if (path.NextWaypointIndex >= path.Waypoints.Count)
        {
            entity.Components.Remove<PathfindingComponentState>();
            return;
        }

        var waypointIndex = SelectNextWaypointIndex(entity, path);
        var waypoint = path.Waypoints[waypointIndex];
        entity.Components.Set(path with { NextWaypointIndex = waypointIndex + 1 });
        entity.Components.Set(movement with { MoveTarget = new Vector2(waypoint.X, waypoint.Y) });
    }

    private void BuildStaticBlockers(EntityWorld world, int movingEntityId, MovementDomain domain)
    {
        if (!CopyCachedEnvironment(world, domain))
        {
            return;
        }

        foreach (var entity in world.OrderedEntities)
        {
            if (entity.Id.Value == movingEntityId
                || !entity.Components.TryGet<CollisionComponentState>(out var collision)
                || !collision.BlocksMovement
                || !IsStaticPathBlocker(world, entity))
            {
                continue;
            }

            PathfindingStaticGrid.AppendCircle(
                new StaticPathCircle(
                    entity.Transform.Position.X,
                    entity.Transform.Position.Y,
                    collision.Radius),
                world.WorldWidth,
                world.WorldHeight,
                _cellSize,
                _obstacles,
                _seenObstacles);
        }
    }

    private static bool IsStaticPathBlocker(EntityWorld world, EntityInstance entity)
    {
        return world.TryGetSpec(entity.SpecId, out var spec)
            && spec.Kind is EntityKind.Building or EntityKind.Turret or EntityKind.Objective;
    }

    private static bool IsFollowingCurrentPath(Vector2 target, PathfindingComponentState path)
    {
        if (path.NextWaypointIndex > 0
            && SamePoint(target, path.Waypoints[path.NextWaypointIndex - 1]))
        {
            return true;
        }

        return path.NextWaypointIndex >= path.Waypoints.Count
            && SamePoint(target, path.Goal);
    }

    private static bool SamePoint(Vector2 point, PathPoint pathPoint)
    {
        return SamePoint(point.X, point.Y, pathPoint.X, pathPoint.Y);
    }

    private static bool SamePoint(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return dx * dx + dy * dy <= SamePointDistanceSquared;
    }

    private static int Quantize(float value)
    {
        return (int)MathF.Round(value * 10f);
    }

    private readonly record struct SharedMoveKey(
        int Owner,
        MovementDomain Domain,
        int IntentX,
        int IntentY,
        MoveCommandMode MoveMode);

    private readonly record struct SharedMoveCandidate(
        EntityInstance Entity,
        MovementComponentState Movement,
        Vector2 Slot,
        Vector2 Intent,
        MovementDomain Domain,
        MoveCommandMode MoveMode);
}
