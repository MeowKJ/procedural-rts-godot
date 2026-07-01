static class RegressionReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireToolProjects(root, result);
        RequireVerifyAllCoverage(root, result);
        RequireDeterministicEvidence(root, result);
        RequireSimHotAllocationEvidence(root, result);
    }

    private static void RequireToolProjects(string root, GateResult result)
    {
        foreach (var project in new[]
        {
            "tools/SimReplay/SimReplay.csproj",
            "tools/CombatBehavior/CombatBehavior.csproj",
            "tools/SimulationSmoke/SimulationSmoke.csproj",
            "tools/FogOfWarQa/FogOfWarQa.csproj",
            "tools/PerfSmoke/PerfSmoke.csproj",
            "tools/PlayerLoopQa/PlayerLoopQa.csproj",
            "tools/CounterReadabilityQa/CounterReadabilityQa.csproj",
            "tools/VerifyAll/VerifyAll.csproj",
        })
        {
            ReviewGateSource.RequireFile(root, result, project.Split('/'));
        }
    }

    private static void RequireVerifyAllCoverage(string root, GateResult result)
    {
        var verifyAll = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        foreach (var token in new[]
        {
            "tools/SimReplay/SimReplay.csproj",
            "tools/CombatBehavior/CombatBehavior.csproj",
            "tools/FogOfWarQa/FogOfWarQa.csproj",
            "tools/ReviewGate/ReviewGate.csproj",
            "tools/PerfSmoke/PerfSmoke.csproj",
            "tools/CounterReadabilityQa/CounterReadabilityQa.csproj",
        })
        {
            RequireText(verifyAll, token, $"VerifyAll must run {token}.", result);
        }
    }

    private static void RequireDeterministicEvidence(string root, GateResult result)
    {
        ReviewGateSource.RequireAnyText(root, result, "AssertDeterministic", "tools/SimReplay");
        ReviewGateSource.RequireAnyText(root, result, "upgrade-progression", "tools/SimReplay");
        ReviewGateSource.RequireAnyText(root, result, "DeterministicStateHash", "scripts/core/entities", "tools/SimReplay", "tools/CombatBehavior");
        ReviewGateSource.RequireAnyText(root, result, "CommandAttackUnits", "tools/AiOpponentLoopQa", "tools/CombatBehavior", "scripts");
    }

    private static void RequireSimHotAllocationEvidence(string root, GateResult result)
    {
        var pathfinding = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "PathfindingSystem.cs");
        RequireText(pathfinding, "_sharedPlanned", "PathfindingSystem must reuse the shared-corridor planned set.", result);
        RequireText(pathfinding, "_sharedGroups", "PathfindingSystem must reuse shared-corridor grouping buffers.", result);
        RequireText(pathfinding, "_sharedAssignments", "PathfindingSystem must reuse shared-corridor assignment lookup.", result);
        RequireText(pathfinding, "_seenObstacles", "PathfindingSystem must reuse blocker de-duplication storage.", result);
        ForbidText(pathfinding, "new HashSet<GridObstacle>()", "PathfindingSystem must not allocate blocker HashSets per path build.", result);
        ForbidText(pathfinding, "new Dictionary<SharedMoveKey", "PathfindingSystem must not allocate shared-corridor dictionaries per tick.", result);

        var production = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProductionSystem.cs");
        RequireText(production, "_producerStepBuffer", "ProductionSystem must reuse its producer tick snapshot.", result);
        RequireText(production, "_spawnObstacles", "ProductionSystem must reuse spawn obstacle storage.", result);
        ForbidText(production, "OrderedEntities.ToList()", "ProductionSystem must not snapshot all entities with ToList each tick.", result);
        ForbidText(production, ".Where(entity => entity.Id.Value != producer.Id.Value)", "ProductionSystem spawn obstacles must not be built with LINQ chains.", result);

        var spawnMath = ReviewGateSource.Read(root, "scripts", "core", "production", "ProductionSpawnMath.cs");
        RequireText(spawnMath, "DirectionOffsets", "ProductionSpawnMath must keep candidate directions as static data.", result);
        RequireText(spawnMath, "RingScales", "ProductionSpawnMath must keep ring scales as static data.", result);
        ForbidText(spawnMath, "CandidateDirections(", "ProductionSpawnMath must not allocate a candidate-direction list per spawn.", result);

        var projectiles = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProjectileSystem.cs");
        ForbidText(projectiles, "OrderedEntities.ToArray()", "ProjectileSystem must not snapshot all entities every tick.", result);
    }
}
