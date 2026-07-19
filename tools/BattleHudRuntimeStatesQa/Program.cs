using System.Diagnostics;
using ProceduralRts.Core;

var failures = new List<string>();
var states = BattleHudRuntimeStateCatalog.States;
var resolutions = BattleHudRuntimeStateCatalog.Resolutions;
var config = BattleHudRuntimeStateCatalog.CaptureConfig;
var visualGateCases = new List<BattleHudVisualGateCase>();

Require(states.Count == 6, "runtime manifest must own exactly the six #604 states", failures);
Require(config == new BattleHudRuntimeCaptureConfig(
        GameLanguage.English,
        2400,
        1729,
        EnemyDifficulty.Normal,
        LaunchMode.Skirmish,
        WorldVisualTheme.DayCommand,
        8,
        6),
    "runtime capture config must freeze language, credits, seed, difficulty, launch mode, theme, settle frames, and render flush frames", failures);
Require(resolutions.SequenceEqual(
    [
        new BattleHudCaptureResolution(1280, 720),
        new BattleHudCaptureResolution(1600, 900),
        new BattleHudCaptureResolution(1920, 1080),
    ]), "runtime manifest must own the three accepted desktop resolutions", failures);
Require(states.Select(state => state.Kind).Distinct().Count() == states.Count, "runtime state kinds must be unique", failures);
Require(states.Select(state => state.CaptureId).Distinct(StringComparer.Ordinal).Count() == states.Count, "runtime capture ids must be unique", failures);

var captureFiles = states
    .SelectMany(state => resolutions.Select(state.CaptureFileName))
    .ToArray();
Require(captureFiles.Length == 18, "six states across three resolutions must produce 18 captures", failures);
Require(captureFiles.Distinct(StringComparer.Ordinal).Count() == captureFiles.Length, "runtime capture filenames must be unique", failures);

foreach (var state in states)
{
    Require(state.SourceKind is BattleHudRuntimeSourceKind.ReadOnlyProjection or BattleHudRuntimeSourceKind.CommandIntent,
        $"{state.Kind} must map to a read-only projection or command intent", failures);
    Require(state.SourceKind == BattleHudRuntimeSourceKind.CommandIntent
        ? state.CommandIntent != BattleHudCommandIntentKind.None
        : state.CommandIntent == BattleHudCommandIntentKind.None,
        $"{state.Kind} source and command intent must agree", failures);
    Require(state.Projection.Credits >= 0, $"{state.Kind} credits must be non-negative", failures);
    foreach (var resolution in resolutions)
    {
        visualGateCases.Add(BattleHudVisualGate.Validate(state, resolution, failures));
    }
}
Require(visualGateCases.Count == 18, "visual gate catalog must cover all 18 state-resolution captures", failures);

var empty = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.Empty).Projection;
Require(empty.Selection.Kind == BattleHudSelectionKind.None && !empty.Production.Visible && empty.Alert is null,
    "empty must be a clean no-selection projection", failures);
Require(empty.StanceStrip == UnitStanceStripProjection.None,
    "empty must keep the stance strip on its zero-selection projection", failures);

var unit = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.UnitSelected).Projection;
Require(unit.Selection.Kind == BattleHudSelectionKind.Unit && !unit.Production.Visible,
    "unit-selected must expose selection detail without production state", failures);
Require(unit.StanceStrip.State == UnitStanceStripSelectionState.Uniform
    && unit.StanceStrip.IsSelected(UnitStance.Hold),
    "unit-selected must source a uniform Hold stance projection for the pilot screenshot", failures);

var building = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.ProductionBuildingSelected).Projection;
Require(building.Selection.Kind == BattleHudSelectionKind.ProductionBuilding
    && building.Production.Visible
    && building.Production.EnoughCredits,
    "production-building-selected must expose an available production projection", failures);

var unavailableSpec = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.UnavailableLowResources);
Require(unavailableSpec.SourceKind == BattleHudRuntimeSourceKind.ReadOnlyProjection
    && unavailableSpec.CommandIntent == BattleHudCommandIntentKind.None
    && unavailableSpec.Projection.Production.Visible
    && !unavailableSpec.Projection.Production.EnoughCredits
    && unavailableSpec.Projection.Credits < unavailableSpec.Projection.Production.Cost,
    "unavailable/low-resource must remain an authoritative read-only credit projection", failures);

var queue = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.QueueProgress).Projection.Production;
Require(queue.QueuedCount > 0 && queue.ActiveProgress is > 0 and < 1 && queue.CanCancel,
    "queue-progress must expose active progress and a cancel intent affordance", failures);

var alert = BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.Alert).Projection.Alert;
Require(alert is { Kind: AlertKind.Economy, RemainingRatio: > 0 },
    "alert must expose a visible read-only economy alert projection", failures);

var root = FindRoot();
var applicator = Read(root, "scripts", "ui", "hud", "HudLayer.RuntimeStates.cs");
var runtimeProbe = Read(root, "scripts", "ui", "hud", "HudLayer.VisualQa.cs");
var capture = Read(root, "scripts", "VisualQaCaptureRoot.cs");
var harness = Read(root, "tools", "VisualQaCapture.sh");
var workflow = Read(root, ".github", "workflows", "verify-all.yml");
var visualGate = Read(root, "tools", "BattleHudRuntimeStatesQa", "BattleHudVisualGate.cs");
var manifestWriter = Read(root, "tools", "BattleHudRuntimeStatesQa", "BattleHudVisualArtifactManifest.cs");
var productionBattleRoot = Read(root, "scripts", "BattleRoot.cs")
    + string.Join("\n", Directory.EnumerateFiles(
        Path.Combine(root, "scripts", "battle-root"),
        "*.cs",
        SearchOption.TopDirectoryOnly).Select(File.ReadAllText));
RequireText(applicator, "ApplyBattleHudRuntimeProjection(BattleHudRuntimeProjection projection)",
    "HudLayer must consume the typed read-only runtime projection", failures);
RequireText(applicator, "SetSelectedUnitStance(projection.StanceStrip.SelectedStance, projection.StanceStrip.SelectedUnitCount)",
    "runtime state applicator must feed the source stance projection through HudLayer", failures);
Require(!applicator.Contains("UnitBattlefield", StringComparison.Ordinal),
    "runtime state applicator must not reach into gameplay authority", failures);
Require(!applicator.Contains("SetProcess(false)", StringComparison.Ordinal)
    && !productionBattleRoot.Contains("SetProcess(false)", StringComparison.Ordinal)
    && !productionBattleRoot.Contains("SetPhysicsProcess(false)", StringComparison.Ordinal),
    "capture-only authority freeze must not leak into HudLayer or production BattleRoot sources", failures);
RequireText(capture, "CaptureBattleHudRuntimeStates", "Visual QA must capture the runtime state manifest", failures);
Require(!capture.Contains("SetSandboxDeveloperControlsVisible(false)", StringComparison.Ordinal),
    "runtime capture must not hide sandbox controls and mask the real launch gate", failures);
RequireText(capture, "AssertNormalSkirmishSandboxHidden", "runtime capture must assert the real Skirmish sandbox gate", failures);
RequireText(capture, "AssertBattleHudRuntimeCaptureConfig(config)",
    "runtime capture must assert the manifest scenario against the live BattleRoot", failures);
RequireText(capture, "options.StartingCredits != config.StartingCredits",
    "runtime capture must verify live starting credits", failures);
RequireText(capture, "options.MapSeed != config.MapSeed",
    "runtime capture must verify the live map seed", failures);
RequireText(capture, "options.EnemyDifficulty != config.EnemyDifficulty",
    "runtime capture must verify live enemy difficulty", failures);
RequireText(capture, "GameText.CurrentLanguage != config.Language",
    "runtime capture must verify the live localization language", failures);
RequireText(capture, "visualTheme.Current != config.Theme",
    "runtime capture must verify the live settled visual theme", failures);
RequireText(capture, "RequiredNode<Control>(\"SandboxDeveloperPanel\").Visible",
    "runtime capture must read the actual SandboxDeveloperPanel visibility", failures);
RequireText(capture, "StageDeterministicBattleCapture", "each Battle load must stage the fixed skirmish config", failures);
RequireText(capture, "await LoadScene(BattleScenePath);", "each runtime state must start from a fresh Battle scene", failures);
RequireOrdered(capture, failures,
    "private async Task CaptureBattleHudRuntimeStates(",
    "await LoadScene(BattleScenePath);",
    "AssertNormalSkirmishSandboxHidden();",
    "FreezeBattleHudRuntimeProjectionAuthority();",
    "foreach (var resolution in BattleHudRuntimeStateCatalog.Resolutions)");
RequireText(capture, "battle.SetProcess(false);",
    "runtime fixture must stop live BattleRoot refresh from overwriting the typed projection", failures);
RequireText(capture, "battle.SetPhysicsProcess(false);",
    "runtime fixture must stop live BattleRoot physics while responsive HUD layout settles", failures);
RequireText(capture, "GuiGetFocusOwner()?.ReleaseFocus()", "runtime captures must clear transient UI focus", failures);
RequireText(capture, "NextFrames(config.SettleFrames)", "runtime captures must use the manifest settle-frame contract", failures);
RequireOrdered(capture, failures,
    "private async Task CaptureBattleHudRuntimeResolution(",
    "GetTree().Paused = false;",
    "SetCaptureSize(new Vector2I(resolution.Width, resolution.Height));",
    "hud.ApplyBattleHudRuntimeProjection(state.Projection);",
    "GetViewport().GuiGetFocusOwner()?.ReleaseFocus();",
    "await NextFrames(config.SettleFrames);",
    "AssertBattleHudRuntimeCaptureConfig(config);",
    "AssertNormalSkirmishSandboxHidden();",
    "hud.ProbeBattleHudRuntimeStructure(",
    "GetTree().Paused = true;",
    "await Capture(",
    "state.CaptureFileName(resolution),",
    "config.RenderFlushFrames);");
RequireText(runtimeProbe, "control.IsVisibleInTree()",
    "runtime visual gate must read real Control tree visibility", failures);
RequireText(runtimeProbe, "control.GetGlobalRect()",
    "runtime visual gate must read real global Control rectangles", failures);
RequireText(runtimeProbe, "EffectiveAlpha(control)",
    "runtime visual gate must reject transparent critical controls", failures);
RequireText(runtimeProbe, "owner-contains:",
    "runtime visual gate must check critical child ownership containment", failures);
RequireText(runtimeProbe, "forbidden-overlap:",
    "runtime visual gate must check the bounded forbidden overlap pairs", failures);
RequireText(runtimeProbe, "payload:alert",
    "runtime visual gate must verify real alert payload and text", failures);
RequireText(runtimeProbe, "_queueMiniStack.ActiveProgress",
    "runtime visual gate must verify the real queue progress surface", failures);
RequireText(runtimeProbe, "HudLayoutMath.MinimumCommandHitTarget",
    "runtime visual gate must enforce real 44px interactive controls", failures);
RequireText(runtimeProbe, "alpha >= BattleHudRuntimeSettledAlpha",
    "runtime visual gate must require settled critical-control alpha", failures);
RequireText(runtimeProbe, "MeasureBattleHudRuntimeLabelText",
    "runtime visual gate must measure critical Label text with the real Godot font", failures);
RequireText(runtimeProbe, "label.GetMinimumSize()",
    "runtime visual gate must compare each critical Label minimum size to its allotted rect", failures);
RequireText(visualGate, "ExpectedByState",
    "typed QA must own an independent exact six-state expectation matrix", failures);
RequireText(visualGate, "RequireExactSet(",
    "typed QA must compare the production catalog to its independent oracle", failures);
Require(!runtimeProbe.Contains("foreach (var signal in state.CriticalSignals)", StringComparison.Ordinal),
    "runtime signal evidence must come from explicit live assertions, not catalog iteration", failures);
RequireText(manifestWriter, "actualSignals.SetEquals(gateCase.RequiredSignals)",
    "artifact manifest must enforce exact live signal markers", failures);
RequireText(manifestWriter, "actualRelations.SetEquals(gateCase.RequiredRelations)",
    "artifact manifest must enforce exact structural relation markers", failures);
RequireText(manifestWriter, "structural.ExactCommit",
    "artifact manifest must verify every structural result commit", failures);
RequireText(manifestWriter, "structural.CaptureRunNonce",
    "artifact manifest must verify every structural result run nonce", failures);
RequireText(capture, "WriteBattleHudRuntimeStructuralEvidence",
    "runtime capture must persist all structural probe evidence", failures);
RequireText(harness, "--write-artifact-manifest",
    "Visual QA harness must build the canonical runtime artifact manifest", failures);
Require(!harness.Contains("for state in", StringComparison.Ordinal),
    "Visual QA harness must not duplicate the typed runtime state catalog in bash", failures);
RequireOrdered(harness, failures,
    ": \"${BATTLE_HUD_CAPTURE_COMMIT:?",
    "checkout_commit=\"$(git rev-parse HEAD)\"",
    "git diff --quiet --",
    "capture_started_seconds=$SECONDS");
RequireText(harness, "BATTLE_HUD_CAPTURE_RUN_NONCE",
    "Visual QA harness must require a capture-run nonce", failures);
RequireText(workflow, "BATTLE_HUD_CAPTURE_COMMIT: ${{ github.sha }}",
    "VerifyAll visual capture must bind evidence to the exact checked-out SHA", failures);
RequireText(workflow, "BATTLE_HUD_CAPTURE_RUN_NONCE: ${{ github.run_id }}-${{ github.run_attempt }}",
    "VerifyAll visual capture must bind evidence to one workflow attempt", failures);
RequireText(workflow, "run: bash tools/VisualQaCapture.sh",
    "VerifyAll must execute the guarded visual capture harness directly", failures);
RequireText(workflow, "test -s artifacts/visual-qa/battle-hud-runtime-artifact-manifest.json",
    "VerifyAll must require the canonical manifest before artifact upload", failures);
RequireText(workflow, "test -s artifacts/visual-qa/battle-hud-runtime-structural-evidence.json",
    "VerifyAll must require structural evidence before artifact upload", failures);
RequireText(workflow, "name: verify-all-${{ github.run_id }}-${{ github.sha }}",
    "VerifyAll artifact name must bind the run and exact SHA", failures);
RequireText(workflow, "if-no-files-found: error",
    "VerifyAll must fail artifact upload when evidence is absent", failures);

Require(BattleHudRuntimeStateCatalog.For(BattleHudRuntimeStateKind.ProductionBuildingSelected).Projection.Status == "PROD READY",
    "production-ready status must fit the compact 1280 top strip", failures);
Require(unavailableSpec.Projection.Status == "LOW CREDITS",
    "low-resource status must fit the compact 1280 top strip", failures);
Require(building.Selection.Detail == "SELL REFUND 300",
    "building refund signal must remain complete in the compact 1280 detail drawer", failures);
Require(states.All(state => state.Projection.Status.Length <= 13),
    "runtime top-strip status copy must stay within the compact reference budget", failures);

if (failures.Count > 0)
{
    throw new InvalidOperationException("BattleHudRuntimeStatesQa FAILED:\n" + string.Join("\n", failures));
}

if (args is [
    "--write-artifact-manifest",
    var manifestPath,
    var structuralEvidencePath,
    var exactCommit,
    var captureRunNonce])
{
    BattleHudVisualArtifactManifestWriter.Write(
        manifestPath,
        structuralEvidencePath,
        exactCommit,
        captureRunNonce,
        ReadRepositoryHead(root),
        visualGateCases);
    Console.WriteLine($"Battle HUD artifact manifest written: {manifestPath} ({visualGateCases.Count} captures, {exactCommit})");
}
else if (args.Length == 0)
{
    Console.WriteLine("BattleHudRuntimeStatesQa PASSED: one typed catalog defines 18 deterministic captures and their runtime probe contracts.");
}
else
{
    throw new ArgumentException(
        "Usage: BattleHudRuntimeStatesQa [--write-artifact-manifest <manifest-path> <structural-evidence-path> <exact-commit> <capture-run-nonce>]");
}

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static void RequireText(string source, string expected, string message, List<string> failures) =>
    Require(source.Contains(expected, StringComparison.Ordinal), message, failures);

static void RequireOrdered(string source, List<string> failures, params string[] markers)
{
    var cursor = 0;
    foreach (var marker in markers)
    {
        var index = source.IndexOf(marker, cursor, StringComparison.Ordinal);
        if (index < 0)
        {
            failures.Add($"runtime capture order is missing or misplaced: {marker}");
            return;
        }

        cursor = index + marker.Length;
    }
}

static string Read(string root, params string[] parts) =>
    File.ReadAllText(Path.Combine([root, .. parts]));

static string FindRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ProceduralRts.csproj")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not locate ProceduralRts.csproj.");
}

static string ReadRepositoryHead(string root)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        },
    };
    process.StartInfo.ArgumentList.Add("rev-parse");
    process.StartInfo.ArgumentList.Add("HEAD");
    process.Start();
    var output = process.StandardOutput.ReadToEnd().Trim();
    var error = process.StandardError.ReadToEnd().Trim();
    process.WaitForExit();
    if (process.ExitCode != 0 || output.Length != 40 || !output.All(Uri.IsHexDigit))
    {
        throw new InvalidOperationException($"Could not resolve repository HEAD: {error}");
    }

    return output.ToLowerInvariant();
}
