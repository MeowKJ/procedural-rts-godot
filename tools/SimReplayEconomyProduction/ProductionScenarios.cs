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
        Assert(Math.Abs(unpoweredQueue.Items[0].Progress) < 0.0001f, "unpowered producer queue should not advance");
        Assert(unpoweredQueue.PauseReason == ProductionPauseReason.Unpowered, $"unpowered producer should report pause reason, got {unpoweredQueue.PauseReason}");
        Assert(cancelledQueue.Items.Count == 0, "cancelled producer queue should be empty after refund");
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

        Console.WriteLine($"OK [production-loop]: produced {producedUnits.Count}, paused queue {unpoweredQueue.Items.Count}, cancelled queue {cancelledQueue.Items.Count}, credits {credits}.");
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
