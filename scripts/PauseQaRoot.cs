using Godot;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class PauseQaRoot : Node
{
    public override void _Ready()
    {
        CallDeferred(nameof(StartQa));
    }

    public void StartQa()
    {
        GD.Print("Pause QA boot: loading Battle.");
        GetTree().Paused = false;
        GetTree().Root.AddChild(new PauseQaRunner { Name = "PauseQaRunner" });
        var error = GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
        if (error != Error.Ok)
        {
            GD.PushError($"Failed to load Battle for pause QA: {error}");
            GetTree().Quit(1);
        }
    }
}

public partial class PauseQaRunner : Node
{
    private const int TimeoutFrames = 900;
    private const int PausedFramesToObserve = 30;

    private int _frames;
    private int _pausedFrames;
    private int _prePauseTick = -1;
    private int _pausedTick = -1;
    private Phase _phase = Phase.WaitForBattle;

    private enum Phase
    {
        WaitForBattle,
        WaitForSimAdvance,
        ObservePaused,
        WaitForResumeAdvance,
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
                throw new InvalidOperationException($"Pause QA timed out in phase {_phase}.");
            }

            if (GetTree().CurrentScene is not BattleRoot battle)
            {
                return;
            }

            switch (_phase)
            {
                case Phase.WaitForBattle:
                    battle.ProcessMode = ProcessModeEnum.Pausable;
                    _phase = Phase.WaitForSimAdvance;
                    return;
                case Phase.WaitForSimAdvance:
                    WaitForInitialTicks(battle);
                    return;
                case Phase.ObservePaused:
                    ObservePausedTicks(battle);
                    return;
                case Phase.WaitForResumeAdvance:
                    WaitForResumedTicks(battle);
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

    private void WaitForInitialTicks(BattleRoot battle)
    {
        if (battle.DebugSimClockTick <= 0)
        {
            return;
        }

        _prePauseTick = battle.DebugSimClockTick;
        var pause = RequiredChild<PauseMenuLayer>(battle, "PauseMenu");
        pause.SetPaused(true);
        if (!GetTree().Paused)
        {
            throw new InvalidOperationException("Pause menu did not pause the scene tree.");
        }

        _pausedTick = battle.DebugSimClockTick;
        _pausedFrames = 0;
        _phase = Phase.ObservePaused;
        GD.Print($"Pause QA: paused at sim tick {_pausedTick}.");
    }

    private void ObservePausedTicks(BattleRoot battle)
    {
        _pausedFrames++;
        if (battle.DebugSimClockTick != _pausedTick)
        {
            throw new InvalidOperationException($"Sim clock advanced while paused: {_pausedTick} -> {battle.DebugSimClockTick}.");
        }

        if (_pausedFrames < PausedFramesToObserve)
        {
            return;
        }

        var pause = RequiredChild<PauseMenuLayer>(battle, "PauseMenu");
        pause.SetPaused(false);
        if (GetTree().Paused)
        {
            throw new InvalidOperationException("Pause menu did not resume the scene tree.");
        }

        _phase = Phase.WaitForResumeAdvance;
        GD.Print("Pause QA: resumed battle.");
    }

    private void WaitForResumedTicks(BattleRoot battle)
    {
        if (battle.DebugSimClockTick <= _pausedTick)
        {
            return;
        }

        if (_pausedTick < _prePauseTick)
        {
            throw new InvalidOperationException("Pause QA recorded an invalid tick sequence.");
        }

        GD.Print($"Pause QA passed: sim tick held at {_pausedTick} while paused and resumed at {battle.DebugSimClockTick}.");
        GetTree().Paused = false;
        GetTree().Quit(0);
    }

    private static T RequiredChild<T>(Node root, string name)
        where T : Node
    {
        return root.FindChild(name, recursive: true, owned: false) as T
            ?? throw new InvalidOperationException($"Missing required child {name}.");
    }
}
