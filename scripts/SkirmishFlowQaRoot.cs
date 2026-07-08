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
    private const int TimeoutFrames = 900;
    private int _frames;
    private int _cleanupFrames;
    private bool _startedBattle;
    private bool _startedAuthoredBattle;
    private bool _validatedAuthoredBattle;
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
                AssertBattleState(battle.State);
                AssertBattleRuntime(battle);
                GD.Print("Skirmish flow QA: main menu setup launched Battle with selected faction, seed, credits, and difficulty.");
                battle.QueueFree();
                _cleanupStarted = true;
            }
            else if (_startedAuthoredBattle && !_validatedAuthoredBattle && GetTree().CurrentScene is BattleRoot authoredBattle)
            {
                GD.Print("Skirmish flow QA: validating authored Battle.");
                AssertAuthoredBattle(authoredBattle);
                GD.Print("Skirmish flow QA: authored MapSpec launched Battle with authored state and runtime units.");
                authoredBattle.QueueFree();
                _validatedAuthoredBattle = true;
                _cleanupStarted = true;
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

    private static void AssertAuthoredBattle(BattleRoot battle)
    {
        var state = battle.State;
        var spec = state.MatchConfig.AuthoredMap
            ?? throw new InvalidOperationException("authored battle did not preserve AuthoredMap on MatchConfig");
        if (state.WorldSize != spec.WorldSize.ToVector2()
            || state.Credits(ProceduralRts.Core.Owner.Player) != 1800
            || state.Credits(ProceduralRts.Core.Owner.Enemy) != 2100
            || state.Buildings.Count != 2
            || state.Units.Count != 2
            || state.ResourceFields.Count != 1
            || state.MapObstacles.Count != 1)
        {
            throw new InvalidOperationException("authored battle did not seed GameState from the supplied MapSpec");
        }

        var playerDesignIds = battle.DebugUnitBattlefieldDesignIds(PlayerSlotId.One);
        var enemyDesignIds = battle.DebugUnitBattlefieldDesignIds(PlayerSlotId.Two);
        if (!playerDesignIds.SequenceEqual(new[] { "dog.guard_tank" })
            || !enemyDesignIds.SequenceEqual(new[] { "cat.tank" }))
        {
            throw new InvalidOperationException("authored battle did not seed runtime UnitBattlefield from authored units");
        }
    }

    private void LaunchAuthoredBattle()
    {
        SkirmishSetupState.PendingMatchConfig = AuthoredMatchConfig(AuthoredQaMap());
        _startedAuthoredBattle = true;
        _cleanupStarted = false;
        _cleanupFrames = 0;
        var error = GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to load authored battle for skirmish flow QA: {error}");
        }
    }

    private static MatchConfig AuthoredMatchConfig(MapSpec spec)
    {
        return new MatchConfig(
            StartingCredits: 0,
            MapSeed: spec.Seed,
            EnemyDifficulty: EnemyDifficulty.Normal,
            WorldSize: spec.WorldSize.ToVector2(),
            PlayerFaction: FactionId.Dog,
            AiFaction: FactionId.Cat,
            AuthoredMap: spec);
    }

    private static MapSpec AuthoredQaMap()
    {
        return new MapSpec
        {
            Id = "qa.authored-flow",
            Seed = 20260709,
            WorldSize = new MapSize(1600, 1000),
            OwnerStarts =
            [
                new(new OwnerId(1), FactionId.Dog, new MapPoint(260, 320), 0, 1800),
                new(new OwnerId(2), FactionId.Cat, new MapPoint(1260, 680), Mathf.Pi, 2100),
            ],
            Resources =
            [
                new("center", new MapPoint(780, 230), 130, 2800, new MapColor("#8fffe1")),
            ],
            Obstacles =
            [
                new("courtyard", new MapRect(690, 450, 220, 120)),
            ],
            Buildings =
            [
                new(BuildingDesignIds.Headquarters, new OwnerId(1), FactionId.Dog, new MapPoint(260, 320), 0),
                new(BuildingDesignIds.Headquarters, new OwnerId(2), FactionId.Cat, new MapPoint(1260, 680), Mathf.Pi),
            ],
            Units =
            [
                new("dog.guard_tank", new OwnerId(1), new MapPoint(380, 320)),
                new("cat.tank", new OwnerId(2), new MapPoint(1140, 680), Mathf.Pi),
            ],
        };
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

        GD.Print("Skirmish flow QA passed: battle scene cleaned up after setup and authored-map flows.");
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
