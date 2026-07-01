static partial class Program
{
    static void RunProjectileTrackingScenario()
    {
        const int Ticks = 36;
        AssertDeterministic("projectile-tracking", BuildProjectileTrackingWorld, Ticks, 6);

        var world = BuildProjectileTrackingWorld();
        var clock = new SimClock();
        var fired = false;
        var damaged = false;
        var sourceRemoved = false;
        var sawProjectileBeforeDamage = false;
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

            if (sawProjectileBeforeDamage && !damaged && !sourceRemoved)
            {
                world.Remove(shooter.Id);
                sourceRemoved = true;
            }
        }

        var target = world.OrderedEntities.Single(entity => entity.SpecId == "replay.projectile.target");
        var targetHp = target.Components.Require<HealthComponentState>().Hp;
        Assert(fired, "tracking projectile scenario should fire a weapon.");
        Assert(sawProjectileBeforeDamage, "tracking ammo should exist as a projectile entity before impact damage.");
        Assert(sourceRemoved, "tracking projectile scenario should remove the source after launch.");
        Assert(damaged, "tracking projectile should eventually impact and damage the target.");
        Assert(targetHp < 160, $"tracking projectile should reduce target hp, got {targetHp:0.0}.");
        Assert(!world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>()), "projectile entity should be removed after impact.");
        Console.WriteLine($"OK [projectile-tracking]: tracking ammo spawned, survived source removal, impacted, and cleaned up; target hp {targetHp:0.0}.");
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

        if (target.IsValid)
        {
            states.Add(new WeaponUserComponentState(
                new[] { new WeaponMountRuntimeState("main", weapon, 0, 0) },
                target,
                CombatTargetKind.Unit,
                AttackTargetIsManual: true));
        }

        return states.ToArray();
    }
}
