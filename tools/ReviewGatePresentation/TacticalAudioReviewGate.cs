static class TacticalAudioReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var layer = ReviewGateSource.Read(root, "scripts", "ui", "TacticalAudioLayer.cs");
        var deduper = ReviewGateSource.Read(root, "scripts", "ui", "TacticalAudioCueDeduper.cs");

        RequireText(layer, "TacticalAudioCueDeduper _deduper", "TacticalAudioLayer must keep per-cue de-duplication state.", result);
        RequireText(layer, "_deduper.TryReserve(cue, Time.GetTicksMsec())", "TacticalAudioLayer must de-duplicate cues before taking an audio player.", result);
        RequireText(layer, "_deduper.Clear()", "TacticalAudioLayer must clear de-duplication state when managed audio resources are released.", result);
        RequireText(deduper, "Dictionary<TacticalAudioCue, ulong> _lastCuePlayedAtMsec", "Tactical audio de-duplication must track last play time per cue.", result);
        RequireText(deduper, "RepeatWindowMsec(TacticalAudioCue cue)", "Tactical audio de-duplication must keep cue-specific repeat windows.", result);
        RequireText(deduper, "TacticalAudioCue.Alert => 220", "Alert audio cues must use a longer under-load de-duplication window.", result);
        RequireText(deduper, "TacticalAudioCue.OutcomeVictory => 0", "Outcome audio cues must remain outside tactical repeat suppression.", result);
    }
}
