using Godot;

static partial class Program
{
    private static void AssertLivePlacementAuthorityParity()
    {
        var owner = OwnerId.FromPlayerSlot(PlayerSlotId.One);
        var battlefield = new UnitBattlefield
        {
            WorldSize = new Vector2(2400, 1600),
        };
        battlefield.SetCredits(PlayerSlotId.One, 5000);
        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        battlefield.UpsertBuildingTarget(
            1,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(320, 320),
            0,
            hqSpec.MaxHp,
            powered: true);

        var spatialAuthority = new ConstructionSystem();
        var powerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var desired = new Vector2(559.75f, 320.25f);
        var facing = Mathf.Pi * 0.5f;
        var playerPreview = battlefield.ValidateBuildingPlacement(
            powerSpec.Kind,
            PlayerSlotId.One,
            desired,
            facing,
            ConstructionPlacementIntent.Direct);
        var aiPreview = battlefield.ValidateBuildingPlacement(
            powerSpec.Kind,
            PlayerSlotId.One,
            desired,
            facing,
            ConstructionPlacementIntent.Direct);
        var simulationPreview = spatialAuthority.QueryBuildingPlacement(
            battlefield.EntityWorld,
            owner,
            powerSpec,
            desired,
            facing,
            ConstructionPlacementIntent.Direct);
        AssertSamePlacement(playerPreview, aiPreview, "player and AI Direct previews");
        AssertSamePlacement(playerPreview, simulationPreview, "preview and simulation Direct authority");
        Assert(playerPreview.IsValid, $"shared Direct parity fixture should be valid; got {playerPreview}");

        var readyDesired = new Vector2(1200, 1000);
        var playerReady = battlefield.ValidateBuildingPlacement(
            hqSpec.Kind,
            PlayerSlotId.One,
            readyDesired,
            0,
            ConstructionPlacementIntent.ReadyTicket);
        var aiReady = battlefield.ValidateBuildingPlacement(
            hqSpec.Kind,
            PlayerSlotId.One,
            readyDesired,
            0,
            ConstructionPlacementIntent.ReadyTicket);
        var simulationReady = spatialAuthority.QueryBuildingPlacement(
            battlefield.EntityWorld,
            owner,
            hqSpec,
            readyDesired,
            0,
            ConstructionPlacementIntent.ReadyTicket);
        AssertSamePlacement(playerReady, aiReady, "player and AI ReadyTicket previews");
        AssertSamePlacement(playerReady, simulationReady, "preview and simulation ReadyTicket authority");
        Assert(!playerReady.IsValid && playerReady.Reason == "placement.outsideBuildRadius",
            $"ReadyTicket should force build authority before visibility; got {playerReady}");

        var hiddenDirect = battlefield.ValidateBuildingPlacement(
            hqSpec.Kind,
            PlayerSlotId.One,
            readyDesired,
            0,
            ConstructionPlacementIntent.Direct);
        var hiddenSimulation = spatialAuthority.QueryBuildingPlacement(
            battlefield.EntityWorld,
            owner,
            hqSpec,
            readyDesired,
            0,
            ConstructionPlacementIntent.Direct);
        AssertSamePlacement(hiddenDirect, hiddenSimulation, "not-visible Direct parity");
        Assert(!hiddenDirect.IsValid && hiddenDirect.Reason == "placement.notVisible",
            $"Direct spec without build authority should still use simulation visibility; got {hiddenDirect}");

        var outsideDesired = new Vector2(-100, 100);
        var outsidePreview = battlefield.ValidateBuildingPlacement(
            hqSpec.Kind,
            PlayerSlotId.One,
            outsideDesired,
            0,
            ConstructionPlacementIntent.Direct);
        var outsideSimulation = spatialAuthority.QueryBuildingPlacement(
            battlefield.EntityWorld,
            owner,
            hqSpec,
            outsideDesired,
            0,
            ConstructionPlacementIntent.Direct);
        AssertSamePlacement(outsidePreview, outsideSimulation, "outside Direct parity");
        var creditsBeforeOutside = battlefield.Credits(PlayerSlotId.One);
        var outsideAccepted = battlefield.ConstructBuilding(
            PlayerSlotId.One,
            UnitFactionId.Dog,
            hqSpec.Kind,
            outsideDesired,
            out _,
            out var outsideStatus,
            0);
        Assert(!outsideAccepted && outsideStatus == outsidePreview.Reason,
            $"simulation should revalidate the original unclamped outside request; got accepted={outsideAccepted}, status={outsideStatus}");
        Assert(battlefield.Credits(PlayerSlotId.One) == creditsBeforeOutside,
            "outside spatial rejection should leave the external credit check/spend unchanged");

        var constructed = battlefield.ConstructBuilding(
            PlayerSlotId.One,
            UnitFactionId.Dog,
            powerSpec.Kind,
            desired,
            out var placed,
            out var status,
            facing);
        Assert(constructed && placed is not null,
            $"valid original desired/facing should survive final simulation revalidation; got {status}");
        Assert(placed!.Value.Position == new Vector2(playerPreview.X, playerPreview.Y),
            $"simulation should place at shared snapped coordinates {playerPreview.X},{playerPreview.Y}; got {placed.Value.Position}");
        Assert(Math.Abs(placed.Value.Facing - facing) < 0.0001f,
            $"simulation should preserve the queried cardinal facing; got {placed.Value.Facing}");
    }

    private static void AssertSamePlacement(PlacementResult expected, PlacementResult actual, string label)
    {
        Assert(expected.X == actual.X
            && expected.Y == actual.Y
            && expected.IsValid == actual.IsValid
            && expected.Reason == actual.Reason,
            $"{label} should match X/Y/IsValid/Reason exactly; expected {expected}, got {actual}");
    }

    private static void AssertPlayerBuildGatewayPreservesDesiredPoint()
    {
        var battlefield = new UnitBattlefield
        {
            WorldSize = new Vector2(1200, 900),
        };
        battlefield.EntityWorld.WorldWidth = battlefield.WorldSize.X;
        battlefield.EntityWorld.WorldHeight = battlefield.WorldSize.Y;
        battlefield.SetCredits(PlayerSlotId.One, 5000);
        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        battlefield.UpsertBuildingTarget(
            1,
            hqSpec.Kind,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(320, 320),
            0,
            hqSpec.MaxHp,
            powered: true);

        var outsidePoint = new PlayerCommandPoint(-100, 100);
        var outsideCommand = new PlayerCommand(
            PlayerSlotId.One,
            1,
            1,
            PlayerCommandKind.Build,
            PlayerCommandPayload.ForSpec(hqSpec.Kind) with
            {
                HasTargetPoint = true,
                TargetPoint = outsidePoint,
            });
        var outsideEnqueued = battlefield.TryEnqueue(
            outsideCommand,
            out var outsideEnvelope,
            out var outsideError,
            out var outsideMessage);
        Assert(outsideEnqueued && outsideError == CommandGatewayValidationError.None,
            $"validly shaped outside Build intent should reach simulation authority; got {outsideError}: {outsideMessage}");
        Assert(outsideEnvelope?.Command is StartConstructionEntityCommand outsideBuild
            && outsideBuild.Position == new Vector2(outsidePoint.X, outsidePoint.Y)
            && outsideBuild.Facing == 0,
            "Build gateway envelope should preserve the original desired point and canonical default facing 0");
        var outsideRejection = battlefield.EntityWorld.Events.Drain()
            .OfType<ConstructionRejectedEvent>()
            .SingleOrDefault();
        Assert(outsideRejection is not null && outsideRejection.Reason == "placement.outside",
            $"unclamped edge Build should be rejected by spatial authority as outside; got {outsideRejection?.Reason}");
        Assert(!battlefield.EntityWorld.OrderedEntities.Any(entity => entity.SpecId != "building.headquarters"),
            "outside gateway Build must not generate a clamped structure");

        var validPoint = new PlayerCommandPoint(559.75f, 320.25f);
        var validCommand = new PlayerCommand(
            PlayerSlotId.One,
            2,
            2,
            PlayerCommandKind.Build,
            PlayerCommandPayload.ForSpec(BuildingDesignIds.PowerPlant) with
            {
                HasTargetPoint = true,
                TargetPoint = validPoint,
            });
        var validEnqueued = battlefield.TryEnqueue(
            validCommand,
            out var validEnvelope,
            out var validError,
            out var validMessage);
        Assert(validEnqueued && validError == CommandGatewayValidationError.None,
            $"valid Build intent should reach simulation authority; got {validError}: {validMessage}");
        Assert(validEnvelope?.Command is StartConstructionEntityCommand validBuild
            && validBuild.Position == new Vector2(validPoint.X, validPoint.Y)
            && validBuild.Facing == 0,
            "valid Build gateway envelope should retain original point and default facing 0");
        var validRejections = battlefield.EntityWorld.Events.Drain()
            .OfType<ConstructionRejectedEvent>()
            .ToArray();
        var placed = battlefield.EntityWorld.OrderedEntities
            .SingleOrDefault(entity => entity.SpecId == "building.powerplant");
        Assert(placed is not null && placed.Transform.Position == new Vector2(544, 336) && placed.Transform.Facing == 0,
            $"valid gateway Build should spawn at parity-snapped 544,336 facing 0; got {placed?.Transform.Position}; rejections [{string.Join(", ", validRejections.Select(rejection => rejection.Reason))}]");
    }

    private static void AssertTypedOrderedEntityView()
    {
        var world = new EntityWorld(seed: 548);
        var spec = PlacementAuthoritySpec("placement.ordered-view");
        var first = world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(100, 100)));
        var removed = world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(200, 100)));
        var third = world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(300, 100)));
        var orderedView = world.OrderedEntities;
        Assert(ReferenceEquals(orderedView, world.OrderedEntities),
            "OrderedEntities should return the maintained canonical read index");
        Assert(orderedView.Count == 3
            && orderedView[0].Id == first.Id
            && orderedView[1].Id == removed.Id
            && orderedView[2].Id == third.Id,
            "ordered membership should begin as [1,2,3]");
        Assert(world.Remove(removed.Id), "ordered-view fixture should remove its middle entity");
        Assert(orderedView.Count == 2 && orderedView[0].Id == first.Id && orderedView[1].Id == third.Id,
            "direct removal should update ordered membership to [1,3]");
        Assert(!world.Remove(removed.Id), "removing an already missing entity should preserve false-return semantics");
        world.QueueRemoval(first.Id);
        world.FlushQueuedRemovals();
        Assert(orderedView.Count == 1 && orderedView[0].Id == third.Id,
            "queued removal should use the same membership authority and leave [3]");
        var fourth = world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(400, 100)));
        Assert(orderedView.Count == 2 && orderedView[0].Id == third.Id && orderedView[1].Id == fourth.Id,
            "monotonic spawn after removals should leave ordered membership [3,4]");
        Assert(world.Count == orderedView.Count,
            "dictionary membership and ordered membership should keep the same count");
        Assert(world.TryGet(third.Id, out var thirdLookup) && ReferenceEquals(thirdLookup, third),
            "ordered membership should share the dictionary's exact third entity reference");
        Assert(world.TryGet(fourth.Id, out var fourthLookup) && ReferenceEquals(fourthLookup, fourth),
            "ordered membership should share the dictionary's exact fourth entity reference");

        var previous = 0;
        for (var index = 0; index < orderedView.Count; index++)
        {
            var entity = orderedView[index];
            Assert(entity.Id.Value > previous, "cached OrderedEntities view must stay in strict EntityId order");
            previous = entity.Id.Value;
        }
    }

    private static void AssertPlacementQueryAllocatesZero()
    {
        const int iterations = 1000;
        var owner = new OwnerId(1);
        var world = CreatePlacementWorld(owner);
        var system = new ConstructionSystem();
        var spec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var desired = new Vector2(320, 336);

        var warm = system.QueryBuildingPlacement(
            world,
            owner,
            spec,
            desired,
            0,
            ConstructionPlacementIntent.Direct);
        Assert(warm.IsValid, $"allocation fixture should warm a valid placement query; got {warm}");

        var entityChecksum = 0;
        var entityScanBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            for (var entityIndex = 0; entityIndex < world.OrderedEntities.Count; entityIndex++)
            {
                var entity = world.OrderedEntities[entityIndex];
                entityChecksum += entity.Id.Value;
            }
        }

        var entityScanAllocated = GC.GetAllocatedBytesForCurrentThread() - entityScanBefore;
        GC.KeepAlive(entityChecksum);
        Assert(entityScanAllocated == 0,
            $"{iterations} ordered membership index scans should allocate exactly 0 bytes; got {entityScanAllocated}");

        var checksum = 0f;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
        {
            var result = system.QueryBuildingPlacement(
                world,
                owner,
                spec,
                desired,
                0,
                ConstructionPlacementIntent.Direct);
            checksum += result.X + result.Y + (result.IsValid ? 1 : 0) + result.Reason.Length;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(checksum);
        Assert(allocated == 0,
            $"{iterations} warmed immutable-world placement queries should allocate exactly 0 bytes; got {allocated}; ordered index scans allocated {entityScanAllocated}");
        Console.WriteLine($"OK [placement-allocation]: {iterations} ordered index scans={entityScanAllocated} bytes, full queries={allocated} bytes.");
    }

    private static EntityWorld CreatePlacementWorld(
        OwnerId owner,
        bool powered = true,
        float buildRadius = 2000,
        float sightRange = 2000)
    {
        var world = new EntityWorld(seed: 548)
        {
            WorldWidth = 1024,
            WorldHeight = 768,
        };
        world.Spawn(
            PlacementAuthoritySpec("placement.authority"),
            owner,
            EntityTransform.At(new Vector2(512, 384)),
            new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
                new VisionComponentState(sightRange),
                new BuildRadiusComponentState(buildRadius),
                new PowerComponentState(0, 0, powered),
            });
        return world;
    }

    private static EntitySpec PlacementAuthoritySpec(string id)
    {
        return new EntitySpec
        {
            Id = id,
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Placement Authority", "placement.authority.name", "placement.authority.role", "PA", IconGlyph.Settings),
        };
    }

    private static EntityInstance SpawnPlacementBuilding(
        EntityWorld world,
        OwnerId owner,
        BuildSpec spec,
        Vector2 position,
        float facing = 0)
    {
        return world.Spawn(
            spec.ToEntitySpec(),
            owner,
            EntityTransform.At(position, facing),
            new EntityComponentState[]
            {
                new ConstructionIdentityComponentState(spec.Kind),
                new HealthComponentState(spec.MaxHp, spec.MaxHp),
                new FootprintComponentState(spec.LogicalFootprint(facing), spec.PlacementDomain),
                new ConstructionComponentState(
                    Progress: 1,
                    BuildTime: spec.BuildTime,
                    Cost: spec.Cost,
                    RefundRatio: spec.RefundRatio),
            });
    }

}
