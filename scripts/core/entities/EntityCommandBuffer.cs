namespace ProceduralRts.Core;

public sealed record SequencedCommandEnvelope(long Sequence, EntityCommand Command);

public sealed class EntityCommandBuffer
{
    private readonly List<SequencedCommandEnvelope> _commands = [];
    private long _nextSequence = 1;

    public int Count => _commands.Count;

    public SequencedCommandEnvelope Enqueue(EntityCommand command)
    {
        var sequenced = new SequencedCommandEnvelope(_nextSequence++, command);
        _commands.Add(sequenced);
        return sequenced;
    }

    public IReadOnlyList<SequencedCommandEnvelope> Snapshot()
    {
        return _commands
            .OrderBy(item => item.Command.Tick)
            .ThenBy(item => item.Command.Issuer.Value)
            .ThenBy(item => item.Sequence)
            .ToList();
    }

    public IReadOnlyList<SequencedCommandEnvelope> DrainUpToTick(int tick)
    {
        var ready = Snapshot()
            .Where(item => item.Command.Tick <= tick)
            .ToList();

        if (ready.Count == 0)
        {
            return ready;
        }

        var readySequences = ready.Select(item => item.Sequence).ToHashSet();
        _commands.RemoveAll(item => readySequences.Contains(item.Sequence));
        return ready;
    }
}
