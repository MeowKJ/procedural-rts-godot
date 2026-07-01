static partial class Program
{
    static void AssertSimClockBacklogMetrics()
    {
        var clock = new SimClock();
        var emittedTicks = clock.Advance(clock.FixedDelta * 12.25);
        Assert(emittedTicks == 8, "SimClock should cap fixed ticks emitted by one hitch frame");
        Assert(clock.LastDroppedBacklogTicks == 4, "SimClock should expose last dropped backlog ticks");
        Assert(clock.DroppedBacklogEvents == 1, "SimClock should count backlog drop events");
        Assert(clock.DroppedBacklogTicks == 4, "SimClock should accumulate dropped backlog ticks");
        Assert(clock.LastDroppedBacklogSeconds > 0, "SimClock should expose dropped backlog seconds");

        var metrics = new SimMetrics();
        metrics.RecordClockBacklogDrop(clock.LastDroppedBacklogTicks, clock.LastDroppedBacklogSeconds);
        Assert(metrics.DroppedBacklogEvents == 1, "SimMetrics should count clock backlog drops");
        Assert(metrics.DroppedBacklogTicks == 4, "SimMetrics should accumulate clock backlog ticks");
        Assert(metrics.DroppedBacklogSeconds > 0, "SimMetrics should accumulate clock backlog seconds");

        clock.Advance(clock.FixedDelta * 0.5);
        Assert(clock.LastDroppedBacklogTicks == 0, "SimClock should reset last-drop ticks when no backlog is dropped");
        Console.WriteLine("OK [sim-clock]: backlog cap metrics recorded.");
    }

    static void AssertSystemTimingMetrics()
    {
        var disabled = new EntityWorld(seed: 7);
        disabled.AddSystem(new CommandSystem());
        disabled.SystemTimingEnabled = false;
        disabled.Step(1, new SimClock().FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        Assert(disabled.Metrics.SystemTimings.Count == 0, "system timing should be off by default unless enabled");

        var enabled = new EntityWorld(seed: 7);
        enabled.AddSystem(new CommandSystem());
        enabled.SystemTimingEnabled = true;
        enabled.Step(1, new SimClock().FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        Assert(enabled.Metrics.SystemTimings.TryGetValue(nameof(CommandSystem), out var timing), "enabled system timing should record CommandSystem");
        Assert(timing.Samples == 1, "system timing should record one sample per stepped system");
        Assert(timing.TotalMs >= 0 && timing.LastMs >= 0 && timing.MaxMs >= 0, "system timing values should be non-negative");

        Console.WriteLine("OK [system-timing]: debug per-system metrics recorded only when enabled.");
    }

    static void AssertSharedCorridorPathing()
    {
        const float Cell = 64f;
        var obstacles = new[]
        {
            new GridObstacle(4, 1),
            new GridObstacle(4, 2),
            new GridObstacle(4, 3),
            new GridObstacle(4, 4),
            new GridObstacle(4, 5),
        };
        var members = new[]
        {
            new PathfindingCorridorMember(1, 96, 128, 608, 208),
            new PathfindingCorridorMember(2, 96, 208, 608, 256),
            new PathfindingCorridorMember(3, 96, 304, 608, 304),
            new PathfindingCorridorMember(4, 96, 384, 608, 352),
        };

        var corridor = PathfindingMath.FindSharedCorridor(
            members,
            608,
            280,
            768,
            512,
            Cell,
            obstacles,
            MovementDomain.Land,
            []);

        Assert(corridor.Assignments.Count == members.Length, "shared corridor should return one assignment per member");
        Assert(corridor.SharedPath.Count is >= 2 and <= 5, $"shared corridor spine should be compact, count {corridor.SharedPath.Count}");
        Assert(corridor.Assignments.All(assignment => assignment.RawCells.Count > assignment.Path.Count), "shared corridor should keep raw A* cells for debug");

        var sharedInterior = corridor.SharedPath.Take(Math.Max(0, corridor.SharedPath.Count - 1)).ToList();
        var membersUsingSpine = corridor.Assignments.Count(assignment =>
            assignment.Path.Any(point => sharedInterior.Any(shared => SamePoint(point, shared))));
        Assert(membersUsingSpine >= 3, $"shared corridor should make most members reuse the spine, got {membersUsingSpine}");

        var maxCorners = 0;
        var maxInflation = 0f;
        foreach (var member in members)
        {
            var assignment = corridor.Assignments.Single(candidate => candidate.Id == member.Id);
            var final = assignment.Path[^1];
            Assert(SamePoint(final, new PathPoint(member.GoalX, member.GoalY)), $"shared corridor member {member.Id} should end at its slot");
            Assert(!assignment.Path.Any(point => MathF.Floor(point.X / Cell) == 4 && MathF.Floor(point.Y / Cell) is >= 1 and <= 5), "shared corridor should avoid blocked wall cells");

            var quality = PathQualityMath.Measure(member.StartX, member.StartY, assignment.Path);
            maxCorners = Math.Max(maxCorners, quality.CornerCount);
            maxInflation = MathF.Max(maxInflation, quality.TravelInflation);
        }

        Assert(maxCorners <= 4, $"shared corridor should cap member corner count, max {maxCorners}");
        Assert(maxInflation <= 1.75f, $"shared corridor should limit member path inflation, max {maxInflation:0.00}");
        Console.WriteLine($"OK [shared-corridor]: spine {corridor.SharedPath.Count} waypoints, {membersUsingSpine}/{members.Length} members reused it, max inflation {maxInflation:0.00}.");
    }

    static void AssertEntityWorldPathfinding()
    {
        const float Cell = 64f;
        var commands = EntityPathfindingCommands();
        var world = BuildEntityPathfindingWorld();
        var buffer = new EntityCommandBuffer();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        var clock = new SimClock();
        world.Step(1, clock.FixedDelta, buffer.DrainUpToTick(1));
        var mover = world.StableEntities.Single(entity => entity.Id.Value == 1);
        var path = mover.Components.Require<PathfindingComponentState>();
        var movement = mover.Components.Require<MovementComponentState>();

        Assert(path.Waypoints.Count is >= 2 and <= 5, $"EntityWorld path should be LOS-simplified, count {path.Waypoints.Count}");
        Assert(path.NextWaypointIndex == 1, "PathfindingSystem should hand MovementSystem the first waypoint immediately");
        Assert(movement.MoveTarget is { } firstWaypoint
            && !SamePoint(new PathPoint(firstWaypoint.X, firstWaypoint.Y), path.Goal),
            "first EntityWorld waypoint should detour before the final goal");
        Assert(!path.Waypoints.Any(IsWallCell), "EntityWorld path waypoints should not sit inside blocked wall cells");

        var quality = PathQualityMath.Measure(96, 96, path.Waypoints);
        Assert(quality.CornerCount <= 4, $"EntityWorld simplified path should keep corners low, got {quality.CornerCount}");
        Assert(quality.TravelInflation <= 1.85f, $"EntityWorld path inflation should stay bounded, got {quality.TravelInflation:0.00}");

        for (var tick = 2; tick <= 700; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        mover = world.StableEntities.Single(entity => entity.Id.Value == 1);
        Assert(mover.Transform.Position.DistanceTo(new Vector2(608, 352)) <= 3f, "EntityWorld mover should arrive at the final path goal");
        Assert(!mover.Components.Has<PathfindingComponentState>(), "PathfindingSystem should remove completed paths");
        Console.WriteLine($"OK [entity-pathfinding]: {path.Waypoints.Count} LOS-pruned waypoints, inflation {quality.TravelInflation:0.00}.");

        static bool IsWallCell(PathPoint point)
        {
            return MathF.Floor(point.X / Cell) == 4 && MathF.Floor(point.Y / Cell) is >= 1 and <= 5;
        }
    }

    static EntityWorld BuildEntityPathfindingWorld()
    {
        const float Cell = 64f;
        var world = new EntityWorld(seed: 4242)
        {
            WorldWidth = 768,
            WorldHeight = 512,
        };
        world.AddSystem(new CommandSystem());
        world.AddSystem(new PathfindingSystem(Cell));
        world.AddSystem(new MovementSystem());

        var moverSpec = new EntitySpec
        {
            Id = "replay.entity_path_mover",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Path Mover", "replay.entity_path_mover.name", "replay.entity_path_mover.role", "EPM", IconGlyph.Move),
            Movement = new MovementSpec(MovementDomain.Land, Speed: 180, TurnRate: 8),
            Collision = new CollisionSpec(Radius: 12, Mass: 1, PushPriority: 1),
        };
        world.Spawn(moverSpec, new OwnerId(1), EntityTransform.At(new Vector2(96, 96)), new EntityComponentState[]
        {
            new MovementComponentState(Vector2.Zero),
            new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2),
            new CollisionComponentState(Radius: 12, Mass: 1, PushPriority: 1, BlocksMovement: true),
        });

        var wallSpec = new EntitySpec
        {
            Id = "replay.entity_path_wall",
            Kind = EntityKind.Building,
            Display = new EntityDisplaySpec("Path Wall", "replay.entity_path_wall.name", "replay.entity_path_wall.role", "WL", IconGlyph.Building),
            Collision = new CollisionSpec(Radius: 30, Mass: 100, PushPriority: 9),
        };

        for (var y = 1; y <= 5; y++)
        {
            world.Spawn(wallSpec, new OwnerId(0), EntityTransform.At(new Vector2((4.5f) * Cell, (y + 0.5f) * Cell)), new EntityComponentState[]
            {
                new CollisionComponentState(Radius: 30, Mass: 100, PushPriority: 9, BlocksMovement: true),
            });
        }

        return world;
    }

    static IReadOnlyList<EntityCommand> EntityPathfindingCommands()
    {
        return
        [
            new MoveEntityCommand(
                new OwnerId(1),
                [new EntityId(1)],
                1,
                new Vector2(608, 352),
                MoveCommandMode.Direct),
        ];
    }
}
