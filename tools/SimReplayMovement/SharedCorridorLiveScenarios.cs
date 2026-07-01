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
