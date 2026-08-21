using ProceduralRts.Tools.Qa;

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
        QaPlayerCommandDriver.AttackBuildingSelection(projectilePresentationBattlefield, PlayerSlotId.One, projectileTarget.Id);
        projectilePresentationBattlefield.Update(1 / 30.0);
        var projectileProjections = projectilePresentationBattlefield.ProjectileProjections();
        var ordinaryProjectileStyle = ProjectileVfxMath.StyleFor(AmmoIds.NeedleDart);
        var seekerProjectileStyle = ProjectileVfxMath.StyleFor(AmmoIds.SeekerRocket);
        if (projectileProjections.Count == 0
            || projectileProjections.All(projectile => projectile.AmmoId != AmmoIds.SeekerRocket)
            || projectileProjections.Any(projectile => projectile.Velocity.LengthSquared() <= 0.01f)
            || projectileProjections.Any(projectile => projectile.TrailWidth <= projectile.CoreWidth)
            || projectileProjections.Any(projectile => projectile.HeadRadius <= 0)
            || ordinaryProjectileStyle.CoreWidth < ProjectileVfxMath.MinimumCoreWidth
            || ordinaryProjectileStyle.HeadRadius < ProjectileVfxMath.MinimumHeadRadius
            || ordinaryProjectileStyle.TrailAlpha < ProjectileVfxMath.MinimumTrailAlpha
            || ordinaryProjectileStyle.MinimumVisibleSeconds < ProjectileVfxMath.MinimumVisibleSeconds
            || projectileProjections.Any(projectile => projectile.AmmoId == AmmoIds.SeekerRocket && projectile.Style != seekerProjectileStyle))
        {
            throw new InvalidOperationException("UnitBattlefield should expose render-ready, readable EntityWorld projectile projections for CombatEffectsLayer");
        }

        var tankProjectileBattlefield = new UnitBattlefield();
        var tankProjectileAttacker = tankProjectileBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(300, 540), 0);
        var tankProjectileTarget = tankProjectileBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(560, 540), Mathf.Pi);
        var tankShotEvents = new List<WeaponFiredEvent>();
        tankProjectileBattlefield.WeaponFired += fired => tankShotEvents.Add(fired);
        tankProjectileBattlefield.SelectUnitsByIds(PlayerSlotId.One, [tankProjectileAttacker.Id]);
        QaPlayerCommandDriver.AttackSelection(tankProjectileBattlefield, PlayerSlotId.One, tankProjectileTarget);
        tankProjectileBattlefield.Update(1 / 30.0);

        var tankShot = tankShotEvents.FirstOrDefault(fired => fired.WeaponId == WeaponIds.VectorCannon);
        var tankCenter = tankProjectileAttacker.Position;
        var muzzleDistance = tankShot?.Muzzle.DistanceTo(tankCenter) ?? 0;
        var mountSpec = tankProjectileAttacker.Spec.Weapons[0];
        var mountFacing = tankProjectileAttacker.WeaponMounts[0].Facing;
        var expectedMuzzle = tankCenter
            + mountSpec.Anchor.Rotated(tankProjectileAttacker.Facing)
            + mountSpec.MuzzleOffset.Rotated(mountFacing);
        var initialBallistic = tankProjectileBattlefield.EntityWorld.OrderedEntities
            .SingleOrDefault(entity => entity.Components.TryGet<ProjectileComponentState>(out var state)
                && state.Source == tankProjectileAttacker.EntityId
                && state.AmmoId == AmmoIds.BallisticCannon);
        var initialBallisticState = initialBallistic?.Components.Require<ProjectileComponentState>();
        tankProjectileBattlefield.Update(1 / 30.0);
        var ballisticProjection = tankProjectileBattlefield.ProjectileProjections()
            .SingleOrDefault(projectile => projectile.Id == initialBallistic?.Id);
        var simulationProjectileCount = tankProjectileBattlefield.EntityWorld.OrderedEntities
            .Count(entity => entity.Components.Has<ProjectileComponentState>());
        if (tankShot is null
            || muzzleDistance < 18f
            || tankShot.Muzzle.DistanceTo(expectedMuzzle) > 0.5f
            || initialBallisticState is null
            || initialBallisticState.Origin.DistanceTo(expectedMuzzle) > 0.5f
            || initialBallisticState.Behavior != ProjectileBehavior.Ballistic
            || initialBallisticState.FlightDuration < initialBallisticState.Age
            || ballisticProjection.AmmoId != AmmoIds.BallisticCannon
            || ballisticProjection.Behavior != ProjectileBehavior.Ballistic
            || ballisticProjection.HitRule != HitRule.BallisticDeviation
            || ballisticProjection.ArcHeight <= 0
            || !ballisticProjection.HasGroundShadow
            || tankProjectileBattlefield.ProjectileProjectionCount() != simulationProjectileCount)
        {
            throw new InvalidOperationException(
                $"rotating-turret tanks should expose a visible ballistic projectile arc from the independent mount muzzle without an instant duplicate trail; "
                + $"shot={tankShot is not null}, muzzle={muzzleDistance:0.0}/{tankShot?.Muzzle.DistanceTo(expectedMuzzle):0.0}, initial={initialBallisticState is not null}, "
                + $"behavior={initialBallisticState?.Behavior}, projection={ballisticProjection.AmmoId}/{ballisticProjection.Behavior}/{ballisticProjection.HitRule}, "
                + $"arc={ballisticProjection.ArcHeight:0.0}, shadow={ballisticProjection.HasGroundShadow}, counts={tankProjectileBattlefield.ProjectileProjectionCount()}/{simulationProjectileCount}");
        }
    }
}
