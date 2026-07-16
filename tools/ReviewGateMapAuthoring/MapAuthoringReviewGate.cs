static class MapAuthoringReviewGate
{
    public static void Check(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapSpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapLoader.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidator.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidationException.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "Program.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "PlacementReservationScenarios.cs");
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
            "MapBuildingPlacementValidator.EnsureValid(spec);\n\n        world.WorldWidth",
            "MapLoader must reject invalid building placement before mutating the world.", result);
        var placementValidator = ReviewGateSource.Read(root, "scripts", "core", "map", "MapBuildingPlacementValidator.cs");
        RequireText(placementValidator, "PlacementReservationMath.WorldRect(", "Map validation must rotate shared reservation metadata.", result);
        RequireText(placementValidator, "PlacementMath.ViolatesClearance(", "Map validation must use shared pair-clearance geometry.", result);
        RequireText(placementValidator, "MapBuildingPlacementConflictKind.Reserved", "Map validation must retain a stable reserved reason.", result);
        var baker = ReviewGateSource.Read(root, "tools", "MapAuthoringQa", "GodotSceneMapBaker.cs");
        RequireText(baker, "MapBuildingPlacementValidator.EnsureValid(spec);", "Authored scene baking must use the shared placement guard.", result);
        var simReplayMap = ReviewGateSource.Read(root, "tools", "SimReplayContent", "MapAuthoringScenarios.cs");
        RequireText(simReplayMap, "MapLoader.Load", "SimReplay must replay authored maps through MapLoader.", result);
        RequireText(simReplayMap, "AssertDeterministic", "SimReplay map authoring scenario must be deterministic.", result);
    }
}
