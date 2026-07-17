static class MapAuthoringBakePlayReviewGate
{
    public static void Check(string root, GateResult result)
    {
        foreach (var path in new[]
        {
            "addons/map_authoring/editor/MapAuthoringArtifactPath.cs",
            "addons/map_authoring/editor/MapAuthoringArtifactWriter.cs",
            "addons/map_authoring/editor/MapAuthoringAtomicFileSystem.cs",
            "addons/map_authoring/editor/MapAuthoringPlayProcess.cs",
            "addons/map_authoring/editor/MapAuthoringPlaySession.cs",
            "addons/map_authoring/editor/MapAuthoringValidationFeature.Actions.cs",
            "addons/map_authoring/samples/AuthoredMapPreview.tscn",
            "assets/maps/authored-map-preview.mapspec.json",
            "scripts/map-preview/AuthoredMapPreviewRequest.cs",
            "scripts/core/map/artifacts/MapArtifactPathPolicy.cs",
            "scripts/map-preview/AuthoredMapPreviewRuntime.cs",
            "scripts/map-preview/AuthoredMapPreviewBootstrap.cs",
            "scenes/AuthoredMapPreviewBootstrap.tscn",
            "tools/MapAuthoringBakePlayQa/MapAuthoringBakePlayQa.csproj",
            "tools/map-authoring-bake-play-smoke.sh",
            "tools/map-authoring-export-pack-qa.sh",
        }) ReviewGateSource.RequireFile(root, result, path.Split('/'));

        var runner = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringValidationRunner.cs");
        RequireText(runner, "MapAuthoringEvaluation", "Validate/Bake/Play must share one immutable fresh evaluation outcome.", result);
        RequireText(runner, "MapSpecSnapshot.Create(map)", "Clean evaluation must own a deep MapSpec snapshot.", result);
        var pathGuard = ReviewGateSource.Read(root, "scripts", "core", "map", "artifacts", "MapArtifactPathPolicy.cs");
        RequireText(pathGuard, "res://assets/maps/", "Editor artifacts must remain under assets/maps.", result);
        RequireText(pathGuard, "FileAttributes.ReparsePoint", "Shared path policy must reject reparse segments.", result);
        RequireText(pathGuard, "RejectLinkOrReparse(assets", "Shared path policy must inspect the assets ancestor.", result);
        var writer = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringArtifactWriter.cs");
        foreach (var token in new[] { "FileOptions.WriteThrough", "Flush(flushToDisk: true)", "MapSpecArtifactCodec.Decode", "ReplaceExisting" })
            RequireText(writer, token, $"Transactional writer must retain {token}.", result);

        var session = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringPlaySession.cs");
        var process = ReviewGateSource.Read(root, "addons", "map_authoring", "editor", "MapAuthoringPlayProcess.cs");
        RequireText(process, "OS.CreateProcess", "Editor Play must spawn an isolated Godot process.", result);
        RequireText(process, "if (headless) result.Add(\"--headless\")", "Headless editors must spawn headless preview children.", result);
        foreach (var token in new[] { "--path", "--scene", "--authored-map-preview", "--authored-map-sha256" })
            RequireText(process, token, $"Owned Play transport must retain {token}.", result);
        foreach (var token in new[] { "_process.Kill(_ownedPid)", "|| !_process.IsRunning(_ownedPid)", "LastDisposeError" })
            RequireText(session, token, $"Owned Play transport must retain {token}.", result);
        ForbidText(session, "StageAuthoredMap", "Editor process must never stage runtime authored state.", result);
        ForbidText(session, "user://", "Production Play transport must not persist a user request.", result);

        var bootstrap = ReviewGateSource.Read(root, "scripts", "map-preview", "AuthoredMapPreviewBootstrap.cs");
        if (Occurrences(bootstrap, "OS.GetCmdlineUserArgs()") != 1)
            result.Error("Runtime bootstrap must read user command-line arguments exactly once.");
        var runtime = ReviewGateSource.Read(root, "scripts", "map-preview", "AuthoredMapPreviewRuntime.cs");
        RequireText(runtime, "MapSpecArtifactCodec.Decode", "Runtime preview must strictly decode canonical bytes.", result);
        RequireText(runtime, "SkirmishSetupState.StageAuthoredMap", "Runtime preview must use only #453 handoff.", result);
        ForbidText(runtime, ".tscn", "Runtime preview loader must never parse an authoring scene.", result);
        ForbidText(runtime, "user://", "Runtime preview must not read a persistent request.", result);
        RequireText(runtime, "Godot.FileAccess.Open", "Fixed menu preview must read its resource from the export PCK.", result);
        if (Occurrences(runtime, "File.ReadAllBytes(absolute)") != 1)
            result.Error("Only CLI child loading may use one absolute filesystem read.");

        var setup = ReviewGateSource.Read(root, "scripts", "core", "match", "SkirmishOptions.cs");
        RequireText(setup, "ClearAuthoredMapHandoff", "Runtime must expose authored-only transient cleanup.", result);
        var menu = ReviewGateSource.Read(root, "scripts", "main-menu", "MainMenuRoot.Build.cs");
        RequireText(menu, "AuthoredMapPreviewButton", "MainMenu must expose one fixed authored preview entry.", result);
        var flow = ReviewGateSource.Read(root, "scripts", "main-menu", "MainMenuRoot.Flow.cs");
        RequireText(flow, "StageCommittedSample", "Fixed menu preview must use the strict committed artifact loader.", result);
        var pause = ReviewGateSource.Read(root, "scripts", "ui", "PauseMenuLayer.cs");
        var outcome = ReviewGateSource.Read(root, "scripts", "ui", "OutcomeScreenLayer.cs");
        ForbidText(pause, "ClearAuthoredMapHandoff", "Failed pause-menu return must preserve authored restart state.", result);
        ForbidText(outcome, "ClearAuthoredMapHandoff", "Failed outcome return must preserve authored restart state.", result);
        var mainMenu = ReviewGateSource.Read(root, "scripts", "MainMenuRoot.cs");
        RequireText(mainMenu, "ClearAuthoredMapHandoff", "Successful MainMenu Ready is the authored clear authority.", result);

        var preset = ReviewGateSource.Read(root, "export_presets.cfg");
        RequireText(preset, "assets/maps/*.mapspec.json", "Export preset must include canonical MapSpec artifacts.", result);
        RequireText(preset, "addons/map_authoring/**", "Export preset must exclude editor and typed-source resources.", result);
        var packProbe = ReviewGateSource.Read(root, "tools", "map-authoring-export-pack-qa.sh");
        RequireText(packProbe, "--main-pack", "Export boundary must execute an actual PCK runtime probe.", result);

        var verify = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        foreach (var token in new[] { "map-authoring-bake-play-qa", "godot-map-authoring-sample-parity", "godot-map-authoring-export-pack", "godot-map-authoring-bake-play-smoke" })
            RequireText(verify, token, $"VerifyAll must register {token}.", result);
    }

    private static int Occurrences(string text, string token)
        => (text.Length - text.Replace(token, "", StringComparison.Ordinal).Length) / token.Length;
}
