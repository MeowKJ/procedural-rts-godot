static partial class Program
{
    static void RunObservableProjectileLifecycleScenario()
    {
        AssertDeterministic("projectile-direct-lifecycle", BuildDirectProjectileWorld, 12, 2);
        AssertDeterministic("projectile-ballistic-lifecycle", BuildBallisticMissWorld, 20, 2);
        AssertDeterministic("projectile-fog-projection", BuildProjectileFogWorld, 2, 1);

        AssertDirectProjectileDoesNotTrack();
        AssertBallisticProjectileArcAndMiss();
        AssertProjectileSurvivesTargetRemoval();
        AssertProjectileProjectionRespectsFog();
        Console.WriteLine("OK [projectile-lifecycle]: direct, ballistic, tracking, target-loss, projection parity, arc shadow, and fog rules.");
    }

    private static void AssertDirectProjectileDoesNotTrack()
    {
        var world = BuildDirectProjectileWorld();
        var clock = new SimClock();
        world.Step(1, clock.FixedDelta, []);
        var firstEvents = world.Events.Drain();
        var fired = firstEvents.OfType<WeaponFiredEvent>().Single();
        Assert(!firstEvents.Any(simEvent => simEvent is EntityDamagedEvent), "direct projectile must not damage on its fire tick");

        var projectile = world.OrderedEntities.Single(entity => entity.Components.Has<ProjectileComponentState>());
        var initial = projectile.Components.Require<ProjectileComponentState>();
        var projections = ProjectilePresentationProjector.Project(world, new PlayerSlotId(1));
        Assert(initial.Behavior == ProjectileBehavior.Direct && initial.HitRule == HitRule.Guaranteed,
            $"NeedleDart should spawn a direct guaranteed projectile, got {initial.Behavior}/{initial.HitRule}");
        Assert(initial.Origin.DistanceSquaredTo(fired.Muzzle) <= 0.01f,
            $"direct projectile should originate at muzzle {fired.Muzzle}, got {initial.Origin}");
        Assert(initial.FlightDuration >= ProjectileVfxMath.MinimumVisibleSeconds,
            $"direct projectile should honor minimum visible flight time, got {initial.FlightDuration:0.000}s");
        Assert(projections.Count == 1 && ProjectilePresentationProjector.Count(world) == 1,
            "projectile projection count should match the single simulation projectile");

        var target = world.OrderedEntities.Single(entity => entity.SpecId == "replay.observable.direct.target");
        target.Transform = EntityTransform.At(new Vector2(180, 300));
        var initialAim = initial.AimPoint;
        var initialDirection = initial.Velocity.Normalized();
        var damaged = false;
        for (var tick = 2; tick <= 12; tick++)
        {
            world.Step(tick, clock.FixedDelta, []);
            damaged |= world.Events.Drain().Any(simEvent => simEvent is EntityDamagedEvent);
            if (world.OrderedEntities.FirstOrDefault(entity => entity.Components.Has<ProjectileComponentState>()) is { } live)
            {
                var state = live.Components.Require<ProjectileComponentState>();
                Assert(state.AimPoint.DistanceSquaredTo(initialAim) <= 0.01f,
                    "direct projectile must keep its fire-time aim point when the target moves");
                Assert(state.Velocity.Normalized().Dot(initialDirection) >= 0.999f,
                    "direct projectile must keep a straight heading rather than tracking");
            }
        }

        Assert(damaged, "direct projectile should apply guaranteed damage at impact time");
        Assert(target.Components.Require<HealthComponentState>().Hp < 160, "direct projectile should reduce moved target hp on impact");
    }

    private static void AssertBallisticProjectileArcAndMiss()
    {
        var world = BuildBallisticMissWorld();
        var clock = new SimClock();
        world.Step(1, clock.FixedDelta, []);
        var firstEvents = world.Events.Drain();
        Assert(!firstEvents.Any(simEvent => simEvent is EntityDamagedEvent), "ballistic projectile must not damage on its fire tick");

        var projectile = world.OrderedEntities.Single(entity => entity.Components.Has<ProjectileComponentState>());
        var state = projectile.Components.Require<ProjectileComponentState>();
        var target = world.OrderedEntities.Single(entity => entity.SpecId == "replay.observable.ballistic.target");
        Assert(state.Behavior == ProjectileBehavior.Ballistic && state.HitRule == HitRule.BallisticDeviation,
            $"BallisticCannon should preserve ballistic deviation metadata, got {state.Behavior}/{state.HitRule}");
        Assert(state.AimPoint.DistanceTo(target.Transform.Position) > state.HitRadius,
            "light-target ballistic sample should deterministically miss its primary hit radius");
        var sampledImpact = state.AimPoint;

        world.Step(2, clock.FixedDelta, []);
        var secondEvents = world.Events.Drain();
        Assert(!secondEvents.Any(simEvent => simEvent is EntityDamagedEvent), "ballistic projectile should remain in flight for multiple ticks");
        var projection = ProjectilePresentationProjector.Project(world, new PlayerSlotId(1)).Single();
        Assert(projection.Behavior == ProjectileBehavior.Ballistic
            && projection.ArcHeight > 0
            && projection.Position != projection.GroundPosition
            && projection.HasGroundShadow,
            "ballistic projection should expose a presentation-only height arc and ground shadow");

        ProjectileImpactEvent? impact = null;
        for (var tick = 3; tick <= 20; tick++)
        {
            world.Step(tick, clock.FixedDelta, []);
            var events = world.Events.Drain();
            impact ??= events.OfType<ProjectileImpactEvent>().FirstOrDefault();
        }

        var damageTaken = 160 - target.Components.Require<HealthComponentState>().Hp;
        Assert(impact is not null && !impact.HitPrimary && impact.Position.DistanceSquaredTo(sampledImpact) <= 0.01f,
            "ballistic miss should resolve at its deterministic sampled landing point");
        Assert(damageTaken > 0 && damageTaken < 15,
            $"ballistic miss should apply only nearby splash rather than direct damage, got {damageTaken:0.0}");
        Assert(!world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>()),
            "ballistic projectile should clean up after landing");
    }

    private static void AssertProjectileSurvivesTargetRemoval()
    {
        var world = BuildDirectProjectileWorld();
        var clock = new SimClock();
        world.Step(1, clock.FixedDelta, []);
        world.Events.Drain();
        var projectile = world.OrderedEntities.Single(entity => entity.Components.Has<ProjectileComponentState>());
        var projectileId = projectile.Id;
        var start = projectile.Transform.Position;
        var aimPoint = projectile.Components.Require<ProjectileComponentState>().AimPoint;
        var target = world.OrderedEntities.Single(entity => entity.SpecId == "replay.observable.direct.target");
        world.Remove(target.Id);

        world.Step(2, clock.FixedDelta, []);
        world.Events.Drain();
        Assert(world.TryGet(projectileId, out var surviving) && surviving.Transform.Position != start,
            "projectile should continue flying after its target is removed");
        Assert(!SimInvariants.Validate(world).Any(violation => violation.Component.StartsWith("Projectile", StringComparison.Ordinal)),
            "stale projectile target identity should remain invariant-safe during last-point flight");

        ProjectileImpactEvent? impact = null;
        for (var tick = 3; tick <= 12; tick++)
        {
            world.Step(tick, clock.FixedDelta, []);
            var events = world.Events.Drain();
            impact ??= events.OfType<ProjectileImpactEvent>().FirstOrDefault();
        }

        Assert(impact is not null && !impact.HitPrimary && impact.Position.DistanceSquaredTo(aimPoint) <= 0.01f,
            "target-loss projectile should safely finish at its last aim point without a primary hit");
        Assert(!world.TryGet(projectileId, out _), "target-loss projectile should clean up after completing visible flight");
    }

    private static void AssertProjectileProjectionRespectsFog()
    {
        var world = BuildProjectileFogWorld();
        var clock = new SimClock();
        world.Step(1, clock.FixedDelta, []);
        world.Events.Drain();
        var simulationCount = world.OrderedEntities.Count(entity => entity.Components.Has<ProjectileComponentState>());
        var ownerProjection = ProjectilePresentationProjector.Project(world, new PlayerSlotId(2));
        var hiddenProjection = ProjectilePresentationProjector.Project(world, new PlayerSlotId(1));
        Assert(simulationCount == 1 && ProjectilePresentationProjector.Count(world) == simulationCount,
            "projectile count API should match simulation entities without allocating projections");
        Assert(ownerProjection.Count == simulationCount, "projectile owner should always receive its own projectile projection");
        Assert(hiddenProjection.Count == 0, "hostile projectile outside viewer vision must not leak through projection APIs");
    }

    private static EntityWorld BuildDirectProjectileWorld()
    {
        var world = NewObservableProjectileWorld(seed: 5261, WeaponKind.NeedleRifle);
        var targetSpec = ProjectileUnitSpec("replay.observable.direct.target", null, 160);
        var shooterSpec = ProjectileUnitSpec("replay.observable.direct.shooter", WeaponKind.NeedleRifle, 160);
        var target = world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(180, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, target.Id, WeaponKind.NeedleRifle));
        return world;
    }

    private static EntityWorld BuildBallisticMissWorld()
    {
        var world = NewObservableProjectileWorld(seed: 5262, WeaponKind.VectorCannon);
        var targetSpec = ProjectileUnitSpec("replay.observable.ballistic.target", null, 160);
        targetSpec = targetSpec with
        {
            Stats = targetSpec.Stats! with { WeightClass = UnitWeightClass.Light },
        };
        var shooterSpec = ProjectileUnitSpec("replay.observable.ballistic.shooter", WeaponKind.VectorCannon, 160);
        var target = world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(320, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, target.Id, WeaponKind.VectorCannon));
        return world;
    }

    private static EntityWorld BuildProjectileFogWorld()
    {
        var world = new EntityWorld(seed: 5263) { WorldWidth = 1000, WorldHeight = 700 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.Relations.Set(new OwnerId(2), new OwnerId(3), PlayerRelation.Hostile);
        world.RegisterCombatDefinitions(
            [WeaponCatalog.Weapons[WeaponKind.NeedleRifle] with { Cooldown = 10 }],
            WeaponCatalog.AmmoDefinitions.Values);
        world.AddSystem(new VisionSystem());
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());

        var observerSpec = ProjectileUnitSpec("replay.observable.fog.observer", null, 160);
        var observerStates = ProjectileUnitState(observerSpec, EntityId.None, WeaponKind.NeedleRifle)
            .Append<EntityComponentState>(new VisionComponentState(80))
            .ToArray();
        world.Spawn(observerSpec, new OwnerId(1), EntityTransform.At(new Vector2(80, 80)), observerStates);

        var targetSpec = ProjectileUnitSpec("replay.observable.fog.target", null, 160);
        var shooterSpec = ProjectileUnitSpec("replay.observable.fog.shooter", WeaponKind.NeedleRifle, 160);
        var target = world.Spawn(targetSpec, new OwnerId(3), EntityTransform.At(new Vector2(820, 500)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        var shooterStates = ProjectileUnitState(shooterSpec, target.Id, WeaponKind.NeedleRifle)
            .Append<EntityComponentState>(new VisionComponentState(240))
            .ToArray();
        world.Spawn(shooterSpec, new OwnerId(2), EntityTransform.At(new Vector2(700, 500)), shooterStates);
        return world;
    }

    private static EntityWorld NewObservableProjectileWorld(ulong seed, WeaponKind weaponKind)
    {
        var world = new EntityWorld(seed) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.RegisterCombatDefinitions(
            [WeaponCatalog.Weapons[weaponKind] with { Cooldown = 10 }],
            WeaponCatalog.AmmoDefinitions.Values);
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());
        return world;
    }
}
