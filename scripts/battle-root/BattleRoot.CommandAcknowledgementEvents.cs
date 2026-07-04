using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void QueueCommandAcknowledgementEvent(CommandAcknowledgementKind kind, Vector2 position)
    {
        _presentationEvents.Raise(new CommandAcknowledgedEvent(
            _simClock.CurrentTick,
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            kind,
            position));
    }

    private void DrainPresentationEvents()
    {
        _presentationEvents.DrainInto(_simEventDrainBuffer);
        ApplyPresentationEvents(_simEventDrainBuffer);
        _simEventDrainBuffer.Clear();
    }

    private void ApplyPresentationEvents(IReadOnlyList<SimEvent> events)
    {
        for (var index = 0; index < events.Count; index++)
        {
            if (events[index] is CommandAcknowledgedEvent acknowledgement
                && acknowledgement.Owner == OwnerId.FromPlayerSlot(PlayerSlotId.One))
            {
                _commandAcknowledgements.Add(acknowledgement.Kind, acknowledgement.Position);
            }
        }
    }
}
