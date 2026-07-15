using Godot;

static partial class Program
{
    static void AssertPlacementGridFootprints()
    {
        var footprint = new PlacementGridFootprint(4, 3);
        Assert(footprint.IsValid, "4x3 placement footprint should be valid");
        Assert(footprint.WorldSize == new Vector2(128, 96), $"4x3 footprint should resolve to 128x96, got {footprint.WorldSize}");

        var rotated = footprint.Rotated(Mathf.Pi * 0.5f);
        Assert(rotated == new PlacementGridFootprint(3, 4), $"quarter turn should swap 4x3 to 3x4, got {rotated}");
        Assert(footprint.Rotated(Mathf.Pi) == footprint, "half turn should preserve footprint dimensions");

        var cardinalFacings = new[] { 0f, Mathf.Pi * 0.5f, Mathf.Pi, Mathf.Pi * 1.5f };
        for (var index = 0; index < cardinalFacings.Length; index++)
        {
            var facing = cardinalFacings[index];
            Assert(PlacementMath.TryNormalizeCardinalFacing(facing, out var normalized),
                $"quarter turn {index} should be accepted as a cardinal building facing");
            Assert(Math.Abs(normalized - facing) < 0.0001f,
                $"quarter turn {index} should normalize without changing its facing");
            var expected = index % 2 == 0 ? footprint : new PlacementGridFootprint(3, 4);
            Assert(footprint.Rotated(normalized) == expected,
                $"quarter turn {index} should use footprint {expected}, got {footprint.Rotated(normalized)}");
        }

        Assert(PlacementMath.TryNormalizeCardinalFacing(-Mathf.Pi * 0.5f, out var wrappedFacing)
            && Math.Abs(wrappedFacing - Mathf.Pi * 1.5f) < 0.0001f,
            "negative quarter turn should normalize to the equivalent four-way facing");
        Assert(!PlacementMath.TryNormalizeCardinalFacing(0.1f, out _),
            "seeded micro-rotation should not be accepted as a cardinal building facing");

        var evenOdd = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        Assert(evenOdd.IsValid && evenOdd.X == 96 && evenOdd.Y == 112,
            $"4x3 anchor parity should snap to 96,112; got {evenOdd.X},{evenOdd.Y}");

        var oddEven = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [],
            requiresBuildAuthority: false,
            logicalFootprint: footprint,
            facing: Mathf.Pi * 0.5f);
        Assert(oddEven.IsValid && oddEven.X == 112 && oddEven.Y == 96,
            $"rotated 3x4 anchor parity should snap to 112,96; got {oddEven.X},{oddEven.Y}");

        var rotationDiscriminator = new PlacementObstacle(32, 64, 32, 32);
        var unrotatedBlocked = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [rotationDiscriminator],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        var rotatedClear = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [rotationDiscriminator],
            requiresBuildAuthority: false,
            logicalFootprint: footprint,
            facing: Mathf.Pi * 0.5f);
        Assert(!unrotatedBlocked.IsValid && unrotatedBlocked.Reason == "placement.blocked",
            "unrotated 4x3 footprint should overlap the rotation discriminator");
        Assert(rotatedClear.IsValid,
            "rotated 3x4 footprint should clear an obstacle touching only its left edge");

        var firstRect = PlacementMath.RectFromCenter(evenOdd.X, evenOdd.Y, footprint.WorldSize.X, footprint.WorldSize.Y);
        var obstacle = new PlacementObstacle(firstRect.X, firstRect.Y, firstRect.Width, firstRect.Height);
        var adjacent = PlacementMath.ValidateBuildableArea(
            224,
            112,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [obstacle],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        var overlapping = PlacementMath.ValidateBuildableArea(
            192,
            112,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [obstacle],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        Assert(adjacent.IsValid, "cell footprints that only share an edge should be valid");
        Assert(!overlapping.IsValid && overlapping.Reason == "placement.blocked", "overlapping logical cells should be rejected");

        foreach (var spec in BuildSpecCatalog.Definitions.Values)
        {
            Assert(spec.FootprintCells.IsValid, $"{spec.Kind} should declare positive FootprintCells");
            Assert(spec.PlacementClearanceCells >= 0, $"{spec.Kind} should declare non-negative placement clearance");
            var logical = spec.LogicalFootprint();
            Assert(logical.X >= spec.Footprint.X && logical.Y >= spec.Footprint.Y,
                $"{spec.Kind} logical footprint should contain its visual geometry");
        }

        Assert(BuildSpecCatalog.For(BuildingDesignIds.GroundTurret).PlacementClearanceCells == 0,
            "ground turret should explicitly allow edge-touch placement");
        Assert(BuildSpecCatalog.For(BuildingDesignIds.AntiAirTurret).PlacementClearanceCells == 0,
            "anti-air turret should explicitly allow edge-touch placement");
        Assert(BuildSpecCatalog.Definitions.Values
                .Where(spec => spec.Kind is not (BuildingDesignIds.GroundTurret or BuildingDesignIds.AntiAirTurret))
                .All(spec => spec.PlacementClearanceCells == 1),
            "ordinary buildings should use the default one-cell placement clearance");

        var rejectedNegativeClearance = false;
        try
        {
            _ = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant) with { PlacementClearanceCells = -1 };
        }
        catch (ArgumentOutOfRangeException)
        {
            rejectedNegativeClearance = true;
        }

        Assert(rejectedNegativeClearance, "BuildSpec should reject negative placement clearance");

        AssertPlacementReservationMetadataAndRotation();
        AssertReservationPlacementKernel();
        AssertAuthoritativePlacementKernel();
        AssertLivePlacementAuthorityParity();
        AssertPlayerBuildGatewayPreservesDesiredPoint();
        AssertTypedOrderedEntityView();
        AssertPlacementQueryAllocatesZero();
        AssertRotatedBuildingFootprintLifecycle();
        AssertGridSafeStartingBases();

        Console.WriteLine("OK [placement-grid]: parity, four-way validation, four fixed plus 12 generated grid-safe bases, and blocking-unit clearance.");
    }


    private static void AssertAuthoritativePlacementKernel()
    {
        var owner = new OwnerId(1);
        var system = new ConstructionSystem();
        var powerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var groundTurretSpec = BuildSpecCatalog.For(BuildingDesignIds.GroundTurret);

        var clearWorld = CreatePlacementWorld(owner);
        var cardinal = system.QueryBuildingPlacement(
            clearWorld,
            owner,
            powerSpec,
            new Vector2(321, 321),
            0,
            ConstructionPlacementIntent.Direct);
        var rotated = system.QueryBuildingPlacement(
            clearWorld,
            owner,
            powerSpec,
            new Vector2(321, 321),
            Mathf.Pi * 0.5f,
            ConstructionPlacementIntent.Direct);
        var microRotation = system.QueryBuildingPlacement(
            clearWorld,
            owner,
            powerSpec,
            new Vector2(321, 321),
            0.01f,
            ConstructionPlacementIntent.Direct);
        Assert(cardinal.IsValid && cardinal.X == 320 && cardinal.Y == 336,
            $"authoritative 4x3 parity should snap to 320,336; got {cardinal}");
        Assert(rotated.IsValid && rotated.X == 336 && rotated.Y == 320,
            $"authoritative rotated 3x4 parity should snap to 336,320; got {rotated}");
        Assert(!microRotation.IsValid && microRotation.Reason == "placement.rotation"
            && microRotation.X == cardinal.X && microRotation.Y == cardinal.Y,
            $"micro rotation should retain the nearest cardinal parity anchor and reject first; got {microRotation}");

        var outside = system.QueryBuildingPlacement(
            clearWorld,
            owner,
            powerSpec,
            Vector2.Zero,
            0,
            ConstructionPlacementIntent.Direct);
        var outsideMicroRotation = system.QueryBuildingPlacement(
            clearWorld,
            owner,
            powerSpec,
            Vector2.Zero,
            0.01f,
            ConstructionPlacementIntent.Direct);
        Assert(!outside.IsValid && outside.Reason == "placement.outside", "hard footprint beyond the world edge should be outside");
        Assert(!outsideMicroRotation.IsValid && outsideMicroRotation.Reason == "placement.rotation",
            "rotation should outrank outside in the placement reason order");

        var overlapWorld = CreatePlacementWorld(owner);
        SpawnPlacementBuilding(overlapWorld, owner, powerSpec, new Vector2(320, 336));
        var blocked = system.QueryBuildingPlacement(
            overlapWorld,
            owner,
            powerSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!blocked.IsValid && blocked.Reason == "placement.blocked",
            $"hard overlap should be blocked before clearance; got {blocked}");

        var ordinaryGapWorld = CreatePlacementWorld(owner);
        var ordinaryObstacle = SpawnPlacementBuilding(
            ordinaryGapWorld,
            owner,
            powerSpec,
            new Vector2(479.999f, 336));
        var gapBelowCell = system.QueryBuildingPlacement(
            ordinaryGapWorld,
            owner,
            powerSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        ordinaryObstacle.Transform = ordinaryObstacle.Transform with { Position = new Vector2(480, 336) };
        var exactCellGap = system.QueryBuildingPlacement(
            ordinaryGapWorld,
            owner,
            powerSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!gapBelowCell.IsValid && gapBelowCell.Reason == "placement.clearance",
            $"31.999 ordinary gap should reject as clearance; got {gapBelowCell}");
        Assert(exactCellGap.IsValid, $"exact 32 ordinary gap should be valid; got {exactCellGap}");

        var ordinaryTurretWorld = CreatePlacementWorld(owner);
        var turretObstacle = SpawnPlacementBuilding(
            ordinaryTurretWorld,
            owner,
            groundTurretSpec,
            new Vector2(463.999f, 336));
        var ordinaryTurretBelow = system.QueryBuildingPlacement(
            ordinaryTurretWorld,
            owner,
            powerSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        turretObstacle.Transform = turretObstacle.Transform with { Position = new Vector2(464, 336) };
        var ordinaryTurretExact = system.QueryBuildingPlacement(
            ordinaryTurretWorld,
            owner,
            powerSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!ordinaryTurretBelow.IsValid && ordinaryTurretBelow.Reason == "placement.clearance",
            "ordinary candidate should contribute one-cell clearance against a turret");
        Assert(ordinaryTurretExact.IsValid, "ordinary-to-turret exact one-cell gap should be valid");

        var turretOrdinaryWorld = CreatePlacementWorld(owner);
        var ordinaryObstacleForTurret = SpawnPlacementBuilding(
            turretOrdinaryWorld,
            owner,
            powerSpec,
            new Vector2(447.999f, 304));
        var turretOrdinaryBelow = system.QueryBuildingPlacement(
            turretOrdinaryWorld,
            owner,
            groundTurretSpec,
            new Vector2(304, 304),
            0,
            ConstructionPlacementIntent.Direct);
        ordinaryObstacleForTurret.Transform = ordinaryObstacleForTurret.Transform with { Position = new Vector2(448, 304) };
        var turretOrdinaryExact = system.QueryBuildingPlacement(
            turretOrdinaryWorld,
            owner,
            groundTurretSpec,
            new Vector2(304, 304),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!turretOrdinaryBelow.IsValid && turretOrdinaryBelow.Reason == "placement.clearance",
            "existing ordinary building should contribute one-cell clearance against a turret candidate");
        Assert(turretOrdinaryExact.IsValid, "turret-to-ordinary exact one-cell gap should be valid independent of order");

        var turretTouchWorld = CreatePlacementWorld(owner);
        var touchingTurret = SpawnPlacementBuilding(
            turretTouchWorld,
            owner,
            groundTurretSpec,
            new Vector2(400, 304));
        var turretTouch = system.QueryBuildingPlacement(
            turretTouchWorld,
            owner,
            groundTurretSpec,
            new Vector2(304, 304),
            0,
            ConstructionPlacementIntent.Direct);
        touchingTurret.Transform = touchingTurret.Transform with { Position = new Vector2(399.999f, 304) };
        var turretOverlap = system.QueryBuildingPlacement(
            turretTouchWorld,
            owner,
            groundTurretSpec,
            new Vector2(304, 304),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(turretTouch.IsValid, "turret-to-turret hard footprints may touch with zero clearance");
        Assert(!turretOverlap.IsValid && turretOverlap.Reason == "placement.blocked",
            "turret-to-turret hard overlap must still be blocked");

        var visibilityOnlyWorld = CreatePlacementWorld(owner, buildRadius: 0, sightRange: 2000);
        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var directWithoutAuthority = system.QueryBuildingPlacement(
            visibilityOnlyWorld,
            owner,
            hqSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        var readyWithoutAuthority = system.QueryBuildingPlacement(
            visibilityOnlyWorld,
            owner,
            hqSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.ReadyTicket);
        Assert(directWithoutAuthority.IsValid, "Direct intent should follow a spec that does not require build authority");
        Assert(!readyWithoutAuthority.IsValid && readyWithoutAuthority.Reason == "placement.outsideBuildRadius",
            "ReadyTicket intent should force build authority");

        var unpoweredWorld = CreatePlacementWorld(owner, powered: false);
        var unpowered = system.QueryBuildingPlacement(
            unpoweredWorld,
            owner,
            powerSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!unpowered.IsValid && unpowered.Reason == "placement.unpowered",
            "unpowered authority should outrank terrain, visibility, and collision reasons");

        var hiddenWorld = CreatePlacementWorld(owner, sightRange: 8);
        var hidden = system.QueryBuildingPlacement(
            hiddenWorld,
            owner,
            powerSpec,
            new Vector2(320, 336),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!hidden.IsValid && hidden.Reason == "placement.notVisible",
            $"both intents should use simulation visibility; got {hidden}");

        var waterWorld = CreatePlacementWorld(owner);
        var impassable = system.QueryBuildingPlacement(
            waterWorld,
            owner,
            powerSpec,
            new Vector2(864, 128),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!impassable.IsValid && impassable.Reason == "placement.impassable",
            $"land building over the deterministic lagoon should be impassable; got {impassable}");
    }

    private static void AssertGridSafeStartingBases()
    {
        foreach (var owner in new[] { Owner.Player, Owner.Enemy })
        {
            foreach (var faction in new[] { FactionId.Dog, FactionId.Cat })
            {
                var loadout = MatchStartLoadouts.For(owner, faction);
                var ownerId = new OwnerId(owner == Owner.Player ? 1 : 2);
                var map = new MapSpec
                {
                    Id = $"placement-grid.{owner}.{faction}",
                    Seed = 535,
                    WorldSize = new MapSize(3600, 2400),
                    OwnerStarts =
                    [
                        new(ownerId, faction, loadout.Buildings[0].Position.ToMapPoint(), loadout.Buildings[0].Facing, 0),
                    ],
                    Buildings = loadout.Buildings
                        .Select(building => new MapBuildingSeedSpec(
                            building.Kind,
                            ownerId,
                            faction,
                            building.Position.ToMapPoint(),
                            building.Facing))
                        .ToArray(),
                    Units = loadout.Units
                        .Select(unit => new MapUnitSeedSpec(
                            unit.DesignId,
                            ownerId,
                            unit.Position.ToMapPoint(),
                            unit.Facing))
                        .ToArray(),
                };

                AssertGridSafeStartingMap(map, $"{owner}/{faction} starting base");
            }
        }

        foreach (var seed in new[] { 535, 10535, SkirmishOptions.SandboxMapSeed })
        {
            foreach (var factions in new[]
            {
                (Player: FactionId.Dog, Enemy: FactionId.Cat),
                (Player: FactionId.Cat, Enemy: FactionId.Dog),
            })
            {
                var config = MatchConfig.Default with
                {
                    MapSeed = seed,
                    PlayerFaction = factions.Player,
                    AiFaction = factions.Enemy,
                };
                var generated = SkirmishMapGenerator.GenerateSpec(config);
                AssertGridSafeStartingMap(
                    generated,
                    $"generated seed {seed} {factions.Player}/{factions.Enemy}");
            }
        }

        var unsafeEnemyDog = SkirmishMapGenerator.GenerateSpec(MatchConfig.Default with
        {
            PlayerFaction = FactionId.Cat,
            AiFaction = FactionId.Dog,
        });
        unsafeEnemyDog = unsafeEnemyDog with
        {
            Buildings = unsafeEnemyDog.Buildings
                .Select(building => building.OwnerId == new OwnerId(2) && building.Kind == BuildingDesignIds.Barracks
                    ? building with { Position = new MapPoint(2848, 1280) }
                    : building)
                .ToArray(),
            Units = unsafeEnemyDog.Units
                .Select(unit => unit.OwnerId == new OwnerId(2) && unit.DesignId == "dog.harvester"
                    ? unit with { Position = new MapPoint(2768, 1248) }
                    : unit)
                .ToArray(),
        };
        var regressionConflicts = InitialBlockingUnitBuildingConflicts(unsafeEnemyDog);
        Assert(regressionConflicts.Any(conflict =>
                conflict.Contains("owner=2 faction=Dog unit=dog.harvester", StringComparison.Ordinal)
                && conflict.Contains($"building={BuildingDesignIds.Barracks}", StringComparison.Ordinal)),
            "initial blocking-unit diagnostics should identify the owner, faction, unit, and building for the former Enemy/Dog overlap");
    }

    private static void AssertGridSafeStartingMap(MapSpec map, string label)
    {
        var buildingConflicts = MapBuildingPlacementValidator.Validate(map);
        Assert(buildingConflicts.Count == 0,
            $"{label} should be snapped, cardinal, inside, non-overlapping, and 32-unit clear; got {string.Join("; ", buildingConflicts)}");

        var unitConflicts = InitialBlockingUnitBuildingConflicts(map);
        Assert(unitConflicts.Count == 0,
            $"{label} blocking units should clear every building hard footprint and reservation; got {string.Join("; ", unitConflicts)}");

        var reservationConflicts = InitialBuildingReservationConflicts(map);
        Assert(reservationConflicts.Count == 0,
            $"{label} buildings should keep hard footprints and reservations mutually clear; got {string.Join("; ", reservationConflicts)}");
    }


    private static void AssertRotatedBuildingFootprintLifecycle()
    {
        var owner = new OwnerId(1);
        var facing = Mathf.Pi * 0.5f;
        var world = new EntityWorld(seed: 528) { WorldWidth = 800, WorldHeight = 600 };
        world.AddSystem(new ConstructionSystem());
        world.ResourceInventory(owner).Credits = 900;

        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var hq = world.Spawn(
            hqSpec.ToEntitySpec(),
            owner,
            EntityTransform.At(new Vector2(320, 240)),
            new EntityComponentState[]
            {
                new ConstructionIdentityComponentState(BuildingDesignIds.Headquarters),
                new HealthComponentState(hqSpec.MaxHp, hqSpec.MaxHp),
                new VisionComponentState(hqSpec.SightRange),
                new FootprintComponentState(hqSpec.LogicalFootprint(), hqSpec.PlacementDomain),
                new ConstructionComponentState(Progress: 1, BuildTime: hqSpec.BuildTime, Cost: hqSpec.Cost, RefundRatio: hqSpec.RefundRatio),
                new PowerComponentState(hqSpec.PowerProvided, hqSpec.PowerUsed, Powered: true),
                new BuildRadiusComponentState(hqSpec.BuildRadius),
            });

        var powerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var previewFootprint = powerSpec.LogicalFootprint(facing);
        Assert(previewFootprint == new Vector2(96, 128),
            $"rotated placement preview should use 96x128 logical footprint, got {previewFootprint}");

        var command = new StartConstructionEntityCommand(
            owner,
            [hq.Id],
            1,
            BuildingDesignIds.PowerPlant,
            new Vector2(520, 248),
            facing);
        var buffer = new EntityCommandBuffer();
        buffer.Enqueue(command);
        var clock = new SimClock();
        world.Step(1, clock.FixedDelta, buffer.DrainUpToTick(1));

        var rejection = world.Events.Drain()
            .OfType<ConstructionRejectedEvent>()
            .FirstOrDefault();
        Assert(rejection is null, $"rotated building placement should succeed, got {rejection?.Reason}");

        var spawned = world.OrderedEntities.Single(entity => entity.Id.Value != hq.Id.Value);
        var runtimeFootprint = spawned.Components.Require<FootprintComponentState>();
        Assert(spawned.Transform.Position == new Vector2(528, 256),
            $"rotated building should use 3x4 anchor parity, got {spawned.Transform.Position}");
        Assert(Math.Abs(spawned.Transform.Facing - facing) < 0.0001f,
            $"rotated building should preserve quarter-turn facing, got {spawned.Transform.Facing}");
        Assert(runtimeFootprint.Size == previewFootprint,
            $"runtime footprint {runtimeFootprint.Size} should match preview/validation footprint {previewFootprint}");
    }
}
