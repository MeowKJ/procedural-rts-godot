static partial class Program
{
    static void RunVeterancyProgressionScenario()
    {
        const int Ticks = 96;
        AssertDeterministic("veterancy-progression", BuildVeterancyWorld, Ticks, 12);

        var world = BuildVeterancyWorld();
        var clock = new SimClock();
        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            world.Events.Drain();
        }

        var veteran = world.OrderedEntities.Single(entity => entity.SpecId == "veterancy.attacker");
        var veterancy = veteran.Components.Require<VeterancyComponentState>();
        var health = veteran.Components.Require<HealthComponentState>();
        var projection = EntityProjector.ProjectOne(world, veteran);
        var weapon = WeaponCatalog.Weapons[WeaponKind.VectorCannon];
        var baseDamage = WeaponMath.BaseDamage(new EntityWorld(seed: 90), veteran.OwnerId, weapon, veteran);
        var rankedDamage = WeaponMath.BaseDamage(world, veteran, weapon, veteran);

        Assert(veterancy.Kills >= 2, $"veterancy should count combat kills, got {veterancy.Kills}.");
        Assert(veterancy.Rank == 3, $"valuable kills should promote to rank 3, got rank {veterancy.Rank}.");
        Assert(rankedDamage > baseDamage, "veterancy rank should derive higher damage without editing weapon data.");
        Assert(health.MaxHp > 100, $"veterancy rank should derive higher max hp, got {health.MaxHp:0.0}.");
        Assert(projection.VeterancyRank == veterancy.Rank && projection.VeterancyKills == veterancy.Kills,
            "EntityProjection should expose owner-neutral veterancy rank and kill count.");
        Assert(WeaponCatalog.Weapons[WeaponKind.VectorCannon].Range == weapon.Range,
            "veterancy must not mutate WeaponDefinition.");

        Console.WriteLine($"OK [veterancy-progression]: kills {veterancy.Kills}, rank {veterancy.Rank}, max hp {health.MaxHp:0.0}, damage {rankedDamage:0.0}.");
    }

    private static EntityWorld BuildVeterancyWorld()
    {
        var world = new EntityWorld(seed: 90) { WorldWidth = 700, WorldHeight = 420 };
        var player = new OwnerId(1);
        var enemy = new OwnerId(2);
        world.Relations.Set(player, enemy, PlayerRelation.Hostile);
        world.AddSystem(new VisionSystem());
        world.AddSystem(new CombatSystem());

        var attacker = world.Spawn(VeterancyAttackerSpec(), player, EntityTransform.At(new Vector2(80, 200)), new EntityComponentState[]
        {
            new HealthComponentState(100, 100),
            new CollisionComponentState(18, 1, 1, true),
            new VisionComponentState(420),
            new WeaponUserComponentState([new WeaponMountRuntimeState("main", WeaponKind.VectorCannon, 0, 0)]),
            new VeterancyComponentState(),
            new StanceComponentState(UnitStance.Aggressive),
            new AutonomyComponentState(420, 520, new Vector2(80, 200)),
        });

        SpawnVeterancyTarget(world, enemy, new Vector2(210, 186));
        SpawnVeterancyTarget(world, enemy, new Vector2(230, 214));
        attacker.Components.Set(attacker.Components.Require<WeaponUserComponentState>() with
        {
            AttackTarget = new EntityId(2),
            AttackTargetIsManual = true,
        });
        return world;
    }

    private static EntitySpec VeterancyAttackerSpec()
    {
        return new EntitySpec
        {
            Id = "veterancy.attacker",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Veterancy Attacker", "veterancy.attacker.name", "veterancy.attacker.role", "VET", IconGlyph.Tank),
            Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, 100, 420, 0, 1),
            Movement = new MovementSpec(MovementDomain.Land, 0, 0),
            Collision = new CollisionSpec(18, 1, 1),
        };
    }

    private static EntitySpec VeterancyTargetSpec()
    {
        return new EntitySpec
        {
            Id = "veterancy.target",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Valuable Target", "veterancy.target.name", "veterancy.target.role", "VTT", IconGlyph.Tank),
            Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, 8, 120, 1200, 1),
            Movement = new MovementSpec(MovementDomain.Land, 0, 0),
            Collision = new CollisionSpec(14, 1, 1),
        };
    }

    private static void SpawnVeterancyTarget(EntityWorld world, OwnerId owner, Vector2 position)
    {
        world.Spawn(VeterancyTargetSpec(), owner, EntityTransform.At(position), new EntityComponentState[]
        {
            new HealthComponentState(8, 8),
            new CollisionComponentState(14, 1, 1, true),
        });
    }
}
