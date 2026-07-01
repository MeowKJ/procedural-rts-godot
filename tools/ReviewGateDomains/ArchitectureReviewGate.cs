static class ArchitectureReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireCoreFiles(root, result);
        RequireEntityWorldPipeline(root, result);
        ForbidDeletedMigrationTypes(root, result);
        RequireCommandBoundary(root, result);
        RequireMovementGridConvergence(root, result);
    }

    private static void RequireCoreFiles(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "entities", "EntityWorld.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "entities", "EntitySpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "entities", "EntityInstance.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "entities", "EntityCommandBuffer.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "progression", "UpgradeResolver.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "progression", "UpgradeState.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "sim", "EntityProjection.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "sim", "SimSystemPipeline.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "BattleRoot.cs");
    }

    private static void RequireEntityWorldPipeline(string root, GateResult result)
    {
        ReviewGateSource.RequireTextInFile(root, result, "private readonly EntityWorld _entityWorld", "scripts", "BattleRoot.cs");
        ReviewGateSource.RequireTextInFile(root, result, "SimSystemPipeline", "scripts", "BattleRoot.EntityWorld.cs");
        ReviewGateSource.RequireTextInFile(root, result, "ConfigureLiveGameplay", "scripts", "core", "sim", "SimSystemPipeline.cs");
        ReviewGateSource.RequireAnyText(root, result, "EntityProjection", "scripts/BattleRoot.EntityWorld.cs", "scripts/world", "scripts/core/units/runtime");
    }

    private static void ForbidDeletedMigrationTypes(string root, GateResult result)
    {
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "UnitDefinition.cs");
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "BuildingDefinition.cs");
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "BuildDefinition.cs");
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "BuildCatalog.cs");
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "runtime", "UnitBattlefieldBuildingTarget.cs");
        ReviewGateSource.ForbidTextInSources(root, result, "UnitBattlefieldBuildingTarget", "scripts");
        ReviewGateSource.ForbidTextInSources(root, result, "BuildingDefinition", "scripts");
        ReviewGateSource.ForbidTextInSources(root, result, "BuildDefinition", "scripts");
        ReviewGateSource.ForbidTextInSources(root, result, "BuildCatalog", "scripts");
    }

    private static void RequireCommandBoundary(string root, GateResult result)
    {
        ReviewGateSource.RequireAnyText(root, result, "EntityCommandBuffer", "scripts/controllers", "scripts/core/units/runtime", "tools/CombatBehavior");
        ReviewGateSource.RequireAnyText(root, result, "MoveEntityCommand", "scripts", "tools/SimReplay", "tools/CombatBehavior");
        ReviewGateSource.RequireAnyText(root, result, "GroupAttackEntityCommand", "scripts", "tools/SimReplay", "tools/CounterReadabilityQa");
        ReviewGateSource.RequireAnyText(root, result, "UpgradeResolver", "scripts/core/sim", "tools/SimReplay");
    }

    private static void RequireMovementGridConvergence(string root, GateResult result)
    {
        var movement = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "sim", "systems", "MovementSystem.cs"));
        RequireText(movement, "SpatialGrid<LocalAvoidanceBody>", "MovementSystem local avoidance must use the shared SpatialGrid idiom.", result);
        RequireText(movement, "LocalAvoidanceMath.ResolveVector(body, _avoidanceGrid", "MovementSystem must query local avoidance through the shared grid.", result);
        ForbidText(movement, "BuildHashInto", "MovementSystem must not rebuild a private spatial hash for local avoidance.", result);
        ForbidText(movement, "Dictionary<GridObstacle, List<LocalAvoidanceBody>>", "MovementSystem must not own a second grid dictionary style.", result);
        var policy = ReviewGateSource.Read(root, "scripts", "core", "pathing", "AdvancedPathingPolicy.cs");
        RequireText(policy, "UseSpatialGridLocalAvoidance", "AdvancedPathingPolicy should name the shared spatial-grid avoidance path.", result);
        ForbidText(policy, "UseSpatialHashLocalAvoidance", "AdvancedPathingPolicy must not advertise the retired spatial-hash avoidance path.", result);
    }
}
