using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void QueueCommandAcknowledgementEvent(CommandAcknowledgementKind kind, Vector2 position, CommandAcknowledgementAudioCue audioCue)
    {
        _presentationEvents.Raise(new CommandAcknowledgedEvent(
            _simClock.CurrentTick,
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
                RecordSandboxCommandLog(acknowledgement);
            }
        }
    }

    private void RecordSandboxCommandLog(CommandAcknowledgedEvent acknowledgement)
    {
        if (_state.Options.LaunchMode != LaunchMode.Sandbox)
        {
            return;
        }

        _sandboxCommandLogSequence++;
        _sandboxCommandLogLines.Insert(0, FormatSandboxCommandLogLine(_sandboxCommandLogSequence, acknowledgement));
        while (_sandboxCommandLogLines.Count > SandboxCommandLogLimit)
        {
            _sandboxCommandLogLines.RemoveAt(_sandboxCommandLogLines.Count - 1);
        }

        RefreshSandboxCommandLog();
    }

    private static string FormatSandboxCommandLogLine(int sequence, CommandAcknowledgedEvent acknowledgement)
    {
        var status = acknowledgement.Kind == CommandAcknowledgementKind.Invalid ? "BAD" : "OK";
        return $"{sequence:000} T{acknowledgement.Tick} {status} {SandboxCommandKindCode(acknowledgement.Kind)} @{Mathf.RoundToInt(acknowledgement.Position.X)},{Mathf.RoundToInt(acknowledgement.Position.Y)}";
    }

    private static string SandboxCommandKindCode(CommandAcknowledgementKind kind)
    {
        return kind switch
        {
            CommandAcknowledgementKind.Attack => "ATK",
            CommandAcknowledgementKind.Repair => "REP",
            CommandAcknowledgementKind.Harvest => "HAR",
            CommandAcknowledgementKind.Rally => "RLY",
            CommandAcknowledgementKind.Invalid => "INV",
            _ => "MOV",
        };
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
