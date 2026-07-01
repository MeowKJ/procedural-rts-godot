namespace ProceduralRts.Core;

/// <summary>
/// Fixed-step simulation clock. Real (Godot) frame deltas are accumulated and
/// converted into a whole number of fixed ticks, so authoritative simulation
/// never advances by a variable real-time delta.
///
/// Sim/View boundary (see docs/EntityFrameworkArchitecture.md): the View feeds
/// real delta in via <see cref="Advance"/>; the Simulation only ever sees the
/// fixed <see cref="FixedDelta"/> per tick. This is the prerequisite for
/// deterministic replay (same seed + same command log => same state hash).
/// </summary>
public sealed class SimClock
{
    public const int DefaultTicksPerSecond = 30;

    // Guard against the "spiral of death" when a frame hitches: cap how many
    // fixed ticks a single Advance call may emit.
    private const int MaxTicksPerAdvance = 8;

    private double _accumulator;

    public SimClock(int ticksPerSecond = DefaultTicksPerSecond)
    {
        if (ticksPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
        }

        TicksPerSecond = ticksPerSecond;
        FixedDelta = 1f / ticksPerSecond;
    }

    public int TicksPerSecond { get; }

    public float FixedDelta { get; }

    /// <summary>Authoritative tick counter, monotonically increasing.</summary>
    public int CurrentTick { get; private set; }

    public int DroppedBacklogEvents { get; private set; }
    public int DroppedBacklogTicks { get; private set; }
    public double DroppedBacklogSeconds { get; private set; }
    public int LastDroppedBacklogTicks { get; private set; }
    public double LastDroppedBacklogSeconds { get; private set; }

    /// <summary>
    /// Feed a real (frame) delta and emit how many fixed ticks should run this
    /// frame. The driver runs the sim once per returned tick.
    /// </summary>
    public int Advance(double realDelta)
    {
        LastDroppedBacklogTicks = 0;
        LastDroppedBacklogSeconds = 0;

        if (realDelta <= 0)
        {
            return 0;
        }

        _accumulator += realDelta;

        var ticks = 0;
        while (_accumulator >= FixedDelta && ticks < MaxTicksPerAdvance)
        {
            _accumulator -= FixedDelta;
            CurrentTick++;
            ticks++;
        }

        // Drop leftover backlog beyond the cap so we do not stay permanently behind.
        if (_accumulator >= FixedDelta)
        {
            LastDroppedBacklogSeconds = _accumulator;
            LastDroppedBacklogTicks = (int)Math.Floor(_accumulator / FixedDelta);
            DroppedBacklogEvents++;
            DroppedBacklogTicks += LastDroppedBacklogTicks;
            DroppedBacklogSeconds += LastDroppedBacklogSeconds;
            _accumulator = 0;
        }

        return ticks;
    }
}
