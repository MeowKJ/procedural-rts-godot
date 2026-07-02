static class PathfindingAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var math = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "pathing", "PathfindingMath.cs"));
        RequireText(math, "for (var index = 0; index < members.Count; index++)", "FindSharedCorridor must scan members without LINQ Average.", result);
        RequireText(math, "AppendUnique(points, sharedPath, entryIndex)", "Shared-corridor entry append must use index-based copy.", result);
        RequireText(math, "AppendUnique(points, sharedPath, 1)", "Shared-corridor connector append must use index-based copy.", result);
        RequireText(math, "for (var index = 1; index < cells.Count; index++)", "ReconstructPath must build path points without LINQ Skip/Select.", result);
        ForbidText(math, "members.Average(", "FindSharedCorridor must not allocate LINQ Average enumerators.", result);
        ForbidText(math, "shared.Path.ToList()", "FindSharedCorridor must not copy the shared path list.", result);
        ForbidText(math, ".Skip(", "PathfindingMath hot paths must not allocate Skip iterators.", result);
        ForbidText(math, ".Select(cell => CellCenter", "ReconstructPath must not allocate Select iterators.", result);

        var pathing = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.PathingAvoidance.cs");
        RequireText(pathing, "for (var index = 1; index < unit.GlobalCorridor.Count; index++)", "Legacy AssignPath must enqueue the global corridor with an index loop.", result);
        ForbidText(pathing, "unit.GlobalCorridor.Skip(1)", "Legacy AssignPath must not allocate a Skip iterator.", result);
    }
}
