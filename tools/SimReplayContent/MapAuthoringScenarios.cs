using ProceduralRts.Core;

static partial class Program
{
    static void RunMapAuthoringScenario()
    {
        AssertDeterministic("map-spec-loader", BuildMapSpecWorld, 12, 4);
        AssertMapRuntimeEnvironment();
    }

    private static EntityWorld BuildMapSpecWorld()
    {
        var spec = SkirmishMapGenerator.GenerateSpec(MatchConfig.Default);
        return MapLoader.Load(
            spec,
            options: new MapLoadOptions(
                ConfigureLiveSystems: true,
                OutcomeViewer: new OwnerId(1)));
    }
}
