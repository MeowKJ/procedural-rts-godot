static class ArchitectureReviewGate
{
    public static void Check(string root, GateResult result)
    {
        UnitBattlefieldCommandDomainReviewGate.Check(root, result);
        RequireCoreFiles(root, result);
        RequireEntityWorldPipeline(root, result);
        RequireCommandBoundary(root, result);
        RequirePlayerControlContracts(root, result);
        CommandGatewayReviewGate.Check(root, result);
        RequireMovementGridConvergence(root, result);
        ForbidDuplicatedWeaponRangeHelpers(root, result); WeaponEngagementConvergenceReviewGate.Check(root, result);
        CombatChemistryReviewGate.Check(root, result);
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
        ReviewGateSource.RequireTextInFile(root, result, "private readonly UnitBattlefield _unitBattlefield", "scripts", "BattleRoot.cs"); ReviewGateSource.RequireTextInFile(root, result, "AdvanceSimulation", "scripts", "battle-root", "BattleRoot.Process.cs");
        ReviewGateSource.RequireTextInFile(root, result, "SimulationTick", "scripts", "core", "units", "runtime", "UnitBattlefield.cs"); ReviewGateSource.RequireTextInFile(root, result, "ConfigureLiveGameplay", "scripts", "core", "sim", "SimSystemPipeline.cs"); ReviewGateSource.RequireTextInFile(root, result, "_unitBattlefield.EntityWorld.ResourceAtmosphere = atmosphere;", "scripts", "battle-root", "BattleRoot.EntityWorld.cs");
        ReviewGateSource.RequireTextInFile(root, result, "ResourceAtmosphereFor(WorldVisualTheme", "scripts", "core", "presentation", "theme", "WorldThemeMath.cs");
        ReviewGateSource.RequireAnyText(root, result, "EntityProjection", "scripts/battle-root/BattleRoot.EntityWorld.cs", "scripts/world", "scripts/core/units/runtime");
    }

    private static void RequireCommandBoundary(string root, GateResult result)
    {
        ReviewGateSource.RequireAnyText(root, result, "EntityCommandBuffer", "scripts/controllers", "scripts/core/units/runtime", "tools/CombatBehavior");
        ReviewGateSource.RequireAnyText(root, result, "MoveEntityCommand", "scripts", "tools/SimReplay", "tools/CombatBehavior");
        ReviewGateSource.RequireAnyText(root, result, "GroupAttackEntityCommand", "scripts", "tools/SimReplay", "tools/CounterReadabilityQa");
        ReviewGateSource.RequireAnyText(root, result, "UpgradeResolver", "scripts/core/sim", "tools/SimReplay");
    }

    private static void RequirePlayerControlContracts(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "players", "PlayerControllerContracts.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "players", "ObservationView.cs");
        var contracts = ReviewGateSource.Read(root, "scripts", "core", "players", "PlayerControllerContracts.cs");
        var observation = ReviewGateSource.Read(root, "scripts", "core", "players", "ObservationView.cs");
        RequireText(contracts, "public interface IPlayerController", "Player control contract must define IPlayerController.", result);
        RequireText(contracts, "public interface IPlayerAgent", "Player control contract must define IPlayerAgent.", result);
        RequireText(contracts, "PlayerControllerContext", "Player control contract must pass fixed-tick context.", result);
        RequireText(contracts, "ObservationView", "Player control contract must read through ObservationView.", result);
        RequireText(contracts, "PlayerCommand", "Player control contract must output PlayerCommand intent.", result);
        RequireText(contracts, "PlayerControllerResult", "Player control contract must return structured controller results.", result);
        RequireText(observation, "public readonly record struct ObservedEntity", "ObservationView must expose visible entity summaries.", result);
        RequireText(observation, "public readonly record struct ObservedPlayerState", "ObservationView must expose player-state summaries.", result);
        RequireText(observation, "public readonly record struct ObservedCommandAffordance", "ObservationView must expose command affordances.", result);
        RequireText(observation, "IReadOnlyList<ObservedEntity> VisibleEntities", "ObservationView visible entities must be read-only.", result);
        RequireText(observation, "IReadOnlyList<ObservedCommandAffordance> CommandAffordances", "ObservationView command affordances must be read-only.", result);

        foreach (var forbidden in new[] { "using Godot", "UnitBattlefield", "EntityWorld", "Node", "SceneTree", "_Process" })
        {
            ReviewGateSource.ForbidTextInSources(root, result, forbidden, "scripts/core/players");
        }
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
    }

    private static void ForbidDuplicatedWeaponRangeHelpers(string root, GateResult result)
    {
        var weaponMath = ReviewGateSource.Read(root, "scripts", "core", "sim", "weapon", "WeaponMath.cs");
        RequireText(weaponMath, "BaseRange(EntityWorld world, EntityInstance attacker, WeaponUserComponentState weapon)", "WeaponMath must own non-deploy weapon range math.", result);

        var systemsRoot = Path.Combine(root, "scripts", "core", "sim", "systems");
        if (!Directory.Exists(systemsRoot))
        {
            result.Error("Simulation systems source directory is missing.");
            return;
        }

        foreach (var path in Directory.EnumerateFiles(systemsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            ForbidRegex(File.ReadAllText(path), @"private\s+static\s+float\s+WeaponRange\s*\(", $"{relative} must call WeaponMath for weapon range math instead of defining a private WeaponRange helper.", result);
        }
    }

}
