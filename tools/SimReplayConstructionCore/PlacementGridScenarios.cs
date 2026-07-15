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

    private static void AssertPlacementReservationMetadataAndRotation()
    {
        var expected = new[]
        {
            (Kind: BuildingDesignIds.Barracks, ReservationKind: PlacementReservationKind.ProductionEgress, Center: 96f),
            (Kind: BuildingDesignIds.VehicleFactory, ReservationKind: PlacementReservationKind.ProductionEgress, Center: 144f),
            (Kind: BuildingDesignIds.Airfield, ReservationKind: PlacementReservationKind.ProductionEgress, Center: 144f),
            (Kind: BuildingDesignIds.Refinery, ReservationKind: PlacementReservationKind.RefineryDock, Center: 128f),
        };
        var origin = new Vector2(512, 384);
        var facings = new[] { 0f, Mathf.Pi * 0.5f, Mathf.Pi, Mathf.Pi * 1.5f };
        var directions = new[] { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up };
        foreach (var entry in expected)
        {
            var spec = BuildSpecCatalog.For(entry.Kind);
            Assert(spec.PlacementReservations.Count == 1,
                $"{entry.Kind} should declare exactly one placement reservation");
            Assert(spec.PlacementReservations[0].Kind == entry.ReservationKind,
                $"{entry.Kind} should declare {entry.ReservationKind}");
            for (var rotation = 0; rotation < facings.Length; rotation++)
            {
                Assert(PlacementReservationMath.TryCenter(
                        spec,
                        entry.ReservationKind,
                        origin,
                        facings[rotation],
                        out var center),
                    $"{entry.Kind} reservation should resolve at cardinal rotation {rotation}");
                var expectedCenter = origin + directions[rotation] * entry.Center;
                Assert(center.DistanceTo(expectedCenter) < 0.001f,
                    $"{entry.Kind} rotation {rotation} center should be {expectedCenter}, got {center}");
            }
        }

        foreach (var spec in BuildSpecCatalog.Definitions.Values)
        {
            if (spec.Kind is BuildingDesignIds.Barracks
                or BuildingDesignIds.VehicleFactory
                or BuildingDesignIds.Airfield
                or BuildingDesignIds.Refinery)
            {
                continue;
            }

            Assert(spec.PlacementReservations.Count == 0,
                $"{spec.Kind} should expose the shared empty reservation array");
        }
    }

    private static void AssertReservationPlacementKernel()
    {
        var owner = new OwnerId(1);
        var system = new ConstructionSystem();
        var barracks = BuildSpecCatalog.For(BuildingDesignIds.Barracks);
        var power = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);

        var outsideWorld = CreatePlacementWorld(owner);
        var outside = system.QueryBuildingPlacement(
            outsideWorld,
            owner,
            barracks,
            new Vector2(928, 320),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!outside.IsValid && outside.Reason == "placement.outside",
            $"reservation extent outside the world should reject before later reasons; got {outside}");

        var hiddenWorld = CreatePlacementWorld(owner, sightRange: 100);
        var hidden = system.QueryBuildingPlacement(
            hiddenWorld,
            owner,
            barracks,
            new Vector2(512, 384),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!hidden.IsValid && hidden.Reason == "placement.notVisible",
            $"visible hard footprint with hidden egress should reject as not visible; got {hidden}");

        var terrainWorld = CreatePlacementWorld(owner);
        var hardOnlyBarracks = barracks with
        {
            PlacementReservations = Array.Empty<PlacementReservationSpec>(),
        };
        var foundReservationOnlyTerrainFailure = false;
        for (var y = 64f; !foundReservationOnlyTerrainFailure && y <= 704; y += PlacementMath.GridSize)
        {
            for (var x = 64f; x <= 960; x += PlacementMath.GridSize)
            {
                var desired = new Vector2(x, y);
                var hardOnly = system.QueryBuildingPlacement(
                    terrainWorld,
                    owner,
                    hardOnlyBarracks,
                    desired,
                    0,
                    ConstructionPlacementIntent.Direct);
                var reserved = system.QueryBuildingPlacement(
                    terrainWorld,
                    owner,
                    barracks,
                    desired,
                    0,
                    ConstructionPlacementIntent.Direct);
                if (hardOnly.IsValid && !reserved.IsValid && reserved.Reason == "placement.impassable")
                {
                    foundReservationOnlyTerrainFailure = true;
                    break;
                }
            }
        }

        Assert(foundReservationOnlyTerrainFailure,
            "reservation terrain samples should reject at least one grid point whose hard footprint remains passable");

        var candidateReservationWorld = CreatePlacementWorld(owner);
        var existingPower = SpawnPlacementBuilding(
            candidateReservationWorld,
            owner,
            power,
            new Vector2(543.999f, 336));
        var candidateReservationBelow = system.QueryBuildingPlacement(
            candidateReservationWorld,
            owner,
            barracks,
            new Vector2(320, 320),
            0,
            ConstructionPlacementIntent.Direct);
        var candidateReservationReady = system.QueryBuildingPlacement(
            candidateReservationWorld,
            owner,
            barracks,
            new Vector2(320, 320),
            0,
            ConstructionPlacementIntent.ReadyTicket);
        existingPower.Transform = existingPower.Transform with { Position = new Vector2(544, 336) };
        var candidateReservationExact = system.QueryBuildingPlacement(
            candidateReservationWorld,
            owner,
            barracks,
            new Vector2(320, 320),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!candidateReservationBelow.IsValid && candidateReservationBelow.Reason == "placement.reserved",
            $"candidate reservation to existing hard gap below 32 should reject as reserved; got {candidateReservationBelow}");
        Assert(!candidateReservationReady.IsValid && candidateReservationReady.Reason == "placement.reserved",
            "Direct and ReadyTicket should share reservation rejection semantics");
        Assert(candidateReservationExact.IsValid,
            $"candidate reservation to existing hard exact 32 gap should be valid; got {candidateReservationExact}");

        var existingReservationWorld = CreatePlacementWorld(owner);
        var existingBarracks = SpawnPlacementBuilding(
            existingReservationWorld,
            owner,
            barracks,
            new Vector2(320.001f, 320));
        var existingReservationBelow = system.QueryBuildingPlacement(
            existingReservationWorld,
            owner,
            power,
            new Vector2(544, 336),
            0,
            ConstructionPlacementIntent.Direct);
        existingBarracks.Transform = existingBarracks.Transform with { Position = new Vector2(320, 320) };
        var existingReservationExact = system.QueryBuildingPlacement(
            existingReservationWorld,
            owner,
            power,
            new Vector2(544, 336),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!existingReservationBelow.IsValid && existingReservationBelow.Reason == "placement.reserved",
            $"candidate hard to existing reservation gap below 32 should reject as reserved; got {existingReservationBelow}");
        Assert(existingReservationExact.IsValid,
            $"candidate hard to existing reservation exact 32 gap should be valid; got {existingReservationExact}");

        var reservationPairWorld = CreatePlacementWorld(owner);
        var reservationPairObstacle = SpawnPlacementBuilding(
            reservationPairWorld,
            owner,
            barracks,
            new Vector2(607.999f, 320),
            Mathf.Pi);
        var reservationPairBelow = system.QueryBuildingPlacement(
            reservationPairWorld,
            owner,
            barracks,
            new Vector2(320, 320),
            0,
            ConstructionPlacementIntent.Direct);
        reservationPairObstacle.Transform = reservationPairObstacle.Transform with { Position = new Vector2(608, 320) };
        var reservationPairExact = system.QueryBuildingPlacement(
            reservationPairWorld,
            owner,
            barracks,
            new Vector2(320, 320),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!reservationPairBelow.IsValid && reservationPairBelow.Reason == "placement.reserved",
            $"reservation-to-reservation 31.999 gap should reject as reserved; got {reservationPairBelow}");
        Assert(reservationPairExact.IsValid,
            $"reservation-to-reservation exact 32 gap should be valid; got {reservationPairExact}");

        var blockedPriorityWorld = CreatePlacementWorld(owner);
        SpawnPlacementBuilding(blockedPriorityWorld, owner, barracks, new Vector2(320, 320));
        var blockedPriority = system.QueryBuildingPlacement(
            blockedPriorityWorld,
            owner,
            barracks,
            new Vector2(320, 320),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!blockedPriority.IsValid && blockedPriority.Reason == "placement.blocked",
            "hard overlap should outrank reservation conflicts");

        var clearancePriorityWorld = CreatePlacementWorld(owner);
        SpawnPlacementBuilding(clearancePriorityWorld, owner, barracks, new Vector2(479.999f, 320));
        var clearancePriority = system.QueryBuildingPlacement(
            clearancePriorityWorld,
            owner,
            barracks,
            new Vector2(320, 320),
            0,
            ConstructionPlacementIntent.Direct);
        Assert(!clearancePriority.IsValid && clearancePriority.Reason == "placement.clearance",
            "hard clearance should outrank reservation conflicts");
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

    private static IReadOnlyList<string> InitialBlockingUnitBuildingConflicts(MapSpec map)
    {
        var conflicts = new List<string>();
        foreach (var unit in map.Units)
        {
            var unitSpec = UnitDesignCatalog.Spec(unit.DesignId);
            if (!unitSpec.Collision.BlocksMovement || unitSpec.Collision.Radius <= 0)
            {
                continue;
            }

            var faction = map.OwnerStarts.First(start => start.OwnerId == unit.OwnerId).Faction;
            foreach (var building in map.Buildings)
            {
                var buildingSpec = BuildSpecCatalog.For(building.Kind);
                PlacementMath.TryNormalizeCardinalFacing(building.Facing, out var cardinalFacing);
                var footprint = buildingSpec.LogicalFootprint(cardinalFacing);
                var rect = PlacementMath.RectFromCenter(
                    building.Position.X,
                    building.Position.Y,
                    footprint.X,
                    footprint.Y);
                var conflictsWithBuilding = CircleIntersectsRect(unit.Position, unitSpec.Collision.Radius, rect);
                for (var reservationIndex = 0;
                     !conflictsWithBuilding && reservationIndex < buildingSpec.PlacementReservations.Count;
                     reservationIndex++)
                {
                    var reservation = PlacementReservationMath.WorldRect(
                        buildingSpec,
                        buildingSpec.PlacementReservations[reservationIndex],
                        building.Position.ToVector2(),
                        cardinalFacing);
                    conflictsWithBuilding = CircleIntersectsRect(unit.Position, unitSpec.Collision.Radius, reservation);
                }

                if (!conflictsWithBuilding
                    && PlacementReservationMath.TryCenter(
                        buildingSpec,
                        PlacementReservationKind.ProductionEgress,
                        building.Position.ToVector2(),
                        cardinalFacing,
                        out var egress))
                {
                    var producerFaction = ProductionKindDesignBridge.UnitFactionFor(faction);
                    var spawnRadius = UnitDesignCatalog.Designs.Values
                        .Where(design => design.Faction == producerFaction
                            && design.Production?.ProducerKind == building.Kind)
                        .Select(design => design.Collision.Radius)
                        .DefaultIfEmpty(0)
                        .Max();
                    var requiredDistance = spawnRadius + unitSpec.Collision.Radius + 6;
                    conflictsWithBuilding = unit.Position.ToVector2().DistanceSquaredTo(egress)
                        < requiredDistance * requiredDistance;
                }

                if (!conflictsWithBuilding)
                {
                    continue;
                }

                conflicts.Add(
                    $"owner={unit.OwnerId.Value} faction={faction} unit={unit.DesignId} "
                    + $"building={building.Kind}@owner={building.OwnerId.Value}");
            }
        }

        return conflicts;
    }

    private static IReadOnlyList<string> InitialBuildingReservationConflicts(MapSpec map)
    {
        var conflicts = new List<string>();
        for (var firstIndex = 0; firstIndex < map.Buildings.Count; firstIndex++)
        {
            var first = map.Buildings[firstIndex];
            var firstSpec = BuildSpecCatalog.For(first.Kind);
            PlacementMath.TryNormalizeCardinalFacing(first.Facing, out var firstFacing);
            var firstHard = PlacementMath.RectFromCenter(
                first.Position.X,
                first.Position.Y,
                firstSpec.LogicalFootprint(firstFacing).X,
                firstSpec.LogicalFootprint(firstFacing).Y);
            for (var secondIndex = firstIndex + 1; secondIndex < map.Buildings.Count; secondIndex++)
            {
                var second = map.Buildings[secondIndex];
                var secondSpec = BuildSpecCatalog.For(second.Kind);
                PlacementMath.TryNormalizeCardinalFacing(second.Facing, out var secondFacing);
                var secondSize = secondSpec.LogicalFootprint(secondFacing);
                var secondHard = PlacementMath.RectFromCenter(
                    second.Position.X,
                    second.Position.Y,
                    secondSize.X,
                    secondSize.Y);
                var clearance = Math.Max(firstSpec.PlacementClearanceCells, secondSpec.PlacementClearanceCells)
                    * PlacementMath.GridSize;
                var hasConflict = false;
                for (var reservationIndex = 0;
                     !hasConflict && reservationIndex < firstSpec.PlacementReservations.Count;
                     reservationIndex++)
                {
                    var reservation = PlacementReservationMath.WorldRect(
                        firstSpec,
                        firstSpec.PlacementReservations[reservationIndex],
                        first.Position.ToVector2(),
                        firstFacing);
                    hasConflict = ReservationRectsConflict(reservation, secondHard, clearance);
                    for (var otherIndex = 0;
                         !hasConflict && otherIndex < secondSpec.PlacementReservations.Count;
                         otherIndex++)
                    {
                        var other = PlacementReservationMath.WorldRect(
                            secondSpec,
                            secondSpec.PlacementReservations[otherIndex],
                            second.Position.ToVector2(),
                            secondFacing);
                        hasConflict = ReservationRectsConflict(reservation, other, clearance);
                    }
                }

                for (var reservationIndex = 0;
                     !hasConflict && reservationIndex < secondSpec.PlacementReservations.Count;
                     reservationIndex++)
                {
                    var reservation = PlacementReservationMath.WorldRect(
                        secondSpec,
                        secondSpec.PlacementReservations[reservationIndex],
                        second.Position.ToVector2(),
                        secondFacing);
                    hasConflict = ReservationRectsConflict(firstHard, reservation, clearance);
                }

                if (hasConflict)
                {
                    conflicts.Add($"{first.Kind}@{first.Position} <-> {second.Kind}@{second.Position}");
                }
            }
        }

        return conflicts;
    }

    private static bool ReservationRectsConflict(PlacementRect first, PlacementRect second, float clearance)
    {
        var xGap = first.EndX <= second.X
            ? second.X - first.EndX
            : second.EndX <= first.X
                ? first.X - second.EndX
                : 0;
        var yGap = first.EndY <= second.Y
            ? second.Y - first.EndY
            : second.EndY <= first.Y
                ? first.Y - second.EndY
                : 0;
        var overlaps = first.X < second.EndX
            && first.EndX > second.X
            && first.Y < second.EndY
            && first.EndY > second.Y;
        return overlaps || (clearance > 0 && xGap < clearance && yGap < clearance);
    }

    private static bool CircleIntersectsRect(MapPoint center, float radius, PlacementRect rect)
    {
        var closestX = Math.Clamp(center.X, rect.X, rect.EndX);
        var closestY = Math.Clamp(center.Y, rect.Y, rect.EndY);
        var deltaX = center.X - closestX;
        var deltaY = center.Y - closestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
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
