namespace ProceduralRts.Core;

public sealed partial class EntityWorld
{
    private MapRuntimeEnvironment _mapEnvironment = MapRuntimeEnvironment.Empty;

    public MapRuntimeEnvironment MapEnvironment => _mapEnvironment;

    internal void InstallMapEnvironment(MapRuntimeEnvironment environment)
    {
        _mapEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
    }
}
