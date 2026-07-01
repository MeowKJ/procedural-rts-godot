static partial class Program
{
    static void AssertConstructionVisibilityGate()
    {
        const int constructionTicks = 4;
        var owner = new OwnerId(1);

        EntityWorld BuildConstructionVisibilityWorld()
        {
            var world = new EntityWorld(seed: 6467) { WorldWidth = 1600, WorldHeight = 1200 };
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(owner).Credits = 900;

            var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
            world.Spawn(hqSpec.ToEntitySpec(), owner, EntityTransform.At(new Vector2(500, 500)), new EntityComponentState[]
            {
                new ConstructionIdentityComponentState(BuildingDesignIds.Headquarters),
                new HealthComponentState(hqSpec.MaxHp, hqSpec.MaxHp),
                new VisionComponentState(280),
                new FootprintComponentState(hqSpec.Footprint, hqSpec.PlacementDomain),
                new ConstructionComponentState(Progress: 1, BuildTime: hqSpec.BuildTime, Cost: hqSpec.Cost, RefundRatio: hqSpec.RefundRatio),
                new PowerComponentState(hqSpec.PowerProvided, hqSpec.PowerUsed, Powered: true),
                new BuildRadiusComponentState(hqSpec.BuildRadius),
            });

            return world;
        }

        var constructionLog = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(owner, new[] { new EntityId(1) }, 1, BuildingDesignIds.PowerPlant, new Vector2(1056, 500)),
            new StartConstructionEntityCommand(owner, new[] { new EntityId(1) }, 2, BuildingDesignIds.PowerPlant, new Vector2(704, 500)),
        };

        AssertDeterministic("construction-visibility", BuildConstructionVisibilityWorld, constructionLog, constructionTicks, 1);

        var world = BuildConstructionVisibilityWorld();
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
        var credits = world.ResourceInventory(owner).Credits;

        Assert(rejected.Any(rejection => rejection.Reason == "placement.notVisible"), "construction placement should reject build starts outside owner build visibility with placement.notVisible");
        Assert(buildings.Count == 2, $"visibility gate should reject the unseen build and accept the visible build; got {buildings.Count}");
        Assert(credits == 600, $"construction visibility gate should spend only the accepted power plant cost, got credits {credits}");

        var acceptedPowerPlant = buildings.Single(entity => entity.Id.Value == 2);
        Assert(acceptedPowerPlant.SpecId == "building.powerplant", $"visible control build should spawn a power plant, got {acceptedPowerPlant.SpecId}");
        Assert(acceptedPowerPlant.Transform.Position == new Vector2(704, 512), $"visible control build should use snapped placement, got {acceptedPowerPlant.Transform.Position}");

        Console.WriteLine($"OK [construction-visibility]: rejected {rejected.Count}, buildings {buildings.Count}, credits {credits}.");
    }

    static void AssertConstructionCancelRefund()
    {
        const int constructionTicks = 230;

        EntityWorld BuildConstructionCancelWorld()
        {
            var world = new EntityWorld(seed: 6465) { WorldWidth = 900, WorldHeight = 900 };
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 800;

            var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
            world.Spawn(hqSpec.ToEntitySpec(), new OwnerId(1), EntityTransform.At(new Vector2(100, 100)), new EntityComponentState[]
            {
                new ConstructionIdentityComponentState(BuildingDesignIds.Headquarters),
                new HealthComponentState(hqSpec.MaxHp, hqSpec.MaxHp),
                new VisionComponentState(hqSpec.SightRange),
                new FootprintComponentState(hqSpec.Footprint, hqSpec.PlacementDomain),
                new ConstructionComponentState(Progress: 1, BuildTime: hqSpec.BuildTime, Cost: hqSpec.Cost, RefundRatio: hqSpec.RefundRatio),
                new PowerComponentState(hqSpec.PowerProvided, hqSpec.PowerUsed, Powered: true),
                new BuildRadiusComponentState(hqSpec.BuildRadius),
            });

            return world;
        }

        var constructionLog = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, BuildingDesignIds.PowerPlant, new Vector2(260, 260)),
            new CancelConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(2) }, 31),
            new StartConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 40, BuildingDesignIds.PowerPlant, new Vector2(260, 260)),
            new CancelConstructionEntityCommand(new OwnerId(1), new[] { new EntityId(3) }, 220),
        };

        AssertDeterministic("construction-cancel", BuildConstructionCancelWorld, constructionLog, constructionTicks, 37);

        var world = BuildConstructionCancelWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var cancelled = new List<ConstructionCancelledEvent>();
        foreach (var command in constructionLog)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= constructionTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            cancelled.AddRange(world.Events.Drain().OfType<ConstructionCancelledEvent>());
        }

        var buildings = world.OrderedEntities
            .Where(entity => entity.Components.TryGet<ConstructionComponentState>(out _))
            .OrderBy(entity => entity.Id.Value)
            .ToList();
        var completedPower = buildings.Single(entity => entity.Id.Value == 3);
        var completedConstruction = completedPower.Components.Require<ConstructionComponentState>();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;
        var expectedRefund = Mathf.RoundToInt(300 * 0.5f * (1 - (30f / 165f)));

        Assert(cancelled.Count == 1, $"only the under-construction building should emit cancellation, got {cancelled.Count}");
        Assert(cancelled[0].Entity.Value == 2, $"cancel event should point at cancelled entity 2, got {cancelled[0].Entity.Value}");
        Assert(cancelled[0].Refund == expectedRefund, $"cancel refund should be based on remaining progress, got {cancelled[0].Refund}, expected {expectedRefund}");
        Assert(!world.TryGet(new EntityId(2), out _), "cancelled under-construction building should be removed from EntityWorld");
        Assert(buildings.Count == 2, $"HQ and completed replacement power plant should remain, got {buildings.Count}");
        Assert(completedConstruction.Progress >= 1, $"replacement power plant should complete, got {completedConstruction.Progress:0.000}");
        Assert(credits == 800 - 300 + expectedRefund - 300, $"completed construction cancel should not refund, got credits {credits}");

        Console.WriteLine($"OK [construction-cancel]: refund {cancelled[0].Refund}, credits {credits}, remaining buildings {buildings.Count}.");
    }

    static void AssertConstructionPausedOffline()
    {
        const int constructionTicks = 6;
        const float StartedProgress = 0.25f;

        EntitySpec ReplayBuildingSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec(id, $"{id}.name", $"{id}.role", id.ToUpperInvariant()[..3], IconGlyph.Building),
            };
        }

        EntityWorld BuildConstructionPauseWorld()
        {
            var world = new EntityWorld(seed: 6469);
            world.AddSystem(new ConstructionSystem());
            world.AddSystem(new PowerSystem());

            var providerSpec = ReplayBuildingSpec("replay.construction_power");
            var consumerSpec = ReplayBuildingSpec("replay.construction_consumer");
            var inertSpec = ReplayBuildingSpec("replay.construction_inert");

            world.Spawn(providerSpec, new OwnerId(1), EntityTransform.At(new Vector2(0, 0)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 12, Used: 0, Powered: true),
            });
            world.Spawn(consumerSpec, new OwnerId(1), EntityTransform.At(new Vector2(120, 0)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 0, BuildTime: 2),
                new PowerComponentState(Provided: 0, Used: 6, Powered: false),
            });
            world.Spawn(providerSpec, new OwnerId(1), EntityTransform.At(new Vector2(240, 0)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 0.5f, BuildTime: 2),
                new PowerComponentState(Provided: 8, Used: 0, Powered: false),
            });

            world.Spawn(providerSpec, new OwnerId(2), EntityTransform.At(new Vector2(0, 160)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 4, Used: 0, Powered: true),
            });
            world.Spawn(providerSpec, new OwnerId(2), EntityTransform.At(new Vector2(120, 160)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 0.95f, BuildTime: 0.5f),
                new PowerComponentState(Provided: 8, Used: 0, Powered: false),
            });
            world.Spawn(consumerSpec, new OwnerId(2), EntityTransform.At(new Vector2(240, 160)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: StartedProgress, BuildTime: 2),
                new PowerComponentState(Provided: 0, Used: 6, Powered: false),
            });

            world.Spawn(providerSpec, new OwnerId(3), EntityTransform.At(new Vector2(0, 320)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: 4, Used: 0, Powered: true),
            });
            world.Spawn(consumerSpec, new OwnerId(3), EntityTransform.At(new Vector2(120, 320)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: StartedProgress, BuildTime: 2),
                new PowerComponentState(Provided: 0, Used: 6, Powered: false),
            });
            world.Spawn(inertSpec, new OwnerId(3), EntityTransform.At(new Vector2(240, 320)), new EntityComponentState[]
            {
                new HealthComponentState(500, 500),
                new ConstructionComponentState(Progress: StartedProgress, BuildTime: 2),
                new PowerComponentState(Provided: 0, Used: 0, Powered: false),
            });

            return world;
        }

        AssertDeterministic("construction-paused-offline", BuildConstructionPauseWorld, constructionTicks, 1);

        var world = BuildConstructionPauseWorld();
        var clock = new SimClock();

        world.Step(1, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        var zeroStartConsumer = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var underConstructionProvider = world.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var recoveringConsumer = world.OrderedEntities.Single(entity => entity.Id.Value == 6);
        var zeroStartConstruction = zeroStartConsumer.Components.Require<ConstructionComponentState>();
        var providerConstruction = underConstructionProvider.Components.Require<ConstructionComponentState>();
        var pausedConstruction = recoveringConsumer.Components.Require<ConstructionComponentState>();
        var recoveringPower = recoveringConsumer.Components.Require<PowerComponentState>();

        Assert(zeroStartConstruction.Progress > 0, "zero-progress powered consumer should begin construction before power gating can pause it");
        Assert(zeroStartConsumer.Components.Require<PowerComponentState>().Powered, "zero-progress consumer should become powered once PowerSystem evaluates owner budget");
        Assert(providerConstruction.Progress > 0.5f, "unpowered construction provider should still advance before it can supply power");
        Assert(Math.Abs(pausedConstruction.Progress - StartedProgress) < 0.0001f, "unpowered construction should preserve progress while paused");
        Assert(pausedConstruction.Paused && pausedConstruction.PauseReason == ConstructionPauseReason.Unpowered, $"unpowered construction should report pause reason, got {pausedConstruction.PauseReason}");
        Assert(recoveringPower.Powered, "backup provider completion should let PowerSystem restore the paused consumer's power");

        world.Step(2, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        var resumedConstruction = recoveringConsumer.Components.Require<ConstructionComponentState>();
        Assert(!resumedConstruction.Paused, $"restored construction should clear pause reason, got {resumedConstruction.PauseReason}");
        Assert(resumedConstruction.Progress > StartedProgress, "restored construction should continue from preserved progress");

        for (var tick = 3; tick <= constructionTicks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        }

        var offlineConstruction = world.OrderedEntities.Single(entity => entity.Id.Value == 8).Components.Require<ConstructionComponentState>();
        var inertConstruction = world.OrderedEntities.Single(entity => entity.Id.Value == 9).Components.Require<ConstructionComponentState>();
        Assert(offlineConstruction.PauseReason == ConstructionPauseReason.Unpowered, "persistently low-power construction should stay paused");
        Assert(Math.Abs(offlineConstruction.Progress - StartedProgress) < 0.0001f, "persistently low-power construction should keep its progress");
        Assert(inertConstruction.PauseReason == ConstructionPauseReason.None, "non-consuming construction should not pause on Powered=false");
        Assert(inertConstruction.Progress > StartedProgress, "non-consuming construction should continue while its PowerComponent is unpowered");

        Console.WriteLine($"OK [construction-paused-offline]: paused at {StartedProgress:0.000}, resumed to {resumedConstruction.Progress:0.000}, offline held {offlineConstruction.Progress:0.000}.");
    }
}
