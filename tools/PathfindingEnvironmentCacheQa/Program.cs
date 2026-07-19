using System.Diagnostics;
using Godot;
using ProceduralRts.Core;

const int UnitCount = 4;
const int WarmedReplans = 128;

var failures = new List<string>();
var individual = RunScenario(sharedCorridor: false, MovementDomain.Land);
var individualRepeat = RunScenario(sharedCorridor: false, MovementDomain.Land);
Require(individual.PathSignature == individualRepeat.PathSignature,
    "individual planning must remain deterministic across equivalent worlds", failures);
Require(individual.Metrics.RasterBuilds == 1 && individual.Metrics.CacheHits == UnitCount - 1,
    $"individual plans should build once then hit {UnitCount - 1} times, got {individual.Metrics}", failures);

var shared = RunScenario(sharedCorridor: true, MovementDomain.Land);
var sharedRepeat = RunScenario(sharedCorridor: true, MovementDomain.Land);
Require(shared.PathSignature == sharedRepeat.PathSignature,
    "shared-corridor planning must remain deterministic across equivalent worlds", failures);
Require(shared.Metrics.RasterBuilds == 1 && shared.Metrics.CacheHits == 0,
    $"one shared corridor should build its authored grid once, got {shared.Metrics}", failures);
Require(shared.Paths.All(path => path.Waypoints.Count >= 2 && path.Waypoints.All(AvoidsWall)),
    "shared paths must retain the authored-wall detour after cache reuse", failures);

var air = RunScenario(sharedCorridor: false, MovementDomain.Air);
Require(air.Paths.All(path => path.Waypoints.Count == 1 && IsGoal(path.Waypoints[0])),
    "air paths must keep ignoring authored static building blockers", failures);

var invalidation = BuildScenario(sharedCorridor: false, MovementDomain.Land);
invalidation.World.Step(1, 0.1f, []);
invalidation.World.WorldWidth += 64;
ResetForReplan(invalidation, targetOffset: 0);
invalidation.World.Step(2, 0.1f, []);
Require(invalidation.Pathfinding.EnvironmentRasterCacheMetrics.RasterBuilds == 2,
    "changing world dimensions must invalidate the authored-environment cache key", failures);

var benchmark = MeasureWarmedSharedReplans();
Require(benchmark.RasterBuildsDuringWarmLoop == 0,
    $"warmed replans must not rebuild authored raster data, got {benchmark.RasterBuildsDuringWarmLoop}", failures);
Require(benchmark.CacheHitsDuringWarmLoop == WarmedReplans,
    $"each warmed shared replan should hit the cache once, got {benchmark.CacheHitsDuringWarmLoop}", failures);
Require(benchmark.AllocatedBytes < 8_000_000,
    $"warmed shared replans allocated {benchmark.AllocatedBytes:N0} bytes, exceeding the bounded 8 MB QA budget", failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("PathfindingEnvironmentCacheQa FAILED");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    System.Environment.Exit(1);
}

Console.WriteLine(
    "PathfindingEnvironmentCacheQa PASSED: " +
    $"individual {individual.Metrics.RasterBuilds} build/{individual.Metrics.CacheHits} hits, " +
    $"shared {shared.Metrics.RasterBuilds} build/{shared.Metrics.CacheHits} hits, " +
    $"warmed {WarmedReplans} shared replans used {benchmark.RasterBuildsDuringWarmLoop} authored-raster rebuilds " +
    $"vs {WarmedReplans} uncached-equivalent rebuilds, {benchmark.AllocatedBytes:N0} bytes, {benchmark.ElapsedMilliseconds:0.0} ms.");

static ScenarioResult RunScenario(bool sharedCorridor, MovementDomain domain)
{
    var scenario = BuildScenario(sharedCorridor, domain);
    scenario.World.Step(1, 0.1f, []);
    return new ScenarioResult(
        ScenarioPathSignature(scenario.Entities),
        scenario.Entities.Select(entity => entity.Components.Require<PathfindingComponentState>()).ToArray(),
        scenario.Pathfinding.EnvironmentRasterCacheMetrics);
}

static ReplanBenchmark MeasureWarmedSharedReplans()
{
    var scenario = BuildScenario(sharedCorridor: true, MovementDomain.Land);
    scenario.World.Step(1, 0.1f, []);

    ResetForReplan(scenario, targetOffset: 0);
    scenario.World.Step(2, 0.1f, []);
    ResetForReplan(scenario, targetOffset: 1);
    scenario.World.Step(3, 0.1f, []);

    var warmed = scenario.Pathfinding.EnvironmentRasterCacheMetrics;
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var stopwatch = Stopwatch.StartNew();
    for (var replan = 0; replan < WarmedReplans; replan++)
    {
        ResetForReplan(scenario, replan);
        scenario.World.Step(replan + 4, 0.1f, []);
    }

    stopwatch.Stop();
    var after = scenario.Pathfinding.EnvironmentRasterCacheMetrics;
    return new ReplanBenchmark(
        after.RasterBuilds - warmed.RasterBuilds,
        after.CacheHits - warmed.CacheHits,
        GC.GetAllocatedBytesForCurrentThread() - allocatedBefore,
        stopwatch.Elapsed.TotalMilliseconds);
}

static void ResetForReplan(Scenario scenario, int targetOffset)
{
    var intent = new Vector2(672, 256 + (targetOffset & 1) * 32);
    for (var index = 0; index < scenario.Entities.Count; index++)
    {
        var entity = scenario.Entities[index];
        var slot = new Vector2(intent.X, intent.Y + (index - 1.5f) * 32);
        var movement = entity.Components.Require<MovementComponentState>();
        entity.Components.Remove<PathfindingComponentState>();
        entity.Components.Set(movement with
        {
            Velocity = default,
            MoveTarget = scenario.SharedCorridor ? slot : intent,
            FormationSlot = scenario.SharedCorridor ? slot : null,
        });
        entity.Components.Set(new CommandableComponentState(
            PlayerIntentTarget: scenario.SharedCorridor ? intent : null));
    }
}

static Scenario BuildScenario(bool sharedCorridor, MovementDomain domain)
{
    var map = new MapSpec
    {
        Id = $"pathfinding.cache.{sharedCorridor}.{domain}",
        Seed = 562,
        WorldSize = new MapSize(768, 512),
        OwnerStarts =
        [
            new(new OwnerId(1), FactionId.Dog, new MapPoint(64, 64), 0, 0),
            new(new OwnerId(2), FactionId.Cat, new MapPoint(704, 448), MathF.PI, 0),
        ],
        TerrainCells = [new MapTerrainCellSpec("ground", new MapRect(0, 0, 768, 512), "ground")],
        Obstacles = [new MapObstacleSpec("wall", new MapRect(256, 128, 64, 256))],
    };
    var world = MapLoader.Load(map);
    var pathfinding = new PathfindingSystem(64);
    world.AddSystem(pathfinding);
    var spec = new EntitySpec
    {
        Id = $"pathfinding.cache.mover.{domain}",
        Kind = EntityKind.Unit,
        Display = new EntityDisplaySpec("Cache Mover", "cache.mover.name", "cache.mover.role", "CM", IconGlyph.Infantry),
        Movement = new MovementSpec(domain, Speed: 120, TurnRate: 6),
        Collision = new CollisionSpec(12, 1, 1, BlocksMovement: true),
    };
    var entities = new List<EntityInstance>(UnitCount);
    var intent = new Vector2(672, 256);
    for (var index = 0; index < UnitCount; index++)
    {
        var slot = new Vector2(intent.X, intent.Y + (index - 1.5f) * 32);
        entities.Add(world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(96, 160 + index * 64)),
        [
            new MovementComponentState(default, sharedCorridor ? slot : intent, sharedCorridor ? slot : null),
            new MovementProfileComponentState(120, 6),
            new CollisionComponentState(12, 1, 1, BlocksMovement: true),
            new CommandableComponentState(PlayerIntentTarget: sharedCorridor ? intent : null),
        ]));
    }

    return new Scenario(world, pathfinding, entities, sharedCorridor);
}

static string ScenarioPathSignature(IReadOnlyList<EntityInstance> entities)
{
    return string.Join("|", entities.Select(entity => string.Join(",", entity.Components
        .Require<PathfindingComponentState>()
        .Waypoints
        .Select(point => $"{point.X:0.###}:{point.Y:0.###}"))));
}

static bool AvoidsWall(PathPoint point)
{
    return MathF.Floor(point.X / 64f) != 4 || MathF.Floor(point.Y / 64f) is < 2 or > 5;
}

static bool IsGoal(PathPoint point)
{
    return MathF.Abs(point.X - 672) < 0.001f && point.Y is >= 208 and <= 304;
}

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

readonly record struct Scenario(
    EntityWorld World,
    PathfindingSystem Pathfinding,
    IReadOnlyList<EntityInstance> Entities,
    bool SharedCorridor);

readonly record struct ScenarioResult(
    string PathSignature,
    IReadOnlyList<PathfindingComponentState> Paths,
    PathfindingEnvironmentRasterCacheMetrics Metrics);

readonly record struct ReplanBenchmark(
    int RasterBuildsDuringWarmLoop,
    int CacheHitsDuringWarmLoop,
    long AllocatedBytes,
    double ElapsedMilliseconds);
