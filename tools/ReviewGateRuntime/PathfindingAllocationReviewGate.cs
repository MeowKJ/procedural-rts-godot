static class PathfindingAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var math = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "pathing", "PathfindingMath.cs"));
        RequireText(math, "for (var index = 0; index < members.Count; index++)", "FindSharedCorridor must scan members without LINQ Average.", result);
        RequireText(math, "AppendUnique(points, sharedPath, entryIndex)", "Shared-corridor entry append must use index-based copy.", result);
        RequireText(math, "AppendUnique(points, sharedPath, 1)", "Shared-corridor connector append must use index-based copy.", result);
        RequireText(math, "ReconstructPath(\n                    workspace,", "FindPathWithDebug must pass workspace into path reconstruction.", result);
        RequireText(math, "for (var index = 1; index < cells.Count; index++)", "ReconstructPath must build path points without LINQ Skip/Select.", result);
        RequireText(math, "var cells = workspace.ReconstructedCells", "ReconstructPath must reuse workspace raw-cell scratch storage.", result);
        RequireText(math, "var points = workspace.ReconstructedPoints", "ReconstructPath must reuse workspace point scratch storage.", result);
        RequireText(math, "SmoothAndPrunePath(\n            workspace,\n            points", "ReconstructPath must route smoothing/pruning through workspace buffers.", result);
        RequireText(math, "new List<PathPoint>(path)", "Pathfinding returned paths must copy only at the durable result boundary.", result);
        RequireText(math, "new List<GridObstacle>(cells)", "ReconstructPath raw cells must copy only at the durable result boundary.", result);
        RequireText(math, "SmoothCollinearInto(IReadOnlyList<PathPoint> points, List<PathPoint> result)", "Path smoothing must write into caller-owned buffers.", result);
        RequireText(math, "PruneByLineOfSightInto(", "Path LOS pruning must write into caller-owned buffers.", result);
        RequireText(math, "BuildPassabilityLookups(obstacles, movementDomain, terrain, blocked, terrainByCell)", "Pathfinding passability setup must use explicit fill helpers.", result);
        RequireText(math, "PathfindingWorkspace workspace", "FindPathWithDebug must expose a caller-owned workspace overload.", result);
        RequireText(math, "var validNeighbors = workspace.ValidNeighbors", "FindPathWithDebug must reuse the workspace neighbor buffer for A* expansion.", result);
        RequireText(math, "CollectValidNeighbors(current, width, height, blocked, terrainByCell, allowedLayers, validNeighbors)", "A* expansion must fill the caller-owned neighbor buffer.", result);
        RequireText(math, "List<PathfindingCorridorAssignment> assignments)", "FindSharedCorridor must expose a caller-owned assignment result buffer overload.", result);
        RequireText(math, "PathfindingWorkspace workspace,\n        IReadOnlyList<PathfindingCorridorMember> members", "FindSharedCorridor must expose a caller-owned workspace overload.", result);
        RequireText(math, "FindPathWithDebug(\n            workspace,\n            anchorX", "Shared-corridor root path must reuse the caller-owned workspace.", result);
        RequireText(math, "FindPathWithDebug(\n                workspace,\n                member.StartX", "Shared-corridor fallback/connector paths must reuse the caller-owned workspace.", result);
        RequireText(math, "FindPathWithDebug(\n                    workspace,\n                    last.X", "Shared-corridor exit paths must reuse the caller-owned workspace.", result);
        RequireText(math, "var rawCells = workspace.SharedCorridorRawCells", "Shared-corridor assignment raw-cell assembly must reuse workspace scratch storage.", result);
        RequireText(math, "var points = workspace.SharedCorridorPoints", "Shared-corridor assignment point assembly must reuse workspace scratch storage.", result);
        RequireText(math, "SmoothAndPrunePath(\n            workspace,\n            points,\n            memberStart", "Shared-corridor assignment smoothing/pruning must use workspace buffers.", result);
        RequireText(math, "new List<GridObstacle>(rawCells)", "Shared-corridor assignment raw cells must copy only at the durable result boundary.", result);
        RequireText(math, "var blocked = workspace.SharedCorridorBlocked", "Shared-corridor assignment LOS checks must reuse dedicated blocked lookup storage.", result);
        RequireText(math, "var terrainByCell = workspace.SharedCorridorTerrainByCell", "Shared-corridor assignment LOS checks must reuse dedicated terrain lookup storage.", result);
        ForbidText(math, "members.Average(", "FindSharedCorridor must not allocate LINQ Average enumerators.", result);
        ForbidText(math, "shared.Path.ToList()", "FindSharedCorridor must not copy the shared path list.", result);
        ForbidText(math, "new List<GridObstacle>(sharedRawCells)", "Shared-corridor assignments must not allocate per-member raw-cell scratch lists.", result);
        ForbidText(math, "var points = new List<PathPoint>()", "Shared-corridor assignments must not allocate per-member point scratch lists.", result);
        ForbidText(math, "var blocked = new HashSet<GridObstacle>()", "Shared-corridor assignment LOS checks must not allocate blocked lookup sets.", result);
        ForbidText(math, "var terrainByCell = new Dictionary<GridObstacle, TerrainLayer>()", "Shared-corridor assignment LOS checks must not allocate terrain lookup dictionaries.", result);
        ForbidText(math, "var cells = new List<GridObstacle> { current }", "ReconstructPath must not allocate raw-cell scratch lists.", result);
        ForbidText(math, "var points = new List<PathPoint>(Math.Max(0, cells.Count - 1))", "ReconstructPath must not allocate point scratch lists.", result);
        ForbidText(math, "var result = new List<PathPoint> { points[0] }", "SmoothCollinear must not allocate local result lists.", result);
        ForbidText(math, "var result = new List<PathPoint>()", "PruneByLineOfSight must not allocate local result lists.", result);
        ForbidText(math, "var shared = FindPathWithDebug(\n            anchorX", "Shared-corridor root path must not use the allocating compatibility workspace.", result);
        ForbidText(math, "var fallback = FindPathWithDebug(\n                member.StartX", "Shared-corridor fallback path must not use the allocating compatibility workspace.", result);
        ForbidText(math, "var connector = FindPathWithDebug(\n                member.StartX", "Shared-corridor connector path must not use the allocating compatibility workspace.", result);
        ForbidText(math, "var exit = FindPathWithDebug(\n                    last.X", "Shared-corridor exit path must not use the allocating compatibility workspace.", result);
        ForbidText(math, "obstacles.ToHashSet()", "Pathfinding passability setup must not allocate blocker sets through LINQ materialization.", result);
        ForbidText(math, "terrain.ToDictionary(", "Pathfinding passability setup must not allocate terrain lookup dictionaries through LINQ materialization.", result);
        ForbidText(math, "IEnumerable<(GridObstacle Cell, float Cost)> ValidNeighbors", "Pathfinding neighbor expansion must not use an iterator helper.", result);
        ForbidText(math, "yield return (cell, offset.Cost)", "Pathfinding neighbor expansion must not allocate yield iterator state.", result);
        ForbidText(math, ".Skip(", "PathfindingMath hot paths must not allocate Skip iterators.", result);
        var pathing = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.PathingAvoidance.cs");
        RequireText(pathing, "for (var index = 1; index < unit.GlobalCorridor.Count; index++)", "Legacy AssignPath must enqueue the global corridor with an index loop.", result);
        ForbidText(pathing, "unit.GlobalCorridor.Skip(1)", "Legacy AssignPath must not allocate a Skip iterator.", result);
        var pathfindingSystem = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "PathfindingSystem.cs");
        RequireText(pathfindingSystem, "private readonly PathfindingWorkspace _pathWorkspace = new();", "PathfindingSystem must own a reusable single-path workspace.", result);
        RequireText(pathfindingSystem, "private readonly List<PathfindingCorridorAssignment> _sharedAssignmentResults = [];", "PathfindingSystem must reuse shared-corridor assignment result storage.", result);
        RequireText(pathfindingSystem, "PathfindingMath.FindPathWithDebug(\n            _pathWorkspace,", "PathfindingSystem single-path planning must use the workspace overload.", result);
        RequireText(pathfindingSystem, "PathfindingMath.FindSharedCorridor(\n                _pathWorkspace,", "PathfindingSystem shared-corridor planning must use the workspace overload.", result);
        RequireText(pathfindingSystem, "_sharedAssignmentResults);", "PathfindingSystem shared-corridor planning must use the assignment buffer overload.", result);
        ForbidText(pathfindingSystem, "PathfindingMath.FindPathWithDebug(\n            entity.Transform.Position.X", "PathfindingSystem single-path planning must not use the allocating compatibility overload.", result);
        var workspace = ReviewGateSource.Read(root, "scripts", "core", "pathing", "PathfindingWorkspace.cs");
        foreach (var token in new[] { "SharedCorridorPoints", "SharedCorridorRawCells", "SharedCorridorBlocked", "SharedCorridorTerrainByCell", "ReconstructedCells", "ReconstructedPoints", "SmoothedPoints", "PrunedPoints", "FinalPathPoints" })
            RequireText(workspace, token, "PathfindingWorkspace must own pathfinding scratch storage.", result);
    }
}
