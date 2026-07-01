static partial class Program
{
    static void AssertDogBuildAuthority()
    {
        AssertDogEngineerCarriesBuildRadius();
        AssertDeployCoreBuildAuthority();
    }

    private static void AssertDogEngineerCarriesBuildRadius()
    {
        const int ticks = 4;
        var owner = new OwnerId(1);
        var dogEngineerSpec = UnitDesignCatalog.Spec("dog.engineer");
        Assert(dogEngineerSpec.Abilities.Any(ability => ability.Kind == AbilityKind.Build && ability.Radius == 220),
            "Dog engineer should author build authority as AbilityKind.Build data.");

        EntityWorld BuildWorld()
        {
            var world = new EntityWorld(seed: 6472) { WorldWidth = 2100, WorldHeight = 1300 };
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(owner).Credits = 1000;
            SpawnCompleted(world, owner, BuildingDesignIds.Headquarters, new Vector2(120, 120), includeBuildRadius: true);
            world.SpawnUnit(dogEngineerSpec, owner, new Vector2(1100, 608));
            return world;
        }

        var commands = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(owner, [new EntityId(2)], 1, BuildingDesignIds.PowerPlant, new Vector2(1248, 608)),
            new StartConstructionEntityCommand(owner, [new EntityId(2)], 2, BuildingDesignIds.PowerPlant, new Vector2(1540, 608)),
        };

        AssertDeterministic("dog-build-authority", BuildWorld, commands, ticks, 1);

        var world = BuildWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var rejected = new List<ConstructionRejectedEvent>();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            rejected.AddRange(world.Events.Drain().OfType<ConstructionRejectedEvent>());
        }

        var engineer = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var engineerRadius = engineer.Components.Require<BuildRadiusComponentState>();
        var engineerRuntime = engineer.Components.Require<AbilityRuntimeComponentState>();
        var acceptedPowerPlants = world.OrderedEntities
            .Where(entity => entity.SpecId == "building.powerplant")
            .ToArray();

        Assert(engineerRadius.Radius == 220, $"Dog engineer build radius should come from Build ability, got {engineerRadius.Radius}.");
        Assert(engineerRuntime.Cooldowns.All(cooldown => cooldown.Kind != AbilityKind.Build),
            "Build ability should be passive build authority, not an active cooldown entry.");
        Assert(acceptedPowerPlants.Length == 1, $"Dog engineer should authorize one forward power plant, got {acceptedPowerPlants.Length}.");
        Assert(rejected.Any(rejection => rejection.Reason == "placement.outsideBuildRadius"),
            "Dog engineer should reject placement outside its carried build radius.");
        Assert(world.ResourceInventory(owner).Credits == 700, $"Only the accepted Dog build should spend credits, got {world.ResourceInventory(owner).Credits}.");

        Console.WriteLine($"OK [dog-build-authority]: engineer radius {engineerRadius.Radius}, accepted {acceptedPowerPlants.Length}, rejected {rejected.Count}.");
    }

    private static void AssertDeployCoreBuildAuthority()
    {
        const int ticks = 20;
        var owner = new OwnerId(1);

        EntityWorld BuildWorld()
        {
            var world = new EntityWorld(seed: 6473) { WorldWidth = 2200, WorldHeight = 1400 };
            world.AddSystem(new AbilitySystem());
            world.AddSystem(new ConstructionSystem());
            world.ResourceInventory(owner).Credits = 1000;
            SpawnCompleted(world, owner, BuildingDesignIds.Headquarters, new Vector2(120, 700), includeBuildRadius: false);
            world.Spawn(DeployCoreSpec(), owner, EntityTransform.At(new Vector2(1200, 700)), DeployCoreComponents());
            return world;
        }

        var commands = new List<EntityCommand>
        {
            new StartConstructionEntityCommand(owner, [new EntityId(2)], 1, BuildingDesignIds.PowerPlant, new Vector2(1376, 700)),
            new AbilityEntityCommand(owner, [new EntityId(2)], 2, AbilityKind.Deploy),
            new StartConstructionEntityCommand(owner, [new EntityId(2)], 2, BuildingDesignIds.PowerPlant, new Vector2(1376, 700)),
            new StartConstructionEntityCommand(owner, [new EntityId(2)], 12, BuildingDesignIds.PowerPlant, new Vector2(1376, 700)),
        };

        AssertDeterministic("deploy-build-authority", BuildWorld, commands, ticks, 2);

        var world = BuildWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        var rejected = new List<ConstructionRejectedEvent>();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            rejected.AddRange(world.Events.Drain().OfType<ConstructionRejectedEvent>());
        }

        var deployCore = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var deploy = deployCore.Components.Require<DeployComponentState>();
        var acceptedPowerPlants = world.OrderedEntities
            .Where(entity => entity.SpecId == "building.powerplant")
            .ToArray();

        Assert(deploy.IsDeployed && deploy.SetupRemaining <= 0, "Deploy+Build core should finish setup before providing build authority.");
        Assert(rejected.Count(rejection => rejection.Reason == "placement.outsideBuildRadius") == 2,
            $"Deploy+Build core should reject before and during setup, got rejections: {string.Join(", ", rejected.Select(rejection => rejection.Reason))}.");
        Assert(acceptedPowerPlants.Length == 1, $"Deploy+Build core should authorize exactly one post-setup build, got {acceptedPowerPlants.Length}.");
        Assert(world.ResourceInventory(owner).Credits == 700, $"Only the post-setup deploy build should spend credits, got {world.ResourceInventory(owner).Credits}.");

        Console.WriteLine($"OK [deploy-build-authority]: setup {deploy.SetupRemaining:0.000}, accepted {acceptedPowerPlants.Length}, rejected {rejected.Count}.");
    }

    private static EntitySpec DeployCoreSpec()
    {
        return new EntitySpec
        {
            Id = "replay.deploy_build_core",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Deploy Build Core", "deploybuild.name", "deploybuild.role", "DBC", IconGlyph.Turret),
            Abilities =
            [
                new AbilitySpec(AbilityKind.Build, Radius: 240),
                new AbilitySpec(AbilityKind.Deploy, Radius: 0.12f, Value: 1),
            ],
        };
    }

    private static IEnumerable<EntityComponentState> DeployCoreComponents()
    {
        yield return new HealthComponentState(320, 320);
        yield return new CommandableComponentState();
        yield return new MovementComponentState(Vector2.Zero);
        yield return new MovementProfileComponentState(MaxSpeed: 90);
        yield return new CollisionComponentState(24, 1.5f, 2, BlocksMovement: true);
        yield return new VisionComponentState(460);
        yield return new BuildRadiusComponentState(240);
        yield return new AbilityRuntimeComponentState([new AbilityCooldownState(AbilityKind.Deploy, 0)]);
    }

    private static void SpawnCompleted(
        EntityWorld world,
        OwnerId owner,
        string kind,
        Vector2 position,
        bool includeBuildRadius)
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

        if (includeBuildRadius && spec.BuildRadius > 0)
        {
            components.Add(new BuildRadiusComponentState(spec.BuildRadius));
        }

        world.Spawn(spec.ToEntitySpec(), owner, EntityTransform.At(position), components);
    }
}
