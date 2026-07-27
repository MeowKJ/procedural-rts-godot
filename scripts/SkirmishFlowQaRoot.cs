using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class SkirmishFlowQaRoot : Node
{
    public override void _Ready()
    {
        CallDeferred(nameof(StartQa));
    }

    public void StartQa()
    {
        GD.Print("Skirmish flow QA boot: loading MainMenu.");
        SkirmishSetupState.PendingOptions = SkirmishOptions.Default;
        GetTree().Root.AddChild(new SkirmishFlowQaRunner { Name = "SkirmishFlowQaRunner" });
        var error = GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        if (error != Error.Ok)
        {
            GD.PushError($"Failed to load main menu for skirmish flow QA: {error}");
            GetTree().Quit(1);
        }
    }
}

public partial class SkirmishFlowQaRunner : Node
{
    private const int TimeoutFrames = 1200;
    private int _frames;
    private int _cleanupFrames;
    private bool _startedBattle;
    private bool _startedAuthoredBattle;
    private bool _validatedAuthoredBattle;
    private bool _startedPostAuthoredBattle;
    private bool _validatedPostAuthoredBattle;
    private bool _cleanupStarted;

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
                throw new InvalidOperationException("Skirmish flow QA timed out before Battle loaded.");
            }

            if (_cleanupStarted)
            {
                AssertBattleCleanedUp();
                return;
            }

            if (!_startedBattle && GetTree().CurrentScene is MainMenuRoot menu)
            {
                GD.Print("Skirmish flow QA: configuring MainMenu.");
                SelectOption(menu, "PlayerFactionSelect", FactionId.Cat);
                SelectOption(menu, "AiFactionSelect", FactionId.Dog);
                SelectOption(menu, "DifficultySelect", EnemyDifficulty.Hard);
                SetSpinBox(menu, "StartingCreditsInput", 3600);
                SetSpinBox(menu, "MapSeedInput", 98765);

                var start = RequiredChild<Button>(menu, "StartSkirmishButton");
                _startedBattle = true;
                start.EmitSignal(BaseButton.SignalName.Pressed);
                return;
            }

            if (_startedBattle && !_startedAuthoredBattle && GetTree().CurrentScene is BattleRoot battle)
            {
                GD.Print("Skirmish flow QA: validating Battle.");
                AssertBattleConfiguration(battle);
                AssertBattleRuntime(battle);
                GD.Print("Skirmish flow QA: main menu setup launched Battle with selected faction, seed, credits, and difficulty.");
                battle.QueueFree();
                _cleanupStarted = true;
            }
            else if (_startedAuthoredBattle
                && !_validatedAuthoredBattle
                && GetTree().CurrentScene is BattleRoot authoredBattle)
            {
                GD.Print("Skirmish flow QA: validating authored Battle.");
                AssertAuthoredBattle(authoredBattle);
                authoredBattle.QueueFree();
                _validatedAuthoredBattle = true;
                _cleanupStarted = true;
            }
            else if (_startedPostAuthoredBattle
                && !_validatedPostAuthoredBattle
                && GetTree().CurrentScene is BattleRoot restartedBattle)
            {
                if (restartedBattle.DebugSimClockTick <= 0)
                {
                    return;
                }

                GD.Print("Skirmish flow QA: validating normal Battle after authored teardown.");
                AssertNormalBattleAfterAuthored(restartedBattle);
                restartedBattle.QueueFree();
                _validatedPostAuthoredBattle = true;
                _cleanupStarted = true;
            }
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void AssertBattleConfiguration(BattleRoot battle)
    {
        var match = battle.DebugMatchConfig;
        if (match.LaunchMode != LaunchMode.Skirmish
            || match.PlayerFaction != FactionId.Cat
            || match.AiFaction != FactionId.Dog
            || match.EnemyDifficulty != EnemyDifficulty.Hard
            || match.StartingCredits != 3600
            || match.MapSeed != 98765
            || battle.DebugRuntimeCredits(PlayerSlotId.One) != 3600
            || battle.DebugRuntimeCredits(PlayerSlotId.Two) != 3600)
        {
            throw new InvalidOperationException("menu skirmish setup did not launch battle with selected options");
        }
    }

    private static void AssertBattleRuntime(BattleRoot battle)
    {
        var playerDesignIds = battle.DebugUnitBattlefieldDesignIds(PlayerSlotId.One);
        var enemyDesignIds = battle.DebugUnitBattlefieldDesignIds(PlayerSlotId.Two);
        var expectedPlayerDesignIds = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Cat)
            .Select(spawn => spawn.DesignId)
            .ToArray();
        var expectedEnemyDesignIds = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Dog)
            .Select(spawn => spawn.DesignId)
            .ToArray();

        if (!playerDesignIds.SequenceEqual(expectedPlayerDesignIds)
            || !enemyDesignIds.SequenceEqual(expectedEnemyDesignIds))
        {
            throw new InvalidOperationException("menu skirmish setup did not seed selected UnitDesign runtime factions");
        }
    }

    private static void SelectOption<TEnum>(Node root, string name, TEnum value)
        where TEnum : struct, Enum
    {
        var option = RequiredChild<OptionButton>(root, name);
        option.Select(Convert.ToInt32(value));
        option.EmitSignal(OptionButton.SignalName.ItemSelected, option.Selected);
    }

    private static void SetSpinBox(Node root, string name, double value)
    {
        var spinBox = RequiredChild<SpinBox>(root, name);
        spinBox.Value = value;
        spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, value);
    }

    private static T RequiredChild<T>(Node root, string name)
        where T : Node
    {
        return root.FindChild(name, recursive: true, owned: false) as T
            ?? throw new InvalidOperationException($"Missing required child {name}.");
    }

    private void AssertBattleCleanedUp()
    {
        _cleanupFrames++;
        if (_cleanupFrames < 3)
        {
            return;
        }

        if (CountNodes<BattleRoot>(GetTree().Root) != 0)
        {
            throw new InvalidOperationException("Skirmish flow QA leaked a BattleRoot after cleanup.");
        }

        if (!_startedAuthoredBattle)
        {
            LaunchAuthoredBattle();
            return;
        }

        if (!_startedPostAuthoredBattle)
        {
            LaunchNormalBattleAfterAuthored();
            return;
        }

        SkirmishSetupState.PendingOptions = SkirmishOptions.Default;
        GD.Print("Skirmish flow QA passed: menu, authored, and post-authored normal battles cleaned up.");
        GetTree().Quit(0);
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
