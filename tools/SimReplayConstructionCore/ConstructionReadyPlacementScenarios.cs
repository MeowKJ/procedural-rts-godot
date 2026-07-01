static partial class Program
{
    static void AssertConstructionReadyPlacement()
    {
        const int Ticks = 235;
        var owner = new OwnerId(1);
        var ticketId = new EntityId(3);

        EntityWorld BuildReadyPlacementWorld()
        {
            var world = new EntityWorld(seed: 6471) { WorldWidth = 1200, WorldHeight = 900 };
            world.AddSystem(new ConstructionSystem());
            world.AddSystem(new PowerSystem());
            world.ResourceInventory(owner).Credits = 1000;
            SpawnCompleted(world, owner, BuildingDesignIds.Headquarters, new Vector2(180, 180));
            SpawnCompleted(world, owner, BuildingDesignIds.PowerPlant, new Vector2(180, 500));
            return world;
        }

        var log = new List<EntityCommand>
        {
            new QueueConstructionEntityCommand(owner, [new EntityId(1)], 1, BuildingDesignIds.Barracks),
            new StartConstructionEntityCommand(owner, [new EntityId(1)], 220, BuildingDesignIds.Barracks, new Vector2(1000, 180), ReadyTicket: ticketId),
            new StartConstructionEntityCommand(owner, [new EntityId(1)], 225, BuildingDesignIds.Barracks, new Vector2(360, 180), ReadyTicket: ticketId),
        };

        AssertDeterministic("construction-ready-placement", BuildReadyPlacementWorld, log, Ticks, 19);

        var world = BuildReadyPlacementWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var rejected = new List<ConstructionRejectedEvent>();
        foreach (var command in log)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= Ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            rejected.AddRange(world.Events.Drain().OfType<ConstructionRejectedEvent>());
        }

        var credits = world.ResourceInventory(owner).Credits;
        var buildings = world.OrderedEntities
            .Where(entity => entity.Components.TryGet<ConstructionComponentState>(out var construction)
                && construction.Phase == ConstructionPhase.Building)
            .OrderBy(entity => entity.Id.Value)
            .ToArray();
        var buildingList = string.Join(", ", buildings.Select(entity => $"{entity.Id.Value}:{entity.SpecId}"));
        var rejectionList = string.Join(", ", rejected.Select(rejection => $"{rejection.Tick}:{rejection.Reason}"));
        var placed = buildings.SingleOrDefault(entity => entity.SpecId == "building.barracks");
        Assert(placed is not null, $"ready-ticket placement should spawn barracks; buildings [{buildingList}], rejected [{rejectionList}]");
        var placedConstruction = placed!.Components.Require<ConstructionComponentState>();

        Assert(rejected.Any(rejection => rejection.Tick == 220 && rejection.Reason == "placement.outsideBuildRadius"),
            "invalid ready-ticket placement should reject without consuming the ticket");
        Assert(!world.TryGet(ticketId, out _), "successful ready-ticket placement should consume the queue ticket");
        Assert(buildings.Length == 3, $"HQ, power plant, and placed barracks should remain as buildings, got {buildings.Length}");
        Assert(placedConstruction.Progress >= 1 && placedConstruction.Phase == ConstructionPhase.Building,
            $"ready-ticket placement should create a complete building, got {placedConstruction.Progress:0.000}/{placedConstruction.Phase}");
        Assert(placed.Components.Has<FootprintComponentState>(), "ready-ticket placement should create a real footprint only after placement");
        Assert(credits == 580, $"ready-ticket placement should spend credits once at queue time only, got {credits}");

        Console.WriteLine($"OK [construction-ready-placement]: rejected {rejected.Count}, placed {placed.Id.Value}, credits {credits}.");

        static void SpawnCompleted(EntityWorld world, OwnerId owner, string kind, Vector2 position)
        {
            var spec = BuildSpecCatalog.For(kind);
            var components = new List<EntityComponentState>
            {
                new ConstructionIdentityComponentState(kind),
                new HealthComponentState(spec.MaxHp, spec.MaxHp),
                new VisionComponentState(spec.SightRange),
                new FootprintComponentState(spec.Footprint, spec.PlacementDomain),
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
}
