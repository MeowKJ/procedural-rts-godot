namespace ProceduralRts.Ui;

public sealed class TacticalAudioCueDeduper
{
    private readonly Dictionary<TacticalAudioCue, ulong> _lastCuePlayedAtMsec = [];

    public bool TryReserve(TacticalAudioCue cue, ulong nowMsec)
    {
        var repeatWindow = RepeatWindowMsec(cue);
        if (repeatWindow > 0
            && _lastCuePlayedAtMsec.TryGetValue(cue, out var lastPlayedAt)
            && nowMsec >= lastPlayedAt
            && nowMsec - lastPlayedAt < repeatWindow)
        {
            return false;
        }

        _lastCuePlayedAtMsec[cue] = nowMsec;
        return true;
    }

    public void Clear()
    {
        _lastCuePlayedAtMsec.Clear();
    }

    public static ulong RepeatWindowMsec(TacticalAudioCue cue)
    {
        return cue switch
        {
            TacticalAudioCue.LowPower => 360,
            TacticalAudioCue.Alert => 220,
            TacticalAudioCue.BuildComplete => 200,
            TacticalAudioCue.Production => 180,
            TacticalAudioCue.Death => 140,
            TacticalAudioCue.Invalid => 120,
            TacticalAudioCue.Attack => 90,
            TacticalAudioCue.Move => 55,
            TacticalAudioCue.Selection => 45,
            TacticalAudioCue.OutcomeVictory => 0,
            TacticalAudioCue.OutcomeDefeat => 0,
            _ => 0,
        };
    }
}
