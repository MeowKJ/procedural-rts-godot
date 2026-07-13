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
    private static readonly Vector2I CaptureSize = new(1600, 900);
    private static readonly Vector2I[] BattleHudCaptureSizes =
    [
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080),
    ];

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
        foreach (var size in BattleHudCaptureSizes)
        {
            SetCaptureSize(size);
            await NextFrames(8);
            await Capture(outputPath, $"battle_hud_{size.X}x{size.Y}.png");
        }

        SetCaptureSize(CaptureSize);
        var hud = RequiredNode<HudLayer>("Hud");
        hud.SetCommandDeckOpen(true);
        await NextFrames(4);
        await Capture(outputPath, "battle_hud_command_deck.png");
        hud.SetCommandDeckOpen(false);

        SetCaptureSize(CaptureSize);
        await NextFrames(8);
        SetBattleTheme(WorldVisualTheme.FogMorning, "visual-qa-fog");
        await Capture(outputPath, "battle_hud_style1b_fog.png");
        SetBattleTheme(WorldVisualTheme.DuskDefense, "visual-qa-dusk");
        await Capture(outputPath, "battle_hud_style1c_dusk.png");
        SetBattleTheme(WorldVisualTheme.DayCommand, "visual-qa-day");
        await NextFrames(2);
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
        battle.State.FogOfWar.Update(battle.State.WorldSize, [(focus, 900f)]);
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
        if (_activeScene?.FindChild(name, recursive: true, owned: false) is T node)
        {
            return node;
        }

        throw new InvalidOperationException($"Required node '{name}' was not found in visual QA scene.");
    }

    private void SetBattleTheme(WorldVisualTheme theme, string driver)
    {
        var grid = RequiredNode<GridLayer>("Grid");
        grid.State?.SetVisualTheme(theme, driver, transitionProgress: 1);
    }

    private async Task Capture(string outputPath, string fileName)
    {
        await NextFrames(6);
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
