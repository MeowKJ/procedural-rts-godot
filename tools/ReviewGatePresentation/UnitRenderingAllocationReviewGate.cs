static class UnitRenderingAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var renderer = ReviewGateSource.Read(root, "scripts", "core", "presentation", "rendering", "UnitVisualRenderer.cs");
        var facingSource = ReviewGateSource.Read(root, "scripts", "core", "presentation", "rendering", "UnitMountFacingSource.cs");
        var runtimeView = ReviewGateSource.Read(root, "scripts", "world", "UnitInstanceView.cs");
        var legacyView = ReviewGateSource.Read(root, "scripts", "world", "UnitView.cs");
        var dynamicIcon = ReviewGateSource.Read(root, "scripts", "ui", "DynamicUnitIcon.cs");
        var unitInstance = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitInstance.cs");

        RequireText(renderer, "UnitMountFacingSource mountFacings = default", "Unit renderer must accept mount-facing sources without dictionaries.", result);
        RequireText(facingSource, "FromRuntimeMounts(IReadOnlyList<WeaponMountRuntimeState> mounts)", "Runtime unit draw must pass existing weapon mount storage.", result);
        RequireText(facingSource, "FromLegacyUnit(UnitSpec spec, float bodyFacing, float turretFacing)", "Legacy unit draw must resolve mount facings without a dictionary.", result);
        RequireText(facingSource, "Single(string mountId, float facing)", "Dynamic unit icons must support a single mount facing without a dictionary.", result);
        RequireText(runtimeView, "UnitMountFacingSource.FromRuntimeMounts(Unit.WeaponMounts)", "UnitInstanceView must draw from runtime mount storage directly.", result);
        RequireText(legacyView, "UnitMountFacingSource.FromLegacyUnit(style.Spec, Unit.Facing, Unit.TurretFacing)", "Legacy UnitView must draw from a mount-facing source.", result);
        RequireText(dynamicIcon, "UnitMountFacingSource.Single(\"main\", turretFacing)", "DynamicUnitIcon must not allocate a mount-facing dictionary.", result);
        ForbidText(runtimeView, "Unit.MountFacings()", "UnitInstanceView draw must not allocate mount-facing dictionaries.", result);
        ForbidText(dynamicIcon, "new Dictionary<string, float>", "DynamicUnitIcon draw must not allocate mount-facing dictionaries.", result);
        ForbidText(unitInstance, "MountFacings()", "UnitInstance must not expose a dictionary-allocating draw helper.", result);
    }
}
