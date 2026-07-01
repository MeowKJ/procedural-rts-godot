static partial class Program
{
    static void AssertRestartCaptureConstruction()
    {
        const int ticks = 90;
        var owner = new OwnerId(1);
        var signalId = new EntityId(2);

        EntityWorld BuildWorld()
        {
            var world = new EntityWorld(seed: 6474)
            {
                ResourceAtmosphere = ResourceAtmosphere.Day,
            };
            world.AddSystem(new CommandSystem());
            world.AddSystem(new RepairSystem());
            world.AddSystem(new ConstructionSystem());
            world.AddSystem(new PowerSystem());
            world.AddSystem(new SignalNetworkSystem());
            world.ResourceInventory(owner).Credits = 500;

            world.Spawn(RepairerSpec(), owner, EntityTransform.At(Vector2.Zero), RepairerComponents());
            world.Spawn(RestartableSignalSpec(), OwnerId.None, EntityTransform.At(new Vector2(32, 0)), RestartableSignalComponents());
            return world;
        }

        var commands = new List<EntityCommand>
        {
            new RepairEntityCommand(owner, [new EntityId(1)], 10, signalId),
        };

        AssertDeterministic("restart-capture-construction", BuildWorld, commands, ticks, 9);

        var idleWorld = BuildWorld();
        var clock = new SimClock();
        for (var tick = 1; tick < 10; tick++)
        {
            idleWorld.Step(tick, clock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            idleWorld.Events.Drain();
        }

        var idleSignal = idleWorld.OrderedEntities.Single(entity => entity.Id.Value == signalId.Value);
        var idleConstruction = idleSignal.Components.Require<ConstructionComponentState>();
        Assert(idleSignal.OwnerId.Value == OwnerId.None.Value, "restart/capture objective should start neutral.");
        Assert(idleConstruction.Phase == ConstructionPhase.RestartCapture && idleConstruction.Progress == 0,
            "RestartCapture construction should not auto-advance before a repair command.");
        Assert(!idleSignal.Components.Has<BuildRadiusComponentState>(), "inactive restart/capture signal should not emit build radius.");

        var world = BuildWorld();
        var buffer = new EntityCommandBuffer();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            world.Events.Drain();
        }

        var signal = world.OrderedEntities.Single(entity => entity.Id.Value == signalId.Value);
        var repairer = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var construction = signal.Components.Require<ConstructionComponentState>();
        var buildRadius = signal.Components.Require<BuildRadiusComponentState>();
        var credits = world.ResourceInventory(owner).Credits;

        Assert(signal.OwnerId.Value == owner.Value, $"repair/restart should capture neutral objective for owner {owner.Value}, got {signal.OwnerId.Value}.");
        Assert(construction.Phase == ConstructionPhase.Building && construction.Progress >= 1,
            $"repair/restart should complete construction, got {construction.Phase}/{construction.Progress:0.000}.");
        Assert(buildRadius.Radius == 180, $"completed signal objective should emit its day build radius, got {buildRadius.Radius}.");
        Assert(!repairer.Components.Has<RepairOrderComponentState>(), "repairer should clear restart/capture order after completion.");
        Assert(credits == 200, $"restart/capture should spend repair-equivalent credits, got {credits}.");

        Console.WriteLine($"OK [restart-capture-construction]: owner {signal.OwnerId.Value}, radius {buildRadius.Radius}, credits {credits}.");
    }

    private static EntitySpec RepairerSpec()
    {
        return new EntitySpec
        {
            Id = "replay.restart_repairer",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Restart Repairer", "restart.repairer.name", "restart.repairer.role", "REP", IconGlyph.Settings),
            Abilities = [new AbilitySpec(AbilityKind.RepairField, Radius: 80, Value: 300)],
        };
    }

    private static IEnumerable<EntityComponentState> RepairerComponents()
    {
        yield return new HealthComponentState(80, 80);
        yield return new CommandableComponentState();
        yield return new MovementComponentState(Vector2.Zero);
        yield return new MovementProfileComponentState(MaxSpeed: 70, ArriveRadius: 4);
        yield return new CollisionComponentState(10, 1, 1, true);
    }

    private static EntitySpec RestartableSignalSpec()
    {
        return new EntitySpec
        {
            Id = "replay.restart_signal",
            Kind = EntityKind.Objective,
            Display = new EntityDisplaySpec("Restart Signal", "restart.signal.name", "restart.signal.role", "SIG", IconGlyph.Building),
        };
    }

    private static IEnumerable<EntityComponentState> RestartableSignalComponents()
    {
        yield return new HealthComponentState(300, 300);
        yield return new ConstructionComponentState(
            Progress: 0,
            BuildTime: 2,
            Cost: 0,
            RefundRatio: 0,
            Phase: ConstructionPhase.RestartCapture);
        yield return new PowerComponentState(Provided: 1, Used: 0, Powered: true);
        yield return new SignalNetworkComponentState(
            SignalNodeKind.SignalTower,
            DayControlRadius: 180,
            NightVisionRadius: 260,
            SafetyAuraMultiplier: 1.5f);
    }
}
