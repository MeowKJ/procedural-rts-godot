using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class PauseQaRoot : Node
{
    private const int MatchLifecycleSeed = 24680;

    public override void _Ready()
    {
        CallDeferred(nameof(StartQa));
    }

    public void StartQa()
    {
        GD.Print("Pause QA boot: loading Battle.");
        GetTree().Paused = false;
        SkirmishSetupState.PendingOptions = new SkirmishOptions(
            3200,
            MatchLifecycleSeed,
            EnemyDifficulty.Hard,
            LaunchMode.Skirmish,
            FactionId.Cat,
            FactionId.Dog);
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
    private const int MatchLifecycleSeed = 24680;

    private int _frames;
    private int _pausedFrames;
    private int _prePauseTick = -1;
    private int _pausedTick = -1;
    private BattleRoot? _firstBattle;
    private BattleRoot? _restartedBattle;
    private Phase _phase = Phase.WaitForBattle;

    private enum Phase
    {
        WaitForBattle,
        WaitForSimAdvance,
        ObservePaused,
        WaitForResumeAdvance,
        TriggerRestart,
        WaitForRestartedBattle,
        TriggerReturnToMenu,
        WaitForMainMenu,
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
                var sceneName = GetTree().CurrentScene?.Name ?? "<none>";
                throw new InvalidOperationException($"Pause QA timed out in phase {_phase}; current scene {sceneName}.");
            }

            if (_phase == Phase.WaitForMainMenu)
            {
                WaitForMainMenu();
                return;
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
                case Phase.TriggerRestart:
                    TriggerRestart(battle);
                    return;
                case Phase.WaitForRestartedBattle:
                    WaitForRestartedBattle(battle);
                    return;
                case Phase.TriggerReturnToMenu:
                    TriggerReturnToMenu(battle);
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

        _firstBattle = battle;
        AssertBattleSeed(battle);
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

        GD.Print($"Pause QA: sim tick held at {_pausedTick} while paused and resumed at {battle.DebugSimClockTick}.");
        _phase = Phase.TriggerRestart;
    }

    private void TriggerRestart(BattleRoot battle)
    {
        var pause = RequiredChild<PauseMenuLayer>(battle, "PauseMenu");
        pause.SetPaused(true);
        RequiredChild<Button>(pause, "PauseRestartButton").EmitSignal(BaseButton.SignalName.Pressed);
        _phase = Phase.WaitForRestartedBattle;
        GD.Print("Pause QA: requested battle restart.");
    }

    private void WaitForRestartedBattle(BattleRoot battle)
    {
        if (ReferenceEquals(battle, _firstBattle) || battle.DebugSimClockTick <= 0)
        {
            return;
        }

        if (GetTree().Paused)
        {
            throw new InvalidOperationException("Restarted battle inherited paused scene-tree state.");
        }

        if (CountNodes<BattleRoot>(GetTree().Root) != 1)
        {
            throw new InvalidOperationException("Battle restart leaked an extra BattleRoot node.");
        }

        AssertBattleSeed(battle);
        _restartedBattle = battle;
        _phase = Phase.TriggerReturnToMenu;
        GD.Print("Pause QA: restarted into a fresh battle with the same seed.");
    }

    private void TriggerReturnToMenu(BattleRoot battle)
    {
        if (!ReferenceEquals(battle, _restartedBattle))
        {
            return;
        }

        var pause = RequiredChild<PauseMenuLayer>(battle, "PauseMenu");
        pause.SetPaused(true);
        RequiredChild<Button>(pause, "PauseMainMenuButton").EmitSignal(BaseButton.SignalName.Pressed);
        _phase = Phase.WaitForMainMenu;
        GD.Print("Pause QA: requested return to main menu.");
    }

    private void WaitForMainMenu()
    {
        if (GetTree().CurrentScene is not MainMenuRoot)
        {
            return;
        }

        if (GetTree().Paused)
        {
            throw new InvalidOperationException("Main menu inherited paused scene-tree state.");
        }

        if (CountNodes<BattleRoot>(GetTree().Root) != 0 || CountNodes<MainMenuRoot>(GetTree().Root) != 1)
        {
            throw new InvalidOperationException("Return to main menu leaked or duplicated scene roots.");
        }

        GD.Print("Pause QA passed: pause/resume, restart, same-seed rematch, and return-to-menu lifecycle are clean.");
        GetTree().Quit(0);
    }

    private static void AssertBattleSeed(BattleRoot battle)
    {
        if (battle.State.MatchConfig.MapSeed != MatchLifecycleSeed
            || battle.State.MatchConfig.PlayerFaction != FactionId.Cat
            || battle.State.MatchConfig.AiFaction != FactionId.Dog)
        {
            throw new InvalidOperationException("Pause QA battle did not use the expected lifecycle skirmish setup.");
        }
    }

    private static T RequiredChild<T>(Node root, string name)
        where T : Node
    {
        return root.FindChild(name, recursive: true, owned: false) as T
            ?? throw new InvalidOperationException($"Missing required child {name}.");
    }

    private static int CountNodes<T>(Node root)
        where T : Node
    {
        var count = root is T ? 1 : 0;
        foreach (var child in root.GetChildren())
        {
            count += CountNodes<T>(child);
        }

        return count;
    }
}
