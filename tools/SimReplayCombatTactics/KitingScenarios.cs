static partial class Program
{
    static void RunKitingScenario()
    {
        const int KitingTicks = 150;
        AssertDeterministic("ranged-min-range-kiting", BuildKitingWorld, KitingTicks, 15);

        var world = BuildKitingWorld();
        var clock = new SimClock();
        var fired = false;
        for (var tick = 1; tick <= KitingTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            fired |= world.Events.Drain().Any(simEvent => simEvent is WeaponFiredEvent);

            if (tick == 1)
            {
                var kiter = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
                var movement = kiter.Components.Require<MovementComponentState>();
                Assert(movement.MoveTarget is { } moveTarget && moveTarget.X < kiter.Transform.Position.X,
                    $"kiter should immediately back away from min-range pressure, got {movement.MoveTarget}");
            }
        }

        var attacker = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var target = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var targetHealth = target.Components.Require<HealthComponentState>();
        var weapon = WeaponCatalog.WeaponDefinitions[WeaponIds.LightRepeater];
        var radius = target.Components.Require<CollisionComponentState>().Radius;
        var effectiveDistance = WeaponMath.EffectiveTargetDistance(
            attacker.Transform.Position.DistanceTo(target.Transform.Position),
            radius);

        Assert(effectiveDistance >= weapon.MinRange - 6,
            $"kiter should restore min-range spacing, got {effectiveDistance:0.0} < {weapon.MinRange:0.0}");
        Assert(fired && targetHealth.Hp < targetHealth.MaxHp,
            "kiter should resume firing once it has rebuilt spacing");
        Console.WriteLine($"OK [ranged-min-range-kiting]: spacing {effectiveDistance:0.0}, target hp {targetHealth.Hp:0.0}.");
    }

    private static EntityWorld BuildKitingWorld()
    {
        var world = new EntityWorld(seed: 911) { WorldWidth = 1200, WorldHeight = 800 };
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());
        world.AddSystem(new MovementSystem());
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

        var kiterSpec = KitingSpec("replay.kiter", UnitWeightClass.Light, 180, WeaponIds.LightRepeater);
        var targetSpec = KitingSpec("replay.chaser", UnitWeightClass.Light, 90, null);
        world.Spawn(kiterSpec, new OwnerId(1), EntityTransform.At(new Vector2(300, 400)), new EntityComponentState[]
        {
            new HealthComponentState(120, 120),
            new MovementComponentState(Vector2.Zero),
            new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2),
            new CollisionComponentState(Radius: 14, Mass: 1, PushPriority: 1, BlocksMovement: true),
            new VisionComponentState(500),
            new StanceComponentState(UnitStance.Hold),
            new WeaponUserComponentState(new[]
            {
                new WeaponMountRuntimeState("main", WeaponIds.LightRepeater, 0, 0),
            }, new EntityId(2), CombatTargetKind.Unit, AttackTargetIsManual: true),
        });
        world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(390, 400)), new EntityComponentState[]
        {
            new HealthComponentState(180, 180),
            new MovementComponentState(Vector2.Zero, new Vector2(300, 400)),
            new MovementProfileComponentState(MaxSpeed: 90, ArriveRadius: 2),
            new CollisionComponentState(Radius: 14, Mass: 1, PushPriority: 1, BlocksMovement: true),
            new VisionComponentState(300),
        });

        return world;
    }

    private static EntitySpec KitingSpec(
        string id,
        UnitWeightClass weight,
        float speed,
        string? weaponId)
    {
        return new EntitySpec
        {
            Id = id,
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec(id, $"{id}.name", $"{id}.role", id.ToUpperInvariant()[..3], IconGlyph.Tank),
            Stats = new StatsSpec(weight, ArmorTag.Vehicle, MaxHp: 180, SightRange: 500, Cost: 100, TechTier: 1),
            Movement = new MovementSpec(MovementDomain.Land, Speed: speed, TurnRate: 8),
            Collision = new CollisionSpec(Radius: 14, Mass: 1, PushPriority: 1),
            Weapons = weaponId is { } kind
                ? [WeaponMountSpec.Independent("main", kind, Vector2.Zero, new Vector2(18, 0), MathF.Tau, 12, fireWhileMoving: true)]
                : [],
        };
    }
}
