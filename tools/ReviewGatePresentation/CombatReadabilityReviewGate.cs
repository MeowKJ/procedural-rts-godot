static class CombatReadabilityReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var math = ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "CombatReadabilityMath.cs");
        var layer = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.cs");
        var visibility = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.Visibility.cs");
        var battleProcess = ReviewGateSource.Read(root, "scripts", "BattleRoot.Process.cs");
        var impact = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.ImpactFlashes.cs");
        var death = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.DeathEffects.cs");
        var muzzle = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.MuzzleFlashes.cs");
        var combatDraw = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.CombatDraw.cs");
        var pulses = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.Pulses.cs");
        var repairFeedback = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.RepairFeedback.cs");
        var repairProjection = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.RepairFeedbackProjection.cs");
        var repairProjectionState = ReviewGateSource.Read(root, "scripts", "core", "sim", "ActiveRepairFeedbackProjection.cs");
        var presentationInput = ReviewGateSource.Read(root, "tools", "CombatBehaviorPresentation", "InputResources.cs");

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
        RequireText(layer, "DrawActiveRepairFeedback();", "Combat effects must draw active repair feedback in the shared world-effects pass.", result);
        RequireText(layer, "_activeRepairFeedbackEffectCount", "Combat effects must count active repair feedback without repeatedly scanning repair orders during draw.", result);
        RequireText(repairFeedback, "ActiveRepairFeedbackProjections(_activeRepairFeedbackProjections)", "Repair feedback rendering must consume read-only UnitBattlefield projections.", result);
        RequireText(repairFeedback, "_activeRepairFeedbackEffectCount = _activeRepairFeedbackProjections.Count", "Repair feedback rendering must cache the current draw-pass repair count.", result);
        RequireText(repairFeedback, "ReadabilityForSegment(repair.RepairerPosition, repair.TargetPosition)", "Repair feedback must use combat readability for repairer-target tethers.", result);
        RequireText(repairFeedback, "DrawArc(\n            repair.TargetPosition", "Repair feedback must mark the active repair target in world space.", result);
        RequireText(repairProjection, "RepairOrderComponentState", "Active repair feedback projection must read existing repair orders instead of adding command semantics.", result);
        RequireText(repairProjection, "repairer.Transform.Position.DistanceTo(target.Transform.Position) > MathF.Max(0, order.Range)", "Active repair feedback should only surface once repair work is in range.", result);
        RequireText(repairProjection, "CanFundRepairFeedback(repairer, order)", "Active repair feedback should stay hidden when the owner cannot fund repair work.", result);
        RequireText(repairProjectionState, "public readonly record struct ActiveRepairFeedbackProjection", "Active repair feedback must use an immutable render-ready projection.", result);
        RequireText(presentationInput, "ActiveRepairFeedbackProjections(repairFeedback)", "CombatBehaviorPresentation must cover active repair feedback projection output.", result);
        RequireText(presentationInput, "active repair feedback projection should stay hidden when the owner cannot fund repair work", "CombatBehaviorPresentation must cover insufficient-credit repair feedback suppression.", result);
        RequireText(presentationInput, "active repair feedback projection should disappear after target is no longer repairable or the repair order is cleared", "CombatBehaviorPresentation must cover repair feedback cleanup after repair target/order cleanup.", result);
    }
}
