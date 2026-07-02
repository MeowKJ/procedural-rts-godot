static class TurretCombatAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var turret = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "TurretCombatSystem.cs");
        RequireText(turret, "SpatialGrid<EntityInstance> _targetGrid", "TurretCombatSystem must reuse a target broadphase grid.", result);
        RequireText(turret, "BuildTargetGrid(world)", "TurretCombatSystem must build the target grid once per step.", result);
        RequireText(turret, "_targetGrid.Reset(maxRange)", "TurretCombatSystem must size the target grid from turret range.", result);
        RequireText(turret, "_targetGridMaxTargetRadius", "Turret target broadphase must include target collision radius slack.", result);
        RequireText(turret, "_targetGrid.Neighbors(turret.Transform.Position, cellRadius)", "Turret auto-target scan must query grid neighbors.", result);
        ForbidRegex(turret, @"ResolveTarget[\s\S]*foreach\s*\(\s*var\s+candidate\s+in\s+world\.OrderedEntities\s*\)", "Turret auto-target scan must not scan every entity.", result);
    }
}
