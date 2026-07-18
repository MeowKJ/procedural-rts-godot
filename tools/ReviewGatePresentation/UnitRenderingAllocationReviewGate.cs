static class UnitRenderingAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var renderer = ReviewGateSource.Read(root, "scripts", "core", "presentation", "rendering", "UnitVisualRenderer.cs");
        var facingSource = ReviewGateSource.Read(root, "scripts", "core", "presentation", "rendering", "UnitMountFacingSource.cs");
        var bodyBatch = ReviewGateSource.Read(root, "scripts", "world", "UnitBodyBatchLayer.cs");
        var runtimeView = ReviewGateSource.Read(root, "scripts", "world", "UnitInstanceView.cs");
        var legacyView = ReviewGateSource.Read(root, "scripts", "world", "UnitView.cs");
        var unitPresentation = ReviewGateSource.Read(root, "scripts", "core", "sim", "UnitPresentationProjection.cs");
        var dynamicIcon = ReviewGateSource.Read(root, "scripts", "ui", "DynamicUnitIcon.cs");
        var unitInstance = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitInstance.cs");
        var combatEffects = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "world", "CombatEffectsLayer.cs"));
        var battleRoot = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "BattleRoot.cs"));
        var unitBattlefield = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefield.cs"));
        var weaponResolution = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponEngagementResolution.cs");

        RequireText(renderer, "UnitMountFacingSource mountFacings = default", "Unit renderer must accept mount-facing sources without dictionaries.", result);
        RequireText(facingSource, "FromRuntimeMounts(IReadOnlyList<WeaponMountRuntimeState> mounts)", "Runtime unit draw must pass existing weapon mount storage.", result);
        RequireText(facingSource, "FromLegacyUnit(UnitSpec spec, float bodyFacing, float turretFacing)", "Legacy unit draw must resolve mount facings without a dictionary.", result);
        RequireText(facingSource, "Single(string mountId, float facing)", "Dynamic unit icons must support a single mount facing without a dictionary.", result);
        RequireText(bodyBatch, "public partial class UnitBodyBatchLayer : Node2D", "Runtime unit body art must have a dedicated batch drawing layer.", result);
        RequireText(bodyBatch, "foreach (var unit in Units)", "Unit body batch layer must draw runtime units through one CanvasItem pass.", result);
        RequireText(bodyBatch, "UnitVisualRenderer.DrawUnitArtRecipe(", "Unit body batch layer must reuse the canonical unit art renderer.", result);
        RequireText(bodyBatch, "public Func<int, UnitPresentationProjection?>? PresentationProvider", "Unit body batch layer must consume runtime presentation projections.", result);
        RequireText(bodyBatch, "UnitMountFacingSource.FromRuntimeMounts(current.Mounts)", "Unit body batch layer must draw mounts from the projection.", result);
        RequireText(bodyBatch, "HasVisibleMovingUnit()", "Unit body batch layer must bypass idle redraw throttling while visible units are moving.", result);
        RequireText(bodyBatch, "QueueRedraw();\n            return;", "Visible moving unit bodies must redraw on the render frame instead of the idle 30 Hz cadence.", result);
        RequireText(bodyBatch, "current.IsMoving", "Unit body batch moving predicate must come from the projection.", result);
        RequireText(runtimeView, "public Func<UnitPresentationProjection?>? PresentationProvider", "UnitInstanceView must consume runtime presentation projections.", result);
        RequireText(runtimeView, "UnitMountFacingSource.FromRuntimeMounts(presentation.Mounts)", "UnitInstanceView must draw mounts from the projection.", result);
        RequireText(runtimeView, "public bool DrawBodyArt { get; init; } = true", "UnitInstanceView must keep a fallback body-art switch for non-batched callers.", result);
        RequireText(runtimeView, "if (DrawBodyArt)", "UnitInstanceView body drawing must be gated so runtime overlays can avoid duplicate body draws.", result);
        RequireText(legacyView, "UnitMountFacingSource.FromLegacyUnit(style.Spec, Unit.Facing, Unit.TurretFacing)", "Legacy UnitView must draw from a mount-facing source.", result);
        RequireText(dynamicIcon, "UnitMountFacingSource.Single(\"main\", turretFacing)", "DynamicUnitIcon must not allocate a mount-facing dictionary.", result);
        RequireText(unitBattlefield, "event Action<WeaponFiredEvent>? WeaponFired", "UnitBattlefield must expose WeaponFiredEvent data for presentation-only muzzle flashes.", result);
        RequireText(battleRoot, "_unitBattlefield.WeaponFired += OnWeaponFired", "BattleRoot must subscribe to runtime WeaponFiredEvent presentation data.", result);
        RequireText(battleRoot, "_combatEffects.AddMuzzleFlash(fired.Muzzle, fired.TargetPosition, accent, fired.LegacyWeaponKind)", "BattleRoot must route WeaponFiredEvent data to muzzle flash VFX.", result);
        RequireText(battleRoot, "new UnitBodyBatchLayer", "BattleRoot must mount the runtime unit body batch layer.", result);
        RequireText(battleRoot, "PresentationProvider = id => _unitBattlefield.UnitPresentationProjection(id)", "Runtime unit batch must request immutable presentation projections.", result);
        RequireText(battleRoot, "PresentationProvider = () => _unitBattlefield.UnitPresentationProjection(unit.Id)", "Runtime unit overlays must request immutable presentation projections.", result);
        RequireText(battleRoot, "DrawBodyArt = false", "Runtime UnitInstanceView overlays must not duplicate batched body art.", result);
        RequireText(battleRoot, "_unitBodyBatchLayer.CullingWorldRect = visibleRect", "Unit body batch layer must use the same world culling rect as other presentation layers.", result);
        RequireText(combatEffects, "List<MuzzleFlashEffect> _muzzleFlashes", "CombatEffectsLayer must pool muzzle flash VFX instead of allocating ad-hoc nodes.", result);
        RequireText(combatEffects, "DrawMuzzleFlashes();", "CombatEffectsLayer must draw weapon-fired muzzle flashes.", result);
        RequireText(combatEffects, "DrawHitPunch(", "CombatEffectsLayer must add visual-only hit punch feedback on damage pulses.", result);
        RequireText(weaponResolution, "MuzzlePosition(context.World, attacker, mount)", "WeaponFiredEvent muzzle must be computed from the firing mount, not the entity center.", result);
        RequireText(unitPresentation, "public readonly record struct UnitPresentationProjection", "Runtime unit feedback must have one immutable presentation contract.", result);
        RequireText(unitPresentation, "EntityProjection Entity", "Runtime unit presentation must retain the deterministic entity projection.", result);
        RequireText(unitPresentation, "float CommandPulse", "Runtime unit presentation must expose command feedback pulse data.", result);
        RequireText(unitPresentation, "float HarvestPulse", "Runtime unit presentation must expose economy feedback pulse data.", result);
        RequireText(unitPresentation, "IReadOnlyList<WeaponMountRuntimeState> Mounts", "Runtime unit presentation must expose read-only mount state.", result);
        ForbidText(runtimeView, "Unit.MountFacings()", "UnitInstanceView draw must not allocate mount-facing dictionaries.", result);
        ForbidText(runtimeView, "Unit.CommandPulse", "UnitInstanceView must not read mutable command feedback directly.", result);
        ForbidText(runtimeView, "Unit.AlertPulse", "UnitInstanceView must not read mutable alert feedback directly.", result);
        ForbidText(runtimeView, "Unit.HarvestPulse", "UnitInstanceView must not read mutable harvest feedback directly.", result);
        ForbidText(runtimeView, "Unit.Cargo", "UnitInstanceView must not read mutable cargo directly.", result);
        ForbidText(runtimeView, "Unit.WeaponMounts", "UnitInstanceView must not read mutable mount state directly.", result);
        ForbidText(runtimeView, "Unit.Position", "UnitInstanceView must not read mutable position directly.", result);
        ForbidText(runtimeView, "Unit.Hp", "UnitInstanceView must not read mutable health directly.", result);
        ForbidText(runtimeView, "Unit.Facing", "UnitInstanceView must not read mutable facing directly.", result);
        ForbidText(dynamicIcon, "new Dictionary<string, float>", "DynamicUnitIcon draw must not allocate mount-facing dictionaries.", result);
        ForbidText(unitInstance, "MountFacings()", "UnitInstance must not expose a dictionary-allocating draw helper.", result);
    }
}
