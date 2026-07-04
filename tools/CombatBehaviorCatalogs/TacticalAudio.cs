using Godot;
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

        if (TacticalAudioCueDeduper.RepeatWindowMsec(TacticalAudioCue.LowPower) <= alertWindow
            || TacticalAudioCueDeduper.RepeatWindowMsec(TacticalAudioCue.BuildComplete) <= TacticalAudioCueDeduper.RepeatWindowMsec(TacticalAudioCue.Death))
        {
            throw new InvalidOperationException("build complete, death, and low-power audio cues should have event-specific repeat windows");
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

        var visibleRect = new Rect2(Vector2.Zero, new Vector2(200, 120));
        var centered = TacticalAudioLayer.SpatialMixFor(new Vector2(100, 60), visibleRect);
        var offscreenRight = TacticalAudioLayer.SpatialMixFor(new Vector2(420, 60), visibleRect);
        var uiCue = TacticalAudioLayer.SpatialMixFor(null, visibleRect);
        if (centered.VolumeDbOffset != 0
            || MathF.Abs(centered.PitchScale - 1) > 0.0001f
            || offscreenRight.VolumeDbOffset >= centered.VolumeDbOffset
            || offscreenRight.PitchScale <= centered.PitchScale
            || uiCue.VolumeDbOffset != 0
            || MathF.Abs(uiCue.PitchScale - 1) > 0.0001f)
        {
            throw new InvalidOperationException("world-positioned tactical audio cues should get a small spatial-ish mix while UI cues remain centered");
        }
    }
}
