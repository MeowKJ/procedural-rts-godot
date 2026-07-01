static partial class Program
{
    static void Fail(string message)
    {
        Console.Error.WriteLine($"FAIL: {message}");
        System.Environment.Exit(1);
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Fail(message);
        }
    }
    static bool SamePoint(PathPoint a, PathPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy <= 1f;
    }
    static List<ulong> RunWorld(Func<EntityWorld> build, IReadOnlyList<EntityCommand> log, int tickCount, int checkpointEvery)
    {
        var world = build();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in log)
        {
            buffer.Enqueue(command);
        }

        // Checkpoint 0 is the pre-simulation baseline, so "did the world evolve?"
        // compares initial vs final even when a scenario reaches steady state early.
        var checkpoints = new List<ulong> { world.DeterministicStateHash() };
        for (var tick = 1; tick <= tickCount; tick++)
        {
            var due = buffer.DrainUpToTick(tick);
            world.Step(tick, clock.FixedDelta, due);
            world.Events.Drain(); // presentation would consume these; drop here.

            if (tick % checkpointEvery == 0)
            {
                checkpoints.Add(world.DeterministicStateHash());
            }
        }

        return checkpoints;
    }

    static void AssertDeterministic(string name, Func<EntityWorld> build, IReadOnlyList<EntityCommand> log, int tickCount, int checkpointEvery)
    {
        var a = RunWorld(build, log, tickCount, checkpointEvery);
        var b = RunWorld(build, log, tickCount, checkpointEvery);

        if (a.Count != b.Count)
        {
            Fail($"[{name}] checkpoint count mismatch: {a.Count} vs {b.Count}");
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                Fail($"[{name}] state hash diverged at checkpoint {i}: {a[i]} vs {b[i]}");
            }
        }

        if (a.Count >= 2 && a[0] == a[^1])
        {
            Fail($"[{name}] state hash never changed; simulation did not advance");
        }

        Console.WriteLine($"OK [{name}]: {tickCount} ticks, {a.Count} checkpoints, deterministic. final={a[^1]}");
    }

    static void AssertDeterministic(string name, Func<EntityWorld> build, int tickCount, int checkpointEvery)
    {
        AssertDeterministic(name, build, Array.Empty<EntityCommand>(), tickCount, checkpointEvery);
    }
}
