namespace ProceduralRts.Core;

/// <summary>
/// Caller-owned scratch storage for deterministic pathfinding. Systems reuse
/// one workspace per planner; returned paths must copy durable results before
/// the workspace is reused.
/// </summary>
public sealed class PathfindingWorkspace
{
    internal HashSet<GridObstacle> Blocked { get; } = [];
    internal Dictionary<GridObstacle, TerrainLayer> TerrainByCell { get; } = [];
    internal Dictionary<GridObstacle, GridObstacle> CameFrom { get; } = [];
    internal Dictionary<GridObstacle, float> GScore { get; } = [];
    internal PriorityQueue<GridObstacle, float> Open { get; } = new();
    internal List<(GridObstacle Cell, float Cost)> ValidNeighbors { get; } = new(8);
    internal List<PathPoint> SharedCorridorPoints { get; } = [];
    internal List<GridObstacle> SharedCorridorRawCells { get; } = [];
    internal HashSet<GridObstacle> SharedCorridorBlocked { get; } = [];
    internal Dictionary<GridObstacle, TerrainLayer> SharedCorridorTerrainByCell { get; } = [];
    internal List<GridObstacle> ReconstructedCells { get; } = [];
    internal List<PathPoint> ReconstructedPoints { get; } = [];
    internal List<PathPoint> SmoothedPoints { get; } = [];
    internal List<PathPoint> PrunedPoints { get; } = [];
    internal List<PathPoint> FinalPathPoints { get; } = [];

    internal void ClearSearch(GridObstacle start)
    {
        CameFrom.Clear();
        GScore.Clear();
        Open.Clear();
        ValidNeighbors.Clear();
        GScore[start] = 0;
    }
}
