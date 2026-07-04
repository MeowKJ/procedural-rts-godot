namespace ProceduralRts.Core;

public sealed record SequencedCommandEnvelope(long Sequence, EntityCommand Command);

public sealed class EntityCommandBuffer
{
    private readonly List<SequencedCommandEnvelope> _commands = [];
    private readonly List<SequencedCommandEnvelope> _snapshotBuffer = [];
    private readonly List<SequencedCommandEnvelope> _drainSnapshotBuffer = [];
    private readonly List<SequencedCommandEnvelope> _readyBuffer = [];
    private readonly HashSet<long> _readySequences = [];
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
        CopyOrderedCommandsInto(_snapshotBuffer);
        return _snapshotBuffer;
    }

    public IReadOnlyList<SequencedCommandEnvelope> DrainUpToTick(int tick)
    {
        CopyOrderedCommandsInto(_drainSnapshotBuffer);
        _readyBuffer.Clear();
        _readySequences.Clear();
        foreach (var item in _drainSnapshotBuffer)
        {
            if (item.Command.Tick <= tick)
            {
                _readyBuffer.Add(item);
                _readySequences.Add(item.Sequence);
            }
        }

        if (_readyBuffer.Count == 0)
        {
            return _readyBuffer;
        }

        _commands.RemoveAll(IsReadySequence);
        return _readyBuffer;
    }

    private void CopyOrderedCommandsInto(List<SequencedCommandEnvelope> result)
    {
        result.Clear();
        result.AddRange(_commands);
        result.Sort(CompareCommands);
    }

    private bool IsReadySequence(SequencedCommandEnvelope item)
    {
        return _readySequences.Contains(item.Sequence);
    }

    private static int CompareCommands(SequencedCommandEnvelope left, SequencedCommandEnvelope right)
    {
        var tick = left.Command.Tick.CompareTo(right.Command.Tick);
        if (tick != 0)
        {
            return tick;
        }

        var issuer = left.Command.Issuer.Value.CompareTo(right.Command.Issuer.Value);
        return issuer != 0 ? issuer : left.Sequence.CompareTo(right.Sequence);
    }
}
