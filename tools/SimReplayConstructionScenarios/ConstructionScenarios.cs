static partial class Program
{
    static void AssertConstructionSystem()
    {
        const int constructionTicks = 430;

        EntityWorld BuildConstructionWorld()
        {
            var world = new EntityWorld(seed: 6464) { WorldWidth = 900, WorldHeight = 900 };
            world.AddSystem(new ConstructionSystem());
            world.AddSystem(new PowerSystem());
            world.AddSystem(new ProductionSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 800;

            var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
            world.Spawn(hqSpec.ToEntitySpec(), new OwnerId(1), EntityTransform.At(new Vector2(100, 100)), new EntityComponentState[]
            {
                new ConstructionIdentityComponentState(BuildingDesignIds.Headquarters),
                new HealthComponentState(hqSpec.MaxHp, hqSpec.MaxHp),
                new VisionComponentState(hqSpec.SightRange),
                new FootprintComponentState(hqSpec.LogicalFootprint(), hqSpec.PlacementDomain),
                new ConstructionComponentState(Progress: 1, BuildTime: hqSpec.BuildTime, Cost: hqSpec.Cost, RefundRatio: hqSpec.RefundRatio),
                new PowerComponentState(hqSpec.PowerProvided, hqSpec.PowerUsed, Powered: true),
                new BuildRadiusComponentState(hqSpec.BuildRadius),
            });

            return world;
        }

        var constructionLog = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, BuildingDesignIds.Barracks, new Vector2(360, 100)),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, BuildingDesignIds.PowerPlant, new Vector2(260, 260)),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 200, BuildingDesignIds.Barracks, new Vector2(360, 100)),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 220, BuildingDesignIds.Refinery, new Vector2(500, 100)),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 240, BuildingDesignIds.PowerPlant, new Vector2(260, 260)),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 260, BuildingDesignIds.PowerPlant, new Vector2(820, 820)),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 280, BuildingDesignIds.PowerPlant, new Vector2(680, 145)),
        };

        AssertDeterministic("construction-loop", BuildConstructionWorld, constructionLog, constructionTicks, 43);

        var world = BuildConstructionWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var rejected = new List<ConstructionRejectedEvent>();
        foreach (var command in constructionLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= constructionTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            rejected.AddRange(world.Events.Drain().OfType<ConstructionRejectedEvent>());
        }

        var buildings = world.OrderedEntities
            .Where(entity => entity.Components.TryGet<ConstructionComponentState>(out _))
            .OrderBy(entity => entity.Id.Value)
            .ToList();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;
        var buildingList = string.Join(", ", buildings.Select(entity =>
        {
            var construction = entity.Components.Require<ConstructionComponentState>();
            return $"{entity.Id.Value}:{entity.SpecId}:{construction.Progress:0.000}";
        }));

        Assert(buildings.Count == 3, $"only HQ, accepted power plant, and accepted barracks should exist; got {buildings.Count} [{buildingList}], credits {credits}");
        Assert(rejected.Any(rejection => rejection.Reason == "placement.missingTech"), "construction placement should reject missing tech/prerequisites with a reason");
        Assert(rejected.Any(rejection => rejection.Reason == "placement.blocked"), "construction placement should reject overlapping footprints with a reason");
        Assert(rejected.Any(rejection => rejection.Reason == "placement.outsideBuildRadius"), "construction placement should reject positions outside owner build radius with a reason");
        Assert(rejected.Any(rejection => rejection.Reason == "placement.impassable"), "construction placement should reject terrain outside the placement domain with a reason");

        var powerPlant = buildings.Single(entity => entity.SpecId == "building.powerplant");
        var barracks = buildings.Single(entity => entity.SpecId == "building.barracks");
        var powerConstruction = powerPlant.Components.Require<ConstructionComponentState>();
        var barracksConstruction = barracks.Components.Require<ConstructionComponentState>();
        var power = powerPlant.Components.Require<PowerComponentState>();
        var queue = barracks.Components.Require<ProductionQueueComponentState>();

        Assert(credits == 80, $"construction should spend accepted costs only, got credits {credits}");
        Assert(powerConstruction.Progress >= 1, $"power plant should complete construction, got {powerConstruction.Progress:0.000}");
        Assert(barracksConstruction.Progress >= 1, $"barracks should complete construction, got {barracksConstruction.Progress:0.000}");
        Assert(power.Powered, "completed power plant should become active for PowerSystem");
        Assert(queue.Items.Count == 0, "completed barracks should expose an empty production queue");

        var replayHash = world.DeterministicStateHash();
        Assert(replayHash != 0, "construction world should produce a deterministic non-zero hash");
        Console.WriteLine($"OK [construction-loop]: buildings {buildings.Count}, credits {credits}, rejected {rejected.Count}, hash {replayHash:X16}.");
    }

    static void AssertConstructionMethodMetadata()
    {
        const int constructionTicks = 3;
        var powerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var dogMethod = BuildSpecCatalog.ConstructionMethodFor(BuildingDesignIds.PowerPlant, UnitFactionId.Dog);
        var catMethod = BuildSpecCatalog.ConstructionMethodFor(BuildingDesignIds.PowerPlant, UnitFactionId.Cat);
        var sharedMethod = BuildSpecCatalog.ConstructionMethod(BuildingDesignIds.PowerPlant, ConstructionMethodKind.SharedRestartCapture);
        var methodBackends = powerSpec.ConstructionMethodMetadata
            .Select(method => (method.BackendCommandKind, method.BackendCommandName))
            .Distinct()
            .ToArray();

        Assert(dogMethod.Kind == ConstructionMethodKind.DogDeployInPlace, "Dog construction policy should resolve DogDeployInPlace metadata");
        Assert(dogMethod.Faction == UnitFactionId.Dog, "Dog construction method should carry Dog faction metadata");
        Assert(dogMethod.PlacementMode == BuildPlacementMode.DeployInPlace, "Dog construction method should expose deploy-in-place placement metadata");
        Assert(catMethod.Kind == ConstructionMethodKind.CatSidebarPlacement, "Cat construction policy should resolve CatSidebarPlacement metadata");
        Assert(catMethod.Faction == UnitFactionId.Cat, "Cat construction method should carry Cat faction metadata");
        Assert(catMethod.PlacementMode == BuildPlacementMode.SidebarPlacement, "Cat construction method should expose sidebar placement metadata");
        Assert(sharedMethod.Kind == ConstructionMethodKind.SharedRestartCapture, "Shared construction method should expose restart-capture metadata");
        Assert(sharedMethod.Faction is null, "Shared construction method should not be tied to one faction");
        Assert(sharedMethod.PlacementMode == BuildPlacementMode.RestartCapture, "Shared construction method should expose restart-capture placement metadata");
        Assert(methodBackends.Length == 1
            && methodBackends[0].BackendCommandKind == EntityCommandKind.Build
            && methodBackends[0].BackendCommandName == nameof(StartConstructionEntityCommand),
            "Construction method metadata should point every method at the same StartConstructionEntityCommand backend");

        var constructionLog = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, BuildingDesignIds.PowerPlant, new Vector2(416, 176)),
            new StartConstructionEntityCommand(new OwnerId(2), new[] { new EntityId(2) }, 1, BuildingDesignIds.PowerPlant, new Vector2(832, 176)),
        };
        Assert(constructionLog.All(command => command is StartConstructionEntityCommand), "Dog and Cat method starts should use the same StartConstructionEntityCommand type");
        AssertDeterministic("construction-methods", BuildConstructionMethodWorld, constructionLog, constructionTicks, 1);

        var world = BuildConstructionMethodWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in constructionLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= constructionTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            world.Events.Drain();
        }

        var startedPowerPlants = world.OrderedEntities
            .Where(entity => entity.SpecId == "building.powerplant")
            .OrderBy(entity => entity.OwnerId.Value)
            .ToArray();
        Assert(startedPowerPlants.Length == 2, $"same construction backend should accept Dog and Cat method starts, got {startedPowerPlants.Length}");
        Assert(startedPowerPlants.All(entity => entity.Components.Require<ConstructionComponentState>().Cost == powerSpec.Cost),
            "method-specific metadata should still spawn regular ConstructionComponentState costs");
        Assert(world.ResourceInventory(new OwnerId(1)).Credits == 300, "Dog method start should spend only the shared BuildSpec cost");
        Assert(world.ResourceInventory(new OwnerId(2)).Credits == 300, "Cat method start should spend only the shared BuildSpec cost");

        Console.WriteLine($"OK [construction-methods]: dog {dogMethod.PlacementMode}, cat {catMethod.PlacementMode}, shared {sharedMethod.PlacementMode}, backend {dogMethod.BackendCommandName}, starts {startedPowerPlants.Length}.");

        static EntityWorld BuildConstructionMethodWorld()
        {
            var world = new EntityWorld(seed: 6468) { WorldWidth = 1400, WorldHeight = 760 };
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 600;
            world.ResourceInventory(new OwnerId(2)).Credits = 600;

            SpawnCompleted(world, new OwnerId(1), BuildingDesignIds.Headquarters, new Vector2(180, 180));
            SpawnCompleted(world, new OwnerId(2), BuildingDesignIds.Headquarters, new Vector2(1080, 180));
            return world;
        }

        static void SpawnCompleted(EntityWorld world, OwnerId owner, string kind, Vector2 position)
        {
            var spec = BuildSpecCatalog.For(kind);
            var components = new List<EntityComponentState>
            {
                new ConstructionIdentityComponentState(kind),
                new HealthComponentState(spec.MaxHp, spec.MaxHp),
                new VisionComponentState(spec.SightRange),
                new FootprintComponentState(spec.LogicalFootprint(), spec.PlacementDomain),
                new ConstructionComponentState(Progress: 1, BuildTime: spec.BuildTime, Cost: spec.Cost, RefundRatio: spec.RefundRatio),
                new PowerComponentState(spec.PowerProvided, spec.PowerUsed, Powered: true),
            };

            if (spec.BuildRadius > 0)
            {
                components.Add(new BuildRadiusComponentState(spec.BuildRadius));
            }

            world.Spawn(spec.ToEntitySpec(), owner, EntityTransform.At(position), components);
        }
    }

    static void AssertConstructionQueueReadyState()
    {
        const int constructionTicks = 180;
        var owner = new OwnerId(1);

        EntityWorld BuildConstructionQueueWorld()
        {
            var world = new EntityWorld(seed: 6470) { WorldWidth = 1200, WorldHeight = 900 };
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(owner).Credits = 1000;

            SpawnCompleted(world, owner, BuildingDesignIds.Headquarters, new Vector2(180, 180));
            return world;
        }

        var constructionLog = new List<EntityCommand>
        {
            new QueueConstructionEntityCommand(owner, new[] { new EntityId(1) }, 1, BuildingDesignIds.PowerPlant),
            new QueueConstructionEntityCommand(owner, new[] { new EntityId(1) }, 170, BuildingDesignIds.Barracks),
        };

        AssertDeterministic("construction-queue-ready", BuildConstructionQueueWorld, constructionLog, constructionTicks, 30);

        var world = BuildConstructionQueueWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var rejected = new List<ConstructionRejectedEvent>();
        foreach (var command in constructionLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= constructionTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            rejected.AddRange(world.Events.Drain().OfType<ConstructionRejectedEvent>());
        }

        var queueTicket = world.OrderedEntities.Single(entity => entity.SpecId == $"construction.queue.{BuildingDesignIds.PowerPlant}");
        var queuedConstruction = queueTicket.Components.Require<ConstructionComponentState>();
        var credits = world.ResourceInventory(owner).Credits;
        var constructionEntities = world.OrderedEntities
            .Where(entity => entity.Components.TryGet<ConstructionComponentState>(out _))
            .OrderBy(entity => entity.Id.Value)
            .ToArray();

        Assert(queuedConstruction.ReadyToPlace, $"queued construction should become ready-to-place, got phase {queuedConstruction.Phase}");
        Assert(Math.Abs(queuedConstruction.Progress - 1) < 0.0001f, $"ready queue ticket should complete queue progress, got {queuedConstruction.Progress:0.000}");
        Assert(!queueTicket.Components.Has<FootprintComponentState>(), "ready queue ticket should not reserve a placed footprint");
        Assert(!queueTicket.Components.Has<BuildRadiusComponentState>(), "ready queue ticket should not provide build authority");
        Assert(rejected.Any(rejection => rejection.Reason == "placement.missingTech"), "ready queue ticket should not satisfy missing tech for later construction");
        Assert(constructionEntities.Length == 2, $"only HQ and the queued ticket should exist, got {constructionEntities.Length}");
        Assert(credits == 700, $"queued construction should spend cost once and rejected follow-up should not spend, got credits {credits}");

        Console.WriteLine($"OK [construction-queue-ready]: ticket {queueTicket.Id.Value} phase {queuedConstruction.Phase}, credits {credits}, rejected {rejected.Count}.");

        static void SpawnCompleted(EntityWorld world, OwnerId owner, string kind, Vector2 position)
        {
            var spec = BuildSpecCatalog.For(kind);
            var components = new List<EntityComponentState>
            {
                new ConstructionIdentityComponentState(kind),
                new HealthComponentState(spec.MaxHp, spec.MaxHp),
                new VisionComponentState(spec.SightRange),
                new FootprintComponentState(spec.LogicalFootprint(), spec.PlacementDomain),
                new ConstructionComponentState(Progress: 1, BuildTime: spec.BuildTime, Cost: spec.Cost, RefundRatio: spec.RefundRatio),
                new PowerComponentState(spec.PowerProvided, spec.PowerUsed, Powered: true),
            };

            if (spec.BuildRadius > 0)
            {
                components.Add(new BuildRadiusComponentState(spec.BuildRadius));
            }

            world.Spawn(spec.ToEntitySpec(), owner, EntityTransform.At(position), components);
        }
    }

    static void AssertConstructionPowerGate()
    {
        const int constructionTicks = 4;

        EntityWorld BuildConstructionPowerGateWorld()
        {
            var world = new EntityWorld(seed: 6466) { WorldWidth = 2200, WorldHeight = 2200 };
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 1200;

            SpawnCompleted(BuildingDesignIds.Headquarters, new Vector2(120, 120), powered: true);
            SpawnCompleted(BuildingDesignIds.PowerPlant, new Vector2(120, 740), powered: true);
            SpawnCompleted(BuildingDesignIds.Barracks, new Vector2(1600, 1600), powered: false);
            return world;

            void SpawnCompleted(string kind, Vector2 position, bool powered)
            {
                var spec = BuildSpecCatalog.For(kind);
                var components = new List<EntityComponentState>
                {
                    new ConstructionIdentityComponentState(kind),
                    new HealthComponentState(spec.MaxHp, spec.MaxHp),
                    new VisionComponentState(spec.SightRange),
                    new FootprintComponentState(spec.LogicalFootprint(), spec.PlacementDomain),
                    new ConstructionComponentState(Progress: 1, BuildTime: spec.BuildTime, Cost: spec.Cost, RefundRatio: spec.RefundRatio),
                    new PowerComponentState(spec.PowerProvided, spec.PowerUsed, powered),
                };

                if (spec.BuildRadius > 0)
                {
                    components.Add(new BuildRadiusComponentState(spec.BuildRadius));
                }

                world.Spawn(spec.ToEntitySpec(), new OwnerId(1), EntityTransform.At(position), components);
            }
        }

        var constructionLog = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, BuildingDesignIds.Refinery, new Vector2(1780, 1600)),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 2, BuildingDesignIds.PowerPlant, new Vector2(320, 120)),
        };

        AssertDeterministic("construction-power-gate", BuildConstructionPowerGateWorld, constructionLog, constructionTicks, 1);

        var world = BuildConstructionPowerGateWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var rejected = new List<ConstructionRejectedEvent>();
        foreach (var command in constructionLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= constructionTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            rejected.AddRange(world.Events.Drain().OfType<ConstructionRejectedEvent>());
        }

        var buildings = world.OrderedEntities
            .Where(entity => entity.Components.TryGet<ConstructionComponentState>(out _))
            .OrderBy(entity => entity.Id.Value)
            .ToList();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;

        Assert(rejected.Any(rejection => rejection.Reason == "placement.unpowered"), "construction placement should reject build authority from an unpowered anchor with placement.unpowered");
        Assert(buildings.Count == 4, $"unpowered anchor rejection should not spawn a refinery, while the powered-anchor build should start; got {buildings.Count}");
        Assert(credits == 900, $"construction power gate should spend only the accepted power plant cost, got credits {credits}");

        var acceptedPowerPlant = buildings.Single(entity => entity.Id.Value == 4);
        var construction = acceptedPowerPlant.Components.Require<ConstructionComponentState>();
        Assert(acceptedPowerPlant.SpecId == "building.powerplant", $"powered anchor control build should spawn a power plant, got {acceptedPowerPlant.SpecId}");
        Assert(construction.Progress > 0 && construction.Progress < 1, $"accepted control build should be under construction, got {construction.Progress:0.000}");

        Console.WriteLine($"OK [construction-power-gate]: rejected {rejected.Count}, buildings {buildings.Count}, credits {credits}.");
    }
}
