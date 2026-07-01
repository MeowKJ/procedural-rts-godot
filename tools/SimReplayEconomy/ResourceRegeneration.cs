static partial class Program
{
    static void AssertResourceRegeneration()
    {
        const int regenTicks = 45;

        EntityWorld BuildRegenerationWorld(ResourceAtmosphere atmosphere = ResourceAtmosphere.Day)
        {
            var world = new EntityWorld(seed: 5152)
            {
                EconomyTuning = EconomyTuningConfig.Default with
                {
                    RegenerationRate = 30f,
                    RegenerationCapRatio = 0.75f,
                    TaintedRegenerationMultiplier = 0.5f,
                    HostileRegenerationMultiplier = 0f,
                    SafeAuraRegenerationMultiplier = 2f,
                    NightRegenerationMultiplier = 0.5f,
                },
                ResourceAtmosphere = atmosphere,
            };
            world.AddSystem(new ResourceSystem());

            var resourceSpec = new EntitySpec
            {
                Id = "replay.regrow_resource",
                Kind = EntityKind.Resource,
                Display = new EntityDisplaySpec("Regrow Resource", "resource.name", "resource.role", "RES", IconGlyph.Credits),
            };
            var auraSpec = new EntitySpec
            {
                Id = "replay.regrow_aura",
                Kind = EntityKind.Objective,
                Display = new EntityDisplaySpec("Signal", "signal.name", "signal.role", "SIG", IconGlyph.Building),
            };

            world.Spawn(auraSpec, OwnerId.None, EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new PowerComponentState(Provided: 1, Used: 0, Powered: true),
                new ResourceRegenerationAuraComponentState(Radius: 80, Multiplier: 2f),
            });
            world.Spawn(resourceSpec with { Id = "replay.clean_aura_resource" }, OwnerId.None, EntityTransform.At(new Vector2(20, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(
                    Amount: 10,
                    MaxAmount: 100,
                    DepletionBehavior: ResourceDepletionBehavior.DepleteThenRegrow),
            });
            world.Spawn(resourceSpec with { Id = "replay.tainted_resource" }, OwnerId.None, EntityTransform.At(new Vector2(220, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(
                    Amount: 10,
                    MaxAmount: 100,
                    DepletionBehavior: ResourceDepletionBehavior.DepleteThenRegrow,
                    CorruptionState: ResourceCorruptionState.Tainted),
            });
            world.Spawn(resourceSpec with { Id = "replay.hostile_resource" }, OwnerId.None, EntityTransform.At(new Vector2(320, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(
                    Amount: 10,
                    MaxAmount: 100,
                    DepletionBehavior: ResourceDepletionBehavior.DepleteThenRegrow,
                    CorruptionState: ResourceCorruptionState.Hostile),
            });
            world.Spawn(resourceSpec with { Id = "replay.non_regrow_resource" }, OwnerId.None, EntityTransform.At(new Vector2(420, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(
                    Amount: 10,
                    MaxAmount: 100,
                    DepletionBehavior: ResourceDepletionBehavior.DepleteToZero),
            });

            return world;
        }

        AssertDeterministic("resource-regen", () => BuildRegenerationWorld(), regenTicks, 15);

        var day = RunRegenerationWorld(BuildRegenerationWorld(ResourceAtmosphere.Day), regenTicks);
        var night = RunRegenerationWorld(BuildRegenerationWorld(ResourceAtmosphere.Night), regenTicks);

        Assert(day.CleanAura > night.CleanAura, $"day regeneration should outpace night, day {day.CleanAura}, night {night.CleanAura}");
        Assert(day.CleanAura == 75, $"resource regeneration should respect 75% cap, got {day.CleanAura}");
        Assert(day.CleanAura > day.Tainted, $"safe aura should outpace tainted regrowth, aura {day.CleanAura}, tainted {day.Tainted}");
        Assert(day.Tainted > day.Hostile, $"tainted nodes should regrow while hostile nodes are suppressed, tainted {day.Tainted}, hostile {day.Hostile}");
        Assert(day.Hostile == 10, $"hostile resource should remain suppressed at 10, got {day.Hostile}");
        Assert(day.NonRegrow == 10, $"DepleteToZero resource should not regrow, got {day.NonRegrow}");
        Assert(day.Hash != night.Hash, "resource atmosphere must affect deterministic state hash");

        Console.WriteLine($"OK [resource-regen]: aura {day.CleanAura}, night {night.CleanAura}, tainted {day.Tainted}, hostile {day.Hostile}, non-regrow {day.NonRegrow}.");
    }

    static void AssertAutoHarvestNearestResource()
    {
        const int ticks = 360;

        EntityWorld BuildAutoHarvestWorld()
        {
            var world = new EntityWorld(seed: 5153)
            {
                EconomyTuning = EconomyTuningConfig.Default with
                {
                    GatherRate = 180f,
                    UnloadRate = 360f,
                    RegenerationRate = 0,
                },
            };
            world.AddSystem(new CommandSystem());
            world.AddSystem(new ResourceSystem());
            world.AddSystem(new MovementSystem());
            world.ResourceInventory(new OwnerId(1)).Credits = 0;

            var harvesterSpec = new EntitySpec
            {
                Id = "replay.auto_harvester",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Harvester", "harvester.name", "harvester.role", "HAR", IconGlyph.Harvester),
            };
            var resourceSpec = new EntitySpec
            {
                Id = "replay.auto_resource",
                Kind = EntityKind.Resource,
                Display = new EntityDisplaySpec("Resource", "resource.name", "resource.role", "RES", IconGlyph.Credits),
            };
            var refinerySpec = new EntitySpec
            {
                Id = "replay.auto_refinery",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Refinery", "refinery.name", "refinery.role", "REF", IconGlyph.Building),
            };

            world.Spawn(harvesterSpec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HarvesterComponentState(),
                new ResourceCargoComponentState(Cargo: 0, Capacity: 40),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2),
            });
            world.Spawn(resourceSpec with { Id = "replay.near_resource" }, OwnerId.None, EntityTransform.At(new Vector2(32, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(Amount: 24, MaxAmount: 24),
            });
            world.Spawn(resourceSpec with { Id = "replay.far_resource" }, OwnerId.None, EntityTransform.At(new Vector2(150, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(Amount: 120, MaxAmount: 120),
            });
            world.Spawn(refinerySpec, new OwnerId(1), EntityTransform.At(new Vector2(-32, 0)), new EntityComponentState[]
            {
                new DockComponentState(),
            });

            return world;
        }

        var autoHarvestLog = new List<EntityCommand>
        {
            new AutoHarvestEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1),
        };

        AssertDeterministic("auto-harvest", BuildAutoHarvestWorld, autoHarvestLog, ticks, 60);

        var world = BuildAutoHarvestWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in autoHarvestLog)
        {
            buffer.Enqueue(command);
        }

        world.Step(1, clock.FixedDelta, buffer.DrainUpToTick(1));
        var harvester = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var firstIntent = harvester.Components.Require<HarvesterComponentState>();
        Assert(firstIntent.FieldId == 2, $"auto-harvest should pick nearest resource first, got {firstIntent.FieldId}");

        for (var tick = 2; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var near = ResourceAmount(world, "replay.near_resource");
        var far = ResourceAmount(world, "replay.far_resource");
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;
        var cargo = harvester.Components.Require<ResourceCargoComponentState>().Cargo;
        Assert(near == 0, $"auto-harvest should deplete the nearest small resource, got {near}");
        Assert(far < 120, $"auto-harvest should continue to the next nearest resource after depletion, far amount {far}");
        Assert(credits + cargo > 24, $"auto-harvest should gather beyond the first resource, credits {credits}, cargo {cargo}");

        Console.WriteLine($"OK [auto-harvest]: near {near}, far {far}, credits {credits}, cargo {cargo}.");
    }

    static (int CleanAura, int Tainted, int Hostile, int NonRegrow, ulong Hash) RunRegenerationWorld(EntityWorld world, int ticks)
    {
        var clock = new SimClock();
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        }

        return (
            ResourceAmount(world, "replay.clean_aura_resource"),
            ResourceAmount(world, "replay.tainted_resource"),
            ResourceAmount(world, "replay.hostile_resource"),
            ResourceAmount(world, "replay.non_regrow_resource"),
            world.DeterministicStateHash());
    }

    static int ResourceAmount(EntityWorld world, string specId)
    {
        return world.OrderedEntities
            .Single(entity => entity.SpecId == specId)
            .Components
            .Require<ResourceNodeComponentState>()
            .Amount;
    }
}
