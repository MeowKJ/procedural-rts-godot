using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class NormalExitQaRoot : Node
{
    public override void _Ready()
    {
        CallDeferred(nameof(StartQa));
    }

    public void StartQa()
    {
        GD.Print("Normal exit QA boot: loading MainMenu.");
        GetTree().Paused = false;
        SkirmishSetupState.PendingOptions = SkirmishOptions.Default;
        GetTree().Root.AddChild(new NormalExitQaRunner { Name = "NormalExitQaRunner" });
        var error = GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        if (error != Error.Ok)
        {
            GD.PushError($"Failed to load MainMenu for normal exit QA: {error}");
            GetTree().Quit(1);
        }
    }
}

public partial class NormalExitQaRunner : Node
{
    private const int TimeoutFrames = 900;
    private const int ExitGraceFrames = 30;

    private int _frames;
    private int _exitFrames;
    private Phase _phase = Phase.WaitForMainMenu;

    private enum Phase
    {
        WaitForMainMenu,
        WaitForBattle,
        ExitRequested,
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        _frames++;
        try
        {
            if (_frames > TimeoutFrames)
            {
                throw new InvalidOperationException($"Normal exit QA timed out in phase {_phase}.");
            }

            switch (_phase)
            {
                case Phase.WaitForMainMenu when GetTree().CurrentScene is MainMenuRoot menu:
                    _phase = Phase.WaitForBattle;
                    RequiredChild<Button>(menu, "StartSkirmishButton").EmitSignal(BaseButton.SignalName.Pressed);
                    return;
                case Phase.WaitForBattle when GetTree().CurrentScene is BattleRoot battle:
                    RequestNormalExit(battle);
                    return;
                case Phase.ExitRequested:
                    _exitFrames++;
                    if (_exitFrames > ExitGraceFrames)
                    {
                        throw new InvalidOperationException("PauseQuitButton did not terminate the process.");
                    }

                    return;
            }
        }
        catch (Exception exception)
        {
            GetTree().Paused = false;
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private void RequestNormalExit(BattleRoot battle)
    {
        if (battle.DebugSimClockTick <= 0)
        {
            return;
        }

        var pause = RequiredChild<PauseMenuLayer>(battle, "PauseMenu");
        pause.SetPaused(true);
        if (!GetTree().Paused)
        {
            throw new InvalidOperationException("PauseMenu did not pause before normal exit.");
        }

        _phase = Phase.ExitRequested;
        RequiredChild<Button>(pause, "PauseQuitButton").EmitSignal(BaseButton.SignalName.Pressed);
        GD.Print("Normal exit QA passed: MainMenu -> StartSkirmishButton -> live Battle -> PauseQuitButton requested clean process exit.");
    }

    private static T RequiredChild<T>(Node root, string name)
        where T : Node
    {
        return root.FindChild(name, recursive: true, owned: false) as T
            ?? throw new InvalidOperationException($"Missing required child {name}.");
    }
}
