static partial class Program
{
    static void AssertM5TurretEntityProjection()
    {
        const int ticks = 40;

        BuildingEntitySeed BuildingSeed(string kind, int id, Vector2 position)
        {
            return new BuildingEntitySeed(
                id,
                kind,
                PlayerSlotId.One,
                UnitFactionId.Dog,
                position,
                0,
                BuildSpecCatalog.For(kind).MaxHp);
        }

        foreach (var (kind, index) in new[] { BuildingDesignIds.GroundTurret, BuildingDesignIds.AntiAirTurret }.Select((kind, index) => (kind, index)))
        {
            var spec = BuildSpecCatalog.For(kind);
            var entitySpec = spec.ToEntitySpec();
            var components = BuildingSeed(kind, index + 100, Vector2.Zero)
                .ToEntityComponents(spec);
            var weapon = components.OfType<WeaponUserComponentState>().SingleOrDefault();

            Assert(entitySpec.Kind == EntityKind.Turret, "armed fixed defenses should enter EntityWorld as EntityKind.Turret");
            Assert(entitySpec.Weapons.Count == 1, "turret guns should be WeaponMountSpec entries on the turret entity");
            Assert(entitySpec.Weapons[0].WeaponId == spec.WeaponId, "turret WeaponMountSpec should preserve the BuildSpec weapon kind");
            Assert(entitySpec.Tags.Contains("Turret") && entitySpec.Tags.Contains("Weapon"), "turret entity specs should carry turret/weapon tags");
            Assert(weapon is not null && weapon.Mounts.Count == 1, "armed fixed defense components should include one WeaponUser mount");
            Assert(weapon!.Mounts[0].WeaponId == spec.WeaponId, "turret runtime weapon mount should preserve the BuildSpec weapon kind");
        }

        foreach (var (kind, index) in new[]
        {
            BuildingDesignIds.PowerPlant,
            BuildingDesignIds.Barracks,
            BuildingDesignIds.VehicleFactory,
            BuildingDesignIds.Refinery,
            BuildingDesignIds.Airfield,
        }.Select((kind, index) => (kind, index)))
        {
            var spec = BuildSpecCatalog.For(kind);
            var entitySpec = spec.ToEntitySpec();
            var components = BuildingSeed(kind, index + 200, Vector2.Zero)
                .ToEntityComponents(spec);

            Assert(spec.WeaponId is null, $"ordinary producer/resource building {kind} should not author a BuildSpec weapon");
            Assert(entitySpec.Kind == EntityKind.Building, $"ordinary producer/resource building {kind} should remain EntityKind.Building");
            Assert(entitySpec.Weapons.Count == 0, $"ordinary producer/resource building {kind} should not gain WeaponMountSpec entries");
            Assert(!entitySpec.Tags.Contains("Turret") && !entitySpec.Tags.Contains("Weapon"), $"ordinary producer/resource building {kind} should not gain turret tags");
            Assert(!components.OfType<WeaponUserComponentState>().Any(), $"ordinary producer/resource building {kind} should not gain WeaponUserComponentState from BuildingSpecId");
        }

        EntitySpec TargetSpec()
        {
            return new EntitySpec
            {
                Id = "replay.m5_turret_target",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Turret Target", "target.name", "target.role", "TGT", IconGlyph.Infantry),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 500, SightRange: 200, Cost: 50, TechTier: 1),
                Collision = new CollisionSpec(Radius: 14, Mass: 1, PushPriority: 1),
            };
        }

        EntitySpec FakeArmedBuildingSpec()
        {
            return new EntitySpec
            {
                Id = "replay.m5_fake_armed_building",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Fake Armed Building", "building.fake.name", "building.fake.role", "FAB", IconGlyph.Building),
                Stats = new StatsSpec(UnitWeightClass.Heavy, ArmorTag.Structure, MaxHp: 500, SightRange: 500, Cost: 100, TechTier: 1),
                Weapons =
                [
                    WeaponMountSpec.Omni("main", WeaponIds.VectorCannon, Vector2.Zero, fireWhileMoving: false),
                ],
            };
        }

        EntityWorld BuildM5TurretEntityWorld()
        {
            var world = new EntityWorld(seed: 6464);
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var groundTurret = BuildingSeed(BuildingDesignIds.GroundTurret, 1, new Vector2(0, 0));
            world.SpawnBuildingTarget(
                groundTurret,
                BuildSpecCatalog.For(BuildingDesignIds.GroundTurret),
                powered: true);

            world.Spawn(FakeArmedBuildingSpec(), new OwnerId(1), EntityTransform.At(new Vector2(0, 120)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 0, Powered: true),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.VectorCannon, 0, 0),
                }),
            });

            world.Spawn(TargetSpec(), new OwnerId(2), EntityTransform.At(new Vector2(240, 0)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new CollisionComponentState(14, 1, 1, BlocksMovement: true),
            });

            return world;
        }

        AssertDeterministic("m5-turret-entities", BuildM5TurretEntityWorld, ticks, 10);

        var world = BuildM5TurretEntityWorld();
        var clock = new SimClock();
        var turretShots = 0;
        var fakeBuildingShots = 0;
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            foreach (var fired in world.Events.Drain().OfType<WeaponFiredEvent>())
            {
                if (fired.Source.Value == 1)
                {
                    turretShots++;
                }
                else if (fired.Source.Value == 2)
                {
                    fakeBuildingShots++;
                }
            }
        }

        var turretSpec = world.StableSpecs.Single(spec => spec.Id == "building.groundturret");
        var fakeBuilding = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var targetHp = world.OrderedEntities.Single(entity => entity.Id.Value == 3)
            .Components.Require<HealthComponentState>().Hp;

        Assert(turretSpec.Kind == EntityKind.Turret, "BuildSpec-spawned ground turret should register as EntityKind.Turret");
        Assert(turretShots > 0, "EntityKind.Turret fixed defense should fire through CombatSystem");
        Assert(fakeBuildingShots > 0, "Any armed entity should fire through the single CombatSystem");
        Assert(targetHp < 500, $"turret entity should damage hostile target, got hp {targetHp}");

        Console.WriteLine($"OK [m5-armed-entities]: turret shots {turretShots}, armed building shots {fakeBuildingShots}, target hp {targetHp}.");
    }

    static void AssertPowerConsequences()
    {
        const int ticks = 90;

        EntitySpec PowerPlantSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Power Plant", "power.name", "power.role", "PWR", IconGlyph.Building),
            };
        }

        EntitySpec TurretSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Turret,
                Display = new EntityDisplaySpec("Ion Turret", "turret.name", "turret.role", "TRT", IconGlyph.Turret),
                Stats = new StatsSpec(UnitWeightClass.Heavy, ArmorTag.Structure, MaxHp: 300, SightRange: 500, Cost: 160, TechTier: 1),
                Weapons =
                [
                    WeaponMountSpec.Omni("main", WeaponIds.IonEmitter, Vector2.Zero, fireWhileMoving: false),
                ],
            };
        }

        EntitySpec TargetSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Target", "target.name", "target.role", "TGT", IconGlyph.Infantry),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 500, SightRange: 200, Cost: 50, TechTier: 1),
            };
        }

        EntitySpec ProducerSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Barracks", "barracks.name", "barracks.role", "BAR", IconGlyph.Building),
                Authoring = new EntityAuthoringMetadata(BuildingSpecId: BuildingDesignIds.Barracks, TechTier: 1),
            };
        }

        EntityWorld BuildPowerWorld(bool enoughPower)
        {
            var world = new EntityWorld(seed: enoughPower ? 7171UL : 7172UL);
            world.AddSystem(new PowerSystem());
            world.AddSystem(new ProductionSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
            world.ResourceInventory(new OwnerId(1)).Credits = 200;

            world.Spawn(PowerPlantSpec("replay.power"), new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: enoughPower ? 4 : 2, Used: 0, Powered: true),
            });
            world.Spawn(TurretSpec("replay.powered_turret"), new OwnerId(1), EntityTransform.At(new Vector2(100, 0)), new EntityComponentState[]
            {
                new HealthComponentState(300, 300),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: true),
                new VisionComponentState(500),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.IonEmitter, 0, 0),
                }),
            });
            world.Spawn(ProducerSpec("replay.powered_barracks"), new OwnerId(1), EntityTransform.At(new Vector2(100, 120)), new EntityComponentState[]
            {
                new HealthComponentState(700, 700),
                new FootprintComponentState(new Vector2(96, 86)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: true),
                new ProductionQueueComponentState(new[]
                {
                    new UnitProductionQueueItem
                    {
                        Id = 1,
                        DesignId = "dog.infantry",
                        Faction = UnitFactionId.Dog,
                        Progress = 0,
                    },
                }),
            });
            world.Spawn(TargetSpec("replay.power_target"), new OwnerId(2), EntityTransform.At(new Vector2(240, 0)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new CollisionComponentState(14, 1, 1, BlocksMovement: true),
            });

            return world;
        }

        AssertDeterministic("power-consequences", () => BuildPowerWorld(enoughPower: true), ticks, 15);
        AssertDeterministic("power-consequences-low", () => BuildPowerWorld(enoughPower: false), ticks, 15);

        var enough = BuildPowerWorld(enoughPower: true);
        var low = BuildPowerWorld(enoughPower: false);
        var enoughClock = new SimClock();
        var lowClock = new SimClock();
        var enoughShots = 0;
        var lowShots = 0;
        for (var tick = 1; tick <= ticks; tick++)
        {
            enough.Step(tick, enoughClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            low.Step(tick, lowClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            enoughShots += enough.Events.Drain().Count(evt => evt is WeaponFiredEvent);
            lowShots += low.Events.Drain().Count(evt => evt is WeaponFiredEvent);
        }

        var enoughTurretPower = enough.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<PowerComponentState>();
        var lowTurretPower = low.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<PowerComponentState>();
        var enoughProducerQueue = enough.OrderedEntities.Single(entity => entity.Id.Value == 3).Components.Require<ProductionQueueComponentState>();
        var lowProducerQueue = low.OrderedEntities.Single(entity => entity.Id.Value == 3).Components.Require<ProductionQueueComponentState>();
        var enoughTargetHp = enough.OrderedEntities.Single(entity => entity.Id.Value == 4).Components.Require<HealthComponentState>().Hp;
        var lowTargetHp = low.OrderedEntities.Single(entity => entity.Id.Value == 4).Components.Require<HealthComponentState>().Hp;

        Assert(enoughTurretPower.Powered, "sufficient owner power should keep turret powered");
        Assert(!lowTurretPower.Powered, "under-powered owner should switch turret offline");
        Assert(enoughShots > 0, "powered turret should fire");
        Assert(lowShots == 0, $"unpowered turret should not fire, got {lowShots} shots");
        Assert(enoughTargetHp < 500, "powered turret should damage the hostile target");
        Assert(Math.Abs(lowTargetHp - 500) < 0.001f, $"unpowered turret should leave target undamaged, got hp {lowTargetHp}");
        Assert(enoughProducerQueue.PauseReason == ProductionPauseReason.None, "powered producer should not be paused by power");
        Assert(lowProducerQueue.PauseReason == ProductionPauseReason.Unpowered, "under-powered producer should report unpowered pause");
        Assert(lowProducerQueue.Items.Count == 1 && Math.Abs(lowProducerQueue.Items[0].Progress) < 0.0001f, "under-powered production should not advance");

        Console.WriteLine($"OK [power-consequences]: powered shots {enoughShots}, offline shots {lowShots}, low-power production paused.");
    }
}
