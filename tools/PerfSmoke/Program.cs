using System.Diagnostics;
using Godot;
using ProceduralRts.Core;

// PerfSmoke: headless simulation performance baseline (docs/RTS99Design.md "性能").
// Spawns N armed units split across two hostile owners, runs the full authoritative
// pipeline for M ticks, and reports per-tick step cost percentiles. Acts as a
// regression gate: fails if the average sim-step time exceeds a budget.
//
// This measures SIMULATION cost only (no Godot rendering); render-side budgets are
// covered by the in-engine PerfHud item in the TODO Performance Optimization Plan.

static void Fail(string message)
{
    Console.Error.WriteLine($"FAIL: {message}");
    System.Environment.Exit(1);
}

static EntityWorld BuildWorld(int unitCount)
{
    var world = new EntityWorld(seed: 9001) { WorldWidth = 4800, WorldHeight = 3200 };
    world.AddSystem(new CommandSystem());
    world.AddSystem(new CombatSystem());
    world.AddSystem(new ProjectileSystem());
    world.AddSystem(new MovementSystem());
    world.AddSystem(new SeparationSystem());
    world.AddSystem(new VisionSystem());
    world.AddSystem(new OutcomeSystem(new OwnerId(1)));
    world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

    // Real authored content so the cost reflects actual unit specs.
    var dog = UnitDesignCatalog.Spec("dog.infantry");
    var cat = UnitDesignCatalog.Spec("cat.basic");

    var perSide = unitCount / 2;
    var cols = (int)MathF.Ceiling(MathF.Sqrt(perSide));
    for (var i = 0; i < perSide; i++)
    {
        var row = i / cols;
        var col = i % cols;
        // Two clusters that will close and fight, exercising all systems under load.
        world.SpawnUnit(dog, new OwnerId(1), new Vector2(900 + col * 44, 700 + row * 44));
        world.SpawnUnit(cat, new OwnerId(2), new Vector2(3200 + col * 44, 700 + row * 44));
    }

    return world;
}

static (double Avg, double P50, double P99, double Max, double AllocBytesPerTick) Measure(int unitCount, int ticks)
{
    var world = BuildWorld(unitCount);
    var clock = new SimClock();
    var samples = new double[ticks];
    var events = new List<SimEvent>();
    var sw = new Stopwatch();
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

    for (var tick = 1; tick <= ticks; tick++)
    {
        sw.Restart();
        world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        world.Events.DrainInto(events);
        events.Clear();
        sw.Stop();
        samples[tick - 1] = sw.Elapsed.TotalMilliseconds;
    }

    var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
    Array.Sort(samples);
    var avg = samples.Average();
    var p50 = samples[(int)(ticks * 0.50)];
    var p99 = samples[Math.Min(ticks - 1, (int)(ticks * 0.99))];
    var max = samples[^1];
    var allocBytesPerTick = (allocatedAfter - allocatedBefore) / (double)ticks;
    return (avg, p50, p99, max, allocBytesPerTick);
}

const int Ticks = 1200; // 40s at 30Hz
var counts = new[] { 50, 100, 200, 400 };

// Budget: at 30Hz the fixed delta is ~33.3ms, so a single sim step must stay well
// under that. We hold the average step to a conservative fraction of the budget.
const double TickBudgetMs = 1000.0 / SimClock.DefaultTicksPerSecond; // 33.3ms
const double AvgBudgetFraction = 0.5; // average step must use <50% of the tick budget

Console.WriteLine($"PerfSmoke: {Ticks} ticks/run, tick budget {TickBudgetMs:0.0}ms (30Hz).");
Console.WriteLine($"{"units",6} | {"avg ms",8} | {"p50 ms",8} | {"p99 ms",8} | {"max ms",8} | {"alloc/tick",11}");

// JIT warmup so the first measured run is not penalized.
Measure(50, 200);

var worstAvg = 0.0;
var worstAvgCount = 0;
var worstAlloc = 0.0;
var worstAllocCount = 0;
foreach (var count in counts)
{
    var (avg, p50, p99, max, allocBytesPerTick) = Measure(count, Ticks);
    Console.WriteLine($"{count,6} | {avg,8:0.000} | {p50,8:0.000} | {p99,8:0.000} | {max,8:0.000} | {allocBytesPerTick,11:0}");
    if (avg > worstAvg)
    {
        worstAvg = avg;
        worstAvgCount = count;
    }

    if (allocBytesPerTick > worstAlloc)
    {
        worstAlloc = allocBytesPerTick;
        worstAllocCount = count;
    }
}

var budget = TickBudgetMs * AvgBudgetFraction;
if (worstAvg > budget)
{
    Fail($"sim step too slow: {worstAvgCount} units averaged {worstAvg:0.000}ms > budget {budget:0.000}ms");
}

Console.WriteLine($"OK: worst average {worstAvg:0.000}ms at {worstAvgCount} units, under budget {budget:0.000}ms.");
Console.WriteLine($"OK: worst allocation {worstAlloc:0} bytes/tick at {worstAllocCount} units.");
Console.WriteLine("PerfSmoke PASSED.");
