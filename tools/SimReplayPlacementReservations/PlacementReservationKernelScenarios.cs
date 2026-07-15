using Godot;

static partial class Program
{
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
}
