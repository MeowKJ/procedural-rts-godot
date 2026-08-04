static class MapAuthoringValidationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        foreach (var file in new[]
        {
            "MapValidationDiagnostic.cs", "MapValidationService.cs", "MapValidationService.Mapping.cs",
            "MapBuildingPlacementGeometry.cs", "MapReachabilityValidator.cs",
        }) ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", file);
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "pathing", "PathfindingStaticGrid.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringValidationQa", "MapAuthoringValidationQa.csproj");
        ReviewGateSource.RequireFile(root, result, "tools", "map-authoring-validation-smoke.sh");
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "qa", "MapAuthoringValidationConflictAcceptance.tscn");
        ReviewGateSource.RequireFile(root, result, "addons", "map_authoring", "qa", "MapAuthoringMixedOverlayAcceptance.tscn");

        var diagnostic = ReviewGateSource.Read(root, "scripts", "core", "map", "MapValidationDiagnostic.cs");
        foreach (var code in new[]
        {
            "map.catalog.unknown", "map.id.empty", "map.id.duplicate", "map.id.runtime_invalid",
            "map.id.runtime_duplicate", "map.owner.start_count", "map.owner.unsupported", "map.owner.reference",
            "map.world.invalid_size", "map.geometry.invalid_rect", "map.geometry.invalid_circle",
            "map.geometry.invalid_cost", "map.geometry.unrepresentable_transform", "map.bounds.outside",
            "map.grid.unsnapped", "map.rotation.non_cardinal", "map.building.overlap",
            "map.building.clearance", "map.building.reserved", "map.building.terrain",
            "map.building.static_obstacle", "map.building.resource", "map.reference.missing",
            "map.reachability.owner_start",
        }) RequireText(diagnostic, code, $"Validation vocabulary must retain {code}.", result);
        RequireText(diagnostic, ".ThenBy(value => value.CodeRank)", "Diagnostics must use explicit code-rank ordering.", result);
        RequireText(diagnostic, ".ThenBy(value => value.Source.StableOrder)", "Diagnostics must use scene source order.", result);

        var service = ReviewGateSource.Read(root, "scripts", "core", "map", "MapValidationService.cs");
        ForbidText(service, "exception.Message", "Structured validation must not parse exception text.", result);
        var geometry = ReviewGateSource.Read(root, "scripts", "core", "map", "MapBuildingPlacementGeometry.cs");
        RequireText(geometry, "PlacementReservationMath.WorldRect", "Overlay and validation geometry must share reservation math.", result);
        var reachability = ReviewGateSource.Read(root, "scripts", "core", "map", "MapReachabilityValidator.cs");
        RequireText(reachability, "PathfindingStaticGrid.Build", "Reachability must reuse the runtime static grid seam.", result);
        RequireText(reachability, "PathfindingMath.FindPathWithDebug", "Reachability must reuse runtime path search.", result);
        var staticGrid = ReviewGateSource.Read(root, "scripts", "core", "pathing", "PathfindingStaticGrid.cs");
        ForbidText(staticGrid, "openEndpoints", "Validation must not punch holes in runtime static blockers.", result);
        var loader = ReviewGateSource.Read(root, "scripts", "core", "map", "MapLoader.cs");
        ForbidText(loader, "MapReachabilityValidator.EnsureValid", "Reachability must remain editor diagnostic-only.", result);
        var mapping = ReviewGateSource.Read(root, "scripts", "core", "map", "MapValidationService.Mapping.cs");
        RequireText(mapping, "conflict.BuildingIndex", "Placement diagnostics must carry explicit primary indices.", result);
        RequireText(mapping, "conflict.OtherIndex", "Placement diagnostics must carry explicit conflict indices.", result);
        ForbidText(mapping, "ReferenceEquals", "Value-equal buildings must not use record/reference searches.", result);

        var plugin = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringPlugin.cs");
        RequireText(plugin, "SetForceDrawOverForwardingEnabled();", "Map overlays must enable force draw forwarding once.", result);
        RequireText(plugin, "_ForwardCanvasForceDrawOverViewport", "Map overlays must use force draw forwarding.", result);
        ForbidText(plugin, "_ForwardCanvasDrawOverViewport", "Map overlays must not double draw through normal forwarding.", result);
        var feature = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringValidationFeature.cs");
        RequireText(feature, "RemoveDock", "Validation teardown must remove its dock.", result);
        RequireText(feature, "SetMainScreenEditor(\"2D\")", "Diagnostic navigation must switch to 2D.", result);
        RequireText(feature, "BakeActiveScene", "Validation dock must bind the transactional Bake action.", result);
        RequireText(feature, "TogglePlayActiveScene", "Validation dock must bind the owned Play/Stop action.", result);
        RequireText(feature, "MapAuthoringStaleMonitor", "Validation must track same-root scene edits.", result);
        RequireText(feature, "_report.Generation != _generation", "Stale generations must block navigation.", result);
        var runner = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringValidationRunner.cs");
        ForbidText(runner, "catch (Exception", "Unexpected projection errors must fail loudly.", result);
        var overlay = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringOverlayPlanner.cs");
        RequireText(overlay, "PlacementMath.GridSize", "Overlay grid must share placement grid authority.", result);
        RequireText(overlay, "InvalidBuildingFallback", "Invalid buildings must retain per-node fallback overlays.", result);
        ForbidText(overlay, "catch", "Per-building overlay programming failures must propagate.", result);

        var verify = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        RequireText(verify, "godot-map-authoring-validation-smoke", "VerifyAll must run validation editor smoke.", result);
        var smoke = ReviewGateSource.Read(root, "tools", "map-authoring-validation-smoke.sh");
        RequireText(smoke, "tools/MapAuthoringValidationQa/MapAuthoringValidationQa.csproj",
            "Validation smoke must run the contract QA exactly once before the editor smoke.", result);
        RequireText(smoke, "--non-headless", "Validation smoke must preserve non-headless mode.", result);
        RequireText(smoke, "--diagnostics-json", "Validation evidence must serialize all 24 exercised diagnostics.", result);
        RequireText(smoke, "MAP_AUTHORING_OUTPUT_COPY", "Validation evidence must copy actual Godot output.", result);
        var smokeDriver = ReviewGateSource.Read(root, "addons", "map_authoring", "qa", "MapAuthoringValidationSmokeScenarios.cs");
        foreach (var evidence in new[] { "diagnostic-dock.png", "source-selection.png", "conflict-selection.png", "rotated-footprint-clearance-reservations.png", "environment-markers.png", "post-reenable-clean.png" })
            RequireText(smokeDriver, evidence, $"Validation smoke must capture {evidence}.", result);
    }
}
