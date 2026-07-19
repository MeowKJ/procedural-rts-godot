using System.Diagnostics;

var root = FindProjectRoot(Directory.GetCurrentDirectory());
var options = VerifyOptions.Parse(args);
var steps = CreateSteps(root, options);
var results = new List<StepResult>(steps.Count);

Console.WriteLine("VerifyAll");
Console.WriteLine($"Root: {root}");
Console.WriteLine($"Steps: {steps.Count}");
Console.WriteLine();

foreach (var step in steps)
{
    if (step.SkipReason is not null)
    {
        Console.WriteLine($"SKIP [{step.Name}] {step.SkipReason}");
        results.Add(StepResult.CreateSkipped(step.Name, step.SkipReason));
        continue;
    }

    Console.WriteLine($"RUN  [{step.Name}] {step.CommandLine}");
    var result = RunStep(root, step);
    results.Add(result);
    Console.WriteLine($"{(result.ExitCode == 0 ? "PASS" : "FAIL")} [{step.Name}] {result.Elapsed.TotalSeconds:0.0}s exit {result.ExitCode}");
    if (result.ExitCode != 0 && !options.ContinueOnFailure)
    {
        break;
    }
}

Console.WriteLine();
Console.WriteLine("Summary:");
foreach (var result in results)
{
    var status = result.Skipped ? "SKIP" : result.ExitCode == 0 ? "PASS" : "FAIL";
    Console.WriteLine($"- {status} {result.Name}");
}

var failed = results.Any(result => !result.Skipped && result.ExitCode != 0);
var skippedRequired = results.Any(result => result.Skipped
    && !(result.Name == "balance-report" && options.AllowMissingBalanceReport));
if (failed || skippedRequired)
{
    Environment.Exit(1);
}

Console.WriteLine("VerifyAll PASSED.");

static IReadOnlyList<VerifyStep> CreateSteps(string root, VerifyOptions options)
{
    var steps = new List<VerifyStep>
    {
        Dotnet("build", "dotnet", "build ProceduralRts.csproj --no-restore"),
        Dotnet("workflow-security-qa", "dotnet", "run --project tools/WorkflowSecurityQa/WorkflowSecurityQa.csproj --no-restore"),
        Dotnet("project-ready-queue", "dotnet", "run --project tools/ProjectReadyQueue/ProjectReadyQueue.csproj --no-restore -- --self-test"),
        Dotnet("project-blueprint", "dotnet", "run --project tools/ProjectBlueprint/ProjectBlueprint.csproj --no-restore -- --self-test"),
        Dotnet("sim-replay", "dotnet", "run --project tools/SimReplay/SimReplay.csproj --no-restore"),
        Dotnet("combat-behavior", "dotnet", "run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore"),
        Dotnet("simulation-smoke", "dotnet", "run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore"),
        Dotnet("fog-qa", "dotnet", "run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore"),
        Dotnet("selection-stress", "dotnet", "run --project tools/SelectionStress/SelectionStress.csproj --no-restore"),
        Dotnet("ai-difficulty-smoke", "dotnet", "run --project tools/AiDifficultySmoke/AiDifficultySmoke.csproj --no-restore"),
        Dotnet("ai-opponent-loop-qa", "dotnet", "run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore"),
        Dotnet("roster-authoring-qa", "dotnet", "run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore"),
        Dotnet("content-authoring-qa", "dotnet", "run --project tools/ContentAuthoringQa/ContentAuthoringQa.csproj --no-restore"),
        Dotnet("map-authoring-export-debug", "dotnet", "build ProceduralRts.csproj -c ExportDebug --no-restore"),
        Dotnet("map-authoring-export-release", "dotnet", "build ProceduralRts.csproj -c ExportRelease --no-restore"),
        Dotnet("map-authoring-catalog-qa", "dotnet", "run --project tools/MapAuthoringCatalogQa/MapAuthoringCatalogQa.csproj --no-restore -- .godot/mono/temp/bin/ExportDebug/ProceduralRts.dll .godot/mono/temp/bin/ExportRelease/ProceduralRts.dll"),
        Dotnet("mapspec-artifact-qa", "dotnet", "run --project tools/MapSpecArtifactQa/MapSpecArtifactQa.csproj --no-restore"),
        Dotnet("map-authoring-qa", "dotnet", "run --project tools/MapAuthoringQa/MapAuthoringQa.csproj --no-restore"),
        Dotnet("pathfinding-environment-cache-qa", "dotnet", "run --project tools/PathfindingEnvironmentCacheQa/PathfindingEnvironmentCacheQa.csproj --no-restore"),
        Dotnet("map-authoring-validation-qa", "dotnet", "run --project tools/MapAuthoringValidationQa/MapAuthoringValidationQa.csproj --no-restore"),
        Dotnet("map-authoring-bake-play-qa", "dotnet", "run --project tools/MapAuthoringBakePlayQa/MapAuthoringBakePlayQa.csproj --no-restore"),
        Dotnet("playable-map-handoff-qa", "dotnet", "run --project tools/PlayableMapHandoffQa/PlayableMapHandoffQa.csproj --no-restore"),
        Dotnet("sandbox-spawn-authoring-qa", "dotnet", "run --project tools/SandboxSpawnAuthoringQa/SandboxSpawnAuthoringQa.csproj --no-restore"),
        Dotnet("player-loop-qa", "dotnet", "run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore"),
        Dotnet("unit-presentation-projection-qa", "dotnet", "run --project tools/UnitPresentationProjectionQa/UnitPresentationProjectionQa.csproj --no-restore"),
        Dotnet("hud-visual-foundation-qa", "dotnet", "run --project tools/HudVisualFoundationQa/HudVisualFoundationQa.csproj --no-restore"),
        Dotnet("battle-hud-runtime-states-qa", "dotnet", "run --project tools/BattleHudRuntimeStatesQa/BattleHudRuntimeStatesQa.csproj --no-restore"),
        Dotnet("cursor-catalog-qa", "dotnet", "run --project tools/CursorCatalogQa/CursorCatalogQa.csproj --no-restore"),
        Dotnet("desktop-hud-qa", "dotnet", "run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore"),
        Dotnet("review-gate", "dotnet", "run --project tools/ReviewGate/ReviewGate.csproj --no-restore"),
    };

    if (!options.SkipPerf)
    {
        steps.Add(Dotnet("perf-smoke", "dotnet", "run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore"));
    }

    var balanceReport = Path.Combine(root, "tools", "BalanceReport", "BalanceReport.csproj");
    steps.Add(File.Exists(balanceReport)
        ? Dotnet("balance-report", "dotnet", "run --project tools/BalanceReport/BalanceReport.csproj --no-restore")
        : new VerifyStep("balance-report", "dotnet", "run --project tools/BalanceReport/BalanceReport.csproj --no-restore", "tools/BalanceReport is not implemented yet; keep the corresponding GitHub issue open."));
    steps.Add(Dotnet("counter-readability-qa", "dotnet", "run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore"));

    if (!options.SkipGodot)
    {
        var godot = GodotExecutableLocator.Find();
        if (godot is null)
        {
            steps.Add(new VerifyStep(
                "godot-battle-headless",
                GodotExecutableLocator.DefaultDisplayName,
                "--headless --path . --scene res://scenes/Battle.tscn --quit-after 2",
                GodotExecutableLocator.MissingMessage));
        }
        else
        {
            steps.Add(new VerifyStep("godot-battle-headless", godot.Path, "--headless --path . --scene res://scenes/Battle.tscn --quit-after 2"));
            steps.Add(new VerifyStep("godot-ui-font-qa", godot.Path, "--headless --path . --scene res://scenes/UiFontQa.tscn"));
            steps.Add(new VerifyStep("godot-display-settings-qa", godot.Path, "--headless --path . --scene res://scenes/DisplaySettingsQa.tscn"));
            steps.Add(new VerifyStep("godot-map-api-bake-qa", godot.Path, "--headless --path . --scene res://scenes/MapApiBakeQa.tscn"));
            steps.Add(new VerifyStep("godot-map-typed-projection-qa", "sh", $"tools/map-typed-projection-qa.sh \"{godot.Path}\""));
            steps.Add(new VerifyStep("godot-map-authoring-plugin-smoke", "sh", $"tools/map-authoring-plugin-smoke.sh \"{godot.Path}\" --headless"));
            steps.Add(new VerifyStep("godot-map-authoring-validation-smoke", "sh", $"tools/map-authoring-validation-smoke.sh \"{godot.Path}\" --headless"));
            steps.Add(new VerifyStep("godot-map-authoring-sample-parity", "sh", $"tools/map-authoring-sample-parity-qa.sh \"{godot.Path}\""));
            steps.Add(new VerifyStep("godot-map-authoring-export-pack", "sh", $"tools/map-authoring-export-pack-qa.sh \"{godot.Path}\""));
            steps.Add(new VerifyStep("godot-map-authoring-bake-play-smoke", "sh", $"tools/map-authoring-bake-play-smoke.sh \"{godot.Path}\" --headless"));
            steps.Add(new VerifyStep("godot-skirmish-flow-qa", godot.Path, "--headless --path . --scene res://scenes/SkirmishFlowQa.tscn"));
            steps.Add(new VerifyStep("godot-active-battle-perf-qa", godot.Path, "--headless --path . --scene res://scenes/ActiveBattlePerfQa.tscn"));
            steps.Add(new VerifyStep("godot-pause-qa", godot.Path, "--headless --path . --scene res://scenes/PauseQa.tscn"));
        }
    }

    return steps;
}

static VerifyStep Dotnet(string name, string fileName, string arguments)
{
    return new VerifyStep(name, fileName, arguments);
}

static StepResult RunStep(string root, VerifyStep step)
{
    var stopwatch = Stopwatch.StartNew();
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo
    {
        FileName = step.FileName,
        Arguments = step.Arguments,
        WorkingDirectory = root,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    foreach (var (key, value) in step.Environment)
    {
        process.StartInfo.Environment[key] = value;
    }

    process.OutputDataReceived += (_, eventArgs) =>
    {
        if (eventArgs.Data is not null)
        {
            Console.WriteLine($"  {eventArgs.Data}");
        }
    };
    process.ErrorDataReceived += (_, eventArgs) =>
    {
        if (eventArgs.Data is not null)
        {
            Console.Error.WriteLine($"  {eventArgs.Data}");
        }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
    stopwatch.Stop();
    return new StepResult(step.Name, process.ExitCode, stopwatch.Elapsed, Skipped: false);
}

static string FindProjectRoot(string start)
{
    var current = new DirectoryInfo(start);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ProceduralRts.csproj")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find ProceduralRts.csproj from current directory.");
}

sealed record VerifyStep(
    string Name,
    string FileName,
    string Arguments,
    string? SkipReason = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null)
{
    public IReadOnlyDictionary<string, string> Environment { get; init; } = EnvironmentVariables ?? new Dictionary<string, string>();
    public string CommandLine
    {
        get
        {
            var env = Environment.Count == 0
                ? ""
                : string.Join(" ", Environment.Select(pair => $"{pair.Key}={pair.Value}")) + " ";
            return $"{env}{FileName} {Arguments}";
        }
    }
}

sealed record StepResult(string Name, int ExitCode, TimeSpan Elapsed, bool Skipped, string? SkipReason = null)
{
    public static StepResult CreateSkipped(string name, string reason)
    {
        return new StepResult(name, 0, TimeSpan.Zero, Skipped: true, reason);
    }
}

sealed record VerifyOptions(bool ContinueOnFailure, bool SkipPerf, bool SkipGodot, bool AllowMissingBalanceReport)
{
    public static VerifyOptions Parse(string[] args)
    {
        var values = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new VerifyOptions(
            ContinueOnFailure: values.Contains("--continue-on-failure"),
            SkipPerf: values.Contains("--skip-perf"),
            SkipGodot: values.Contains("--skip-godot"),
            AllowMissingBalanceReport: values.Contains("--allow-missing-balance-report"));
    }
}
