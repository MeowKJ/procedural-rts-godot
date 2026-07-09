static class CombatReadabilityReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var math = ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "CombatReadabilityMath.cs");
        var layer = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.cs");
        var visibility = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.Visibility.cs");
        var battleProcess = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.Process.cs");
        var impact = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.ImpactFlashes.cs");
        var death = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.DeathEffects.cs");
        var muzzle = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.MuzzleFlashes.cs");
        var combatDraw = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.CombatDraw.cs");
        var pulses = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.Pulses.cs");

        RequireText(math, "CombatReadabilityMath", "Combat readability must be a pure policy that can be regression tested.", result);
        RequireText(math, "!visibleToPlayer && !exploredByPlayer", "Unseen fog must suppress transient combat effects completely.", result);
        RequireText(math, "commandMarkerCount > 0", "Combat effects must back off while command markers are active.", result);
        RequireText(layer, "public int CommandMarkerCount", "CombatEffectsLayer must accept command marker pressure without depending on the command layer.", result);
        RequireText(battleProcess, "_combatEffects.CommandMarkerCount = _commandAcknowledgements.ActiveRingCount", "BattleRoot must feed command marker pressure into combat readability.", result);
        RequireText(visibility, "ReadabilityFor(Vector2 position)", "CombatEffectsLayer must evaluate point readability before drawing effects.", result);
        RequireText(visibility, "ReadabilityForSegment(Vector2 start, Vector2 end)", "CombatEffectsLayer must evaluate segment readability before drawing beams/projectiles.", result);
        RequireText(visibility, "ReadableWidth", "CombatEffectsLayer must reduce effect stroke weight under readability pressure.", result);
        RequireText(impact, "ReadabilityFor(effect.Position)", "Impact flashes must use combat readability policy.", result);
        RequireText(death, "DrawDeathFragments(effect, t, fade, readability)", "Death VFX must share combat readability scaling.", result);
        RequireText(muzzle, "ReadabilityFor(effect.Position)", "Muzzle flashes must use combat readability policy.", result);
        RequireText(combatDraw, "ReadabilityForSegment(start, end)", "Beams must use combat readability policy.", result);
        RequireText(combatDraw, "ReadabilityForSegment(tail, position)", "Projectiles must use combat readability policy.", result);
        RequireText(pulses, "DrawHitPunch", "Hit pulses must keep their punch marks under combat readability scaling.", result);
    }
}
