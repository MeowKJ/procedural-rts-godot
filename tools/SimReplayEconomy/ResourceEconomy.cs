static partial class Program
{
    static void AssertResourceSystem()
    {
        const int resourceTicks = 260;
        EntityWorld BuildResourceWorld()
        {
            var world = new EntityWorld(seed: 5150);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new ResourceSystem());
            world.AddSystem(new MovementSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 25;

            var harvesterSpec = new EntitySpec
            {
                Id = "replay.harvester",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Harvester", "harvester.name", "harvester.role", "HAR", IconGlyph.Harvester),
            };
            var resourceSpec = new EntitySpec
            {
                Id = "replay.resource",
                Kind = EntityKind.Resource,
                Display = new EntityDisplaySpec("Resource", "resource.name", "resource.role", "RES", IconGlyph.Credits),
            };
            var refinerySpec = ReplayRefinerySpec("replay.refinery");

            world.Spawn(harvesterSpec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), new EntityComponentState[]
            {
                new HarvesterComponentState(),
                new ResourceCargoComponentState(Cargo: 0, Capacity: 50),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2),
            });
            world.Spawn(resourceSpec, OwnerId.None, EntityTransform.At(new Vector2(32, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(
                    Amount: 120,
                    MaxAmount: 120,
                    GatherRateModifier: 1.15f,
                    DepletionBehavior: ResourceDepletionBehavior.DepleteToZero,
                    VisibilityRule: ResourceVisibilityRule.VisibleWhenExplored,
                    CorruptionState: ResourceCorruptionState.Clean),
            });
            world.Spawn(refinerySpec, new OwnerId(1), EntityTransform.At(new Vector2(-160, 0)), new EntityComponentState[]
            {
                new DockComponentState(),
            });

            return world;
        }

        var harvestLog = new List<EntityCommand>
        {
            new HarvestEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, new EntityId(2)),
        };

        AssertDeterministic("resource-loop", BuildResourceWorld, harvestLog, resourceTicks, 40);

        var world = BuildResourceWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in harvestLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= resourceTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var harvester = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var resource = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var refinery = world.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var cargo = harvester.Components.Require<ResourceCargoComponentState>();
        var node = resource.Components.Require<ResourceNodeComponentState>();
        var dock = refinery.Components.Require<DockComponentState>();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;
        var metrics = world.Metrics;

        Assert(credits > 25, $"resource system should bank unloaded Credits, got {credits}");
        Assert(node.Amount < 120, $"resource node should be depleted by gathering, got {node.Amount}");
        Assert(cargo.Cargo >= 0 && cargo.Cargo <= cargo.Capacity, "harvester cargo must stay bounded");
        Assert(dock.ReservedByEntityId is null || dock.ReservedByEntityId == harvester.Id.Value, "dock reservation should remain owned or empty");
        Assert(dock.DockedEntityId is null || dock.DockedEntityId == harvester.Id.Value, "dock occupancy should remain owned or empty");
        Assert(metrics.CreditsBanked == 120, $"economy metrics should record banked credits, got {metrics.CreditsBanked}");
        Assert(metrics.CreditsPerMinute > 0, $"economy metrics should expose credits/minute, got {metrics.CreditsPerMinute:0.00}");
        Assert(metrics.ResourceTripCompletions >= 1, $"economy metrics should record completed trips, got {metrics.ResourceTripCompletions}");
        Assert(metrics.AverageResourceTripSeconds > 0, "economy metrics should expose average resource trip time");
        Assert(metrics.HarvesterActiveTripSeconds > 0, "economy metrics should record active harvester trip time");
        Assert(metrics.HarvesterIdleSeconds > 0, "economy metrics should record idle harvester time after depletion");

        AssertDockCongestionMetrics();
        AssertRotatedRefineryDocks();
        AssertHarvesterRetreatsUnderFire();
        AssertEconomyTuningChangesThroughput(BuildResourceWorld, harvestLog, resourceTicks);

        Console.WriteLine($"OK [resource-loop]: credits {credits}, node amount {node.Amount}, cargo {cargo.Cargo}.");
    }

    static void AssertEconomyTuningChangesThroughput(
        Func<EntityWorld> buildResourceWorld,
        IReadOnlyList<EntityCommand> harvestLog,
        int resourceTicks)
    {
        var tunedTicks = Math.Min(resourceTicks, 120);
        var slow = RunTunedEconomy(buildResourceWorld, harvestLog, tunedTicks, new EconomyTuningConfig(
            GatherDistance: 24f,
            DockDistance: 30f,
            GatherRate: 35f,
            UnloadRate: 35f));
        var fast = RunTunedEconomy(buildResourceWorld, harvestLog, tunedTicks, new EconomyTuningConfig(
            GatherDistance: 24f,
            DockDistance: 30f,
            GatherRate: 220f,
            UnloadRate: 440f));

        Assert(fast.Credits > slow.Credits, $"economy tuning should change banked credits, slow {slow.Credits}, fast {fast.Credits}");
        Assert(fast.CreditsPerMinute > slow.CreditsPerMinute, $"economy tuning should change credits/minute, slow {slow.CreditsPerMinute:0.00}, fast {fast.CreditsPerMinute:0.00}");
        Assert(fast.Hash != slow.Hash, "economy tuning must be folded into deterministic state hash");
    }

    static (int Credits, double CreditsPerMinute, ulong Hash) RunTunedEconomy(
        Func<EntityWorld> buildResourceWorld,
        IReadOnlyList<EntityCommand> harvestLog,
        int resourceTicks,
        EconomyTuningConfig tuning)
    {
        var world = buildResourceWorld();
        world.EconomyTuning = tuning;
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in harvestLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= resourceTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        return (
            world.ResourceInventory(new OwnerId(1)).Credits,
            world.Metrics.CreditsPerMinute,
            world.DeterministicStateHash());
    }
    static void AssertDockCongestionMetrics()
    {
        var world = new EntityWorld(seed: 5151);
        world.AddSystem(new ResourceSystem());

        var harvesterSpec = new EntitySpec
        {
            Id = "replay.waiting_harvester",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Harvester", "harvester.name", "harvester.role", "HAR", IconGlyph.Harvester),
        };
        var refinerySpec = ReplayRefinerySpec("replay.waiting_refinery");

        world.Spawn(harvesterSpec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), new EntityComponentState[]
        {
            new HarvesterComponentState(HarvesterMode.ReturningToRefinery),
            new ResourceCargoComponentState(Cargo: 30, Capacity: 30),
            new MovementComponentState(Vector2.Zero),
        });
        world.Spawn(harvesterSpec, new OwnerId(1), EntityTransform.At(new Vector2(4, 0)), new EntityComponentState[]
        {
            new HarvesterComponentState(HarvesterMode.ReturningToRefinery),
            new ResourceCargoComponentState(Cargo: 30, Capacity: 30),
            new MovementComponentState(Vector2.Zero),
        });
        world.Spawn(refinerySpec, new OwnerId(1), EntityTransform.At(new Vector2(-128, 0)), new EntityComponentState[]
        {
            new DockComponentState(),
        });

        var clock = new SimClock();
        for (var tick = 1; tick <= 20; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        }

        Assert(world.Metrics.DockWaitSeconds > 0, "economy metrics should record dock wait time under congestion");
        Assert(world.Metrics.RefineryCongestionEvents >= 1, $"economy metrics should count refinery congestion events, got {world.Metrics.RefineryCongestionEvents}");
        Assert(world.Metrics.CreditsBanked == 60, $"both waiting harvesters should eventually unload 60 credits, got {world.Metrics.CreditsBanked}");
    }

    static void AssertRotatedRefineryDocks()
    {
        var facings = new[] { 0f, Mathf.Pi * 0.5f, Mathf.Pi, Mathf.Pi * 1.5f };
        var directions = new[] { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up };
        for (var rotation = 0; rotation < facings.Length; rotation++)
        {
            var world = new EntityWorld(seed: (ulong)(5160 + rotation));
            world.AddSystem(new ResourceSystem());
            var center = new Vector2(500, 500);
            var expectedDock = center + directions[rotation] * 128;
            var harvesterSpec = new EntitySpec
            {
                Id = $"replay.rotated_harvester.{rotation}",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Harvester", "harvester.name", "harvester.role", "HAR", IconGlyph.Harvester),
            };
            var harvester = world.Spawn(
                harvesterSpec,
                new OwnerId(1),
                EntityTransform.At(expectedDock + directions[rotation] * 64),
                new EntityComponentState[]
                {
                    new HarvesterComponentState(HarvesterMode.ReturningToRefinery),
                    new ResourceCargoComponentState(Cargo: 20, Capacity: 20),
                    new MovementComponentState(Vector2.Zero),
                });
            var refinery = world.Spawn(
                ReplayRefinerySpec($"replay.rotated_refinery.{rotation}"),
                new OwnerId(1),
                EntityTransform.At(center, facings[rotation]),
                new EntityComponentState[]
                {
                    new DockComponentState(),
                });

            var clock = new SimClock();
            world.Step(1, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            var returning = harvester.Components.Require<HarvesterComponentState>();
            var movement = harvester.Components.Require<MovementComponentState>();
            Assert(returning.RefineryId == refinery.Id.Value && returning.Mode == HarvesterMode.ReturningToRefinery,
                $"rotation {rotation} should reserve its refinery dock while approaching");
            Assert(movement.MoveTarget is { } target && target.DistanceTo(expectedDock) < 0.001f,
                $"rotation {rotation} return target should be exact dock {expectedDock}, got {movement.MoveTarget}");

            harvester.Transform = harvester.Transform with { Position = expectedDock };
            world.Step(2, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            Assert(harvester.Components.Require<HarvesterComponentState>().Mode == HarvesterMode.Unloading,
                $"rotation {rotation} should arrive and unload at the shared dock point");
            for (var tick = 3; tick <= 20; tick++)
            {
                world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            }

            Assert(world.ResourceInventory(new OwnerId(1)).Credits == 20,
                $"rotation {rotation} should bank all cargo through the shared dock point");
        }
    }

    static void AssertHarvesterRetreatsUnderFire()
    {
        var world = new EntityWorld(seed: 5152);
        world.AddSystem(new CommandSystem());
        world.AddSystem(new ResourceSystem());
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());
        world.AddSystem(new MovementSystem());

        var harvesterSpec = new EntitySpec
        {
            Id = "replay.retreat_harvester",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Harvester", "harvester.name", "harvester.role", "HAR", IconGlyph.Harvester),
            Stats = new StatsSpec(UnitWeightClass.Heavy, ArmorTag.Vehicle, MaxHp: 140, SightRange: 260, Cost: 500, TechTier: 1),
            Movement = new MovementSpec(MovementDomain.Land, Speed: 180, TurnRate: 7),
            Collision = new CollisionSpec(Radius: 16, Mass: 1.5f, PushPriority: 2),
        };
        var resourceSpec = new EntitySpec
        {
            Id = "replay.retreat_resource",
            Kind = EntityKind.Resource,
            Display = new EntityDisplaySpec("Resource", "resource.name", "resource.role", "RES", IconGlyph.Credits),
        };
        var refinerySpec = ReplayRefinerySpec("replay.retreat_refinery");

        var harvester = world.Spawn(harvesterSpec, new OwnerId(1), EntityTransform.At(new Vector2(80, 0)), new EntityComponentState[]
        {
            new HealthComponentState(140, 140),
            new HarvesterComponentState(HarvesterMode.Gathering, FieldId: 2),
            new ResourceCargoComponentState(Cargo: 0, Capacity: 50),
            new MovementComponentState(Vector2.Zero),
            new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2),
            new CollisionComponentState(Radius: 16, Mass: 1.5f, PushPriority: 2, BlocksMovement: true),
        });
        world.Spawn(resourceSpec, OwnerId.None, EntityTransform.At(new Vector2(80, 0)), new EntityComponentState[]
        {
            new ResourceNodeComponentState(
                Amount: 120,
                MaxAmount: 120,
                GatherRateModifier: 1,
                DepletionBehavior: ResourceDepletionBehavior.DepleteToZero,
                VisibilityRule: ResourceVisibilityRule.VisibleWhenExplored,
                CorruptionState: ResourceCorruptionState.Clean),
        });
        world.Spawn(refinerySpec, new OwnerId(1), EntityTransform.At(new Vector2(-128, 0)), new EntityComponentState[]
        {
            new DockComponentState(),
            new CollisionComponentState(Radius: 20, Mass: 10, PushPriority: 5, BlocksMovement: true),
        });
        SpawnSoldier(world, CombatSpec(), new OwnerId(2), new Vector2(110, 0));

        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        buffer.Enqueue(new AttackEntityCommand(new OwnerId(2), new[] { new EntityId(4) }, 1, harvester.Id, CombatTargetKind.Unit));

        for (var tick = 1; tick <= 110; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var state = harvester.Components.Require<HarvesterComponentState>();
        var health = harvester.Components.Require<HealthComponentState>();
        Assert(health.Hp < health.MaxHp, "hostile attack should damage the active harvester.");
        Assert(state.Mode == HarvesterMode.Idle, $"under-fire harvester should retreat and idle near refinery, got {state.Mode}.");
        Assert(!state.Retreating, "retreat marker should clear once the harvester reaches safety.");
        Assert(state.FieldId is null, "retreated harvester should stop the exposed field assignment.");
        Assert(harvester.Transform.Position.DistanceTo(Vector2.Zero) <= world.EconomyTuning.DockDistance,
            $"retreated harvester should move back to the refinery dock, pos {harvester.Transform.Position}.");
        Console.WriteLine($"OK [harvester-retreat]: hp {health.Hp:0.0}, pos {harvester.Transform.Position}, mode {state.Mode}.");
    }

    static EntitySpec ReplayRefinerySpec(string id)
    {
        return BuildSpecCatalog.For(BuildingDesignIds.Refinery).ToEntitySpec() with { Id = id };
    }
}
