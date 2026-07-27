static partial class Program
{
    static void RunUpgradeProgressionScenario()
    {
        var owner = new OwnerId(1);
        var spec = UnitDesignCatalog.Spec("dog.guard_tank");
        var baseSpeed = spec.Movement.Speed;
        var baseSight = spec.Stats.SightRange;
        var baseRange = WeaponCatalog.WeaponDefinitions[spec.PrimaryWeapon.WeaponId].Range;

        var world = new EntityWorld(seed: 5150);
        var attacker = world.SpawnUnit(spec, owner, Vector2.Zero);
        var targetSpec = UnitDesignCatalog.Spec("cat.tank").ToEntitySpec();
        var target = world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(120, 0)), new EntityComponentState[]
        {
            new HealthComponentState(targetSpec.Stats!.MaxHp, targetSpec.Stats.MaxHp),
        });

        var weapon = WeaponCatalog.WeaponDefinitions[spec.PrimaryWeapon.WeaponId];
        var baseDamage = WeaponMath.BaseDamage(world, owner, weapon, target);

        world.Upgrades(owner).Complete(UpgradeIds.FocusedMunitions);
        world.Upgrades(owner).Complete(UpgradeIds.ExtendedBarrels);
        world.Upgrades(owner).Complete(UpgradeIds.OpticArray);
        world.Upgrades(owner).Complete(UpgradeIds.ServoTuning);

        Assert(spec.Movement.Speed == baseSpeed, "UpgradeState must not mutate UnitSpec movement.");
        Assert(spec.Stats.SightRange == baseSight, "UpgradeState must not mutate UnitSpec stats.");
        Assert(WeaponCatalog.WeaponDefinitions[spec.PrimaryWeapon.WeaponId].Range == baseRange, "UpgradeState must not mutate WeaponDefinition.");
        Assert(UpgradeResolver.MoveSpeed(world, attacker, baseSpeed) > baseSpeed, "ServoTuning should derive higher move speed.");
        Assert(UpgradeResolver.SightRange(world, attacker, baseSight) > baseSight, "OpticArray should derive higher sight range.");
        Assert(UpgradeResolver.WeaponRange(world, attacker, baseRange) > baseRange, "ExtendedBarrels should derive higher weapon range.");
        Assert(WeaponMath.BaseDamage(world, owner, weapon, target) > baseDamage, "FocusedMunitions should derive higher damage.");

        AssertVisionAndMovementUseDerivedValues();
        AssertUpgradeStateHash();

        Console.WriteLine("OK [upgrade-progression]: derived damage/range/sight/speed apply without mutating base specs.");
    }

    private static void AssertVisionAndMovementUseDerivedValues()
    {
        var owner = new OwnerId(1);
        var hostile = new OwnerId(2);
        var upgraded = BuildUpgradeReadWorld(withUpgrades: true, owner, hostile);
        var baseline = BuildUpgradeReadWorld(withUpgrades: false, owner, hostile);

        baseline.Step(0, 1, []);
        upgraded.Step(0, 1, []);

        var targetId = new EntityId(2);
        Assert(!baseline.Visibility.IsVisible(owner, targetId), "Baseline sight should not reveal the target.");
        Assert(upgraded.Visibility.IsVisible(owner, targetId), "OpticArray should let VisionSystem reveal the target.");

        var baselineMover = baseline.OrderedEntities.First(entity => entity.Id.Value == 1);
        var upgradedMover = upgraded.OrderedEntities.First(entity => entity.Id.Value == 1);
        Assert(upgradedMover.Transform.Position.X > baselineMover.Transform.Position.X + 10,
            "ServoTuning should make MovementSystem advance farther in the same tick.");
    }

    private static EntityWorld BuildUpgradeReadWorld(bool withUpgrades, OwnerId owner, OwnerId hostile)
    {
        var world = new EntityWorld(seed: 16);
        world.AddSystem(new VisionSystem());
        world.AddSystem(new MovementSystem());
        world.Relations.Set(owner, hostile, PlayerRelation.Hostile);
        if (withUpgrades)
        {
            world.Upgrades(owner).Complete(UpgradeIds.OpticArray);
            world.Upgrades(owner).Complete(UpgradeIds.ServoTuning);
        }

        var moverSpec = new EntitySpec
        {
            Id = "upgrade.mover",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Upgrade Mover", "upgrade.mover.name", "upgrade.mover.role", "UPG", IconGlyph.Infantry),
        };
        world.Spawn(moverSpec, owner, EntityTransform.At(Vector2.Zero), new EntityComponentState[]
        {
            new MovementComponentState(Vector2.Zero, MoveTarget: new Vector2(200, 0)),
            new MovementProfileComponentState(MaxSpeed: 100, ArriveRadius: 1),
            new VisionComponentState(100),
        });

        world.Spawn(moverSpec, hostile, EntityTransform.At(new Vector2(115, 0)), new EntityComponentState[]
        {
            new HealthComponentState(100, 100),
        });
        return world;
    }

    private static void AssertUpgradeStateHash()
    {
        var owner = new OwnerId(1);
        var noUpgrade = new EntityWorld(seed: 7);
        var upgraded = new EntityWorld(seed: 7);
        upgraded.Upgrades(owner).Complete(UpgradeIds.FocusedMunitions);
        Assert(noUpgrade.DeterministicStateHash() != upgraded.DeterministicStateHash(),
            "Completed upgrades must be folded into deterministic state hash.");
    }
}
