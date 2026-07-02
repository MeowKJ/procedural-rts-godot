static partial class Program
{
    private static void AssertLegacyCombatRules()
    {
        var tankDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericLightTank);
        var infantryDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericInfantry);

        var ballisticMissState = EmptyState();
        var cannonVsLight = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        var lightTarget = Unit(2, UnitDesignIds.GenericInfantry, Owner.Enemy, new Vector2(760, 500), UnitStance.Hold);
        cannonVsLight.AttackTargetId = lightTarget.Id;
        cannonVsLight.AttackTargetKind = CombatTargetKind.Unit;
        ballisticMissState.Units.AddRange([cannonVsLight, lightTarget]);
        Advance(ballisticMissState, 0.9f);
        if (lightTarget.Hp < infantryDescriptor.MaxHp)
        {
            throw new InvalidOperationException("ballistic cannon should have poor accuracy against light units");
        }

        var ballisticHitState = EmptyState();
        var cannonVsTank = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        var mediumTarget = Unit(2, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(760, 500), UnitStance.Hold);
        cannonVsTank.AttackTargetId = mediumTarget.Id;
        cannonVsTank.AttackTargetKind = CombatTargetKind.Unit;
        ballisticHitState.Units.AddRange([cannonVsTank, mediumTarget]);
        Advance(ballisticHitState, 0.9f);
        if (mediumTarget.Hp >= tankDescriptor.MaxHp)
        {
            throw new InvalidOperationException("ballistic cannon should hit medium armored targets normally");
        }

        var staticWeaponState = EmptyState();
        var defensiveHq = staticWeaponState.PlaceBuilding(BuildingDesignIds.Headquarters, Owner.Player, new Vector2(500, 500));
        var staticTarget = Unit(2, UnitDesignIds.GenericInfantry, Owner.Enemy, new Vector2(650, 500), UnitStance.Hold);
        staticWeaponState.Units.Add(staticTarget);
        Advance(staticWeaponState, 0.05f);
        if (defensiveHq is null || defensiveHq.AttackTargetId != staticTarget.Id || staticTarget.Hp >= infantryDescriptor.MaxHp)
        {
            throw new InvalidOperationException("static building weapons should acquire and damage enemies through the shared combat source");
        }

        if (staticWeaponState.Beams.Any(beam => beam.SourceKind != CombatSourceKind.Building))
        {
            throw new InvalidOperationException("building beam effects should preserve building source kind");
        }

        var fixedForwardState = EmptyState();
        var fixedInfantry = Unit(1, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        fixedInfantry.Facing = 0;
        fixedInfantry.TurretFacing = 0;
        var behindTarget = Unit(2, UnitDesignIds.GenericInfantry, Owner.Enemy, new Vector2(330, 500), UnitStance.Hold);
        fixedInfantry.AttackTargetId = behindTarget.Id;
        fixedInfantry.AttackTargetKind = CombatTargetKind.Unit;
        fixedForwardState.Units.AddRange([fixedInfantry, behindTarget]);
        Advance(fixedForwardState, 0.05f);
        if (behindTarget.Hp < infantryDescriptor.MaxHp || fixedForwardState.Projectiles.Any(projectile => projectile.SourceId == fixedInfantry.Id))
        {
            throw new InvalidOperationException("fixed-forward weapons should not fire before the body has turned into its forward arc");
        }

        Advance(fixedForwardState, 0.8f);
        if (behindTarget.Hp >= infantryDescriptor.MaxHp)
        {
            throw new InvalidOperationException("light fixed-forward units should turn their body and attack instead of staying unable to fire");
        }

        var deathEventState = EmptyState();
        var deathAttacker = Unit(1, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        var deathVictim = Unit(2, UnitDesignIds.GenericInfantry, Owner.Enemy, new Vector2(575, 500), UnitStance.Hold);
        deathVictim.Hp = 1;
        deathEventState.Units.AddRange([deathAttacker, deathVictim]);
        var deaths = new List<UnitDeathInfo>();
        deathEventState.UnitsRemoved += removed => deaths.AddRange(removed);
        deathAttacker.Selected = true;
        deathEventState.CommandAttackSelected(deathVictim);
        Advance(deathEventState, 0.1f);
        if (deaths.Count != 1
            || deaths[0].Id != deathVictim.Id
            || deaths[0].KillingAmmoKind != AmmoKind.ElectromagneticLance
            || deaths[0].OverkillDamage <= 0
            || deaths[0].WeightClass != UnitWeightClass.Light)
        {
            throw new InvalidOperationException("unit death events should carry death VFX context including killing ammo, overkill damage, and unit weight");
        }

        var infantryGraceRangeState = EmptyState();
        var graceInfantry = Unit(1, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        graceInfantry.Facing = 0;
        graceInfantry.TurretFacing = 0;
        var graceTarget = Unit(2, UnitDesignIds.GenericInfantry, Owner.Enemy, new Vector2(704, 500), UnitStance.Hold);
        graceInfantry.AttackTargetId = graceTarget.Id;
        graceInfantry.AttackTargetKind = CombatTargetKind.Unit;
        infantryGraceRangeState.Units.AddRange([graceInfantry, graceTarget]);
        Advance(infantryGraceRangeState, 0.35f);
        if (graceTarget.Hp >= infantryDescriptor.MaxHp)
        {
            throw new InvalidOperationException("projectile fire authorization should be slightly longer than the engagement stop range");
        }

        var turretStateState = EmptyState();
        var idleTurret = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        idleTurret.Stance = UnitStance.Ignore;
        var reloadingTurret = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 580), UnitStance.Hold);
        reloadingTurret.Stance = UnitStance.Ignore;
        reloadingTurret.AttackCooldownRemaining = 0.5f;
        var trackingTurret = Unit(3, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 660), UnitStance.Hold);
        var trackingTarget = Unit(4, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(760, 660), UnitStance.Hold);
        trackingTurret.AttackTargetId = trackingTarget.Id;
        trackingTurret.AttackTargetKind = CombatTargetKind.Unit;
        trackingTurret.TurretFacing = Mathf.Pi;
        var firingTurret = Unit(5, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 740), UnitStance.Hold);
        var firingTarget = Unit(6, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(760, 740), UnitStance.Hold);
        firingTurret.AttackTargetId = firingTarget.Id;
        firingTurret.AttackTargetKind = CombatTargetKind.Unit;
        firingTurret.TurretFacing = 0;
        turretStateState.Units.AddRange([idleTurret, reloadingTurret, trackingTurret, trackingTarget, firingTurret, firingTarget]);
        Advance(turretStateState, 0.05f);
        if (idleTurret.TurretState != TurretState.Idle
            || reloadingTurret.TurretState != TurretState.Reloading
            || trackingTurret.TurretState != TurretState.Tracking
            || firingTurret.TurretState != TurretState.Firing
            || turretStateState.Projectiles.All(projectile => projectile.SourceId != firingTurret.Id))
        {
            throw new InvalidOperationException($"unit turret state transitions should deterministically cover idle, reloading, tracking, and firing; got idle={idleTurret.TurretState}, reload={reloadingTurret.TurretState}, tracking={trackingTurret.TurretState}, firing={firingTurret.TurretState}, projectiles={turretStateState.Projectiles.Count}");
        }

        var cannonProjectile = turretStateState.Projectiles.First(projectile => projectile.SourceId == firingTurret.Id);
        var cannonProjectileStyle = ProjectileVfxMath.StyleFor(cannonProjectile.AmmoKind);
        if (cannonProjectile.TrailWidth != cannonProjectileStyle.TrailWidth
            || cannonProjectile.CoreWidth != cannonProjectileStyle.CoreWidth
            || cannonProjectile.HeadRadius != cannonProjectileStyle.HeadRadius
            || cannonProjectile.CoreWidth < ProjectileVfxMath.MinimumCoreWidth
            || cannonProjectile.HeadRadius < ProjectileVfxMath.MinimumHeadRadius)
        {
            throw new InvalidOperationException("legacy GameState projectile visuals should use the shared readable projectile style");
        }

        var sharedThreatState = EmptyState();
        var attacker = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(800, 1000), UnitStance.Hold);
        var directTarget = Unit(2, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(1000, 1000), UnitStance.PassiveRetaliate);
        var nearbyGuard = Unit(3, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(1160, 1000), UnitStance.Hold);
        attacker.AttackTargetId = directTarget.Id;
        attacker.AttackTargetKind = CombatTargetKind.Unit;
        attacker.AttackTargetAllowsPursuit = false;
        sharedThreatState.Units.AddRange([attacker, directTarget, nearbyGuard]);
        var attackedEventCount = 0;
        sharedThreatState.EntityAttacked += (owner, factionId, position, label) =>
        {
            if (owner == Owner.Enemy && label == tankDescriptor.Label)
            {
                attackedEventCount++;
            }
        };

        Advance(sharedThreatState, 0.65f);

        if (attackedEventCount == 0)
        {
            throw new InvalidOperationException("combat damage should emit an entity-attacked event for HUD alerts");
        }

        if (nearbyGuard.AttackTargetId != attacker.Id)
        {
            throw new InvalidOperationException("nearby hold guard should copy shared threat after an ally is attacked");
        }

        if (nearbyGuard.AttackTargetAllowsPursuit)
        {
            throw new InvalidOperationException("hold guard should not pursue a shared threat");
        }

        if (nearbyGuard.AlertPulse <= 0)
        {
            throw new InvalidOperationException("shared threat response should create alert feedback");
        }

        var manualState = EmptyState();
        var playerA = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(800, 1000), UnitStance.Hold);
        var playerB = Unit(4, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(650, 1000), UnitStance.Hold);
        var attackedEnemy = Unit(2, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(1000, 1000), UnitStance.PassiveRetaliate);
        var manualEnemy = Unit(3, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(1080, 1000), UnitStance.Hold);
        playerA.AttackTargetId = attackedEnemy.Id;
        playerA.AttackTargetKind = CombatTargetKind.Unit;
        manualEnemy.AttackTargetId = playerB.Id;
        manualEnemy.AttackTargetKind = CombatTargetKind.Unit;
        manualEnemy.AttackTargetIsManual = true;
        manualEnemy.AttackTargetAllowsPursuit = true;
        manualState.Units.AddRange([playerA, playerB, attackedEnemy, manualEnemy]);

        Advance(manualState, 0.65f);

        if (manualEnemy.AttackTargetId != playerB.Id || !manualEnemy.AttackTargetIsManual)
        {
            throw new InvalidOperationException("shared threat should not overwrite a manual attack command");
        }
    }
}
