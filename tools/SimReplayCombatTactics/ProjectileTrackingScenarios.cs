static partial class Program
{
    static void RunProjectileTrackingScenario()
    {
        const int Ticks = 50;
        AssertDeterministic("projectile-tracking", BuildProjectileTrackingWorld, Ticks, 6);

        var world = BuildProjectileTrackingWorld();
        var clock = new SimClock();
        var fired = false;
        var damaged = false;
        var sourceRemoved = false;
        var sawProjectileBeforeDamage = false;
        var trackingCorrected = false;
        var movedTargetPosition = new Vector2(360, 300);
        var shooter = world.OrderedEntities.Single(entity => entity.SpecId == "replay.projectile.shooter");
        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            var events = world.Events.Drain();
            fired |= events.Any(simEvent => simEvent is WeaponFiredEvent);
            damaged |= events.Any(simEvent => simEvent is EntityDamagedEvent);
            if (!damaged && world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>()))
            {
                sawProjectileBeforeDamage = true;
            }

            if (sourceRemoved
                && world.OrderedEntities.FirstOrDefault(entity => entity.Components.Has<ProjectileComponentState>()) is { } liveProjectile)
            {
                var state = liveProjectile.Components.Require<ProjectileComponentState>();
                trackingCorrected |= state.AimPoint.DistanceSquaredTo(movedTargetPosition) <= 1f
                    && MathF.Abs(state.Velocity.Y) > 1f;
            }

            if (sawProjectileBeforeDamage && !damaged && !sourceRemoved)
            {
                var movingTarget = world.OrderedEntities.Single(entity => entity.SpecId == "replay.projectile.target");
                movingTarget.Transform = EntityTransform.At(movedTargetPosition, movingTarget.Transform.Facing);
                world.Remove(shooter.Id);
                sourceRemoved = true;
            }
        }

        var target = world.OrderedEntities.Single(entity => entity.SpecId == "replay.projectile.target");
        var targetHp = target.Components.Require<HealthComponentState>().Hp;
        Assert(fired, "tracking projectile scenario should fire a weapon.");
        Assert(sawProjectileBeforeDamage, "tracking ammo should exist as a projectile entity before impact damage.");
        Assert(sourceRemoved, "tracking projectile scenario should remove the source after launch.");
        Assert(trackingCorrected, "tracking projectile should update its aim point and steering after the target moves.");
        Assert(damaged, "tracking projectile should eventually impact and damage the target.");
        Assert(targetHp < 160, $"tracking projectile should reduce target hp, got {targetHp:0.0}.");
        Assert(!world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>()), "projectile entity should be removed after impact.");
        Console.WriteLine($"OK [projectile-tracking]: tracking ammo spawned, survived source removal, impacted, and cleaned up; target hp {targetHp:0.0}.");
    }

    static void RunProjectileInterceptScenario()
    {
        const int Ticks = 8;
        AssertDeterministic("projectile-intercept", BuildProjectileInterceptWorld, Ticks, 2);

        var world = BuildProjectileInterceptWorld();
        var clock = new SimClock();
        var fired = false;
        var intercepted = false;
        var sawCounterProjectile = false;
        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            var events = world.Events.Drain();
            fired |= events.Any(simEvent => simEvent is WeaponFiredEvent);
            sawCounterProjectile |= world.OrderedEntities.Any(entity =>
                entity.Components.TryGet<ProjectileComponentState>(out var projectile)
                && projectile.Source.Value == 2
                && projectile.Damage == 0);
            if (fired && !world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>()))
            {
                intercepted = true;
            }
        }

        var targetHp = HealthOf(world, "replay.intercept.target");
        var interceptor = world.OrderedEntities.Single(entity => entity.SpecId == "replay.intercept.interceptor");
        var interceptorWeapon = interceptor.Components.Require<WeaponUserComponentState>();
        Assert(fired, "projectile intercept scenario should fire at least one weapon.");
        Assert(intercepted, "interceptor should remove the seeker projectile entity before impact.");
        Assert(sawCounterProjectile, "non-beam interceptor fire should create its own visible counter-projectile lifecycle.");
        Assert(MathF.Abs(targetHp - 160) <= 0.001f, $"intercepted projectile should not damage the defended target, got {targetHp:0.0} hp.");
        Assert(interceptorWeapon.Mounts[0].CooldownRemaining > 0, "interceptor should consume mount cooldown when it removes a projectile.");
        Console.WriteLine($"OK [projectile-intercept]: interceptor cooldown {interceptorWeapon.Mounts[0].CooldownRemaining:0.00}, target hp {targetHp:0.0}.");
    }

    static void RunProjectileSplashScenario()
    {
        const int Ticks = 12;
        AssertDeterministic("projectile-splash", BuildProjectileSplashWorld, Ticks, 2);

        var world = BuildProjectileSplashWorld();
        var clock = new SimClock();
        var sawProjectileBeforeDamage = false;
        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            var damaged = world.Events.Drain().Any(simEvent => simEvent is EntityDamagedEvent);
            if (!damaged && world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>()))
            {
                sawProjectileBeforeDamage = true;
            }
        }

        var primary = HealthOf(world, "replay.splash.primary");
        var nearbyHostile = HealthOf(world, "replay.splash.nearby_hostile");
        var nearbyAlly = HealthOf(world, "replay.splash.nearby_ally");
        var farHostile = HealthOf(world, "replay.splash.far_hostile");
        Assert(sawProjectileBeforeDamage, "ballistic cannon should exist as a projectile before impact damage.");
        Assert(primary < 160, $"ballistic primary target should take direct damage, got {primary:0.0} hp.");
        Assert(nearbyHostile < 160, $"nearby hostile should take splash damage, got {nearbyHostile:0.0} hp.");
        Assert(MathF.Abs(nearbyAlly - 160) <= 0.001f, $"nearby allied unit should not take splash damage, got {nearbyAlly:0.0} hp.");
        Assert(MathF.Abs(farHostile - 160) <= 0.001f, $"far hostile should be outside splash radius, got {farHostile:0.0} hp.");
        Console.WriteLine($"OK [projectile-splash]: primary {primary:0.0}, nearby hostile {nearbyHostile:0.0}, ally/far untouched.");
    }

    static void RunAttackGroundScenario()
    {
        const int Ticks = 12;
        var attackGroundLog = new List<EntityCommand>
        {
            new AttackGroundEntityCommand(new OwnerId(1), [new EntityId(1)], 1, new Vector2(260, 220)),
        };
        AssertDeterministic("attack-ground-splash", BuildAttackGroundSplashWorld, attackGroundLog, Ticks, 2);

        var world = BuildAttackGroundSplashWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in attackGroundLog)
        {
            buffer.Enqueue(command);
        }

        var firedAtGround = false;
        var sawGroundProjectileBeforeDamage = false;
        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            var events = world.Events.Drain();
            foreach (var simEvent in events)
            {
                firedAtGround |= simEvent is WeaponFiredEvent fired
                    && fired.TargetPosition.DistanceSquaredTo(new Vector2(260, 220)) <= 1f;
            }

            if (!events.Any(simEvent => simEvent is EntityDamagedEvent)
                && world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>()))
            {
                sawGroundProjectileBeforeDamage = true;
            }
        }

        var nearbyHostile = HealthOf(world, "replay.attack_ground.nearby_hostile");
        var nearbyAlly = HealthOf(world, "replay.attack_ground.nearby_ally");
        var farHostile = HealthOf(world, "replay.attack_ground.far_hostile");
        Assert(firedAtGround, "attack-ground should raise a weapon fire event at the requested point.");
        Assert(sawGroundProjectileBeforeDamage, "attack-ground ballistic shot should remain visible before splash impact.");
        Assert(nearbyHostile < 160, $"attack-ground splash should damage nearby hostile, got {nearbyHostile:0.0} hp.");
        Assert(MathF.Abs(nearbyAlly - 160) <= 0.001f, $"attack-ground splash should not damage nearby ally, got {nearbyAlly:0.0} hp.");
        Assert(MathF.Abs(farHostile - 160) <= 0.001f, $"attack-ground far hostile should stay outside splash radius, got {farHostile:0.0} hp.");

        var noSplashWorld = BuildAttackGroundNoSplashWorld();
        var noSplashClock = new SimClock();
        var noSplashBuffer = new EntityCommandBuffer();
        noSplashBuffer.Enqueue(new AttackGroundEntityCommand(new OwnerId(1), [new EntityId(1)], 1, new Vector2(520, 220)));
        var noSplashFired = false;
        for (var tick = 1; tick <= Ticks; tick++)
        {
            noSplashWorld.Step(tick, noSplashClock.FixedDelta, noSplashBuffer.DrainUpToTick(tick));
            noSplashFired |= noSplashWorld.Events.Drain().Any(simEvent => simEvent is WeaponFiredEvent);
        }

        var noSplashTargetHp = HealthOf(noSplashWorld, "replay.attack_ground.no_splash_target");
        var noSplashShooter = noSplashWorld.OrderedEntities.Single(entity => entity.SpecId == "replay.attack_ground.no_splash_shooter");
        Assert(!noSplashFired, "non-splash weapon should not fire at ground.");
        Assert(!noSplashShooter.Components.Has<AttackGroundOrderComponentState>(), "non-splash weapon should not retain an attack-ground order.");
        Assert(MathF.Abs(noSplashTargetHp - 160) <= 0.001f, $"non-splash attack-ground should leave target hp unchanged, got {noSplashTargetHp:0.0}.");

        var movingWorld = BuildAttackGroundMoveIntoRangeWorld();
        var movingClock = new SimClock();
        var movingBuffer = new EntityCommandBuffer();
        movingBuffer.Enqueue(new AttackGroundEntityCommand(new OwnerId(1), [new EntityId(1)], 1, new Vector2(620, 220)));
        var movedBeforeFire = false;
        var movingFired = false;
        for (var tick = 1; tick <= 90; tick++)
        {
            movingWorld.Step(tick, movingClock.FixedDelta, movingBuffer.DrainUpToTick(tick));
            var shooter = movingWorld.OrderedEntities.Single(entity => entity.SpecId == "replay.attack_ground.moving_shooter");
            if (!movingFired && shooter.Transform.Position.X > 170)
            {
                movedBeforeFire = true;
            }

            movingFired |= movingWorld.Events.Drain().Any(simEvent => simEvent is WeaponFiredEvent);
        }

        var movingTargetHp = HealthOf(movingWorld, "replay.attack_ground.moving_target");
        var finalMovingShooter = movingWorld.OrderedEntities.Single(entity => entity.SpecId == "replay.attack_ground.moving_shooter");
        Assert(movedBeforeFire, $"out-of-range attack-ground should move toward range before firing, shooter ended at {finalMovingShooter.Transform.Position.X:0.0}.");
        Assert(movingFired, "out-of-range attack-ground should fire after moving into range.");
        Assert(movingTargetHp < 160, $"out-of-range attack-ground should eventually splash the target point, got {movingTargetHp:0.0} hp.");

        Console.WriteLine($"OK [attack-ground-splash]: ground fire damaged hostile {nearbyHostile:0.0}, moved into range, left ally/far/no-splash untouched.");
    }

    static void RunWeaponStateMachineScenario()
    {
        const int Ticks = 8;
        AssertDeterministic("weapon-state-machine", BuildWeaponStateMachineWorld, Ticks, 2);

        var world = BuildWeaponStateMachineWorld();
        var clock = new SimClock();
        var phases = new HashSet<WeaponMountPhase>();
        var fired = false;
        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            fired |= world.Events.Drain().Any(simEvent => simEvent is WeaponFiredEvent);
            var mount = MountOf(world, "replay.weapon_state.shooter");
            phases.Add(mount.Phase);
        }

        var targetHp = HealthOf(world, "replay.weapon_state.target");
        var finalMount = MountOf(world, "replay.weapon_state.shooter");
        Assert(phases.Contains(WeaponMountPhase.Warmup), "weapon state machine should enter warmup before firing.");
        Assert(phases.Contains(WeaponMountPhase.Fire), "weapon state machine should expose a fire phase when the shot resolves.");
        Assert(phases.Contains(WeaponMountPhase.Cooldown), "weapon state machine should enter cooldown after firing.");
        Assert(phases.Contains(WeaponMountPhase.Reload), "weapon state machine should enter reload after cooldown when reload time is authored.");
        Assert(fired, "weapon state machine scenario should raise a WeaponFiredEvent.");
        Assert(targetHp < 160, $"weapon state machine shot should damage the target, got {targetHp:0.0} hp.");
        Assert(finalMount.WarmupRemaining > 0, "weapon state machine should be able to reacquire and begin the next warmup after reload.");
        Console.WriteLine($"OK [weapon-state-machine]: phases {string.Join(',', phases.Order())}, target hp {targetHp:0.0}, next warmup {finalMount.WarmupRemaining:0.00}.");
    }

    private static EntityWorld BuildProjectileTrackingWorld()
    {
        var world = new EntityWorld(seed: 4242) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());

        var shooterSpec = ProjectileUnitSpec("replay.projectile.shooter", WeaponKind.RocketPod, 160);
        var targetSpec = ProjectileUnitSpec("replay.projectile.target", null, 160);
        var target = world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(360, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, target.Id, WeaponKind.RocketPod));
        return world;
    }

    private static EntityWorld BuildProjectileInterceptWorld()
    {
        var world = new EntityWorld(seed: 7070) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());

        var shooterSpec = ProjectileUnitSpec("replay.intercept.shooter", WeaponKind.RocketPod, 160);
        var targetSpec = ProjectileUnitSpec("replay.intercept.target", null, 160);
        var interceptorSpec = ProjectileUnitSpec("replay.intercept.interceptor", WeaponKind.SkySpear, 160);
        var target = world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(360, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(interceptorSpec, new OwnerId(2), EntityTransform.At(new Vector2(180, 220)), ProjectileUnitState(interceptorSpec, EntityId.None, WeaponKind.SkySpear));
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, target.Id, WeaponKind.RocketPod));
        return world;
    }

    private static EntityWorld BuildProjectileSplashWorld()
    {
        var world = new EntityWorld(seed: 5050) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.Relations.Set(new OwnerId(1), new OwnerId(3), PlayerRelation.Allied);
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());

        var shooterSpec = ProjectileUnitSpec("replay.splash.shooter", WeaponKind.VectorCannon, 160);
        var targetSpec = ProjectileUnitSpec("replay.splash.target", null, 160);
        var primary = world.Spawn(targetSpec with { Id = "replay.splash.primary" }, new OwnerId(2), EntityTransform.At(new Vector2(260, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(targetSpec with { Id = "replay.splash.nearby_hostile" }, new OwnerId(2), EntityTransform.At(new Vector2(294, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(targetSpec with { Id = "replay.splash.nearby_ally" }, new OwnerId(3), EntityTransform.At(new Vector2(286, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(targetSpec with { Id = "replay.splash.far_hostile" }, new OwnerId(2), EntityTransform.At(new Vector2(340, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, primary.Id, WeaponKind.VectorCannon));
        return world;
    }

    private static EntityWorld BuildAttackGroundSplashWorld()
    {
        var world = new EntityWorld(seed: 8181) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.Relations.Set(new OwnerId(1), new OwnerId(3), PlayerRelation.Allied);
        world.AddSystem(new CommandSystem());
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());

        var shooterSpec = ProjectileUnitSpec("replay.attack_ground.shooter", WeaponKind.VectorCannon, 160);
        var targetSpec = ProjectileUnitSpec("replay.attack_ground.target", null, 160);
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, EntityId.None, WeaponKind.VectorCannon));
        world.Spawn(targetSpec with { Id = "replay.attack_ground.nearby_hostile" }, new OwnerId(2), EntityTransform.At(new Vector2(280, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(targetSpec with { Id = "replay.attack_ground.nearby_ally" }, new OwnerId(3), EntityTransform.At(new Vector2(286, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(targetSpec with { Id = "replay.attack_ground.far_hostile" }, new OwnerId(2), EntityTransform.At(new Vector2(340, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        return world;
    }

    private static EntityWorld BuildAttackGroundNoSplashWorld()
    {
        var world = new EntityWorld(seed: 8282) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.AddSystem(new CommandSystem());
        world.AddSystem(new CombatSystem());

        var shooterSpec = ProjectileUnitSpec("replay.attack_ground.no_splash_shooter", WeaponKind.NeedleRifle, 160);
        var targetSpec = ProjectileUnitSpec("replay.attack_ground.no_splash_target", null, 160);
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(520, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        return world;
    }

    private static EntityWorld BuildAttackGroundMoveIntoRangeWorld()
    {
        var world = new EntityWorld(seed: 8383) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.AddSystem(new CommandSystem());
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());
        world.AddSystem(new MovementSystem());

        var shooterSpec = ProjectileUnitSpec("replay.attack_ground.moving_shooter", WeaponKind.VectorCannon, 160);
        var targetSpec = ProjectileUnitSpec("replay.attack_ground.moving_target", null, 160);
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), MovingAttackGroundState(shooterSpec, WeaponKind.VectorCannon));
        world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(620, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        return world;
    }

    private static EntityWorld BuildWeaponStateMachineWorld()
    {
        var world = new EntityWorld(seed: 6161) { WorldWidth = 900, WorldHeight = 600 };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.RegisterCombatDefinitions(
            [
                WeaponCatalog.Weapons[WeaponKind.NeedleRifle] with
                {
                    Warmup = 0.07f,
                    Cooldown = 0.06f,
                    Reload = 0.04f,
                },
            ],
            WeaponCatalog.AmmoDefinitions.Values);
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());

        var shooterSpec = ProjectileUnitSpec("replay.weapon_state.shooter", WeaponKind.NeedleRifle, 160);
        var targetSpec = ProjectileUnitSpec("replay.weapon_state.target", null, 160);
        var target = world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(240, 220)), ProjectileUnitState(targetSpec, EntityId.None, WeaponKind.NeedleRifle));
        world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 220)), ProjectileUnitState(shooterSpec, target.Id, WeaponKind.NeedleRifle));
        return world;
    }

    private static float HealthOf(EntityWorld world, string specId)
    {
        return world.OrderedEntities
            .Single(entity => entity.SpecId == specId)
            .Components.Require<HealthComponentState>()
            .Hp;
    }

    private static WeaponMountRuntimeState MountOf(EntityWorld world, string specId)
    {
        return world.OrderedEntities
            .Single(entity => entity.SpecId == specId)
            .Components.Require<WeaponUserComponentState>()
            .Mounts[0];
    }

    private static EntitySpec ProjectileUnitSpec(string id, WeaponKind? weapon, float hp)
    {
        return new EntitySpec
        {
            Id = id,
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec(id, id + ".name", id + ".role", "PJT", IconGlyph.AttackMove),
            Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, hp, SightRange: 520, Cost: 0, TechTier: 1),
            Movement = new MovementSpec(MovementDomain.Land, Speed: 0, TurnRate: 0),
            Collision = new CollisionSpec(Radius: 14, Mass: 1, PushPriority: 1),
            Weapons = weapon is { } kind
                ? [WeaponMountSpec.Independent("main", kind, Vector2.Zero, new Vector2(14, 0), MathF.Tau, 12, fireWhileMoving: true)]
                : [],
        };
    }

    private static EntityComponentState[] ProjectileUnitState(EntitySpec spec, EntityId target, WeaponKind weapon)
    {
        var states = new List<EntityComponentState>
        {
            new HealthComponentState(spec.Stats!.MaxHp, spec.Stats!.MaxHp),
            new CollisionComponentState(spec.Collision!.Radius, spec.Collision.Mass, spec.Collision.PushPriority, spec.Collision.BlocksMovement),
        };

        if (spec.Weapons.Count > 0)
        {
            states.Add(new WeaponUserComponentState(
                new[] { new WeaponMountRuntimeState("main", weapon, 0, 0) },
                target,
                CombatTargetKind.Unit,
                AttackTargetIsManual: target.IsValid));
        }

        return states.ToArray();
    }

    private static EntityComponentState[] MovingAttackGroundState(EntitySpec spec, WeaponKind weapon)
    {
        var states = ProjectileUnitState(spec, EntityId.None, weapon).ToList();
        states.Add(new MovementComponentState(Vector2.Zero));
        states.Add(new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2, TurnRate: 12));
        return states.ToArray();
    }
}
