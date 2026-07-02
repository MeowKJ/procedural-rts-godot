namespace ProceduralRts.Core;

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

    internal void ClearSearch(GridObstacle start)
    {
        CameFrom.Clear();
        GScore.Clear();
        Open.Clear();
        ValidNeighbors.Clear();
        GScore[start] = 0;
    }
}
