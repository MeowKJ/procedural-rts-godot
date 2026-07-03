static class ProjectileImpactReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var ammo = ReviewGateSource.Read(root, "scripts", "core", "combat", "AmmoDefinition.cs");
        RequireText(ammo, "float SplashRadius", "AmmoDefinition must expose data-driven splash radius.", result);
        RequireText(ammo, "float SplashMinDamageRatio", "AmmoDefinition must expose data-driven splash falloff.", result);

        var ballistic = ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "BallisticCannonAmmo.cs");
        RequireText(ballistic, "ProjectileBehavior.Ballistic", "Ballistic cannon ammo must remain ballistic.", result);
        RequireText(ballistic, "SplashRadius:", "Ballistic cannon ammo must keep explicit splash metadata.", result);

        var resolution = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponEngagementResolution.cs");
        RequireText(resolution, "ApplySplashDamage", "Weapon engagement resolution must apply ammo-driven splash damage.", result);
        RequireText(resolution, "Relations.CanAttack(attackerOwner, candidate.OwnerId)", "Splash damage must remain relation-aware.", result);

        ReviewGateSource.RequireTextInFile(
            root,
            result,
            "projectile-splash",
            "tools",
            "SimReplayCombatTactics",
            "ProjectileTrackingScenarios.cs");
    }
}
