using System.Text.Json;
using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class AuthoredMapPreviewRuntimeSmoke : Node
{
    private const int TimeoutFrames = 1200;
    private int _frames;
    private int _phase;
    private int _phaseFrames;

    public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

    public override void _Process(double delta)
    {
        _frames++;
        _phaseFrames++;
        try
        {
            if (_frames > TimeoutFrames) throw new InvalidOperationException("Authored runtime smoke timed out.");
            switch (_phase)
            {
                case 0 when GetTree().CurrentScene is BattleRoot authored && _phaseFrames > 3:
                    ValidateAuthored(authored);
                    Capture("authored-battle.png");
                    Require(authored.DebugCommandFirstPlayerUnit(new Vector2(1200, 900)) == 1,
                        "One real authored player command must be accepted.");
                    NextPhase();
                    break;
                case 1 when GetTree().CurrentScene is BattleRoot && _phaseFrames > 4:
                    Capture("applied-command.png");
                    ChangeScene("res://scenes/MainMenu.tscn");
                    NextPhase();
                    break;
                case 2 when GetTree().CurrentScene is MainMenuRoot menu && _phaseFrames > 3:
                    Require(SkirmishSetupState.PendingMatchConfig.AuthoredMap is null,
                        "Return menu must clear authored handoff.");
                    Require(menu.FindChild("AuthoredMapPreviewButton", recursive: true, owned: false) is Button,
                        "Exported MainMenu must expose the fixed Authored Map Preview entry.");
                    RequireMenuLayout(menu);
                    Capture("menu-preview.png");
                    menu.FindChild("AuthoredMapPreviewButton", recursive: true, owned: false)
                        .EmitSignal(BaseButton.SignalName.Pressed);
                    NextPhase();
                    break;
                case 3 when GetTree().CurrentScene is BattleRoot menuPreview && _phaseFrames > 4:
                    ValidateAuthored(menuPreview);
                    ChangeScene("res://scenes/MainMenu.tscn");
                    NextPhase();
                    break;
                case 4 when GetTree().CurrentScene is MainMenuRoot returnMenu && _phaseFrames > 3:
                    Require(SkirmishSetupState.PendingMatchConfig.AuthoredMap is null,
                        "Second return menu must clear fixed-preview handoff.");
                    Capture("return-menu.png");
                    returnMenu.FindChild("StartSkirmishButton", recursive: true, owned: false)
                        .EmitSignal(BaseButton.SignalName.Pressed);
                    NextPhase();
                    break;
                case 5 when GetTree().CurrentScene is BattleRoot normal && _phaseFrames > 4:
                    Require(normal.State.ActiveMapSpec is null && normal.State.MatchConfig == MatchConfig.Default,
                        "Normal skirmish after preview must have no stale authored state.");
                    Capture("normal-no-stale.png");
                    WriteLifecycle();
                    GD.Print("Authored map preview runtime smoke PASSED: loaded sample, accepted command, returned, and launched clean normal skirmish.");
                    GetTree().Quit();
                    _phase = 6;
                    break;
            }
        }
        catch (Exception exception)
        {
            SkirmishSetupState.ClearAuthoredMapHandoff();
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void ValidateAuthored(BattleRoot battle)
    {
        var map = battle.State.ActiveMapSpec ?? throw new InvalidOperationException("Authored map missing.");
        Require(map.Id == "authored-map-preview" && map.WorldSize == new MapSize(3600, 2400),
            "Runtime preview loaded the wrong map identity or bounds.");
        Require(battle.State.Credits(ProceduralRts.Core.Owner.Player) == 2600
            && battle.State.Credits(ProceduralRts.Core.Owner.Enemy) == 2800,
            "Runtime preview must preserve asymmetric sample credits.");
        Require(map.Buildings.Count == 4 && map.Units.Count == 2 && map.Resources.Count == 1
            && map.Obstacles.Count == 1 && map.TerrainCells.Count == 1 && map.Triggers.Count == 1
            && map.Objectives.Count == 1 && map.NarrativeNodes.Count == 1,
            "Runtime preview must preserve every authored collection.");
    }

    private static void RequireMenuLayout(MainMenuRoot menu)
    {
        var names = new[]
        {
            "StartSkirmishButton", "AuthoredMapPreviewButton", "SandboxButton", "SettingsButton", "QuitButton",
        };
        var buttons = names.Select(name => menu.FindChild(name, recursive: true, owned: false) as Button
            ?? throw new InvalidOperationException($"Menu button missing: {name}.")).ToArray();
        var rectangles = buttons.Select(button => new Rect2(button.Position, button.Size)).ToArray();
        for (var first = 0; first < rectangles.Length; first++)
            for (var second = first + 1; second < rectangles.Length; second++)
                Require(!rectangles[first].Intersects(rectangles[second]),
                    $"Menu controls overlap: {names[first]} / {names[second]}.");
        var footer = menu.FindChild("NextFooter", recursive: true, owned: false) as Label
            ?? throw new InvalidOperationException("Menu footer missing.");
        Require(rectangles[^1].End.Y <= footer.Position.Y,
            "Quit button must end above the footer without overlap.");
    }

    private void NextPhase() { _phase++; _phaseFrames = 0; }

    private void ChangeScene(string path)
    {
        var error = GetTree().ChangeSceneToFile(path);
        if (error != Error.Ok) throw new InvalidOperationException($"Runtime smoke scene change failed: {error}.");
    }

    private void Capture(string file)
    {
        if (DisplayServer.GetName() == "headless") return;
        var directory = ProjectSettings.GlobalizePath("res://artifacts/issue-569");
        DirAccess.MakeDirRecursiveAbsolute(directory);
        var image = GetViewport().GetTexture().GetImage();
        Require(image.SavePng(Path.Combine(directory, file)) == Error.Ok, $"Runtime screenshot failed: {file}.");
    }

    private static void WriteLifecycle()
    {
        var directory = ProjectSettings.GlobalizePath("res://artifacts/issue-569");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "lifecycle.json"), JsonSerializer.Serialize(new
        {
            authoredLoaded = true, commandAccepted = true, menuPreviewLoaded = true,
            returnCleared = true, normalSkirmishClean = true,
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
