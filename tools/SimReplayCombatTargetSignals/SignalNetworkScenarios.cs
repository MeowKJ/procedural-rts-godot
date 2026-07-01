static partial class Program
{
    static void AssertSignalNetworkSystem()
    {
        const int regenTicks = 45;

        EntitySpec SignalSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Objective,
                Display = new EntityDisplaySpec("Signal Node", "signal.name", "signal.role", "SIG", IconGlyph.Building),
            };
        }

        EntitySpec ResourceSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Resource,
                Display = new EntityDisplaySpec("Signal Resource", "resource.name", "resource.role", "RES", IconGlyph.Credits),
            };
        }

        EntitySpec TargetSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Night Target", "target.name", "target.role", "TGT", IconGlyph.Infantry),
            };
        }

        EntityWorld BuildDaySignalWorld(bool powered)
        {
            var world = new EntityWorld(seed: powered ? 8181UL : 8182UL)
            {
                ResourceAtmosphere = ResourceAtmosphere.Day,
                EconomyTuning = EconomyTuningConfig.Default with
                {
                    RegenerationRate = 30f,
                    RegenerationCapRatio = 0.75f,
                },
            };
            world.AddSystem(new PowerSystem());
            world.AddSystem(new SignalNetworkSystem());
            world.AddSystem(new ResourceSystem());

            world.Spawn(SignalSpec("replay.signal_day"), new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(300, 300),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: powered ? 1 : 0, Used: powered ? 0 : 1, Powered: true),
                new SignalNetworkComponentState(SignalNodeKind.SafeZone, DayControlRadius: 160, NightVisionRadius: 260, SafetyAuraMultiplier: 2f),
            });
            world.Spawn(ResourceSpec("replay.signal_resource"), OwnerId.None, EntityTransform.At(new Vector2(80, 0)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(
                    Amount: 10,
                    MaxAmount: 100,
                    DepletionBehavior: ResourceDepletionBehavior.DepleteThenRegrow,
                    CorruptionState: ResourceCorruptionState.Tainted),
            });

            return world;
        }

        EntityWorld BuildNightSignalWorld(bool powered)
        {
            var world = new EntityWorld(seed: powered ? 8183UL : 8184UL)
            {
                ResourceAtmosphere = ResourceAtmosphere.Night,
            };
            world.AddSystem(new PowerSystem());
            world.AddSystem(new SignalNetworkSystem());
            world.AddSystem(new VisionSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            world.Spawn(SignalSpec("replay.signal_night"), new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(300, 300),
                new ConstructionComponentState(Progress: 1),
                new PowerComponentState(Provided: powered ? 1 : 0, Used: powered ? 0 : 1, Powered: true),
                new SignalNetworkComponentState(SignalNodeKind.SignalTower, DayControlRadius: 160, NightVisionRadius: 260, SafetyAuraMultiplier: 2f),
            });
            world.Spawn(TargetSpec("replay.signal_target"), new OwnerId(2), EntityTransform.At(new Vector2(220, 0)), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
            });

            return world;
        }

        AssertDeterministic("signal-network-day", () => BuildDaySignalWorld(powered: true), regenTicks, 15);
        AssertDeterministic("signal-network-night", () => BuildNightSignalWorld(powered: true), 3, 1);

        var poweredDay = BuildDaySignalWorld(powered: true);
        var unpoweredDay = BuildDaySignalWorld(powered: false);
        var poweredDayClock = new SimClock();
        var unpoweredDayClock = new SimClock();
        for (var tick = 1; tick <= regenTicks; tick++)
        {
            poweredDay.Step(tick, poweredDayClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            unpoweredDay.Step(tick, unpoweredDayClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        }

        var daySignal = poweredDay.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var inactiveSignal = unpoweredDay.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var poweredResource = poweredDay.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<ResourceNodeComponentState>();
        var unpoweredResource = unpoweredDay.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<ResourceNodeComponentState>();
        Assert(daySignal.Components.Require<BuildRadiusComponentState>().Radius == 160, "powered day signal should emit build radius");
        Assert(!daySignal.Components.Has<VisionComponentState>(), "day signal should not emit night vision");
        Assert(daySignal.Components.Require<ResourceRegenerationAuraComponentState>().Radius == 160, "day signal should emit a safety resource aura");
        Assert(poweredResource.Amount > unpoweredResource.Amount, $"powered signal aura should boost nearby resource regen, powered {poweredResource.Amount}, unpowered {unpoweredResource.Amount}");
        Assert(!inactiveSignal.Components.Has<BuildRadiusComponentState>(), "unpowered signal should not emit build radius");
        Assert(!inactiveSignal.Components.Has<ResourceRegenerationAuraComponentState>(), "unpowered signal should not emit safety aura");

        var poweredNight = BuildNightSignalWorld(powered: true);
        var unpoweredNight = BuildNightSignalWorld(powered: false);
        var nightClock = new SimClock();
        poweredNight.Step(1, nightClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        unpoweredNight.Step(1, nightClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        var nightSignal = poweredNight.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var nightInactiveSignal = unpoweredNight.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var targetId = new EntityId(2);
        Assert(!nightSignal.Components.Has<BuildRadiusComponentState>(), "night signal should not emit build radius");
        Assert(nightSignal.Components.Require<VisionComponentState>().SightRange == 260, "powered night signal should emit vision");
        Assert(nightSignal.Components.Require<ResourceRegenerationAuraComponentState>().Radius == 260, "night signal should emit safety aura from night radius");
        Assert(poweredNight.Visibility.IsVisible(new OwnerId(1), targetId), "powered night signal should reveal hostile target in vision radius");
        Assert(!nightInactiveSignal.Components.Has<VisionComponentState>(), "unpowered night signal should not emit vision");
        Assert(!unpoweredNight.Visibility.IsVisible(new OwnerId(1), targetId), "unpowered night signal should not reveal hostile target");

        Console.WriteLine($"OK [signal-network]: day build radius 160, resource {poweredResource.Amount}>{unpoweredResource.Amount}; night vision reveals target.");
    }
}
