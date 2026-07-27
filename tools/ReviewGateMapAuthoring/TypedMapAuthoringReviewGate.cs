static class TypedMapAuthoringReviewGate
{
    private static readonly string[] NodeFiles =
    [
        "MapRoot.cs", "OwnerStart.cs", "Building.cs",
        "Unit.cs", "Resource.cs", "Obstacle.cs",
        "TerrainRegion.cs", "Trigger.cs",
        "Objective.cs", "Narrative.cs",
    ];

    private static readonly string[] SemanticIdNodeFiles =
    [
        "MapRoot.cs", "Resource.cs", "Obstacle.cs",
        "TerrainRegion.cs", "Trigger.cs",
        "Objective.cs", "Narrative.cs",
    ];

    public static void Check(string root, GateResult result)
    {
        CheckCompileBoundary(root, result);
        CheckTypedNodes(root, result);
        CheckPlugin(root, result);
        CheckProjection(root, result);
        CheckEvidence(root, result);
    }

    private static void CheckCompileBoundary(string root, GateResult result)
    {
        var project = ReviewGateSource.Read(root, "ProceduralRts.csproj");
        RequireText(project, "Condition=\"'$(Configuration)' != 'Debug'\"", "Editor authoring sources must be excluded from export configurations.", result);
        foreach (var directory in new[] { "editor", "nodes", "projection", "qa" })
        {
            RequireText(project, $"addons\\map_authoring\\{directory}\\**\\*.cs", $"Export builds must exclude map_authoring/{directory}.", result);
        }
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringCatalogQa", "MapAuthoringExportBoundaryScenarios.cs");
    }

    private static void CheckTypedNodes(string root, GateResult result)
    {
        foreach (var file in NodeFiles)
        {
            ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "nodes", file);
            var source = ReviewGateSource.Read(root, "addons", "map_authoring", "nodes", file);
            RequireText(source, "[Tool]", $"{file} must remain editor-aware.", result);
            RequireText(source, ": Node2D", $"{file} must remain a typed Node2D script.", result);
            ForbidText(source, "metadata/map_kind", "Typed nodes must not use metadata fallback.", result);
            ForbidText(source, "GlobalClass", "Typed nodes must have one plugin registration authority.", result);
        }
        foreach (var file in SemanticIdNodeFiles)
        {
            var source = ReviewGateSource.Read(root, "addons", "map_authoring", "nodes", file);
            RequireText(source, "[Export] public string Id", $"{file} must persist its MapSpec semantic id.", result);
        }
        foreach (var file in new[] { "OwnerStart.cs", "Building.cs", "Unit.cs" })
        {
            var source = ReviewGateSource.Read(root, "addons", "map_authoring", "nodes", file);
            ForbidText(source, "[Export] public string Id", $"{file} must not export an id absent from MapSpec.", result);
        }
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapAuthoringKeyCatalog.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapAuthoringCatalog.cs");
    }

    private static void CheckPlugin(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "plugin.cfg");
        var project = ReviewGateSource.Read(root, "project.godot");
        RequireText(project, "res://addons/map_authoring/plugin.cfg", "Map Authoring plugin must be enabled for editor sessions.", result);
        foreach (var runtimeSetting in new[] { "window/size/mode=0", "renderer/rendering_method=\"forward_plus\"", "anti_aliasing/quality/msaa_2d=0", "viewport/hdr_2d=false" })
        {
            RequireText(project, runtimeSetting, $"Plugin enablement must preserve runtime setting {runtimeSetting}.", result);
        }
        var plugin = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringPlugin.cs");
        RequireText(plugin, "MapAuthoringTypeRegistry.ValidateTypeNames", "Plugin must validate custom type names before mutating registration state.", result);
        var validationIndex = plugin.IndexOf("MapAuthoringTypeRegistry.ValidateTypeNames", StringComparison.Ordinal);
        var beginIndex = plugin.IndexOf("MapAuthoringRegistrationState.Begin", StringComparison.Ordinal);
        if (validationIndex < 0 || beginIndex < 0 || validationIndex > beginIndex)
        {
            result.Error("Map Authoring native type-name validation must run before registration begins.");
        }
        RequireText(plugin, "AddCustomType", "Plugin must register typed nodes.", result);
        RequireText(plugin, "RemoveCustomType", "Plugin must remove typed nodes.", result);
        RequireText(plugin, "AddInspectorPlugin", "Plugin must register one catalog Inspector.", result);
        RequireText(plugin, "RemoveInspectorPlugin", "Plugin must remove its catalog Inspector.", result);
        RequireText(plugin, "for (var index = _registeredTypes.Count - 1", "Plugin must tear custom types down in reverse order.", result);
        var registry = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringTypeRegistry.cs");
        foreach (var type in new[] { "MapRoot", "OwnerStart", "Building", "Unit", "ResourceField", "Obstacle", "TerrainRegion", "Trigger", "Objective", "Narrative" })
        {
            RequireText(registry, $"Type(\"{type}\"", $"Plugin registry must include {type}.", result);
        }
        RequireText(registry, "Type(\"ResourceField\", \"Resource\")", "ResourceField must intentionally map to typed Resource.cs without colliding with Godot.Resource.", result);
        ForbidText(registry, "Type(\"Resource\", \"Resource\")", "Custom type name must not collide with native Godot.Resource.", result);
        RequireText(registry, "nativeClassExists(descriptor.Name)", "Registry validation must reject native class collisions deterministically.", result);
    }

    private static void CheckProjection(string root, GateResult result)
    {
        var baker = ReviewGateSource.Read(root, "addons", "map_authoring", "baker", "GodotMapSpecBaker.cs");
        RequireText(baker, "IMapSpecSceneProjector projector", "Formal baker must accept typed projection without editor coupling.", result);
        var projector = ReviewGateSource.Read(root, "addons", "map_authoring", "projection", "TypedMapSceneProjector.cs");
        RequireText(projector, "MapSceneProjection.SceneOrder", "Typed projection must preserve shared scene preorder.", result);
        RequireText(projector, "must not use metadata/map_kind fallback", "Typed projection must reject metadata fallback.", result);
        RequireText(projector, "RejectUnsupportedMetadata(mapRoot)", "Typed projection must reject metadata on its root before traversal.", result);
        RequireText(projector, "MapRoot", "Typed projection must require typed MapRoot.", result);
        var inspector = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringInspectorPlugin.cs");
        RequireText(inspector, "MapCatalogOptionProperty", "Inspector must persist stable catalog strings.", result);
        RequireText(inspector, "MapQuarterTurnProperty", "Building Inspector must use the four-state rotation editor.", result);
        var optionProperty = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapCatalogOptionProperty.cs");
        RequireText(optionProperty, "public MapCatalogOptionProperty()", "Catalog editor property must support safe Godot hot-reload construction.", result);
        RequireText(optionProperty, "GetEditedObject()", "Catalog editor property must resolve options from its current edited object.", result);
        RequireText(optionProperty, "GetEditedProperty()", "Catalog editor property must resolve options from its current edited property.", result);
        RequireText(optionProperty, "MapAuthoringInspectorCatalog.TryOptions", "Catalog editor property must repopulate from the authoritative Inspector catalog.", result);
        RequireText(optionProperty, "GetSignalConnectionList", "Catalog editor property must rebind a surviving native control.", result);
        RequireText(optionProperty, "Disconnect", "Catalog editor property rebind must remove stale handlers.", result);
        RequireText(optionProperty, "Unknown: {value}", "Inspector must keep unknown persisted ids visible.", result);
        ForbidText(optionProperty, "options[0]", "Inspector must not replace unknown ids with its first option.", result);
        var rotations = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapBuildingQuarterTurns.cs");
        foreach (var label in new[] { "0°", "90°", "180°", "270°" }) RequireText(rotations, label, $"Building rotation must include {label}.", result);
        var rotationProperty = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapQuarterTurnProperty.cs");
        RequireText(rotationProperty, "public MapQuarterTurnProperty()", "Rotation editor property must support safe Godot hot-reload construction.", result);
        RequireText(rotationProperty, "EnsureControl()", "Rotation editor property must recreate its control after hot reload.", result);
        RequireText(rotationProperty, "GetSignalConnectionList", "Rotation editor property must rebind a surviving native control.", result);
        RequireText(rotationProperty, "Disconnect", "Rotation editor property rebind must remove stale handlers.", result);
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "projection", "MapAuthoringTransformException.cs");
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "projection", "TypedMapTransformValidation.cs");
        var environmentProjection = ReviewGateSource.Read(root, "addons", "map_authoring", "projection", "TypedMapEnvironmentProjection.cs");
        RequireText(environmentProjection, "TypedMapTransformValidation.Circle", "Resource projection must validate its effective circle basis.", result);
        RequireText(environmentProjection, "TypedMapTransformValidation.Rect", "Rect projection must reject unsupported effective bases.", result);
        var entityProjection = ReviewGateSource.Read(root, "addons", "map_authoring", "projection", "TypedMapEntityProjection.cs");
        RequireText(entityProjection, "TypedMapTransformValidation.Entity", "Entity projection must reject unsupported effective scale/skew/reflection.", result);
        RequireText(entityProjection, "RequirePersisted(node.Rotation)", "Building projection must reject modulo-equivalent persisted rotations before transform normalization.", result);
        RequireText(entityProjection, "RequireRootLocal(transform.Rotation)", "Building projection must validate final root-local cardinal rotation.", result);
        RequireText(entityProjection, "node.HasRuntimeId ? node.RuntimeId : null", "Building runtime id must use explicit presence semantics.", result);
    }

    private static void CheckEvidence(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "qa", "MapTypedProjectionQa.tscn");
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "qa", "MapAuthoringEditorAcceptance.tscn");
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "qa", "MapAuthoringUnknownCatalogAcceptance.tscn");
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "qa", "MapAuthoringEditorPersistenceSmoke.cs");
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "qa", "MapTypedTransformScenarios.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "map-authoring-plugin-smoke.sh");
        ReviewGateSource.RequireFile(root, result, "tools", "map-authoring-godot-run.sh");
        ReviewGateSource.RequireFile(root, result, "tools", "map-typed-projection-qa.sh");
        var smoke = ReviewGateSource.Read(root, "tools", "map-authoring-plugin-smoke.sh");
        RequireText(smoke, "--non-headless", "Plugin smoke must preserve a non-headless editor evidence mode.", result);
        var smokeDriver = ReviewGateSource.Read(root, "addons", "map_authoring", "qa", "MapAuthoringPluginSmokeDriver.cs");
        RequireText(smokeDriver, "OpenSceneFromPath(AcceptanceScenePath)", "Plugin smoke must open the stable editor acceptance fixture.", result);
        RequireText(smokeDriver, "ReloadSceneFromPath(AcceptanceScenePath)", "Plugin smoke must prove the editor fixture can reopen.", result);
        RequireText(smokeDriver, "unknown.visual-sentinel.building", "Plugin smoke must use a visible stable unknown-catalog sentinel.", result);
        RequireText(smokeDriver, "ReloadSceneFromPath(UnknownScenePath)", "Plugin smoke must prove unknown catalog strings survive editor reload.", result);
        RequireText(smokeDriver, "ValidateTypeNameRejectionIsSideEffectFree", "Plugin smoke must prove rejected custom type names do not mutate registration state.", result);
        if (smokeDriver.Split("MapAuthoringCreateDialogSmoke.Run", StringSplitOptions.None).Length - 1 < 2)
        {
            result.Error("Non-headless plugin smoke must validate Create Dialog before and after plugin re-enable.");
        }
        var persistence = ReviewGateSource.Read(root, "addons", "map_authoring", "qa", "MapAuthoringEditorPersistenceSmoke.cs");
        RequireText(persistence, "MapCatalogOptionProperty", "Persistence smoke must use the actual catalog Inspector property.", result);
        RequireText(persistence, "MapQuarterTurnProperty", "Persistence smoke must use the actual rotation Inspector property.", result);
        RequireText(persistence, "SaveScene()", "Persistence smoke must save an actual editor scene.", result);
        RequireText(persistence, "ReloadSceneFromPath(TempScenePath)", "Persistence smoke must reload saved values from disk.", result);
        var transformQa = ReviewGateSource.Read(root, "addons", "map_authoring", "qa", "MapTypedTransformScenarios.cs");
        RequireText(transformQa, "MapAuthoringTransformException", "Typed transform QA must prove unsupported nested transforms fail closed.", result);
        var runner = ReviewGateSource.Read(root, "tools", "map-authoring-godot-run.sh");
        RequireText(runner, "SCRIPT ERROR:", "Godot authoring QA must fail on script errors even when Godot exits zero.", result);
        RequireText(runner, "missing stable .uid sidecar", "Godot authoring QA must require stable UID sidecars for every authoring script.", result);
        RequireText(runner, "worktree changed", "Godot authoring QA must prove it leaves the worktree unchanged.", result);
        var verify = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        foreach (var step in new[] { "map-authoring-export-debug", "map-authoring-export-release", "map-authoring-catalog-qa", "godot-map-typed-projection-qa", "godot-map-authoring-plugin-smoke" })
        {
            RequireText(verify, step, $"VerifyAll must run {step}.", result);
        }
    }
}
