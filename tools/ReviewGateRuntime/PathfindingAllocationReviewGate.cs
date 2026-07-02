static class PathfindingAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var math = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "pathing", "PathfindingMath.cs"));
        RequireText(math, "for (var index = 0; index < members.Count; index++)", "FindSharedCorridor must scan members without LINQ Average.", result);
        RequireText(math, "AppendUnique(points, sharedPath, entryIndex)", "Shared-corridor entry append must use index-based copy.", result);
        RequireText(math, "AppendUnique(points, sharedPath, 1)", "Shared-corridor connector append must use index-based copy.", result);
        RequireText(math, "for (var index = 1; index < cells.Count; index++)", "ReconstructPath must build path points without LINQ Skip/Select.", result);
        RequireText(math, "BuildPassabilityLookups(obstacles, movementDomain, terrain, blocked, terrainByCell)", "Pathfinding passability setup must use explicit fill helpers.", result);
        RequireText(math, "PathfindingWorkspace workspace", "FindPathWithDebug must expose a caller-owned workspace overload.", result);
        RequireText(math, "var validNeighbors = workspace.ValidNeighbors", "FindPathWithDebug must reuse the workspace neighbor buffer for A* expansion.", result);
        RequireText(math, "CollectValidNeighbors(current, width, height, blocked, terrainByCell, allowedLayers, validNeighbors)", "A* expansion must fill the caller-owned neighbor buffer.", result);
        RequireText(math, "List<PathfindingCorridorAssignment> assignments)", "FindSharedCorridor must expose a caller-owned assignment result buffer overload.", result);
        ForbidText(math, "members.Average(", "FindSharedCorridor must not allocate LINQ Average enumerators.", result);
        ForbidText(math, "shared.Path.ToList()", "FindSharedCorridor must not copy the shared path list.", result);
        ForbidText(math, "obstacles.ToHashSet()", "Pathfinding passability setup must not allocate blocker sets through LINQ materialization.", result);
        ForbidText(math, "terrain.ToDictionary(", "Pathfinding passability setup must not allocate terrain lookup dictionaries through LINQ materialization.", result);
        ForbidText(math, "IEnumerable<(GridObstacle Cell, float Cost)> ValidNeighbors", "Pathfinding neighbor expansion must not use an iterator helper.", result);
        ForbidText(math, "yield return (cell, offset.Cost)", "Pathfinding neighbor expansion must not allocate yield iterator state.", result);
        ForbidText(math, ".Skip(", "PathfindingMath hot paths must not allocate Skip iterators.", result);
        ForbidText(math, ".Select(cell => CellCenter", "ReconstructPath must not allocate Select iterators.", result);

        var pathing = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.PathingAvoidance.cs");
        RequireText(pathing, "for (var index = 1; index < unit.GlobalCorridor.Count; index++)", "Legacy AssignPath must enqueue the global corridor with an index loop.", result);
        ForbidText(pathing, "unit.GlobalCorridor.Skip(1)", "Legacy AssignPath must not allocate a Skip iterator.", result);

        var pathfindingSystem = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "PathfindingSystem.cs");
        RequireText(pathfindingSystem, "private readonly PathfindingWorkspace _pathWorkspace = new();", "PathfindingSystem must own a reusable single-path workspace.", result);
        RequireText(pathfindingSystem, "private readonly List<PathfindingCorridorAssignment> _sharedAssignmentResults = [];", "PathfindingSystem must reuse shared-corridor assignment result storage.", result);
        RequireText(pathfindingSystem, "PathfindingMath.FindPathWithDebug(\n            _pathWorkspace,", "PathfindingSystem single-path planning must use the workspace overload.", result);
        RequireText(pathfindingSystem, "_sharedAssignmentResults);", "PathfindingSystem shared-corridor planning must use the assignment buffer overload.", result);
        ForbidText(pathfindingSystem, "PathfindingMath.FindPathWithDebug(\n            entity.Transform.Position.X", "PathfindingSystem single-path planning must not use the allocating compatibility overload.", result);
    }
}
