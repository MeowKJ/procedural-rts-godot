static class WeaponEngagementConvergenceReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var weaponSystem = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponSystem.cs");
        var resolution = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponEngagementResolution.cs");
        var combat = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "combat", "CombatEngagementSystem.cs");
        var turret = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "TurretCombatSystem.cs");
        var buildingTarget = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "BuildingTargetCombatSystem.cs");
        var projectile = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProjectileSystem.cs");

        RequireText(weaponSystem, "public static int Tick(", "WeaponSystem must own the shared weapon mount engagement loop.", result);
        RequireText(weaponSystem, "WeaponEngagementMath.RotateToward", "WeaponSystem must use shared mount rotation math.", result);
        RequireText(weaponSystem, "WeaponEngagementResolution.Fire", "WeaponSystem must route firing through shared engagement resolution.", result);
        RequireText(weaponSystem, "WeaponMath.TargetPriority", "WeaponSystem must use shared weapon target priority math.", result);
        RequireText(resolution, "public static void Fire(", "WeaponEngagementResolution must own shared fire dispatch.", result);
        RequireText(resolution, "public static void ApplyProjectileImpact(", "WeaponEngagementResolution must own projectile impact dispatch.", result);
        RequireText(combat, "WeaponSystem.Tick(", "Mobile CombatSystem must call the shared weapon engagement loop.", result);
        RequireText(turret, "WeaponSystem.Tick(", "TurretCombatSystem must call the shared weapon engagement loop.", result);
        RequireText(buildingTarget, "WeaponSystem.Tick(", "BuildingTargetCombatSystem must call the shared weapon engagement loop.", result);
        RequireText(projectile, "WeaponEngagementResolution.ApplyProjectileImpact", "ProjectileSystem must route impacts through shared engagement resolution.", result);
        RequireText(projectile, "WeaponEngagementResolution.MuzzlePosition", "Projectile interception must reuse shared muzzle math.", result);
        ForbidText(combat + turret + buildingTarget, "new WeaponFiredEvent(", "Combat systems must not bypass WeaponEngagementResolution for fire events.", result);
    }
}
