using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class MainMenuRoot
{
    private void StartSkirmish()
    {
        LaunchBattle(CurrentSkirmishOptions(), GameText.T("menu.status.loading"));
    }

    private void StartSandbox()
    {
        LaunchBattle(SkirmishOptions.Sandbox, "Loading developer sandbox...");
    }

    private void StartAuthoredMapPreview()
    {
        try
        {
            _ = AuthoredMapPreviewRuntime.StageCommittedSample();
            _status.Text = "Loading Authored Map Preview...";
            var error = GetTree().ChangeSceneToFile(BattleScenePath);
            if (error != Error.Ok) throw new InvalidOperationException($"Battle scene load failed: {error}.");
        }
        catch (Exception exception)
        {
            SkirmishSetupState.ClearAuthoredMapHandoff();
            _status.Text = $"Authored Map Preview blocked: {exception.Message}";
        }
    }

    private void LaunchBattle(SkirmishOptions options, string status)
    {
        SkirmishSetupState.PendingOptions = options;
        _status.Text = status;
        var error = GetTree().ChangeSceneToFile(BattleScenePath);
        if (error != Error.Ok)
        {
            _status.Text = GameText.Format("common.sceneLoadFailed", error);
        }
    }

    private void OpenSettings()
    {
        _settings.Open();
        _status.Text = GameText.T("menu.status.settingsOpen");
    }

    private void QuitGame()
    {
        GetTree().Quit();
    }

    private SkirmishOptions CurrentSkirmishOptions()
    {
        return new SkirmishOptions(
            StartingCredits: Mathf.RoundToInt((float)_startingCredits.Value),
            MapSeed: Mathf.RoundToInt((float)_mapSeed.Value),
            EnemyDifficulty: (EnemyDifficulty)_difficulty.Selected,
            PlayerFaction: SelectedPlayableFaction(_playerFaction, FactionId.Dog),
            AiFaction: SelectedPlayableFaction(_aiFaction, FactionId.Cat));
    }

    private void RefreshSkirmishSummary()
    {
        if (_setupSummary is null)
        {
            return;
        }

        var options = CurrentSkirmishOptions();
        _setupSummary.Text = GameText.Format(
            "menu.skirmish.summary",
            FactionShortLabel(options.PlayerFaction),
            FactionShortLabel(options.AiFaction),
            DifficultyLabel(options.EnemyDifficulty),
            options.StartingCredits,
            options.MapSeed);
    }

    private static FactionId SelectedPlayableFaction(OptionButton button, FactionId fallback)
    {
        var selected = (FactionId)button.GetSelectedId();
        return FactionCatalog.Definitions.ContainsKey(selected) ? selected : fallback;
    }

    private static string DifficultyLabel(EnemyDifficulty difficulty)
    {
        return difficulty switch
        {
            EnemyDifficulty.Easy => GameText.T("menu.difficulty.easy"),
            EnemyDifficulty.Hard => GameText.T("menu.difficulty.hard"),
            _ => GameText.T("menu.difficulty.normal"),
        };
    }

    private static string FactionLabel(FactionId faction)
    {
        return faction switch
        {
            FactionId.Cat => GameText.T("faction.cat.name"),
            FactionId.Corruption => GameText.T("faction.corruption.locked"),
            _ => GameText.T("faction.dog.name"),
        };
    }

    private static string FactionShortLabel(FactionId faction)
    {
        return faction switch
        {
            FactionId.Cat => GameText.T("faction.cat.short"),
            FactionId.Corruption => GameText.T("faction.corruption.short"),
            _ => GameText.T("faction.dog.short"),
        };
    }
}
