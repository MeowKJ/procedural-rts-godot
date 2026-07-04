static partial class Program
{
    static void RunEntitySharedCorridorScenario()
    {
        const int SharedCorridorTicks = 700;
        var groupIds = Enumerable.Range(1, 4).Select(id => new EntityId(id)).ToList();
        var target = new Vector2(608, 280);
        var commands = new List<EntityCommand>
        {
            new GroupMoveEntityCommand(new OwnerId(1), groupIds, 1, target, MoveCommandMode.Direct),
        };

        AssertDeterministic("entity-shared-corridor", BuildSharedCorridorWorld, commands, SharedCorridorTicks, 100);

        var world = BuildSharedCorridorWorld();
        var buffer = new EntityCommandBuffer();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        world.Step(1, new SimClock().FixedDelta, buffer.DrainUpToTick(1));
        var paths = groupIds
            .Select(id => world.TryGet(id, out var entity) && entity.Components.TryGet<PathfindingComponentState>(out var path)
                ? path
                : null)
            .Where(path => path is not null)
            .Select(path => path!)
            .ToList();
        Assert(paths.Count == 4, $"shared corridor should create path state for all 4 movers, got {paths.Count}");
        Assert(CountSharedInteriorWaypoints(paths) >= 3, "shared corridor should make most live movers reuse a common spine waypoint");
        Assert(paths.All(path => path.Waypoints.All(point => !BlockedWallPoint(point))), "live shared corridor should avoid the static wall");

        for (var tick = 2; tick <= SharedCorridorTicks; tick++)
        {
            world.Step(tick, new SimClock().FixedDelta, []);
        }

        var arrived = groupIds.Count(id =>
            world.TryGet(id, out var entity)
            && entity.Components.TryGet<MovementComponentState>(out var movement)
            && movement.FormationSlot is { } slot
            && entity.Transform.Position.DistanceTo(slot) <= 8f);
        Assert(arrived >= 3, $"shared corridor movers should arrive near their formation slots, arrived {arrived}/4");
        Console.WriteLine("OK [entity-shared-corridor]: GroupMoveEntityCommand planned a shared live corridor and reached formation slots.");
    }

    static void RunEntityArcTurnCornerPathingScenario()
    {
        const int ArcTurnCornerTicks = 700;
        var unitId = new EntityId(1);
        var target = new Vector2(608, 280);
        var commands = new List<EntityCommand>
        {
            new MoveEntityCommand(new OwnerId(1), new[] { unitId }, 1, target, MoveCommandMode.Direct),
        };

        AssertDeterministic("entity-arc-turn-corner-pathing", BuildArcTurnCornerWorld, commands, ArcTurnCornerTicks, 100);

        var world = BuildArcTurnCornerWorld();
        var buffer = new EntityCommandBuffer();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        var clock = new SimClock();
        PathPoint? firstWaypoint = null;
        var visitedFirstWaypoint = false;
        var sawEarlyCornerAdvance = false;
        var maxWaypointCount = 0;
        var idleWithPathTicks = 0;
        for (var tick = 1; tick <= ArcTurnCornerTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            world.Events.Drain();

            if (!world.TryGet(unitId, out var entity)
                || !entity.Components.TryGet<PathfindingComponentState>(out var path)
                || !entity.Components.TryGet<MovementProfileComponentState>(out var profile))
            {
                continue;
            }

            maxWaypointCount = Math.Max(maxWaypointCount, path.Waypoints.Count);
            if (firstWaypoint is null && path.Waypoints.Count >= 2)
            {
                firstWaypoint = path.Waypoints[0];
            }

            if (firstWaypoint is { } first)
            {
                var firstPoint = new Vector2(first.X, first.Y);
                if (entity.Transform.Position.DistanceSquaredTo(firstPoint) <= profile.ArriveRadius * profile.ArriveRadius)
                {
                    visitedFirstWaypoint = true;
                }

                if (path.NextWaypointIndex >= 2 && !visitedFirstWaypoint)
                {
                    sawEarlyCornerAdvance = true;
                }
            }

            if (entity.Components.TryGet<MovementComponentState>(out var movement)
                && movement.MoveTarget is null
                && entity.Transform.Position.DistanceSquaredTo(target) > 64f)
            {
                idleWithPathTicks++;
            }
        }

        Assert(world.TryGet(unitId, out var finalEntity), "arc-turn corner unit missing after replay");
        Assert(maxWaypointCount >= 2, $"arc-turn corner path should include an obstacle corner, max waypoints {maxWaypointCount}");
        Assert(sawEarlyCornerAdvance, "arc-turn corner path should advance before snapping exactly onto the first corner waypoint");
        Assert(idleWithPathTicks == 0, $"arc-turn corner path idled with path before arrival for {idleWithPathTicks} ticks");
        Assert(finalEntity.Transform.Position.DistanceTo(target) <= 10f, $"arc-turn corner unit did not reach target: pos {finalEntity.Transform.Position}, target {target}");
        Assert(world.Metrics.MovementStuckSeconds <= 0.05, $"arc-turn corner movement stuck for {world.Metrics.MovementStuckSeconds:0.000}s");

        Console.WriteLine($"OK [entity-arc-turn-corner-pathing]: ArcTurn path advanced through a {maxWaypointCount}-waypoint obstacle corner without idle path ticks.");
    }

    private static EntityWorld BuildSharedCorridorWorld()
    {
        const float cell = 64f;
        var world = new EntityWorld(seed: 9090) { WorldWidth = 768, WorldHeight = 512 };
        world.AddSystem(new CommandSystem());
        world.AddSystem(new PathfindingSystem(cell));
        world.AddSystem(new MovementSystem());
        world.AddSystem(new SeparationSystem());

        var unitSpec = new EntitySpec
        {
            Id = "shared.live.unit",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Shared Unit", "shared.unit.name", "shared.unit.role", "SU", IconGlyph.Infantry),
            Movement = new MovementSpec(MovementDomain.Land, Speed: 165, TurnRate: 7),
            Collision = new CollisionSpec(14, 1, 1, BlocksMovement: true),
        };
        var blockerSpec = new EntitySpec
        {
            Id = "shared.live.blocker",
            Kind = EntityKind.Building,
            Display = new EntityDisplaySpec("Blocker", "shared.blocker.name", "shared.blocker.role", "BLK", IconGlyph.Building),
            Collision = new CollisionSpec(31, 8, 100, BlocksMovement: true),
        };

        var starts = new[]
        {
            new Vector2(96, 128),
            new Vector2(96, 208),
            new Vector2(96, 304),
            new Vector2(96, 384),
        };
        foreach (var start in starts)
        {
            world.Spawn(unitSpec, new OwnerId(1), EntityTransform.At(start), new EntityComponentState[]
            {
                new MovementComponentState(Velocity: default),
                new MovementProfileComponentState(MaxSpeed: 165, ArriveRadius: 7),
                new CollisionComponentState(14, 1, 1, BlocksMovement: true),
                new CommandableComponentState(),
            });
        }

        for (var y = 1; y <= 5; y++)
        {
            world.Spawn(
                blockerSpec,
                new OwnerId(2),
                EntityTransform.At(new Vector2((4.5f) * cell, (y + 0.5f) * cell)),
                new EntityComponentState[]
                {
                    new CollisionComponentState(31, 8, 100, BlocksMovement: true),
                });
        }

        return world;
    }

    private static EntityWorld BuildArcTurnCornerWorld()
    {
        const float cell = 64f;
        var world = new EntityWorld(seed: 9091) { WorldWidth = 768, WorldHeight = 512 };
        world.AddSystem(new CommandSystem());
        world.AddSystem(new PathfindingSystem(cell));
        world.AddSystem(new MovementSystem());
        world.AddSystem(new SeparationSystem());

        var unitSpec = new EntitySpec
        {
            Id = "arc.turn.corner.unit",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Arc Turn Unit", "arc.unit.name", "arc.unit.role", "AT", IconGlyph.Tank),
            Movement = new MovementSpec(MovementDomain.Land, Speed: 132, TurnRate: 4.6f, TurnMode: TurnMode.ArcTurn),
            Collision = new CollisionSpec(14, 1, 1, BlocksMovement: true),
        };
        var blockerSpec = new EntitySpec
        {
            Id = "arc.turn.corner.blocker",
            Kind = EntityKind.Building,
            Display = new EntityDisplaySpec("Blocker", "arc.blocker.name", "arc.blocker.role", "BLK", IconGlyph.Building),
            Collision = new CollisionSpec(31, 8, 100, BlocksMovement: true),
        };

        world.Spawn(unitSpec, new OwnerId(1), new EntityTransform(new Vector2(96, 128), 0), new EntityComponentState[]
        {
            new MovementComponentState(Velocity: default),
            new MovementProfileComponentState(MaxSpeed: 132, ArriveRadius: 7, TurnRate: 4.6f, TurnMode: TurnMode.ArcTurn),
            new CollisionComponentState(14, 1, 1, BlocksMovement: true),
            new CommandableComponentState(),
        });

        for (var y = 1; y <= 5; y++)
        {
            world.Spawn(
                blockerSpec,
                new OwnerId(2),
                EntityTransform.At(new Vector2(4.5f * cell, (y + 0.5f) * cell)),
                new EntityComponentState[]
                {
                    new CollisionComponentState(31, 8, 100, BlocksMovement: true),
                });
        }

        return world;
    }

    private static int CountSharedInteriorWaypoints(IReadOnlyList<PathfindingComponentState> paths)
    {
        return paths
            .SelectMany(path => path.Waypoints.Take(Math.Max(0, path.Waypoints.Count - 1)))
            .GroupBy(point => (X: MathF.Round(point.X), Y: MathF.Round(point.Y)))
            .Select(group => group.Count())
            .DefaultIfEmpty(0)
            .Max();
    }

    private static bool BlockedWallPoint(PathPoint point)
    {
        return MathF.Floor(point.X / 64f) == 4 && MathF.Floor(point.Y / 64f) is >= 1 and <= 5;
    }
}
