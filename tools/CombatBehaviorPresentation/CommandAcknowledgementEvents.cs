static partial class Program
{
    private static void AssertCommandAcknowledgementEvents()
    {
        var events = new SimEventSink();
        var drainedEvents = new List<SimEvent>();
        var position = new Vector2(320, 440);

        events.Raise(new CommandAcknowledgedEvent(
            12,
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            CommandAcknowledgementKind.Harvest,
            position,
            CommandAcknowledgementAudioCue.Move));
        events.DrainInto(drainedEvents);

        if (drainedEvents.Count != 1
            || drainedEvents[0] is not CommandAcknowledgedEvent acknowledgement
            || acknowledgement.Tick != 12
            || acknowledgement.Owner != OwnerId.FromPlayerSlot(PlayerSlotId.One)
            || acknowledgement.Kind != CommandAcknowledgementKind.Harvest
            || acknowledgement.Position != position
            || acknowledgement.AudioCue != CommandAcknowledgementAudioCue.Move)
        {
            throw new InvalidOperationException("command acknowledgement feedback should travel as a caller-buffer-drained SimEvent");
        }

        events.DrainInto(drainedEvents);
        if (drainedEvents.Count != 0)
        {
            throw new InvalidOperationException("command acknowledgement SimEvent drain should clear the sink without allocating stale snapshots");
        }
    }
}
