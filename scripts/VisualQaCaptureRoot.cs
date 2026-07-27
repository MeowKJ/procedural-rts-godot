using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;
using System.IO;

namespace ProceduralRts;

public partial class VisualQaCaptureRoot : Node
{
    private const string MainMenuScenePath = "res://scenes/MainMenu.tscn";
    private const string BattleScenePath = "res://scenes/Battle.tscn";
    private const string OutputDirectory = "artifacts/visual-qa";
    private const int DefaultRenderFlushFrames = 6;
    private static readonly Vector2I CaptureSize = new(1600, 900);
    private Node? _activeScene;
    private Vector2I _captureSize = CaptureSize;

    public override async void _Ready()
    {
        try
        {
            GetViewport().SetEmbeddingSubwindows(false);
            var outputPath = Path.Combine(ProjectSettings.GlobalizePath("res://"), OutputDirectory);
            Directory.CreateDirectory(outputPath);

            await CaptureMainMenu(outputPath);
            await CaptureMainMenuSettings(outputPath);
            await CaptureBattleHud(outputPath);
            await CaptureProjectileLifecycle(outputPath);
            await CapturePause(outputPath);
            await CaptureOutcome(outputPath);
            await UnloadActiveScene();

            GD.Print($"Visual QA screenshots saved to {outputPath}");
            GetTree().Quit();
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task CaptureMainMenu(string outputPath)
    {
        await LoadScene(MainMenuScenePath);
        SetCaptureSize(CaptureSize);
        await Capture(outputPath, "main_menu.png");
    }

    private async Task CaptureMainMenuSettings(string outputPath)
    {
        await LoadScene(MainMenuScenePath);
        SetCaptureSize(CaptureSize);
        var settings = RequiredNode<SettingsOverlayLayer>("Settings");
        settings.Open();
        await Capture(outputPath, "main_menu_settings.png");
    }

    private async Task CaptureBattleHud(string outputPath)
    {
        await LoadScene(BattleScenePath);
        SetCaptureSize(CaptureSize);
        await Capture(outputPath, "battle_hud.png");
        foreach (var resolution in BattleHudRuntimeStateCatalog.Resolutions)
        {
            var size = new Vector2I(resolution.Width, resolution.Height);
            SetCaptureSize(size);
            await NextFrames(8);
            await Capture(outputPath, $"battle_hud_{size.X}x{size.Y}.png");
        }

        SetCaptureSize(CaptureSize);
        var hud = RequiredNode<HudLayer>("Hud");
        hud.SetMoveCommandMode(MoveCommandMode.Direct);
        await NextFrames(2);
        await Capture(outputPath, "battle_hud_command_ribbon.png");
        hud.SetCommandDeckOpen(true);
        await NextFrames(4);
        await CaptureSparseCommandDeck(outputPath, hud);
        await CapturePopulatedCommandDeck(outputPath, hud);
        hud.SetCommandDeckOpen(false);
        await NextFrames(4);
        await CaptureSelectionDetail(outputPath, hud);
        await CaptureBattleHudRuntimeStates(outputPath);

        await LoadScene(BattleScenePath);
        hud = RequiredNode<HudLayer>("Hud");

        SetCaptureSize(CaptureSize);
        await NextFrames(8);
        SetBattleTheme(WorldVisualTheme.FogMorning, "visual-qa-fog");
        await Capture(outputPath, "battle_hud_style1b_fog.png");
        SetBattleTheme(WorldVisualTheme.DuskDefense, "visual-qa-dusk");
        await Capture(outputPath, "battle_hud_style1c_dusk.png");
        SetBattleTheme(WorldVisualTheme.NightRadar, "visual-qa-night-radar");
        await Capture(outputPath, "battle_hud_style1d_night.png");
        SetBattleThemeTransition(
            WorldVisualTheme.DayCommand,
            WorldVisualTheme.NightRadar,
            0.5f,
            "visual-qa-day-night-transition");
        await Capture(outputPath, "battle_hud_theme_transition.png");
        SetBattleTheme(WorldVisualTheme.NightRadar, "visual-qa-foundation-states");
        hud.DebugConfigureHudVisualFoundationQa();
        await NextFrames(12);
        await Capture(outputPath, "battle_hud_foundation_states.png");
        SetBattleTheme(WorldVisualTheme.DayCommand, "visual-qa-day");
        await NextFrames(2);
    }

    private async Task CaptureSparseCommandDeck(string outputPath, HudLayer hud)
    {
        GetTree().Paused = true;
        try
        {
            hud.SetCommandCardState([]);
            hud.SetProductionProviderLaneState([]);
            hud.SetProductionQueueSummary(GameText.T("ui.queue.empty"), canCancel: false);
            hud.SetHudContext(hasSelection: false, hasBuildingSelection: false, buildModeActive: false);
            await Capture(outputPath, "battle_hud_command_deck.png");
        }
        finally
        {
            GetTree().Paused = false;
        }
    }

    private async Task CaptureSelectionDetail(string outputPath, HudLayer hud)
    {
        GetTree().Paused = true;
        try
        {
            hud.SetHudContext(hasSelection: true, hasBuildingSelection: false, buildModeActive: false);
            hud.SetSelectionInfo(
                "ALLEY RUNNER",
                GameText.T("ui.status.ready"),
                "HP 180  SPD 92  RNG 140",
                "Recon infantry / direct fire",
                "unit",
                IconGlyph.Infantry);
            await Capture(outputPath, "battle_hud_selection_detail.png");
        }
        finally
        {
            GetTree().Paused = false;
        }
    }

    private async Task CapturePopulatedCommandDeck(string outputPath, HudLayer hud)
    {
        GetTree().Paused = true;
        try
        {
            hud.SetCommandCardState(
            [
                new ProductionOptionState(
                    ProductionCategory.Infantry,
                    BuildingDesignIds.Barracks,
                    "cat.basic",
                    "CB",
                    IconGlyph.Infantry,
                    IconGlyph.Infantry,
                    new Color("#55d6c2"),
                    120,
                    5.2f,
                    true,
                    true,
                    4,
                    0.56f,
                    ""),
            ]);
            hud.SetProductionProviderLaneState(
            [
                new ProductionProviderLaneState(
                    ProductionProviderLaneScope.Auto,
                    0,
                    BuildingDesignIds.Barracks,
                    GameText.T("ui.providerLane.auto"),
                    "A",
                    2,
                    4,
                    0.56f,
                    true,
                    ""),
                new ProductionProviderLaneState(
                    ProductionProviderLaneScope.All,
                    0,
                    BuildingDesignIds.Barracks,
                    GameText.T("ui.providerLane.all"),
                    "ALL",
                    2,
                    4,
                    0.56f,
                    true,
                    ""),
                new ProductionProviderLaneState(
                    ProductionProviderLaneScope.Specific,
                    101,
                    BuildingDesignIds.Barracks,
                    GameText.Format("ui.providerLane.specific", "Barracks", 1),
                    "B1",
                    1,
                    4,
                    0.56f,
                    true,
                    ""),
            ]);
            hud.SetProductionQueueSummary(
                GameText.Format("ui.queue.summary", "ALLEY RUNNER", 56, 4, 160),
                canCancel: true);
            hud.SetHudContext(hasSelection: false, hasBuildingSelection: false, buildModeActive: false);
            await Capture(outputPath, "battle_hud_command_deck_queue.png");

            var denseStates = UnitDesignCatalog.Designs.Values
                .Select(design => design.ToSpec())
                .Where(spec => spec.Production?.Category == ProductionCategory.Infantry)
                .Take(9)
                .Select((spec, index) => new ProductionOptionState(
                    ProductionCategory.Infantry,
                    spec.Production!.ProducerKind,
                    spec.Id,
                    spec.ShortCode,
                    spec.Icon,
                    spec.Production.CategoryIcon,
                    new Color("#62C9C4"),
                    spec.Stats.Cost,
                    spec.Production.Duration,
                    true,
                    true,
                    index % 4,
                    index % 3 * 0.28f,
                    ""))
                .ToArray();
            hud.SetCommandCardState(denseStates);
            hud.SetHudContext(hasSelection: false, hasBuildingSelection: false, buildModeActive: false);
            await NextFrames(3);
            await Capture(outputPath, "battle_hud_command_deck_dense.png");
        }
        finally
        {
            GetTree().Paused = false;
        }
    }

    private async Task CapturePause(string outputPath)
    {
        await LoadScene(BattleScenePath);
        SetCaptureSize(CaptureSize);
        var pause = RequiredNode<PauseMenuLayer>("PauseMenu");
        pause.SetPaused(true);
        await Capture(outputPath, "pause_menu.png");
    }

    private async Task CaptureProjectileLifecycle(string outputPath)
    {
        await LoadScene(BattleScenePath);
        SetCaptureSize(CaptureSize);
        SetBattleTheme(WorldVisualTheme.DuskDefense, "visual-qa-projectiles");
        if (_activeScene is not BattleRoot battle)
        {
            throw new InvalidOperationException("BattleRoot was not active for projectile visual QA.");
        }

        var focus = battle.DebugConfigureProjectileVisualQaScenario();
        var combatEffects = RequiredNode<CombatEffectsLayer>("CombatEffects");
        var battlefield = combatEffects.UnitBattlefield
            ?? throw new InvalidOperationException("Projectile visual QA requires the live UnitBattlefield.");
        RequiredNode<Control>("CommandPreview").Visible = false;
        var previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        Input.WarpMouse(new Vector2(20, 20));
        try
        {
            var capturedDirect = false;
            var capturedBallistic = false;
            var capturedTracking = false;
            for (var frame = 0; frame < 240; frame++)
            {
                var visible = battlefield.ProjectileProjections(PlayerSlotId.One);
                var hasDirect = false;
                var hasBallistic = false;
                var hasTracking = false;
                foreach (var projectile in visible)
                {
                    var isMidFlight = projectile.FlightProgress is >= 0.25f and <= 0.75f;
                    hasDirect |= isMidFlight && projectile.Behavior == ProjectileBehavior.Direct;
                    hasBallistic |= isMidFlight && projectile.Behavior == ProjectileBehavior.Ballistic;
                    hasTracking |= isMidFlight && projectile.Behavior == ProjectileBehavior.Tracking;
                }

                if (!capturedDirect && hasDirect)
                {
                    await CaptureProjectileFrame(outputPath, "battle_projectile_direct.png", battle, focus);
                    capturedDirect = true;
                }

                if (!capturedBallistic && hasBallistic)
                {
                    await CaptureProjectileFrame(outputPath, "battle_projectile_ballistic.png", battle, focus);
                    capturedBallistic = true;
                }

                if (!capturedTracking && hasTracking)
                {
                    await CaptureProjectileFrame(outputPath, "battle_projectile_tracking.png", battle, focus);
                    capturedTracking = true;
                }

                if (capturedDirect && capturedBallistic && capturedTracking)
                {
                    return;
                }

                await NextFrames(1);
            }

            throw new InvalidOperationException(
                "Projectile visual QA did not capture Direct, Ballistic, and Tracking rounds at mid-flight within 240 frames.");
        }
        finally
        {
            Input.MouseMode = previousMouseMode;
        }
    }

    private async Task CaptureProjectileFrame(string outputPath, string fileName, BattleRoot battle, Vector2 focus)
    {
        battle.DebugRevealFog(focus, 900f);
        RequiredNode<FogOfWarLayer>("FogOfWar").QueueRedraw();
        GetTree().Paused = true;
        try
        {
            await Capture(outputPath, fileName);
        }
        finally
        {
            GetTree().Paused = false;
        }
    }

    private async Task CaptureOutcome(string outputPath)
    {
        await LoadScene(BattleScenePath);
        SetCaptureSize(CaptureSize);
        var outcome = RequiredNode<OutcomeScreenLayer>("OutcomeScreen");
        outcome.ShowOutcome(GameOutcome.Victory, GameText.T("ui.outcome.enemyHqDestroyed"));
        await Capture(outputPath, "outcome_victory.png");
    }

    private async Task LoadScene(string scenePath)
    {
        GetTree().Paused = false;
        await UnloadActiveScene();

        if (scenePath == BattleScenePath)
        {
            StageDeterministicBattleCapture();
        }

        var packed = GD.Load<PackedScene>(scenePath);
        _activeScene = packed.Instantiate();
        AddChild(_activeScene);
        await NextFrames(8);
    }

    private async Task UnloadActiveScene()
    {
        if (_activeScene is null)
        {
            return;
        }

        _activeScene.QueueFree();
        _activeScene = null;
        await NextFrames(3);
    }

    private T RequiredNode<T>(string name) where T : Node
    {
        if (_activeScene is T root && root.Name == name)
        {
            return root;
        }

        if (_activeScene?.FindChild(name, recursive: true, owned: false) is T node)
        {
            return node;
        }

        throw new InvalidOperationException($"Required node '{name}' was not found in visual QA scene.");
    }

    private void SetBattleTheme(WorldVisualTheme theme, string driver)
    {
        var battle = RequiredNode<BattleRoot>("BattleRoot");
        battle.DebugSetVisualTheme(theme, driver, transitionProgress: 1);
    }

    private void SetBattleThemeTransition(
        WorldVisualTheme current,
        WorldVisualTheme target,
        float transitionProgress,
        string driver)
    {
        var battle = RequiredNode<BattleRoot>("BattleRoot");
        battle.DebugSetVisualTheme(current, $"{driver}-start", transitionProgress: 1);
        battle.DebugSetVisualTheme(target, driver, transitionProgress);
    }

    private async Task Capture(
        string outputPath,
        string fileName,
        int renderFlushFrames = DefaultRenderFlushFrames)
    {
        await NextFrames(renderFlushFrames);
        var image = GetViewport().GetTexture().GetImage();
        GD.Print(
            $"Capture metrics {fileName}: requested {_captureSize.X}x{_captureSize.Y}, " +
            $"window {GetTree().Root.Size.X}x{GetTree().Root.Size.Y}, " +
            $"content {GetTree().Root.ContentScaleSize.X}x{GetTree().Root.ContentScaleSize.Y}, " +
            $"viewport {GetViewport().GetVisibleRect().Size.X}x{GetViewport().GetVisibleRect().Size.Y}, " +
            $"image {image.GetWidth()}x{image.GetHeight()}.");
        if (image.GetWidth() != _captureSize.X || image.GetHeight() != _captureSize.Y)
        {
            throw new InvalidOperationException(
                $"Screenshot {fileName} resolved to {image.GetWidth()}x{image.GetHeight()}, expected {_captureSize.X}x{_captureSize.Y}.");
        }

        var path = Path.Combine(outputPath, fileName);
        var error = image.SavePng(path);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to save screenshot {path}: {error}");
        }

        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists || fileInfo.Length < 4096)
        {
            throw new InvalidOperationException($"Screenshot {path} was empty or too small.");
        }

        GD.Print($"Captured {path}");
    }

    private async Task NextFrames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void SetCaptureSize(Vector2I size)
    {
        var window = GetTree().Root;
        window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
        window.ContentScaleSize = size;
        DisplayServer.WindowSetSize(size);
        window.Size = size;
        _captureSize = size;
    }
}
