static partial class Program
{
    static void AssertConstructionDestroyedLifecycle()
    {
        const int ticks = 4;
        var owner = new OwnerId(1);
        var rebuildPosition = new Vector2(352, 160);

        EntityWorld BuildWorld()
        {
            var world = new EntityWorld(seed: 6475) { WorldWidth = 1200, WorldHeight = 900 };
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(owner).Credits = 900;

            SpawnCompleted(world, owner, BuildingDesignIds.Headquarters, new Vector2(160, 160));
            SpawnConstruction(world, owner, BuildingDesignIds.PowerPlant, rebuildPosition, hp: 0, progress: 0.35f, ConstructionPhase.Building);
            SpawnConstruction(world, owner, BuildingDesignIds.Barracks, new Vector2(544, 160), hp: -5, progress: 1, ConstructionPhase.Building);
            world.Spawn(RestartObjectiveSpec(), OwnerId.None, EntityTransform.At(new Vector2(160, 448)), RestartObjectiveComponents());
            return world;
        }

        var commands = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(owner, [new EntityId(1)], 2, BuildingDesignIds.PowerPlant, rebuildPosition),
        };

        AssertDeterministic("construction-destroyed-lifecycle", BuildWorld, commands, ticks, 1);

        var world = BuildWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var destroyed = new List<ConstructionDestroyedEvent>();
        var entityDestroyed = new List<EntityDestroyedEvent>();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            foreach (var evt in world.Events.Drain())
            {
                if (evt is ConstructionDestroyedEvent constructionDestroyed)
                {
                    destroyed.Add(constructionDestroyed);
                }
                else if (evt is EntityDestroyedEvent genericDestroyed)
                {
                    entityDestroyed.Add(genericDestroyed);
                }
            }
        }

        var ids = world.OrderedEntities.Select(entity => entity.Id.Value).ToHashSet();
        var replacement = world.OrderedEntities.SingleOrDefault(entity =>
            entity.Id.Value > 4 && entity.SpecId == "building.powerplant");
        Assert(destroyed.Count == 3, $"dead construction entities should emit three ConstructionDestroyedEvents, got {destroyed.Count}.");
        Assert(entityDestroyed.Count == 3, $"dead construction entities should emit three EntityDestroyedEvents, got {entityDestroyed.Count}.");
        Assert(!ids.Contains(2) && !ids.Contains(3) && !ids.Contains(4), "dead construction entities should be removed from EntityWorld.");
        Assert(replacement is not null, "destroyed footprint should be released so replacement construction can start.");
        Assert(world.ResourceInventory(owner).Credits == 600, $"replacement construction should spend exactly once, got {world.ResourceInventory(owner).Credits}.");
        Assert(destroyed.Any(evt => evt.BuildingSpecId == BuildingDesignIds.PowerPlant && evt.Progress < 1),
            "under-construction destroyed event should keep progress evidence.");
        Assert(destroyed.Any(evt => evt.Phase == ConstructionPhase.RestartCapture),
            "restart/capture destroyed event should keep phase evidence.");

        Console.WriteLine($"OK [construction-destroyed-lifecycle]: destroyed {destroyed.Count}, replacement {replacement!.Id.Value}, credits {world.ResourceInventory(owner).Credits}.");
    }

    private static void SpawnCompleted(EntityWorld world, OwnerId owner, string kind, Vector2 position)
    {
        var spec = BuildSpecCatalog.For(kind);
        world.Spawn(spec.ToEntitySpec(), owner, EntityTransform.At(position), CompletedComponents(spec));
    }

    private static void SpawnConstruction(
        EntityWorld world,
        OwnerId owner,
        string kind,
        Vector2 position,
        float hp,
        float progress,
        ConstructionPhase phase)
    {
        var spec = BuildSpecCatalog.For(kind);
        var components = CompletedComponents(spec).ToList();
        components.RemoveAll(component => component is HealthComponentState or ConstructionComponentState);
        components.Add(new HealthComponentState(hp, spec.MaxHp));
        components.Add(new ConstructionComponentState(progress, spec.BuildTime, spec.Cost, spec.RefundRatio, Phase: phase));
        world.Spawn(spec.ToEntitySpec(), owner, EntityTransform.At(position), components);
    }

    private static IEnumerable<EntityComponentState> CompletedComponents(BuildSpec spec)
    {
        yield return new ConstructionIdentityComponentState(spec.Kind);
        yield return new HealthComponentState(spec.MaxHp, spec.MaxHp);
        yield return new VisionComponentState(spec.SightRange);
        yield return new FootprintComponentState(spec.Footprint, spec.PlacementDomain);
        yield return new ConstructionComponentState(Progress: 1, BuildTime: spec.BuildTime, Cost: spec.Cost, RefundRatio: spec.RefundRatio);
        yield return new PowerComponentState(spec.PowerProvided, spec.PowerUsed, Powered: true);
        if (spec.BuildRadius > 0)
        {
            yield return new BuildRadiusComponentState(spec.BuildRadius);
        }
    }

    private static EntitySpec RestartObjectiveSpec()
    {
        return new EntitySpec
        {
            Id = "replay.destroyed_restart_objective",
            Kind = EntityKind.Objective,
            Display = new EntityDisplaySpec("Destroyed Restart Objective", "destroyed.restart.name", "destroyed.restart.role", "DRS", IconGlyph.Building),
        };
    }

    private static IEnumerable<EntityComponentState> RestartObjectiveComponents()
    {
        yield return new HealthComponentState(0, 240);
        yield return new ConstructionComponentState(Progress: 0.4f, BuildTime: 2, Phase: ConstructionPhase.RestartCapture);
        yield return new SignalNetworkComponentState(SignalNodeKind.SignalTower, 180, 260);
    }
}
