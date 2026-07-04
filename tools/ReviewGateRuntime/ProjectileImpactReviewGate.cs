static class ProjectileImpactReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var ammo = ReviewGateSource.Read(root, "scripts", "core", "combat", "AmmoDefinition.cs");
        RequireText(ammo, "float SplashRadius", "AmmoDefinition must expose data-driven splash radius.", result);
        RequireText(ammo, "float SplashMinDamageRatio", "AmmoDefinition must expose data-driven splash falloff.", result);
        RequireText(ammo, "bool Interceptable", "AmmoDefinition must expose data-driven projectile interception metadata.", result);

        var ballistic = ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "BallisticCannonAmmo.cs");
        RequireText(ballistic, "ProjectileBehavior.Ballistic", "Ballistic cannon ammo must remain ballistic.", result);
        RequireText(ballistic, "SplashRadius:", "Ballistic cannon ammo must keep explicit splash metadata.", result);

        var seeker = ReviewGateSource.Read(root, "scripts", "core", "combat", "ammo", "SeekerRocketAmmo.cs");
        RequireText(seeker, "Interceptable: true", "Seeker rockets must remain gameplay-interceptable projectile entities.", result);

        var skySpear = ReviewGateSource.Read(root, "scripts", "core", "combat", "weapons", "SkySpearWeapon.cs");
        RequireText(skySpear, "CanInterceptProjectiles: true", "Sky Spear weapons must retain projectile interception capability.", result);

        var resolution = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponEngagementResolution.cs");
        RequireText(resolution, "ApplySplashDamage", "Weapon engagement resolution must apply ammo-driven splash damage.", result);
        RequireText(resolution, "Relations.CanAttack(attackerOwner, candidate.OwnerId)", "Splash damage must remain relation-aware.", result);

        var projectileSystem = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProjectileSystem.cs");
        RequireText(projectileSystem, "TryIntercept", "ProjectileSystem must resolve gameplay projectile interception before impact.", result);
        RequireText(projectileSystem, "definition.CanInterceptProjectiles", "Projectile interception must be driven by weapon metadata.", result);
        RequireText(projectileSystem, "mount with { CooldownRemaining = definition.Cooldown }", "Projectile interception must consume the interceptor mount cooldown.", result);

        var combatEffects = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.cs");
        RequireText(combatEffects, "List<BeamEffect> _beamEffects", "CombatEffectsLayer must own live beam presentation effects outside legacy GameState.Beams.", result);
        RequireText(combatEffects, "public void AddBeam(", "CombatEffectsLayer must expose a presentation-only beam effect bridge.", result);
        RequireText(combatEffects, "+ _beamEffects.Count", "ActiveEffectCount must include live beam presentation effects.", result);

        var combatDraw = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.CombatDraw.cs");
        RequireText(combatDraw, "foreach (var beam in _beamEffects)", "CombatEffectsLayer must draw live presentation beam effects.", result);
        RequireText(combatDraw, "private void DrawBeam(", "Legacy and live beam paths must share the same draw helper.", result);

        var battleRootEvents = ReviewGateSource.Read(root, "scripts", "BattleRoot.Events.cs");
        RequireText(battleRootEvents, "AddBeamIfNeeded", "BattleRoot attack callbacks must bridge live UnitBattlefield attacks into beam effects.", result);

        var battleRootBeams = ReviewGateSource.Read(root, "scripts", "battle", "BattleRoot.BeamEffects.cs");
        RequireText(battleRootBeams, "ammo.Behavior != ProjectileBehavior.Beam", "Beam bridge must be gated by ammo behavior data.", result);
        RequireText(battleRootBeams, "AmmoKindForPrimaryWeapon", "Building-target beam bridge must resolve attacker weapon ammo data.", result);

        ReviewGateSource.RequireTextInFile(
            root,
            result,
            "projectile-splash",
            "tools",
            "SimReplayCombatTactics",
            "ProjectileTrackingScenarios.cs");
        ReviewGateSource.RequireTextInFile(
            root,
            result,
            "projectile-intercept",
            "tools",
            "SimReplayCombatTactics",
            "ProjectileTrackingScenarios.cs");
    }
}
