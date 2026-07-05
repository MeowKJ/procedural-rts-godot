using Godot;
using ProceduralRts.Core;
using System.IO;

namespace ProceduralRts;

public partial class ActiveBattlePerfQaRoot : Node
{
    private const string BattleScenePath = "res://scenes/Battle.tscn";
    private const string OutputDirectory = "artifacts/active-battle-perf";
    private const int SampleFrames = 60;
    private const double InteractiveFrameBudgetMs = 18.5;
    private const double HeadlessFrameBudgetMs = 24.0;
    private const double InteractiveProcessBudgetMs = 10.0;
    private const double HeadlessProcessBudgetMs = 15.0;
    private const double SimBudgetMs = 4.0;
    private const double InteractiveFogBudgetMs = 8.0;
    private const double HeadlessFogBudgetMs = 16.0;
    private static readonly Vector2I TestSize = new(1920, 1080);

    private BattleRoot? _battle;

    public override async void _Ready()
    {
        try
        {
            DisplayAudioSettings.ApplyFrameRateMode(FrameRateMode.Fps144, persist: false);
            DisplayServer.WindowSetSize(TestSize);
            GetWindow().Size = TestSize;
            GetViewport().SetEmbeddingSubwindows(false);

            var packed = GD.Load<PackedScene>(BattleScenePath);
            _battle = packed.Instantiate<BattleRoot>();
            AddChild(_battle);
            await NextFrames(24);
            DisplayServer.WindowSetSize(TestSize);
            GetWindow().Size = TestSize;
            DisplayAudioSettings.ApplyFrameRateMode(FrameRateMode.Fps144, persist: false);
            if (!IsHeadlessDisplay())
            {
                DisplayServer.WindowMoveToForeground();
            }

            await NextFrames(6);
            if (!IsHeadlessDisplay())
            {
                Require(DisplayServer.WindowGetSize() == TestSize, $"Expected 1920x1080 test window, got {DisplayServer.WindowGetSize()}.");
            }

            var initial = _battle.DebugConfigureActiveBattlePerformanceScenario();
            Require(initial.LiveUnitCount >= 52, $"Expected at least 52 live units after active-battle seed, got {initial.LiveUnitCount}.");
            Require(initial.PlayerBuildingCount >= 4, $"Expected active player base with at least 4 core buildings, got {initial.PlayerBuildingCount}.");
            Require(initial.EnemyBuildingCount >= 4, $"Expected active enemy base with at least 4 core buildings, got {initial.EnemyBuildingCount}.");
            Require(initial.PlayerCommandedUnitCount >= 20, $"Expected at least 20 commanded player attackers, got {initial.PlayerCommandedUnitCount}.");
            Require(initial.EnemyCommandedUnitCount >= 20, $"Expected at least 20 commanded enemy attackers, got {initial.EnemyCommandedUnitCount}.");

            await NextFrames(30);
            _battle.DebugClearPresentationMetrics();
            await NextFrames(SampleFrames);

            var final = _battle.DebugActiveBattlePerformanceSnapshot();
            var counts = _battle.DebugPerfHudCounts();
            var metrics = _battle.PresentationMetrics.Snapshot();
            var summary =
                $"metrics: live {final.LiveUnitCount}, visible {final.VisibleUnitCount}, entities {counts.LiveEntityCount}, " +
                $"frame avg {metrics.AverageFrameMs:0.00}ms, 1% low {metrics.OnePercentLowFrameMs:0.00}ms, " +
                $"process avg {metrics.AverageProcessMs:0.00}ms, render* avg {metrics.AverageRenderEstimateMs:0.00}ms, " +
                $"sim avg {metrics.AverageSimStepMs:0.00}ms, fog {final.LastFogUpdateMs:0.00}ms.";
            Require(metrics.SampleCount >= SampleFrames - 2, $"Expected {SampleFrames} metric samples, got {metrics.SampleCount}.");
            Require(final.LiveUnitCount >= 40, $"Expected 40+ live units during active battle. {summary}");
            Require(final.VisibleUnitCount >= 40, $"Expected 40+ visible units at 1920x1080. {summary}");
            Require(counts.LiveEntityCount >= 50, $"Expected active entities from both bases and armies. {summary}");
            var frameBudgetMs = IsHeadlessDisplay() ? HeadlessFrameBudgetMs : InteractiveFrameBudgetMs;
            Require(metrics.AverageFrameMs <= frameBudgetMs, $"Expected average frame <= {frameBudgetMs:0.0}ms. {summary}");
            var processBudgetMs = IsHeadlessDisplay() ? HeadlessProcessBudgetMs : InteractiveProcessBudgetMs;
            Require(metrics.AverageProcessMs <= processBudgetMs, $"Expected average process <= {processBudgetMs:0.0}ms. {summary}");
            Require(metrics.AverageSimStepMs <= SimBudgetMs, $"Expected average sim <= {SimBudgetMs:0.0}ms. {summary}");
            var fogBudgetMs = IsHeadlessDisplay() ? HeadlessFogBudgetMs : InteractiveFogBudgetMs;
            Require(final.LastFogUpdateMs <= fogBudgetMs, $"Expected fog update <= {fogBudgetMs:0.0}ms. {summary}");

            var outputPath = Path.Combine(ProjectSettings.GlobalizePath("res://"), OutputDirectory);
            Directory.CreateDirectory(outputPath);
            var screenshotPath = Path.Combine(outputPath, "active_battle_perf_1920x1080.png");
            await SaveScreenshot(screenshotPath);

            GD.Print(
                $"ActiveBattlePerfQa PASSED: {final.LiveUnitCount} live units, {final.VisibleUnitCount} visible units, " +
                $"buildings P/E {final.PlayerBuildingCount}/{final.EnemyBuildingCount}, commanded P/E {final.PlayerCommandedUnitCount}/{final.EnemyCommandedUnitCount}, " +
                $"frame avg {metrics.AverageFrameMs:0.00}ms, 1% low {metrics.OnePercentLowFrameMs:0.00}ms, " +
                $"process avg {metrics.AverageProcessMs:0.00}ms, sim avg {metrics.AverageSimStepMs:0.00}ms, " +
                $"fog {final.LastFogUpdateMs:0.00}ms/{final.FogTextureUploads} uploads, screenshot {screenshotPath}.");
            _battle.QueueFree();
            _battle = null;
            await NextFrames(2);
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task SaveScreenshot(string path)
    {
        await NextFrames(2);
        var displayName = DisplayServer.GetName();
        if (IsHeadlessDisplay())
        {
            SaveHeadlessScreenshotNote(path, $"DisplayServer '{displayName}' does not expose a viewport image.");
            return;
        }

        Image? image;
        try
        {
            image = GetViewport().GetTexture()?.GetImage();
        }
        catch (Exception exception)
        {
            SaveHeadlessScreenshotNote(path, exception.Message);
            return;
        }

        if (image is null)
        {
            SaveHeadlessScreenshotNote(path, "Viewport texture image was unavailable.");
            return;
        }

        var error = image.SavePng(path);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to save active battle perf screenshot {path}: {error}.");
        }

        var fileInfo = new FileInfo(path);
        Require(fileInfo.Exists && fileInfo.Length >= 4096, $"Screenshot {path} was empty or too small.");
    }

    private static void SaveHeadlessScreenshotNote(string pngPath, string reason)
    {
        var notePath = Path.ChangeExtension(pngPath, ".txt");
        File.WriteAllText(
            notePath,
            $"ActiveBattlePerfQa ran under a renderer without viewport image capture. Reason: {reason}{System.Environment.NewLine}");
        GD.Print($"Active battle perf screenshot skipped: {notePath}");
    }

    private static bool IsHeadlessDisplay()
    {
        var displayName = DisplayServer.GetName();
        return Godot.OS.HasFeature("headless")
            || displayName.Contains("headless", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("dummy", StringComparison.OrdinalIgnoreCase);
    }

    private async Task NextFrames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
