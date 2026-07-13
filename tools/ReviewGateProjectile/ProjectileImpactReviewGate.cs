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
        RequireText(resolution, "ammo.Behavior != ProjectileBehavior.Beam", "Every non-beam ammo behavior must spawn a projectile entity.", result);
        RequireText(resolution, "ProjectileImpactEvent", "Projectile impacts must expose their deterministic landing point to presentation.", result);

        var projectileState = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityComponentState.cs");
        RequireText(projectileState, "ProjectileBehavior Behavior", "Projectile state must persist its authored behavior.", result);
        RequireText(projectileState, "HitRule HitRule", "Projectile state must persist its hit rule.", result);
        RequireText(projectileState, "Vector2 AimPoint", "Projectile state must persist its last deterministic aim/impact point.", result);
        RequireText(projectileState, "float FlightDuration", "Projectile state must persist deterministic flight progress for presentation.", result);

        var projectileSystem = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProjectileSystem.cs");
        RequireText(projectileSystem, "TryIntercept", "ProjectileSystem must resolve gameplay projectile interception before impact.", result);
        RequireText(projectileSystem, "definition.CanInterceptProjectiles", "Projectile interception must be driven by weapon metadata.", result);
        RequireText(projectileSystem, "WeaponSystem.BeginRecovery(mount, definition)", "Projectile interception must consume the interceptor mount through the shared weapon state machine.", result);
        RequireText(projectileSystem, "TryLiveTarget", "ProjectileSystem must continue safely when a target disappears.", result);
        RequireText(projectileSystem, "AimPoint = aimPoint", "Tracking projectiles must persist their latest aim point while direct/ballistic rounds keep fixed points.", result);

        var weaponSystem = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponSystem.cs");
        RequireText(weaponSystem, "static class WeaponSystem", "WeaponSystem must own shared mount state-machine stepping.", result);
        RequireText(weaponSystem, "WeaponMountPhase.Warmup", "WeaponSystem must model warmup before fire.", result);
        RequireText(weaponSystem, "WeaponMountPhase.Fire", "WeaponSystem must expose a fire phase when shots resolve.", result);
        RequireText(weaponSystem, "WeaponMountPhase.Cooldown", "WeaponSystem must model post-fire cooldown.", result);
        RequireText(weaponSystem, "WeaponMountPhase.Reload", "WeaponSystem must model post-cooldown reload.", result);
        RequireText(weaponSystem, "BeginRecovery", "WeaponSystem must centralize fire-to-recovery transitions.", result);

        var mountState = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "WeaponMountRuntimeState.cs");
        RequireText(mountState, "WeaponMountPhase Phase", "Weapon mount runtime state must persist the active state-machine phase.", result);
        RequireText(mountState, "float WarmupRemaining", "Weapon mount runtime state must persist warmup remaining time.", result);
        RequireText(mountState, "float ReloadRemaining", "Weapon mount runtime state must persist reload remaining time.", result);

        var hashOrdering = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityStateHash.Ordering.cs");
        RequireText(hashOrdering, "Add(hash, (int)mount.Phase)", "Deterministic state hash must include weapon mount phase.", result);
        RequireText(hashOrdering, "Add(hash, mount.WarmupRemaining)", "Deterministic state hash must include weapon warmup time.", result);
        RequireText(hashOrdering, "Add(hash, mount.ReloadRemaining)", "Deterministic state hash must include weapon reload time.", result);

        var combatEffects = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.cs");
        RequireText(combatEffects, "List<BeamEffect> _beamEffects", "CombatEffectsLayer must own live beam presentation effects outside legacy GameState.Beams.", result);
        RequireText(combatEffects, "public void AddBeam(", "CombatEffectsLayer must expose a presentation-only beam effect bridge.", result);
        RequireText(combatEffects, "+ _beamEffects.Count", "ActiveEffectCount must include live beam presentation effects.", result);

        var muzzleEffects = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.MuzzleFlashes.cs");
        ForbidText(muzzleEffects, "ShotTrail", "WeaponFiredEvent muzzle flashes must not create duplicate instant shot trails for live projectiles.", result);

        var combatDraw = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.CombatDraw.cs");
        RequireText(combatDraw, "foreach (var beam in _beamEffects)", "CombatEffectsLayer must draw live presentation beam effects.", result);
        RequireText(combatDraw, "private void DrawBeam(", "Legacy and live beam paths must share the same draw helper.", result);
        RequireText(combatDraw, "projectile.GroundPosition", "Ballistic projectiles must expose a ground position for their shadow.", result);

        var battleRootEvents = ReviewGateSource.Read(root, "scripts", "BattleRoot.Events.cs");
        RequireText(battleRootEvents, "AddBeamIfNeeded", "BattleRoot attack callbacks must bridge live UnitBattlefield attacks into beam effects.", result);

        var battleRootBeams = ReviewGateSource.Read(root, "scripts", "battle", "BattleRoot.BeamEffects.cs");
        RequireText(battleRootBeams, "ammo.Behavior != ProjectileBehavior.Beam", "Beam bridge must be gated by ammo behavior data.", result);
        RequireText(battleRootBeams, "AmmoKindForPrimaryWeapon", "Building-target beam bridge must resolve attacker weapon ammo data.", result);

        var visualQa = ReviewGateSource.Read(root, "scripts", "VisualQaCaptureRoot.cs");
        RequireText(visualQa, "battle_projectile_lifecycle.png", "Visual QA must capture the live mixed projectile lifecycle.", result);
        RequireText(visualQa, "hasDirect && hasBallistic && hasTracking", "Projectile Visual QA must prove all non-beam behaviors are visible together.", result);
        RequireText(visualQa, "DebugConfigureProjectileVisualQaScenario", "Projectile Visual QA must use its sparse three-lane combat scene.", result);
        ReviewGateSource.RequireTextInFile(root, result, "battle_projectile_lifecycle.png", "tools", "VisualQaCapture.sh");

        ReviewGateSource.RequireTextInFile(root, result, "projectile-splash", "tools", "SimReplayCombatTactics", "ProjectileTrackingScenarios.cs");
        ReviewGateSource.RequireTextInFile(root, result, "projectile-intercept", "tools", "SimReplayCombatTactics", "ProjectileTrackingScenarios.cs");
        ReviewGateSource.RequireTextInFile(root, result, "weapon-state-machine", "tools", "SimReplayCombatTactics", "ProjectileTrackingScenarios.cs");
        ReviewGateSource.RequireTextInFile(root, result, "projectile-lifecycle", "tools", "SimReplayProjectileLifecycle", "ObservableProjectileScenarios.cs");
    }
}
