static partial class Program
{
    static void RunDerivedRegenerationScenario()
    {
        const int Ticks = 30;
        AssertDeterministic("derived-regeneration", BuildDerivedRegenerationWorld, Ticks, 10);

        var world = BuildDerivedRegenerationWorld();
        var clock = new SimClock();
        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            world.Events.Drain();
        }

        var staticHp = Hp(world, 1);
        var baseHp = Hp(world, 2);
        var upgradedHp = Hp(world, 3);
        var veteranHp = Hp(world, 4);
        var capped = world.OrderedEntities.Single(entity => entity.Id.Value == 5)
            .Components.Require<HealthComponentState>();

        Assert(Mathf.IsEqualApprox(staticHp, 50), $"entities without regeneration should not self-repair, got {staticHp:0.0}.");
        Assert(baseHp > staticHp, $"base regeneration should repair damage, got {baseHp:0.0}.");
        Assert(upgradedHp > baseHp, $"FieldRepairs should derive faster regeneration, base {baseHp:0.0}, upgraded {upgradedHp:0.0}.");
        Assert(veteranHp > upgradedHp, $"veterancy should derive faster regeneration, upgraded {upgradedHp:0.0}, veteran {veteranHp:0.0}.");
        Assert(capped.Hp <= capped.MaxHp && Mathf.IsEqualApprox(capped.Hp, capped.MaxHp),
            $"regeneration should cap at max hp, got {capped.Hp:0.0}/{capped.MaxHp:0.0}.");

        Console.WriteLine($"OK [derived-regeneration]: static {staticHp:0.0}, base {baseHp:0.0}, upgraded {upgradedHp:0.0}, veteran {veteranHp:0.0}, capped {capped.Hp:0.0}.");
    }

    private static EntityWorld BuildDerivedRegenerationWorld()
    {
        var world = new EntityWorld(seed: 314) { WorldWidth = 600, WorldHeight = 400 };
        world.AddSystem(new RegenerationSystem());
        world.Upgrades(new OwnerId(2)).Complete(UpgradeIds.FieldRepairs);
        world.Upgrades(new OwnerId(3)).Complete(UpgradeIds.FieldRepairs);

        SpawnRegenProbe(world, new OwnerId(1), 50, hasRegen: false, x: 80);
        SpawnRegenProbe(world, new OwnerId(1), 50, hasRegen: true, x: 150);
        SpawnRegenProbe(world, new OwnerId(2), 50, hasRegen: true, x: 220);
        SpawnRegenProbe(world, new OwnerId(3), 50, hasRegen: true, x: 290, rank: 3);
        SpawnRegenProbe(world, new OwnerId(3), 99, hasRegen: true, x: 360, rank: 3);
        return world;
    }

    private static void SpawnRegenProbe(
        EntityWorld world,
        OwnerId owner,
        float hp,
        bool hasRegen,
        float x,
        int rank = 0)
    {
        var components = new List<EntityComponentState>
        {
            new HealthComponentState(hp, 100),
            new CollisionComponentState(10, 1, 1, true),
        };
        if (hasRegen)
        {
            components.Add(new RegenerationComponentState(HpPerSecond: 12));
        }

        if (rank > 0)
        {
            components.Add(new VeterancyComponentState(Kills: rank, Experience: 12, Rank: rank));
        }

        world.Spawn(RegenProbeSpec(), owner, EntityTransform.At(new Vector2(x, 200)), components);
    }

    private static EntitySpec RegenProbeSpec()
    {
        return new EntitySpec
        {
            Id = "regen.probe",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Regen Probe", "regen.probe.name", "regen.probe.role", "REG", IconGlyph.Settings),
            Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, 100, 300, 0, 1),
            Movement = new MovementSpec(MovementDomain.Land, 0, 0),
            Collision = new CollisionSpec(10, 1, 1),
        };
    }

    private static float Hp(EntityWorld world, int entityId)
    {
        return world.OrderedEntities.Single(entity => entity.Id.Value == entityId)
            .Components.Require<HealthComponentState>().Hp;
    }
}
