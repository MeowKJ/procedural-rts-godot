static class MapAuthoringReviewGate
{
    public static void Check(string root, GateResult result)
    {
        MapSpecArtifactReviewGate.Check(root, result);
        TypedMapAuthoringReviewGate.Check(root, result);
        MapAuthoringValidationReviewGate.Check(root, result);
        MapAuthoringBakePlayReviewGate.Check(root, result);
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapSpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapLoader.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidator.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidator.Environment.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapBuildingPlacementValidationException.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapEnvironmentSpecValidator.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapRuntimeEnvironment.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapOwnerTopologyValidator.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapSemanticValidator.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapPlacementRules.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "Program.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "MapEnvironmentScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "PlacementReservationScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "PlayableMapHandoffQa", "PlayableMapHandoffQa.csproj");
        ReviewGateSource.RequireFile(root, result, "tools", "PlayableMapHandoffQa", "Program.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "PlayableMapHandoffQa", "PlayableMapHandoffScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "PlayableMapHandoffQa", "MapPreflightAtomicScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "PlayableMapHandoffQa", "MapEnvironmentHashScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "SimReplayContent", "MapRuntimeEnvironmentScenarios.cs");
        ReviewGateSource.RequireTextInFile(root, result, "map-authoring-qa", "tools", "VerifyAll", "Program.cs");
        ReviewGateSource.RequireTextInFile(root, result, "playable-map-handoff-qa", "tools", "VerifyAll", "Program.cs");
        ReviewGateSource.RequireTextInFile(root, result, "RunMapAuthoringScenario", "tools", "SimReplay", "Program.cs");
        var mapSpec = ReviewGateSource.Read(root, "scripts", "core", "map", "MapSpec.cs");
        ForbidText(mapSpec, "using Godot", "MapSpec must stay pure C# without Godot imports.", result);
        ForbidText(mapSpec, "Vector2", "MapSpec must not expose Godot Vector2.", result);
        ForbidText(mapSpec, "Godot.Color", "MapSpec must not expose Godot Color.", result);
        var loader = ReviewGateSource.Read(root, "scripts", "core", "map", "MapLoader.cs");
        ForbidText(loader, ".tscn", "MapLoader must never read Godot scene files.", result);
        RequireText(
            loader,
            "public static MapRuntimeEnvironment Prepare(MapSpec spec)",
            "MapLoader must expose the shared fail-closed authored-map preparation boundary.", result);
        RequireText(
            loader,
            "MapOwnerTopologyValidator.EnsureValid(spec);\n        MapSemanticValidator.EnsureValid(spec);\n        MapBuildingPlacementValidator.EnsureValid(spec);",
            "MapLoader must complete owner, catalog/id, and placement preflight before mutation.", result);
        RequireText(
            loader,
            "MapBuildingPlacementValidator.EnsureValid(spec);\n        return MapRuntimeEnvironment.From(spec);",
            "MapLoader preparation must validate before constructing the immutable environment.", result);
        RequireText(
            loader,
            "var environment = Prepare(spec);\n\n        world.WorldWidth",
            "MapLoader must prepare the map before mutating the world.", result);
        RequireText(loader, "world.InstallMapEnvironment(environment);", "MapLoader must install the validated immutable environment.", result);
        RequireText(loader, "reservedBuildingIds", "MapLoader auto building ids must skip all explicit authored ids.", result);
        var placementValidator = ReviewGateSource.Read(root, "scripts", "core", "map", "MapBuildingPlacementValidator.cs");
        RequireText(placementValidator, "MapBuildingPlacementGeometry.Create", "Map validation must consume shared placement geometry.", result);
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
        RequireText(runtimeEnvironment, "IReadOnlyList<MapRuntimeTriggerArea>", "Runtime map environment must retain authored triggers without editor types.", result);
        RequireText(runtimeEnvironment, "IReadOnlyList<MapRuntimeNarrativeNode>", "Runtime map environment must retain authored narrative metadata without editor types.", result);
        var entityWorld = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityWorld.cs");
        RequireText(entityWorld, "MapEnvironment.OwnerStarts.Count", "Deterministic state hash must include authored owner starts.", result);
        RequireText(entityWorld, "MapEnvironment.Triggers.Count", "Deterministic state hash must include authored triggers.", result);
        RequireText(entityWorld, "MapEnvironment.Objectives.Count", "Deterministic state hash must include authored objectives.", result);
        RequireText(entityWorld, "MapEnvironment.NarrativeNodes.Count", "Deterministic state hash must include authored narrative metadata.", result);
        RequireText(runtimeEnvironment, "AppendAuthoredTerrainGrid", "Runtime environment must rasterize authored terrain for pathfinding.", result);
        RequireText(runtimeEnvironment, "AppendStaticObstacleGrid", "Runtime environment must rasterize static obstacles for pathfinding.", result);
        var constructionEnvironment = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "construction", "ConstructionSystem.PlacementEnvironment.cs");
        var constructionObstacles = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "construction", "ConstructionSystem.PlacementObstacles.cs");
        RequireText(constructionEnvironment, "world.MapEnvironment.StaticObstacles", "Construction must consume static environment rectangles.", result);
        RequireText(constructionEnvironment, "ResourceNodeComponentState", "Construction must treat live resource nodes as placement exclusions.", result);
        RequireText(constructionObstacles, "MapPlacementRules.ResourceClearance(spec)", "Construction must use the shared one-cell resource rule.", result);
        RequireText(constructionEnvironment, "world.MapEnvironment.SampleTerrain", "Construction must consume authored terrain with procedural fallback.", result);
        var pathfinding = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "sim", "systems", "PathfindingSystem.cs"));
        RequireText(pathfinding, "PathfindingStaticGrid.FillEnvironment", "Pathfinding must consume the shared authored static-grid seam.", result);
        RequireText(pathfinding, "PathfindingStaticGrid.AppendCircle", "Pathfinding must consume shared static circle rasterization.", result);
        RequireText(pathfinding, "group[0].Domain,\n                _terrain", "Shared corridors must receive authored terrain.", result);
        RequireText(pathfinding, "domain,\n            _terrain", "Single-entity paths must receive authored terrain.", result);
        var simReplayMap = ReviewGateSource.Read(root, "tools", "SimReplayContent", "MapAuthoringScenarios.cs");
        RequireText(simReplayMap, "MapLoader.Load", "SimReplay must replay authored maps through MapLoader.", result);
        RequireText(simReplayMap, "AssertDeterministic", "SimReplay map authoring scenario must be deterministic.", result);
        var battleRoot = ReviewGateSource.Read(root, "scripts", "BattleRoot.cs");
        RequireText(battleRoot, "var world = MapLoader.Load(_runtimeMapSpec);", "BattleRoot startup must load its runtime map once through MapLoader.", result);
        RequireText(battleRoot, "UnitBattlefield.AdoptLoadedMap(world, _runtimeMapSpec)", "UnitBattlefield must adopt the exact MapLoader world instead of respawning entities.", result);
        RequireText(battleRoot, "public bool DebugEntityWorldShadowEnabled => false;", "Authored BattleRoot must have no separate EntityWorld shadow.", result);
        var skirmishSetup = ReviewGateSource.Read(root, "scripts", "core", "match", "SkirmishOptions.cs");
        RequireText(skirmishSetup, "MapLoader.Prepare(map);", "Authored match staging must reject invalid maps before publishing pending state.", result);
        var authoredSkirmishFlow = ReviewGateSource.Read(root, "scripts", "qa", "SkirmishFlowQaRunner.AuthoredMap.cs");
        RequireText(authoredSkirmishFlow, "StageAuthoredMap", "SkirmishFlowQa must launch a real authored battle.", result);
        RequireText(authoredSkirmishFlow, "AssertNormalBattleAfterAuthored", "SkirmishFlowQa must prove normal restart clears authored state.", result);
        RequireText(authoredSkirmishFlow, "DebugUsesSingleAuthoredEntityWorld", "SkirmishFlowQa must prove authored BattleRoot observes the MapLoader world identity.", result);
        var playableHandoffQa = ReviewGateSource.Read(root, "tools", "PlayableMapHandoffQa", "PlayableMapHandoffScenarios.cs");
        RequireText(playableHandoffQa, "UnitBattlefield.AdoptLoadedMap", "Playable handoff QA must retain loaded-world adoption parity assertions.", result);
        var mapAuthoringProgram = ReviewGateSource.Read(root, "tools", "MapAuthoringQa", "Program.cs");
        ForbidText(mapAuthoringProgram, "PlayableMapHandoffScenarios.Run", "MapAuthoringQa must not run the extracted playable handoff suite twice.", result);
        var atomicPreflightQa = ReviewGateSource.Read(root, "tools", "PlayableMapHandoffQa", "MapPreflightAtomicScenarios.cs");
        RequireText(atomicPreflightQa, "ReferenceEquals(existing.MapEnvironment, environmentBefore)", "Map authoring QA must prove invalid preflight leaves an existing EntityWorld unchanged.", result);
        var environmentHashQa = ReviewGateSource.Read(root, "tools", "PlayableMapHandoffQa", "MapEnvironmentHashScenarios.cs");
        RequireText(environmentHashQa, "one-field", "Map authoring QA must prove environment metadata hash sensitivity.", result);
    }
}
