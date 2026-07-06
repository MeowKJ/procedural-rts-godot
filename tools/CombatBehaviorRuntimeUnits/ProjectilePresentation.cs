static partial class Program
{
    private static void AssertUnitBattlefieldProjectilePresentation()
    {
        var projectilePresentationBattlefield = new UnitBattlefield();
        var rocketAttacker = projectilePresentationBattlefield.Spawn("dog.rocket", PlayerSlotId.One, new Vector2(300, 500), 0);
        var projectileTarget = projectilePresentationBattlefield.UpsertBuildingTarget(
            88,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(620, 500),
            Mathf.Pi,
            BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
        projectilePresentationBattlefield.SelectUnitsByIds(PlayerSlotId.One, [rocketAttacker.Id]);
        projectilePresentationBattlefield.CommandAttackSelected(PlayerSlotId.One, projectileTarget.Id);
        projectilePresentationBattlefield.Update(1 / 30.0);
        var projectileProjections = projectilePresentationBattlefield.ProjectileProjections();
        var ordinaryProjectileStyle = ProjectileVfxMath.StyleFor(AmmoKind.NeedleDart);
        var seekerProjectileStyle = ProjectileVfxMath.StyleFor(AmmoKind.SeekerRocket);
        if (projectileProjections.Count == 0
            || projectileProjections.All(projectile => projectile.LegacyAmmoKind != AmmoKind.SeekerRocket)
            || projectileProjections.Any(projectile => projectile.Velocity.LengthSquared() <= 0.01f)
            || projectileProjections.Any(projectile => projectile.TrailWidth <= projectile.CoreWidth)
            || projectileProjections.Any(projectile => projectile.HeadRadius <= 0)
            || ordinaryProjectileStyle.CoreWidth < ProjectileVfxMath.MinimumCoreWidth
            || ordinaryProjectileStyle.HeadRadius < ProjectileVfxMath.MinimumHeadRadius
            || ordinaryProjectileStyle.TrailAlpha < ProjectileVfxMath.MinimumTrailAlpha
            || projectileProjections.Any(projectile => projectile.LegacyAmmoKind == AmmoKind.SeekerRocket && projectile.Style != seekerProjectileStyle))
        {
            throw new InvalidOperationException("UnitBattlefield should expose render-ready, readable EntityWorld projectile projections for CombatEffectsLayer");
        }

        var tankProjectileBattlefield = new UnitBattlefield();
        var tankProjectileAttacker = tankProjectileBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(300, 540), 0);
        var tankProjectileTarget = tankProjectileBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(560, 540), Mathf.Pi);
        var tankShotEvents = new List<WeaponFiredEvent>();
        tankProjectileBattlefield.WeaponFired += fired => tankShotEvents.Add(fired);
        tankProjectileBattlefield.SelectUnitsByIds(PlayerSlotId.One, [tankProjectileAttacker.Id]);
        tankProjectileBattlefield.CommandAttackSelected(PlayerSlotId.One, tankProjectileTarget);
        tankProjectileBattlefield.Update(1 / 30.0);

        var tankShot = tankShotEvents.FirstOrDefault(fired => fired.LegacyWeaponKind == WeaponKind.VectorCannon);
        var tankCenter = tankProjectileAttacker.Position;
        var muzzleDistance = tankShot?.Muzzle.DistanceTo(tankCenter) ?? 0;
        var mountSpec = tankProjectileAttacker.Spec.Weapons[0];
        var mountFacing = tankProjectileAttacker.WeaponMounts[0].Facing;
        var expectedMuzzle = tankCenter
            + mountSpec.Anchor.Rotated(tankProjectileAttacker.Facing)
            + mountSpec.MuzzleOffset.Rotated(mountFacing);
        var shotStyle = ShotTrailVfxMath.StyleFor(WeaponKind.VectorCannon, tankShot?.Muzzle.DistanceTo(tankShot.TargetPosition) ?? 0);
        if (tankShot is null
            || muzzleDistance < 18f
            || tankShot.Muzzle.DistanceTo(expectedMuzzle) > 0.5f
            || !ShotTrailVfxMath.ShouldCreate(WeaponKind.VectorCannon)
            || ShotTrailVfxMath.ShouldCreate(WeaponKind.NeedleRifle)
            || !shotStyle.Draw
            || shotStyle.Width <= shotStyle.CoreWidth
            || tankProjectileBattlefield.ProjectileProjections().Any(projectile => projectile.LegacyAmmoKind == AmmoKind.BallisticCannon))
        {
            throw new InvalidOperationException("rotating-turret tanks should expose visible ballistic shot presentation from the independent mount muzzle");
        }
    }
}
