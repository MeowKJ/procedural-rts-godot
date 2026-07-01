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

    public override async void _Ready()
    {
        try
        {
            SetCaptureSize(CaptureSize);
            GetViewport().SetEmbeddingSubwindows(false);
            var outputPath = Path.Combine(ProjectSettings.GlobalizePath("res://"), OutputDirectory);
            Directory.CreateDirectory(outputPath);

            await CaptureMainMenu(outputPath);
            await CaptureMainMenuSettings(outputPath);
            await CaptureBattleHud(outputPath);
            await CapturePause(outputPath);
            await CaptureOutcome(outputPath);

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
        await Capture(outputPath, "main_menu.png");
    }

    private async Task CaptureMainMenuSettings(string outputPath)
    {
        await LoadScene(MainMenuScenePath);
        var settings = RequiredNode<SettingsOverlayLayer>("Settings");
        settings.Open();
        await Capture(outputPath, "main_menu_settings.png");
    }

    private async Task CaptureBattleHud(string outputPath)
    {
        SetCaptureSize(CaptureSize);
        await LoadScene(BattleScenePath);
        await Capture(outputPath, "battle_hud.png");
        foreach (var size in BattleHudCaptureSizes)
        {
            SetCaptureSize(size);
            await NextFrames(8);
            await Capture(outputPath, $"battle_hud_{size.X}x{size.Y}.png");
        }

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
        SetCaptureSize(CaptureSize);
        await LoadScene(BattleScenePath);
        var pause = RequiredNode<PauseMenuLayer>("PauseMenu");
        pause.SetPaused(true);
        await Capture(outputPath, "pause_menu.png");
    }

    private async Task CaptureOutcome(string outputPath)
    {
        SetCaptureSize(CaptureSize);
        await LoadScene(BattleScenePath);
        var outcome = RequiredNode<OutcomeScreenLayer>("OutcomeScreen");
        outcome.ShowOutcome(GameOutcome.Victory, GameText.T("ui.outcome.enemyHqDestroyed"));
        await Capture(outputPath, "outcome_victory.png");
    }

    private async Task LoadScene(string scenePath)
    {
        GetTree().Paused = false;
        if (_activeScene is not null)
        {
            _activeScene.QueueFree();
            _activeScene = null;
            await NextFrames(2);
        }

        var packed = GD.Load<PackedScene>(scenePath);
        _activeScene = packed.Instantiate();
        AddChild(_activeScene);
        await NextFrames(8);
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

    private static void SetCaptureSize(Vector2I size)
    {
        DisplayServer.WindowSetSize(size);
    }
}
