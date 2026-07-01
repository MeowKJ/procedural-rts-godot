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
    private const int TimeoutFrames = 600;
    private int _frames;
    private bool _startedBattle;

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

            if (_startedBattle && GetTree().CurrentScene is BattleRoot battle)
            {
                GD.Print("Skirmish flow QA: validating Battle.");
                AssertBattleState(battle.State);
                AssertBattleRuntime(battle);
                GD.Print("Skirmish flow QA passed: main menu setup launched Battle with selected faction, seed, credits, and difficulty.");
                GetTree().Quit(0);
            }
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void AssertBattleState(GameState state)
    {
        if (state.Options.LaunchMode != LaunchMode.Skirmish
            || state.Options.PlayerFaction != FactionId.Cat
            || state.Options.AiFaction != FactionId.Dog
            || state.Options.EnemyDifficulty != EnemyDifficulty.Hard
            || state.Options.StartingCredits != 3600
            || state.Options.MapSeed != 98765
            || state.MatchConfig.PlayerFaction != FactionId.Cat
            || state.MatchConfig.AiFaction != FactionId.Dog
            || state.Credits(ProceduralRts.Core.Owner.Player) != 3600
            || state.Credits(ProceduralRts.Core.Owner.Enemy) != 3600)
        {
            throw new InvalidOperationException("menu skirmish setup did not launch battle with selected options");
        }

        var expectedPlayerDesignIds = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Cat)
            .Select(spawn => spawn.DesignId);
        var expectedEnemyDesignIds = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Dog)
            .Select(spawn => spawn.DesignId);
        if (!expectedPlayerDesignIds.All(designId =>
                state.Units.Any(unit => unit.Owner == ProceduralRts.Core.Owner.Player && unit.FactionId == FactionId.Cat && unit.DesignId == designId))
            || !expectedEnemyDesignIds.All(designId =>
                state.Units.Any(unit => unit.Owner == ProceduralRts.Core.Owner.Enemy && unit.FactionId == FactionId.Dog && unit.DesignId == designId)))
        {
            throw new InvalidOperationException("menu skirmish setup did not seed selected faction loadouts");
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
}
