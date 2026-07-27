static partial class Program
{
    static EntitySpec InvariantSpec(string id = "replay.invariant")
    {
        return new EntitySpec
        {
            Id = id,
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Invariant", "invariant.name", "invariant.role", "INV", IconGlyph.Infantry),
        };
    }

    static void AssertSimInvariants()
    {
        var valid = new EntityWorld(seed: 17);
        valid.AddSystem(new CommandSystem());
        valid.SimInvariantsEnabled = true;
        var spec = InvariantSpec();
        valid.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(10, 10)), new EntityComponentState[]
        {
            new HealthComponentState(100, 100),
            new MovementComponentState(Vector2.Zero, new Vector2(20, 20)),
            new MovementProfileComponentState(120),
            new CollisionComponentState(10, 1, 1, true),
            new VisionComponentState(400),
            new WeaponUserComponentState(new[]
            {
                new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, 0, 0),
            }),
            new CommandQueueComponentState(Array.Empty<EntityCommand>()),
        });

        valid.Step(1, new SimClock().FixedDelta, Array.Empty<SequencedCommandEnvelope>());

        AssertInvariantViolation("nan transform", world =>
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(new Vector2(float.NaN, 0)));
        });

        AssertInvariantViolation("invalid hp", world =>
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(150, 100),
            });
        });

        AssertInvariantViolation("missing attack target", world =>
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new WeaponUserComponentState(Array.Empty<WeaponMountRuntimeState>(), new EntityId(99), CombatTargetKind.Unit, true),
            });
        });

        AssertInvariantViolation("dead attack target", world =>
        {
            var target = world.Spawn(spec, new OwnerId(2), EntityTransform.At(new Vector2(100, 0)), new EntityComponentState[]
            {
                new HealthComponentState(0, 100),
            });
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new WeaponUserComponentState(Array.Empty<WeaponMountRuntimeState>(), target.Id, CombatTargetKind.Unit, true),
            });
        });

        AssertInvariantViolation("negative auto reacquire cooldown", world =>
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new WeaponUserComponentState(Array.Empty<WeaponMountRuntimeState>(), AutoReacquireCooldownRemaining: -0.01f),
            });
        });

        AssertInvariantViolation("negative last-known target memory", world =>
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new WeaponUserComponentState(
                    Array.Empty<WeaponMountRuntimeState>(),
                    LastKnownTargetPosition: Vector2.Zero,
                    LastKnownTargetRemaining: -0.01f),
            });
        });

        AssertInvariantViolation("expired last-known target memory", world =>
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new WeaponUserComponentState(
                    Array.Empty<WeaponMountRuntimeState>(),
                    LastKnownTargetPosition: new Vector2(10, 0),
                    LastKnownTargetRemaining: 0),
            });
        });

        AssertInvariantViolation("duplicate dock reservation", world =>
        {
            var harvester = world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero));
            world.Spawn(InvariantSpec("replay.dock.a"), new OwnerId(1), EntityTransform.At(new Vector2(10, 0)), new EntityComponentState[]
            {
                new DockComponentState(ReservedByEntityId: harvester.Id.Value),
            });
            world.Spawn(InvariantSpec("replay.dock.b"), new OwnerId(1), EntityTransform.At(new Vector2(20, 0)), new EntityComponentState[]
            {
                new DockComponentState(ReservedByEntityId: harvester.Id.Value),
            });
        });

        AssertInvariantViolation("overlong command queue", world =>
        {
            var subjects = new[] { new EntityId(1) };
            var queued = Enumerable.Range(0, SimInvariants.MaxCommandQueueItems + 1)
                .Select(tick => new StopEntityCommand(new OwnerId(1), subjects, tick))
                .Cast<EntityCommand>()
                .ToArray();
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new CommandQueueComponentState(queued),
            });
        });

        AssertInvariantViolation("completed paused construction", world =>
        {
            world.Spawn(spec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new ConstructionComponentState(Progress: 1, PauseReason: ConstructionPauseReason.Unpowered),
            });
        });

        Console.WriteLine("OK [sim-invariants]: valid world passes; malformed transforms, hp, targets, target cooldowns, last-known memory, docks, queues, and construction pauses fail.");
    }

    static void AssertInvariantViolation(string name, Action<EntityWorld> arrange)
    {
        var world = new EntityWorld(seed: 31);
        arrange(world);
        var violations = SimInvariants.Validate(world);
        if (violations.Count == 0)
        {
            Fail($"sim-invariants [{name}] did not report a violation");
        }

        try
        {
            world.SimInvariantsEnabled = true;
            world.Step(1, new SimClock().FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            Fail($"sim-invariants [{name}] enabled Step did not throw");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Sim invariant failure", StringComparison.Ordinal))
        {
            // Expected: the runtime toggle asserts after the tick.
        }
    }

    static void AssertPresentationMetrics()
    {
        var metrics = new PresentationMetrics(capacity: 100);
        for (var i = 0; i < 99; i++)
        {
            metrics.RecordFrame(frameMs: 10, processMs: 4, simStepMs: 1);
        }

        metrics.RecordFrame(frameMs: 50, processMs: 12, simStepMs: 5);
        var snapshot = metrics.Snapshot();
        Assert(snapshot.SampleCount == 100, "presentation metrics should fill the rolling sample window");
        Assert(Math.Abs(snapshot.AverageFrameMs - 10.4) < 0.001, "presentation metrics should average frame time");
        Assert(Math.Abs(snapshot.OnePercentLowFrameMs - 50) < 0.001, "presentation metrics should expose worst 1% frame time");
        Assert(snapshot.OnePercentLowFps > 19.9 && snapshot.OnePercentLowFps < 20.1, "presentation metrics should convert 1% low frame time to FPS");
        Assert(snapshot.LastProcessMs == 12 && snapshot.LastRenderEstimateMs == 38, "presentation metrics should track process and render-estimate timings");
        Assert(snapshot.AverageSimStepMs > 1, "presentation metrics should track sim-step timings");

        for (var i = 0; i < 100; i++)
        {
            metrics.RecordFrame(frameMs: 16, processMs: 6, simStepMs: 2);
        }

        snapshot = metrics.Snapshot();
        Assert(snapshot.SampleCount == 100, "presentation metrics should keep a fixed rolling capacity");
        Assert(Math.Abs(snapshot.AverageFrameMs - 16) < 0.001, "presentation metrics should evict old frame samples");
        Assert(Math.Abs(snapshot.OnePercentLowFrameMs - 16) < 0.001, "presentation metrics should compute 1% low from the current window");

        Console.WriteLine("OK [presentation-metrics]: rolling averages and 1% low frame time recorded.");
    }

    static void AssertSimEventDrainInto()
    {
        var sink = new SimEventSink();
        var events = new List<SimEvent>
        {
            new EntityDestroyedEvent(0, new EntityId(99), new OwnerId(9), Vector2.Zero),
        };

        sink.DrainInto(events);
        Assert(events.Count == 0, "DrainInto should clear the destination when no events are pending");

        sink.Raise(new WeaponFiredEvent(1, new EntityId(1), "main", WeaponIds.NeedleRifle, Vector2.Zero, Vector2.One));
        sink.Raise(new EntityDamagedEvent(1, new EntityId(2), new EntityId(1), 12, Vector2.One));
        sink.DrainInto(events);
        Assert(events.Count == 2, "DrainInto should move pending events into a reusable destination");

        sink.DrainInto(events);
        Assert(events.Count == 0, "DrainInto should empty the sink after draining");

        Console.WriteLine("OK [sim-events]: event drain buffer can be reused without snapshot arrays.");
    }
}
