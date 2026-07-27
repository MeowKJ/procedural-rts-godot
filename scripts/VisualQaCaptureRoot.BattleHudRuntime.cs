using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using System.IO;
using System.Text.Json;

namespace ProceduralRts;

public partial class VisualQaCaptureRoot
{
    private readonly List<BattleHudRuntimeStructuralEvidence> _battleHudRuntimeStructuralEvidence = [];

    private async Task CaptureBattleHudRuntimeStates(string outputPath)
    {
        var config = BattleHudRuntimeStateCatalog.CaptureConfig;
        var (exactCommit, captureRunNonce) = RequiredBattleHudRuntimeProvenance();
        _battleHudRuntimeStructuralEvidence.Clear();
        File.Delete(Path.Combine(outputPath, BattleHudRuntimeStateCatalog.StructuralEvidenceFileName));
        try
        {
            foreach (var state in BattleHudRuntimeStateCatalog.States)
            {
                await LoadScene(BattleScenePath);
                SetBattleTheme(config.Theme, $"visual-qa-runtime-{state.CaptureId}");
                var hud = RequiredNode<HudLayer>("Hud");
                AssertBattleHudRuntimeCaptureConfig(config);
                AssertNormalSkirmishSandboxHidden();
                FreezeBattleHudRuntimeProjectionAuthority();
                foreach (var resolution in BattleHudRuntimeStateCatalog.Resolutions)
                {
                    await CaptureBattleHudRuntimeResolution(
                        outputPath,
                        hud,
                        state,
                        resolution,
                        config,
                        exactCommit,
                        captureRunNonce);
                }
            }

            WriteBattleHudRuntimeStructuralEvidence(outputPath, exactCommit, captureRunNonce);
        }
        finally
        {
            GetTree().Paused = false;
            SetCaptureSize(CaptureSize);
        }
    }

    private async Task CaptureBattleHudRuntimeResolution(
        string outputPath,
        HudLayer hud,
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        BattleHudRuntimeCaptureConfig config,
        string exactCommit,
        string captureRunNonce)
    {
        GetTree().Paused = false;
        SetCaptureSize(new Vector2I(resolution.Width, resolution.Height));
        hud.ApplyBattleHudRuntimeProjection(state.Projection);
        GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        await NextFrames(config.SettleFrames);
        AssertBattleHudRuntimeCaptureConfig(config);
        AssertNormalSkirmishSandboxHidden();
        _battleHudRuntimeStructuralEvidence.Add(hud.ProbeBattleHudRuntimeStructure(
            state,
            resolution,
            exactCommit,
            captureRunNonce));

        GetTree().Paused = true;
        try
        {
            await Capture(
                outputPath,
                state.CaptureFileName(resolution),
                config.RenderFlushFrames);
        }
        finally
        {
            GetTree().Paused = false;
        }
    }

    private void WriteBattleHudRuntimeStructuralEvidence(
        string outputPath,
        string exactCommit,
        string captureRunNonce)
    {
        var expectedCount = BattleHudRuntimeStateCatalog.States.Count
            * BattleHudRuntimeStateCatalog.Resolutions.Count;
        if (_battleHudRuntimeStructuralEvidence.Count != expectedCount)
        {
            throw new InvalidOperationException(
                $"Battle HUD runtime probe produced {_battleHudRuntimeStructuralEvidence.Count} results, expected {expectedCount}.");
        }

        if (_battleHudRuntimeStructuralEvidence.Any(item => item.ExactCommit != exactCommit
            || item.CaptureRunNonce != captureRunNonce))
        {
            throw new InvalidOperationException(
                "Battle HUD runtime probe produced inconsistent capture provenance.");
        }

        var path = Path.Combine(outputPath, BattleHudRuntimeStateCatalog.StructuralEvidenceFileName);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(_battleHudRuntimeStructuralEvidence, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            }) + System.Environment.NewLine);
    }

    private static (string ExactCommit, string CaptureRunNonce) RequiredBattleHudRuntimeProvenance()
    {
        var exactCommit = System.Environment.GetEnvironmentVariable("BATTLE_HUD_CAPTURE_COMMIT") ?? "";
        var captureRunNonce = System.Environment.GetEnvironmentVariable("BATTLE_HUD_CAPTURE_RUN_NONCE") ?? "";
        if (exactCommit.Length != 40 || !exactCommit.All(Uri.IsHexDigit))
        {
            throw new InvalidOperationException(
                "BATTLE_HUD_CAPTURE_COMMIT must be the exact 40-character checkout SHA.");
        }

        if (string.IsNullOrWhiteSpace(captureRunNonce))
        {
            throw new InvalidOperationException("BATTLE_HUD_CAPTURE_RUN_NONCE is required.");
        }

        return (exactCommit.ToLowerInvariant(), captureRunNonce);
    }

    private void AssertNormalSkirmishSandboxHidden()
    {
        if (_activeScene is not BattleRoot battle
            || battle.DebugMatchConfig.LaunchMode != LaunchMode.Skirmish)
        {
            throw new InvalidOperationException("Battle HUD runtime capture requires a real Skirmish launch.");
        }

        if (RequiredNode<Control>("SandboxDeveloperPanel").Visible)
        {
            throw new InvalidOperationException(
                "Normal-skirmish HUD exposed SandboxDeveloperPanel before capture.");
        }
    }

    private void AssertBattleHudRuntimeCaptureConfig(BattleHudRuntimeCaptureConfig config)
    {
        if (_activeScene is not BattleRoot battle)
        {
            throw new InvalidOperationException("Battle HUD runtime capture config requires BattleRoot.");
        }

        var match = battle.DebugMatchConfig;
        if (match.StartingCredits != config.StartingCredits
            || match.MapSeed != config.MapSeed
            || match.EnemyDifficulty != config.EnemyDifficulty
            || match.LaunchMode != config.LaunchMode)
        {
            throw new InvalidOperationException(
                $"Battle HUD runtime config differs from capture config: {match}.");
        }

        if (GameText.CurrentLanguage != config.Language)
        {
            throw new InvalidOperationException(
                $"Battle HUD runtime language {GameText.CurrentLanguage} differs from {config.Language}.");
        }

        var visualTheme = battle.DebugVisualTheme;
        if (visualTheme.Current != config.Theme
            || visualTheme.Target != config.Theme
            || visualTheme.TransitionProgress < 0.999f)
        {
            throw new InvalidOperationException(
                $"Battle HUD runtime theme {visualTheme} differs from settled {config.Theme}.");
        }
    }

    private void FreezeBattleHudRuntimeProjectionAuthority()
    {
        if (_activeScene is not BattleRoot battle)
        {
            throw new InvalidOperationException("Battle HUD runtime capture requires BattleRoot.");
        }

        battle.SetProcess(false);
        battle.SetPhysicsProcess(false);
    }

    private static void StageDeterministicBattleCapture()
    {
        var config = BattleHudRuntimeStateCatalog.CaptureConfig;
        GameText.CurrentLanguage = config.Language;
        SkirmishSetupState.PendingOptions = new SkirmishOptions(
            config.StartingCredits,
            config.MapSeed,
            config.EnemyDifficulty,
            config.LaunchMode);
    }
}
