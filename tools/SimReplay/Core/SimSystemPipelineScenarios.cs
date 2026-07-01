static partial class Program
{
    static void AssertLiveSimSystemPipeline()
    {
        var world = new EntityWorld(seed: 101);
        SimSystemPipeline.ConfigureLiveGameplay(world, new OwnerId(1));

        var friendlySpec = PipelineSpec("pipeline.friendly");
        var hostileSpec = PipelineSpec("pipeline.hostile");
        world.Spawn(friendlySpec, new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
        {
            new HealthComponentState(100, 100),
            new VisionComponentState(260),
            new WeaponUserComponentState(Array.Empty<WeaponMountRuntimeState>()),
            new MovementComponentState(default),
            new MovementProfileComponentState(120),
        });
        world.Spawn(hostileSpec, new OwnerId(2), EntityTransform.At(new Vector2(120, 0)), new EntityComponentState[]
        {
            new HealthComponentState(100, 100),
            new VisionComponentState(260),
        });
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

        world.Step(1, new SimClock().FixedDelta, Array.Empty<SequencedCommandEnvelope>());

        Assert(world.Visibility.IsVisible(new OwnerId(1), new EntityId(2)), "live SimSystemPipeline should run VisionSystem before consumers");
        Console.WriteLine("OK [live-sim-system-pipeline]: shared live EntityWorld system order is executable.");
    }

    static EntitySpec PipelineSpec(string id)
    {
        return new EntitySpec
        {
            Id = id,
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Pipeline", "pipeline.name", "pipeline.role", "PIP", IconGlyph.Infantry),
        };
    }
}
