namespace ProceduralRts.Core;

/// <summary>
/// Observability for the immutable authored-environment raster held by one
/// <see cref="PathfindingSystem"/>. Dynamic EntityWorld blockers are never
/// included in these counts and remain rebuilt for every plan.
/// </summary>
public readonly record struct PathfindingEnvironmentRasterCacheMetrics(
    int RasterBuilds,
    int CacheHits);

public sealed partial class PathfindingSystem
{
    private readonly List<GridObstacle> _cachedEnvironmentObstacles = [];
    private readonly List<GridTerrain> _cachedEnvironmentTerrain = [];
    private readonly HashSet<GridObstacle> _cachedEnvironmentSeenObstacles = [];
    private MapRuntimeEnvironment? _cachedEnvironment;
    private float _cachedEnvironmentWorldWidth;
    private float _cachedEnvironmentWorldHeight;
    private float _cachedEnvironmentCellSize;
    private MovementDomain _cachedEnvironmentDomain;
    private bool _cachedEnvironmentAllowsDynamicBlockers;
    private int _environmentRasterBuilds;
    private int _environmentRasterCacheHits;

    /// <summary>
    /// Test and profiling evidence for authored-grid reuse. It is intentionally
    /// read-only: environment ownership remains inside the system.
    /// </summary>
    public PathfindingEnvironmentRasterCacheMetrics EnvironmentRasterCacheMetrics =>
        new(_environmentRasterBuilds, _environmentRasterCacheHits);

    private bool CopyCachedEnvironment(EntityWorld world, MovementDomain domain)
    {
        if (!MatchesCachedEnvironment(world, domain))
        {
            _cachedEnvironmentAllowsDynamicBlockers = PathfindingStaticGrid.FillEnvironment(
                world.MapEnvironment,
                world.WorldWidth,
                world.WorldHeight,
                _cellSize,
                domain,
                _cachedEnvironmentObstacles,
                _cachedEnvironmentTerrain,
                _cachedEnvironmentSeenObstacles);
            _cachedEnvironment = world.MapEnvironment;
            _cachedEnvironmentWorldWidth = world.WorldWidth;
            _cachedEnvironmentWorldHeight = world.WorldHeight;
            _cachedEnvironmentCellSize = _cellSize;
            _cachedEnvironmentDomain = domain;
            _environmentRasterBuilds++;
        }
        else
        {
            _environmentRasterCacheHits++;
        }

        _obstacles.Clear();
        _obstacles.AddRange(_cachedEnvironmentObstacles);
        _terrain.Clear();
        _terrain.AddRange(_cachedEnvironmentTerrain);
        _seenObstacles.Clear();
        for (var index = 0; index < _cachedEnvironmentObstacles.Count; index++)
        {
            _seenObstacles.Add(_cachedEnvironmentObstacles[index]);
        }

        return _cachedEnvironmentAllowsDynamicBlockers;
    }

    private bool MatchesCachedEnvironment(EntityWorld world, MovementDomain domain)
    {
        return ReferenceEquals(_cachedEnvironment, world.MapEnvironment)
            && _cachedEnvironmentWorldWidth == world.WorldWidth
            && _cachedEnvironmentWorldHeight == world.WorldHeight
            && _cachedEnvironmentCellSize == _cellSize
            && _cachedEnvironmentDomain == domain;
    }
}
