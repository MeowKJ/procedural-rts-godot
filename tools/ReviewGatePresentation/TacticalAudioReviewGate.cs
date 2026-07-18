static class TacticalAudioReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var layer = ReviewGateSource.Read(root, "scripts", "ui", "TacticalAudioLayer.cs");
        var deduper = ReviewGateSource.Read(root, "scripts", "ui", "TacticalAudioCueDeduper.cs");
        var events = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.Events.cs");
        var feedback = ReviewGateSource.Read(root, "scripts", "battle", "BattleRoot.PresentationFeedback.cs");
        var lifecycle = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.Lifecycle.cs");
        var alerts = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.Alerts.cs");

        RequireText(layer, "TacticalAudioCueDeduper _deduper", "TacticalAudioLayer must keep per-cue de-duplication state.", result);
        RequireText(layer, "_deduper.TryReserve(cue, Time.GetTicksMsec())", "TacticalAudioLayer must de-duplicate cues before taking an audio player.", result);
        RequireText(layer, "_deduper.Clear()", "TacticalAudioLayer must clear de-duplication state when managed audio resources are released.", result);
        RequireText(layer, "SpatialMixFor(Vector2? worldPosition, Rect2? listenerWorldRect)", "TacticalAudioLayer must keep lightweight world-positioned cue mixing.", result);
        RequireText(deduper, "Dictionary<TacticalAudioCue, ulong> _lastCuePlayedAtMsec", "Tactical audio de-duplication must track last play time per cue.", result);
        RequireText(deduper, "RepeatWindowMsec(TacticalAudioCue cue)", "Tactical audio de-duplication must keep cue-specific repeat windows.", result);
        RequireText(deduper, "TacticalAudioCue.Alert => 220", "Alert audio cues must use a longer under-load de-duplication window.", result);
        RequireText(deduper, "TacticalAudioCue.LowPower => 360", "Low-power audio cues must have their own under-load repeat window.", result);
        RequireText(deduper, "TacticalAudioCue.OutcomeVictory => 0", "Outcome audio cues must remain outside tactical repeat suppression.", result);
        RequireText(events, "PlayDeathCue", "Death events must route through the shared tactical death cue helper.", result);
        RequireText(feedback, "PlayAudioCue(TacticalAudioCue.Death", "Death events must route to a dedicated tactical audio cue.", result);
        RequireText(lifecycle, "PlayAudioCue(TacticalAudioCue.BuildComplete", "Player building completion must route to a dedicated tactical audio cue.", result);
        RequireText(alerts, "PlayAudioCue(TacticalAudioCue.LowPower", "Low-power alerts must route to a dedicated tactical audio cue.", result);
    }
}
