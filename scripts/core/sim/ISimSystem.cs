namespace ProceduralRts.Core;

/// <summary>
/// Per-tick context handed to every <see cref="ISimSystem"/>. Carries the
/// authoritative world, the fixed delta for this tick, the tick index, and the
/// commands that became due on this tick (already drained from the buffer by the
/// driver, in stable order).
/// </summary>
public sealed class SimContext
{
    public SimContext(EntityWorld world, int tick, float fixedDelta, IReadOnlyList<SequencedCommandEnvelope> commands)
    {
        World = world;
        Tick = tick;
        FixedDelta = fixedDelta;
        Commands = commands;
    }

    public EntityWorld World { get; }

    public int Tick { get; }

    public float FixedDelta { get; }

    public IReadOnlyList<SequencedCommandEnvelope> Commands { get; }
}

/// <summary>
/// A behavior unit of the authoritative simulation. Systems own behavior;
/// <see cref="EntityInstance"/> only holds state. Systems must iterate entities
/// in stable <see cref="EntityId"/> order and must not depend on Godot nodes,
/// the scene tree, or real wall-clock time.
/// </summary>
public interface ISimSystem
{
    void Step(SimContext context);
}
