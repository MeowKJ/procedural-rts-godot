static partial class Program
{
    static void AssertProductionSystem()
    {
        const int productionTicks = 380;

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

        EntityWorld BuildProductionWorld()
        {
            var world = new EntityWorld(seed: 6161) { WorldWidth = 1800, WorldHeight = 1200 };
            world.AddSystem(new ProductionSystem());
            world.AddSystem(new MovementSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 620;

            var producerSpec = ProducerSpec("replay.barracks");
            world.Spawn(producerSpec, new OwnerId(1), EntityTransform.At(new Vector2(360, 500)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new FootprintComponentState(new Vector2(96, 86)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: true),
                new RallyPointComponentState(new Vector2(760, 500)),
                new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>()),
                new CollisionComponentState(54, 8, 100, BlocksMovement: true),
            });
            world.Spawn(producerSpec, new OwnerId(1), EntityTransform.At(new Vector2(360, 700)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new FootprintComponentState(new Vector2(96, 86)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: true),
                new RallyPointComponentState(new Vector2(760, 700)),
                new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>()),
                new CollisionComponentState(54, 8, 100, BlocksMovement: true),
            });
            world.Spawn(producerSpec, new OwnerId(1), EntityTransform.At(new Vector2(360, 900)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new FootprintComponentState(new Vector2(96, 86)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: false),
                new RallyPointComponentState(new Vector2(760, 900)),
                new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>()),
                new CollisionComponentState(54, 8, 100, BlocksMovement: true),
            });
            world.Spawn(producerSpec, new OwnerId(1), EntityTransform.At(new Vector2(1080, 900)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new FootprintComponentState(new Vector2(96, 86)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: true),
                new RallyPointComponentState(new Vector2(1320, 900)),
                new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>()),
                new CollisionComponentState(54, 8, 100, BlocksMovement: true),
            });

            return world;
        }

        var productionLog = new List<EntityCommand>
        {
            new ProduceEntityCommand(new OwnerId(1), new[] { new EntityId(1), new EntityId(2), new EntityId(3), new EntityId(4) }, 1, "dog.infantry"),
            new CancelProductionEntityCommand(new OwnerId(1), new[] { new EntityId(4) }, 2),
        };

        AssertDeterministic("production-loop", BuildProductionWorld, productionLog, productionTicks, 40);

        var world = BuildProductionWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in productionLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= productionTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var producedUnits = world.OrderedEntities.Where(entity => entity.SpecId == "dog.infantry").ToList();
        var poweredProducerQueues = world.OrderedEntities
            .Where(entity => entity.Id.Value is 1 or 2)
            .Select(entity => entity.Components.Require<ProductionQueueComponentState>())
            .ToArray();
        var unpoweredQueue = world.OrderedEntities
            .Single(entity => entity.Id.Value == 3)
            .Components.Require<ProductionQueueComponentState>();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;

        Assert(producedUnits.Count == 2, $"two powered producers should spawn two units, got {producedUnits.Count}");
        Assert(poweredProducerQueues.All(queue => queue.Items.Count == 0), "powered producer queues should complete independently");
        var cancelledQueue = world.OrderedEntities
            .Single(entity => entity.Id.Value == 4)
            .Components.Require<ProductionQueueComponentState>();

        Assert(unpoweredQueue.Items.Count == 1, "unpowered producer should keep its queued item");
        Assert(unpoweredQueue.Items is List<UnitProductionQueueItem>, "unpowered producer queue should use reusable queue storage after enqueue");
        Assert(poweredProducerQueues.All(queue => queue.Items is List<UnitProductionQueueItem>), "completed producer queues should keep reusable queue storage");
        Assert(Math.Abs(unpoweredQueue.Items[0].Progress) < 0.0001f, "unpowered producer queue should not advance");
        Assert(unpoweredQueue.PauseReason == ProductionPauseReason.Unpowered, $"unpowered producer should report pause reason, got {unpoweredQueue.PauseReason}");
        Assert(cancelledQueue.Items.Count == 0, "cancelled producer queue should be empty after refund");
        Assert(cancelledQueue.Items is List<UnitProductionQueueItem>, "cancelled producer queue should keep reusable queue storage after removal");
        Assert(cancelledQueue.PauseReason == ProductionPauseReason.None, "cancelled empty queue should clear pause reason");
        Assert(credits == 200, $"four queued infantry minus one half refund should leave 200 credits, got {credits}");
        Assert(producedUnits.All(unit =>
        {
            var expectedY = unit.Transform.Position.Y < 600 ? 500 : 700;
            var rally = new Vector2(760, expectedY);
            return (unit.Components.TryGet<CommandableComponentState>(out var commandable)
                    && commandable.PlayerIntentTarget == rally)
                || unit.Transform.Position.DistanceTo(rally) < 8f;
        }), "produced units should receive or reach producer rally points");

        AssertFixedProductionEgress();

        Console.WriteLine($"OK [production-loop]: produced {producedUnits.Count}, paused queue {unpoweredQueue.Items.Count}, cancelled queue {cancelledQueue.Items.Count}, credits {credits}.");
    }

    static void AssertFixedProductionEgress()
    {
        var cases = new[]
        {
            (BuildingKind: BuildingDesignIds.Barracks, UnitDesignId: "dog.infantry", Facing: 0f),
            (BuildingKind: BuildingDesignIds.VehicleFactory, UnitDesignId: "dog.guard_tank", Facing: Mathf.Pi * 0.5f),
            (BuildingKind: BuildingDesignIds.Airfield, UnitDesignId: "dog.sky_patrol_aircraft", Facing: Mathf.Pi),
        };
        for (var caseIndex = 0; caseIndex < cases.Length; caseIndex++)
        {
            var fixture = cases[caseIndex];
            var world = new EntityWorld(seed: (ulong)(6170 + caseIndex)) { WorldWidth = 1600, WorldHeight = 1200 };
            world.AddSystem(new ProductionSystem());
            var buildingSpec = BuildSpecCatalog.For(fixture.BuildingKind);
            var unitSpec = UnitDesignCatalog.Spec(fixture.UnitDesignId);
            Assert(buildingSpec.ToEntitySpec().Tags.Contains("Producer"),
                $"{fixture.BuildingKind} should be classified as a producer");
            var producer = world.Spawn(
                buildingSpec.ToEntitySpec(),
                new OwnerId(1),
                EntityTransform.At(new Vector2(500, 500), fixture.Facing),
                new EntityComponentState[]
                {
                    new HealthComponentState(buildingSpec.MaxHp, buildingSpec.MaxHp),
                    new ConstructionComponentState(Progress: 1),
                    new PowerComponentState(0, buildingSpec.PowerUsed, Powered: true),
                    new ProductionQueueComponentState(new List<UnitProductionQueueItem>
                    {
                        new()
                        {
                            Id = 1,
                            Kind = ProductionKindDesignBridge.ProductionKindFor(unitSpec),
                            DesignId = unitSpec.Id,
                            Faction = unitSpec.Faction,
                            Progress = unitSpec.Production!.Duration,
                        },
                    }),
                });

            var clock = new SimClock();
            world.Step(1, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            var produced = world.OrderedEntities.Single(entity => entity.SpecId == fixture.UnitDesignId);
            Assert(PlacementReservationMath.TryCenter(
                    buildingSpec,
                    PlacementReservationKind.ProductionEgress,
                    producer.Transform.Position,
                    producer.Transform.Facing,
                    out var expected),
                $"{fixture.BuildingKind} should resolve a cardinal production egress");
            Assert(produced.Transform.Position.DistanceTo(expected) < 0.001f,
                $"{fixture.BuildingKind} should spawn {fixture.UnitDesignId} at exact egress {expected}, got {produced.Transform.Position}");
            Assert(producer.Components.Require<ProductionQueueComponentState>().Items.Count == 0,
                $"{fixture.BuildingKind} should dequeue only after its exact egress spawn succeeds");
        }

        var blockedWorld = new EntityWorld(seed: 6179) { WorldWidth = 1600, WorldHeight = 1200 };
        blockedWorld.AddSystem(new ProductionSystem());
        var barracks = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var infantry = UnitDesignCatalog.Spec("dog.infantry");
        var center = new Vector2(500, 500);
        PlacementReservationMath.TryCenter(
            barracks,
            PlacementReservationKind.ProductionEgress,
            center,
            0,
            out var egress);
        var blockedProducer = blockedWorld.Spawn(
            barracks.ToEntitySpec(),
            new OwnerId(1),
            EntityTransform.At(center),
            new EntityComponentState[]
            {
                new HealthComponentState(barracks.MaxHp, barracks.MaxHp),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(0, barracks.PowerUsed, Powered: true),
                new ProductionQueueComponentState(new List<UnitProductionQueueItem>
                {
                    new()
                    {
                        Id = 1,
                        Kind = ProductionKindDesignBridge.ProductionKindFor(infantry),
                        DesignId = infantry.Id,
                        Faction = infantry.Faction,
                        Progress = infantry.Production!.Duration,
                    },
                }),
            });
        var blockerSpec = new EntitySpec
        {
            Id = "replay.egress_blocker",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Blocker", "blocker.name", "blocker.role", "BLK", IconGlyph.StanceHold),
        };
        var blocker = blockedWorld.Spawn(
            blockerSpec,
            new OwnerId(1),
            EntityTransform.At(egress),
            new EntityComponentState[]
            {
                new CollisionComponentState(24, 1, 1, BlocksMovement: true),
            });
        var blockedClock = new SimClock();
        blockedWorld.Step(1, blockedClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        var blockedQueue = blockedProducer.Components.Require<ProductionQueueComponentState>();
        Assert(blockedWorld.OrderedEntities.All(entity => entity.SpecId != infantry.Id),
            "blocked egress must not spawn at an alternate or fallback point");
        Assert(blockedQueue.Items.Count == 1
            && Math.Abs(blockedQueue.Items[0].Progress - infantry.Production!.Duration) < 0.0001f,
            "blocked egress should retain the first queue item at progress 1");

        blocker.Transform = blocker.Transform with { Position = new Vector2(900, 900) };
        blockedWorld.Step(2, blockedClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        var retried = blockedWorld.OrderedEntities.Single(entity => entity.SpecId == infantry.Id);
        Assert(retried.Transform.Position.DistanceTo(egress) < 0.001f,
            "cleared egress should retry on the next tick at the same exact point");
        Assert(blockedProducer.Components.Require<ProductionQueueComponentState>().Items.Count == 0,
            "successful retry should dequeue the completed item exactly once");
    }
    static void AssertResourceRallyProduction()
    {
        const int ticks = 470;

        EntitySpec FactorySpec()
        {
            return new EntitySpec
            {
                Id = "replay.vehicle_factory",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Factory", "factory.name", "factory.role", "FAC", IconGlyph.Building),
                Authoring = new EntityAuthoringMetadata(BuildingSpecId: BuildingDesignIds.VehicleFactory, TechTier: 1),
            };
        }

        EntitySpec ResourceSpec()
        {
            return new EntitySpec
            {
                Id = "replay.rally_resource",
                Kind = EntityKind.Resource,
                Display = new EntityDisplaySpec("Rally Resource", "resource.name", "resource.role", "RES", IconGlyph.Credits),
            };
        }

        EntityWorld BuildResourceRallyWorld()
        {
            var world = new EntityWorld(seed: 6262) { WorldWidth = 1600, WorldHeight = 1000 };
            world.AddSystem(new ProductionSystem());
            world.AddSystem(new ResourceSystem());
            world.AddSystem(new MovementSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 700;

            world.Spawn(FactorySpec(), new OwnerId(1), EntityTransform.At(new Vector2(360, 500)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new FootprintComponentState(new Vector2(120, 100)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 3, Powered: true),
                new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>()),
                new CollisionComponentState(64, 8, 100, BlocksMovement: true),
            });
            world.Spawn(ResourceSpec(), new OwnerId(0), EntityTransform.At(new Vector2(680, 500)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(Amount: 100, MaxAmount: 100),
            });
            world.Spawn(new EntitySpec
            {
                Id = "replay.rally_refinery",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Refinery", "refinery.name", "refinery.role", "REF", IconGlyph.Building),
            }, new OwnerId(1), EntityTransform.At(new Vector2(340, 680)), new EntityComponentState[]
            {
                new HealthComponentState(800, 800),
                new DockComponentState(),
            });

            return world;
        }

        var log = new List<EntityCommand>
        {
            new SetRallyPointEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, new Vector2(680, 500), new EntityId(2)),
            new ProduceEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 2, "dog.harvester"),
        };
        AssertDeterministic("resource-rally-production", BuildResourceRallyWorld, log, ticks, 47);

        var world = BuildResourceRallyWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in log)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var producerRally = world.OrderedEntities.Single(entity => entity.Id.Value == 1).Components.Require<RallyPointComponentState>();
        var resource = world.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<ResourceNodeComponentState>();
        var harvester = world.OrderedEntities.Single(entity => entity.SpecId == "dog.harvester");
        var harvesterState = harvester.Components.Require<HarvesterComponentState>();
        var cargo = harvester.Components.Require<ResourceCargoComponentState>();
        var commandable = harvester.Components.Require<CommandableComponentState>();

        Assert(producerRally.TargetEntityId == 2, $"producer rally should retain resource target entity id 2, got {producerRally.TargetEntityId}");
        Assert(harvesterState.FieldId == 2, $"produced harvester should target rally resource id 2, got {harvesterState.FieldId}");
        Assert(harvesterState.Mode != HarvesterMode.Idle, "produced harvester should enter the harvest loop from a resource rally");
        Assert(resource.Amount < 100 || cargo.Cargo > 0, $"resource rally should make harvester gather, resource {resource.Amount}, cargo {cargo.Cargo}");
        Assert(commandable.PlayerIntentTarget == new Vector2(680, 500), "produced harvester command visual should point at resource rally");
        Console.WriteLine($"OK [resource-rally-production]: mode {harvesterState.Mode}, field {harvesterState.FieldId}, resource {resource.Amount}, cargo {cargo.Cargo}.");
    }

    static void AssertFriendlyUnitRallyProduction()
    {
        const int ticks = 260;
        var staleRallyPoint = new Vector2(680, 500);
        var friendlyCurrentPosition = new Vector2(980, 540);

        EntitySpec ProducerSpec()
        {
            return new EntitySpec
            {
                Id = "replay.friendly_rally_barracks",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Barracks", "barracks.name", "barracks.role", "BAR", IconGlyph.Building),
                Authoring = new EntityAuthoringMetadata(BuildingSpecId: BuildingDesignIds.Barracks, TechTier: 1),
            };
        }

        EntityWorld BuildFriendlyUnitRallyWorld()
        {
            var world = new EntityWorld(seed: 6464) { WorldWidth = 1800, WorldHeight = 1200 };
            world.AddSystem(new ProductionSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 300;

            world.Spawn(ProducerSpec(), new OwnerId(1), EntityTransform.At(new Vector2(360, 500)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new FootprintComponentState(new Vector2(96, 86)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: true),
                new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>()),
                new CollisionComponentState(54, 8, 100, BlocksMovement: true),
            });
            world.SpawnUnit(UnitDesignCatalog.Spec("dog.guard_tank"), new OwnerId(1), friendlyCurrentPosition);
            return world;
        }

        var log = new List<EntityCommand>
        {
            new SetRallyPointEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, staleRallyPoint, new EntityId(2)),
            new ProduceEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 2, "dog.infantry"),
        };
        AssertDeterministic("friendly-unit-rally-production", BuildFriendlyUnitRallyWorld, log, ticks, 26);

        var world = BuildFriendlyUnitRallyWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in log)
        {
            buffer.Enqueue(command);
        }

        EntityInstance? produced = null;
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            produced = world.OrderedEntities.FirstOrDefault(entity => entity.SpecId == "dog.infantry");
            if (produced is not null)
            {
                break;
            }
        }

        var producerRally = world.OrderedEntities.Single(entity => entity.Id.Value == 1).Components.Require<RallyPointComponentState>();
        var friendlyTarget = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var commandable = produced?.Components.Require<CommandableComponentState>();
        var movement = produced?.Components.Require<MovementComponentState>();

        Assert(producerRally.Target == staleRallyPoint, "friendly-unit rally should retain the clicked point on the producer for deterministic command echo");
        Assert(producerRally.TargetEntityId == 2, $"friendly-unit rally should retain target entity id 2, got {producerRally.TargetEntityId}");
        Assert(produced is not null, "friendly-unit rally should still produce the queued infantry");
        Assert(commandable?.PlayerIntentTarget == friendlyTarget.Transform.Position, $"produced unit should rally to the friendly unit's current position {friendlyTarget.Transform.Position}, got {commandable?.PlayerIntentTarget}");
        Assert(commandable?.CommandVisualTarget == friendlyTarget.Transform.Position, "friendly-unit rally command visual should use the target unit's current position");
        Assert(movement?.MoveTarget == friendlyTarget.Transform.Position, "friendly-unit rally movement target should use the target unit's current position");
        Assert(commandable?.PlayerIntentTarget != staleRallyPoint, "friendly-unit rally should not fall back to the stale clicked point while the target entity is live");
        Console.WriteLine($"OK [friendly-unit-rally-production]: targetEntity {producerRally.TargetEntityId}, stale {staleRallyPoint}, current {friendlyTarget.Transform.Position}.");

        var hostileWorld = BuildFriendlyUnitRallyWorld();
        hostileWorld.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        hostileWorld.OrderedEntities.Single(entity => entity.Id.Value == 2).OwnerId = new OwnerId(2);
        var hostileBuffer = new EntityCommandBuffer();
        foreach (var command in log)
        {
            hostileBuffer.Enqueue(command);
        }

        var hostileClock = new SimClock();
        EntityInstance? hostileRallyProduced = null;
        for (var tick = 1; tick <= ticks; tick++)
        {
            hostileWorld.Step(tick, hostileClock.FixedDelta, hostileBuffer.DrainUpToTick(tick));
            hostileRallyProduced = hostileWorld.OrderedEntities.FirstOrDefault(entity => entity.SpecId == "dog.infantry");
            if (hostileRallyProduced is not null)
            {
                break;
            }
        }

        var hostileCommandable = hostileRallyProduced?.Components.Require<CommandableComponentState>();
        Assert(hostileCommandable?.PlayerIntentTarget == staleRallyPoint, "hostile entity rally must fall back to the static clicked rally point instead of dynamic target tracking");
    }

    static void AssertRepeatProduction()
    {
        const int ticks = 370;

        EntitySpec ProducerSpec()
        {
            return new EntitySpec
            {
                Id = "replay.repeat_barracks",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Repeat Barracks", "barracks.name", "barracks.role", "BAR", IconGlyph.Building),
                Authoring = new EntityAuthoringMetadata(BuildingSpecId: BuildingDesignIds.Barracks, TechTier: 1),
            };
        }

        EntityWorld BuildRepeatProductionWorld()
        {
            var world = new EntityWorld(seed: 6363) { WorldWidth = 1400, WorldHeight = 900 };
            world.AddSystem(new ProductionSystem());
            world.AddSystem(new MovementSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 240;

            world.Spawn(ProducerSpec(), new OwnerId(1), EntityTransform.At(new Vector2(360, 500)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new FootprintComponentState(new Vector2(96, 86)),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 0, Used: 2, Powered: true),
                new RallyPointComponentState(new Vector2(760, 500)),
                new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>()),
                new CollisionComponentState(54, 8, 100, BlocksMovement: true),
            });

            return world;
        }

        var log = new List<EntityCommand>
        {
            new SetRepeatProductionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, Enabled: true, OutputSpecId: "dog.infantry"),
        };
        AssertDeterministic("repeat-production", BuildRepeatProductionWorld, log, ticks, 37);

        var world = BuildRepeatProductionWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in log)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var producedUnits = world.OrderedEntities.Where(entity => entity.SpecId == "dog.infantry").ToList();
        var queue = world.OrderedEntities.Single(entity => entity.Id.Value == 1).Components.Require<ProductionQueueComponentState>();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;

        Assert(producedUnits.Count == 2, $"repeat production should spend 240 credits on exactly two infantry, got {producedUnits.Count}");
        Assert(queue.RepeatOutputSpecId == "dog.infantry", $"repeat output should remain armed, got {queue.RepeatOutputSpecId}");
        Assert(queue.Items.Count == 0, $"repeat producer should wait empty when credits are exhausted, got {queue.Items.Count} queued items");
        Assert(queue.Items is List<UnitProductionQueueItem>, "repeat producer should keep reusable queue storage after dequeue");
        Assert(credits == 0, $"repeat production should spend all available credits, got {credits}");
        Assert(producedUnits.All(unit =>
        {
            var rally = new Vector2(760, 500);
            return unit.Components.TryGet<CommandableComponentState>(out var commandable)
                && commandable.PlayerIntentTarget == rally;
        }), "repeat-produced units should preserve producer rally behavior");
        Console.WriteLine($"OK [repeat-production]: produced {producedUnits.Count}, repeat {queue.RepeatOutputSpecId}, queued {queue.Items.Count}, credits {credits}.");
    }
}
