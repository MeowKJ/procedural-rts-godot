using ProceduralRts.Ui;

static partial class Program
{
    private static void AssertTacticalAudioDedupe()
    {
        var deduper = new TacticalAudioCueDeduper();
        var moveWindow = TacticalAudioCueDeduper.RepeatWindowMsec(TacticalAudioCue.Move);
        if (!deduper.TryReserve(TacticalAudioCue.Move, 1000)
            || deduper.TryReserve(TacticalAudioCue.Move, 1000 + moveWindow - 1)
            || !deduper.TryReserve(TacticalAudioCue.Move, 1000 + moveWindow))
        {
            throw new InvalidOperationException("move audio cues should be de-duplicated only inside their repeat window");
        }

        var alertWindow = TacticalAudioCueDeduper.RepeatWindowMsec(TacticalAudioCue.Alert);
        if (alertWindow <= moveWindow
            || !deduper.TryReserve(TacticalAudioCue.Alert, 2000)
            || deduper.TryReserve(TacticalAudioCue.Alert, 2000 + alertWindow - 1)
            || !deduper.TryReserve(TacticalAudioCue.Alert, 2000 + alertWindow))
        {
            throw new InvalidOperationException("alert audio cues should use a longer load-shedding repeat window");
        }

        if (!deduper.TryReserve(TacticalAudioCue.OutcomeVictory, 3000)
            || !deduper.TryReserve(TacticalAudioCue.OutcomeVictory, 3000))
        {
            throw new InvalidOperationException("outcome audio cues should not be suppressed by tactical cue de-duplication");
        }

        deduper.Clear();
        if (!deduper.TryReserve(TacticalAudioCue.Alert, 2000))
        {
            throw new InvalidOperationException("audio cue de-duplication reset should clear previous cue timestamps");
        }
    }
}
