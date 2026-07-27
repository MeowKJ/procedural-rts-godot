using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void QueueCommandAcknowledgementEvent(CommandAcknowledgementKind kind, Vector2 position, CommandAcknowledgementAudioCue audioCue)
    {
        _presentationEvents.Raise(new CommandAcknowledgedEvent(
            _unitBattlefield.SimulationTick,
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            kind,
            position,
            audioCue));
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
                PlayCommandAcknowledgementAudio(acknowledgement.AudioCue, acknowledgement.Position);
            }
        }
    }

    private void PlayCommandAcknowledgementAudio(CommandAcknowledgementAudioCue audioCue, Vector2 position)
    {
        switch (audioCue)
        {
            case CommandAcknowledgementAudioCue.Move:
                PlayAudioCue(TacticalAudioCue.Move, position);
                break;
            case CommandAcknowledgementAudioCue.Attack:
                PlayAudioCue(TacticalAudioCue.Attack, position);
                break;
            case CommandAcknowledgementAudioCue.Invalid:
                PlayAudioCue(TacticalAudioCue.Invalid, position);
                break;
        }
    }
}
