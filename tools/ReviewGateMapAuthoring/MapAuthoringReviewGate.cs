static class MapAuthoringReviewGate
{
    public static void Check(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapSpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapLoader.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidator.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidator.Environment.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidationException.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapEnvironmentSpecValidator.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapRuntimeEnvironment.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapPlacementRules.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "Program.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "MapEnvironmentScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "PlacementReservationScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "SimReplayContent", "MapRuntimeEnvironmentScenarios.cs");
        ReviewGateSource.RequireTextInFile(root, result, "map-authoring-qa", "tools", "VerifyAll", "Program.cs");
        ReviewGateSource.RequireTextInFile(root, result, "RunMapAuthoringScenario", "tools", "SimReplay", "Program.cs");
        var mapSpec = ReviewGateSource.Read(root, "scripts", "core", "map", "MapSpec.cs");
        ForbidText(mapSpec, "using Godot", "MapSpec must stay pure C# without Godot imports.", result);
        ForbidText(mapSpec, "Vector2", "MapSpec must not expose Godot Vector2.", result);
        ForbidText(mapSpec, "Godot.Color", "MapSpec must not expose Godot Color.", result);
        var loader = ReviewGateSource.Read(root, "scripts", "core", "map", "MapLoader.cs");
        ForbidText(loader, ".tscn", "MapLoader must never read Godot scene files.", result);
        RequireText(
            loader,
            "MapBuildingPlacementValidator.EnsureValid(spec);\n        var environment = MapRuntimeEnvironment.From(spec);\n\n        world.WorldWidth",
            "MapLoader must validate and construct the environment before mutating the world.", result);
        RequireText(loader, "world.InstallMapEnvironment(environment);", "MapLoader must install the validated immutable environment.", result);
        var placementValidator = ReviewGateSource.Read(root, "scripts", "core", "map", "MapBuildingPlacementValidator.cs");
        RequireText(placementValidator, "PlacementReservationMath.WorldRect(", "Map validation must rotate shared reservation metadata.", result);
        RequireText(placementValidator, "PlacementMath.ViolatesClearance(", "Map validation must use shared pair-clearance geometry.", result);
        RequireText(placementValidator, "MapBuildingPlacementConflictKind.Reserved", "Map validation must retain a stable reserved reason.", result);
        var environmentValidator = ReviewGateSource.Read(root, "scripts", "core", "map", "MapBuildingPlacementValidator.Environment.cs");
        RequireText(environmentValidator, "MapBuildingPlacementConflictKind.Terrain", "Map validation must retain stable terrain evidence.", result);
        RequireText(environmentValidator, "MapBuildingPlacementConflictKind.StaticObstacle", "Map validation must retain stable static-obstacle evidence.", result);
        RequireText(environmentValidator, "MapBuildingPlacementConflictKind.Resource", "Map validation must retain stable resource evidence.", result);
        RequireText(environmentValidator, "PlacementMath.ViolatesClearance", "Environment placement must share canonical clearance geometry.", result);
        RequireText(environmentValidator, "MapPlacementRules.ResourceClearance(spec)", "Map validation must use the shared one-cell resource rule.", result);
        var runtimeEnvironment = ReviewGateSource.Read(root, "scripts", "core", "map", "MapRuntimeEnvironment.cs");
        RequireText(runtimeEnvironment, "Array.AsReadOnly", "Runtime map environment must own immutable collection copies.", result);
        RequireText(runtimeEnvironment, "AppendAuthoredTerrainGrid", "Runtime environment must rasterize authored terrain for pathfinding.", result);
        RequireText(runtimeEnvironment, "AppendStaticObstacleGrid", "Runtime environment must rasterize static obstacles for pathfinding.", result);
        var constructionPlacement = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "construction", "ConstructionSystem.PlacementQueries.cs");
        RequireText(constructionPlacement, "world.MapEnvironment.StaticObstacles", "Construction must consume static environment rectangles.", result);
        RequireText(constructionPlacement, "ResourceNodeComponentState", "Construction must treat live resource nodes as placement exclusions.", result);
        RequireText(constructionPlacement, "MapPlacementRules.ResourceClearance(spec)", "Construction must use the shared one-cell resource rule.", result);
        RequireText(constructionPlacement, "world.MapEnvironment.SampleTerrain", "Construction must consume authored terrain with procedural fallback.", result);
        var pathfinding = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "PathfindingSystem.cs");
        RequireText(pathfinding, "world.MapEnvironment.AppendAuthoredTerrainGrid", "Pathfinding must consume authored terrain overrides.", result);
        RequireText(pathfinding, "world.MapEnvironment.AppendStaticObstacleGrid", "Pathfinding must consume authored static blockers.", result);
        RequireText(pathfinding, "group[0].Domain,\n                _terrain", "Shared corridors must receive authored terrain.", result);
        RequireText(pathfinding, "domain,\n            _terrain", "Single-entity paths must receive authored terrain.", result);
        var baker = ReviewGateSource.Read(root, "tools", "MapAuthoringQa", "GodotSceneMapBaker.cs");
        RequireText(baker, "MapBuildingPlacementValidator.EnsureValid(spec);", "Authored scene baking must use the shared placement guard.", result);
        var simReplayMap = ReviewGateSource.Read(root, "tools", "SimReplayContent", "MapAuthoringScenarios.cs");
        RequireText(simReplayMap, "MapLoader.Load", "SimReplay must replay authored maps through MapLoader.", result);
        RequireText(simReplayMap, "AssertDeterministic", "SimReplay map authoring scenario must be deterministic.", result);
    }
}
