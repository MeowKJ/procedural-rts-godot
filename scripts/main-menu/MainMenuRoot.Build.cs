using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class MainMenuRoot
{
    private void BuildHeader()
    {
        var title = UiFactory.MakeLabel(GameText.T("menu.title"), 34, Cyan);
        title.Position = new Vector2(34, 28);
        title.CustomMinimumSize = new Vector2(420, 42);
        AddChild(title);

        var subtitle = UiFactory.MakeLabel(GameText.T("menu.subtitle"), 13, InkMuted);
        subtitle.Position = new Vector2(38, 72);
        subtitle.CustomMinimumSize = new Vector2(430, 22);
        AddChild(subtitle);

        _status = UiFactory.MakeLabel(GameText.T("menu.status.online"), 13, Amber);
        _status.HorizontalAlignment = HorizontalAlignment.Right;
        _status.SetAnchorsPreset(LayoutPreset.TopRight);
        _status.OffsetLeft = -360;
        _status.OffsetTop = 34;
        _status.OffsetRight = -34;
        _status.OffsetBottom = 58;
        AddChild(_status);
    }

    private void BuildMissionPanel()
    {
        var panel = UiFactory.MakePanel("MissionPanel", new Color("#06121b", 0.86f), new Color("#59f1ff", 0.40f));
        panel.SetAnchorsPreset(LayoutPreset.BottomLeft);
        panel.OffsetLeft = 34;
        panel.OffsetTop = -214;
        panel.OffsetRight = 574;
        panel.OffsetBottom = -34;
        AddChild(panel);

        var heading = UiFactory.MakeLabel(GameText.T("menu.mission.title"), 18, Ink);
        heading.Position = new Vector2(18, 14);
        heading.CustomMinimumSize = new Vector2(486, 26);
        panel.AddChild(heading);

        var line = UiFactory.MakeLabel(GameText.T("menu.mission.build"), 12, InkMuted);
        line.Position = new Vector2(18, 48);
        line.CustomMinimumSize = new Vector2(500, 22);
        panel.AddChild(line);

        var grid = new MenuTelemetry { Name = "Telemetry" };
        grid.Position = new Vector2(18, 86);
        grid.CustomMinimumSize = new Vector2(496, 66);
        panel.AddChild(grid);
    }

    private void BuildCommandSidebar()
    {
        var sidebar = UiFactory.MakePanel("CommandSidebar", new Color("#071019", 0.94f), new Color("#8fffe1", 0.46f));
        sidebar.SetAnchorsPreset(LayoutPreset.RightWide);
        sidebar.OffsetLeft = -376;
        sidebar.OffsetTop = 24;
        sidebar.OffsetRight = -24;
        sidebar.OffsetBottom = -24;
        AddChild(sidebar);

        var title = UiFactory.MakeLabel(GameText.T("menu.sidebar.title"), 20, Mint);
        title.Position = new Vector2(24, 24);
        title.CustomMinimumSize = new Vector2(260, 28);
        sidebar.AddChild(title);

        var detail = UiFactory.MakeLabel(GameText.T("menu.sidebar.detail"), 12, InkMuted);
        detail.Position = new Vector2(26, 54);
        detail.CustomMinimumSize = new Vector2(260, 22);
        sidebar.AddChild(detail);

        var setupPanel = UiFactory.MakePanel("SkirmishSetup", new Color("#02060a", 0.46f), new Color("#59f1ff", 0.28f));
        setupPanel.Position = new Vector2(24, 92);
        setupPanel.CustomMinimumSize = new Vector2(304, 264);
        sidebar.AddChild(setupPanel);

        BuildSkirmishSetup(setupPanel);

        var start = UiFactory.MakeButton(GameText.T("menu.startSkirmish"), Cyan);
        start.Name = "StartSkirmishButton";
        start.Position = new Vector2(24, 384);
        start.CustomMinimumSize = new Vector2(304, 48);
        start.Pressed += StartSkirmish;
        sidebar.AddChild(start);
        start.GrabFocus();

        var sandbox = UiFactory.MakeButton("SANDBOX", Mint);
        sandbox.Position = new Vector2(24, 444);
        sandbox.CustomMinimumSize = new Vector2(304, 48);
        sandbox.TooltipText = "F5: launch developer sandbox with extra units, buildings, resources, and daytime theme";
        sandbox.Pressed += StartSandbox;
        sidebar.AddChild(sandbox);

        var settings = UiFactory.MakeButton(GameText.T("common.settings"), Amber);
        settings.Position = new Vector2(24, 504);
        settings.CustomMinimumSize = new Vector2(304, 48);
        settings.Pressed += OpenSettings;
        sidebar.AddChild(settings);

        var quit = UiFactory.MakeButton(GameText.T("common.quit"), Danger);
        quit.Position = new Vector2(24, 564);
        quit.CustomMinimumSize = new Vector2(304, 48);
        quit.Pressed += QuitGame;
        sidebar.AddChild(quit);

        var footer = UiFactory.MakeLabel(GameText.T("menu.footer.next"), 11, InkMuted);
        footer.SetAnchorsPreset(LayoutPreset.BottomWide);
        footer.OffsetLeft = 24;
        footer.OffsetTop = -56;
        footer.OffsetRight = -24;
        footer.OffsetBottom = -28;
        sidebar.AddChild(footer);
    }

    private void BuildSkirmishSetup(Control setupPanel)
    {
        var setupTitle = UiFactory.MakeLabel(GameText.T("menu.skirmish.title"), 13, Cyan);
        setupTitle.Position = new Vector2(14, 10);
        setupTitle.CustomMinimumSize = new Vector2(270, 22);
        setupPanel.AddChild(setupTitle);

        AddSetupLabel(setupPanel, GameText.T("menu.skirmish.playerFaction"), 42);
        _playerFaction = MakeFactionSelect(FactionId.Dog);
        _playerFaction.Name = "PlayerFactionSelect";
        _playerFaction.Position = new Vector2(142, 38);
        _playerFaction.CustomMinimumSize = new Vector2(140, 28);
        _playerFaction.ItemSelected += _ => RefreshSkirmishSummary();
        setupPanel.AddChild(_playerFaction);

        AddSetupLabel(setupPanel, GameText.T("menu.skirmish.aiFaction"), 82);
        _aiFaction = MakeFactionSelect(FactionId.Cat);
        _aiFaction.Name = "AiFactionSelect";
        _aiFaction.Position = new Vector2(142, 78);
        _aiFaction.CustomMinimumSize = new Vector2(140, 28);
        _aiFaction.ItemSelected += _ => RefreshSkirmishSummary();
        setupPanel.AddChild(_aiFaction);

        AddSetupLabel(setupPanel, GameText.T("menu.skirmish.difficulty"), 122);
        _difficulty = MakeDifficultySelect();
        _difficulty.Name = "DifficultySelect";
        _difficulty.Position = new Vector2(142, 118);
        _difficulty.CustomMinimumSize = new Vector2(140, 28);
        _difficulty.ItemSelected += _ => RefreshSkirmishSummary();
        setupPanel.AddChild(_difficulty);

        AddSetupLabel(setupPanel, GameText.T("menu.skirmish.credits"), 162);
        _startingCredits = MakeSpinBox(800, 6000, 200, SkirmishOptions.DefaultStartingCredits);
        _startingCredits.Name = "StartingCreditsInput";
        _startingCredits.Position = new Vector2(142, 158);
        _startingCredits.CustomMinimumSize = new Vector2(140, 28);
        _startingCredits.ValueChanged += _ => RefreshSkirmishSummary();
        setupPanel.AddChild(_startingCredits);

        AddSetupLabel(setupPanel, GameText.T("menu.skirmish.seed"), 202);
        _mapSeed = MakeSpinBox(1, 999999, 1, SkirmishOptions.DefaultMapSeed);
        _mapSeed.Name = "MapSeedInput";
        _mapSeed.Position = new Vector2(142, 198);
        _mapSeed.CustomMinimumSize = new Vector2(140, 28);
        _mapSeed.ValueChanged += _ => RefreshSkirmishSummary();
        setupPanel.AddChild(_mapSeed);

        _setupSummary = UiFactory.MakeLabel("", 11, InkMuted);
        _setupSummary.Position = new Vector2(14, 234);
        _setupSummary.CustomMinimumSize = new Vector2(270, 20);
        setupPanel.AddChild(_setupSummary);
        RefreshSkirmishSummary();
    }

    private static void AddSetupLabel(Control parent, string text, float y)
    {
        var label = UiFactory.MakeLabel(text, 11, InkMuted);
        label.Position = new Vector2(14, y);
        label.CustomMinimumSize = new Vector2(120, 22);
        parent.AddChild(label);
    }

    private static OptionButton MakeDifficultySelect()
    {
        var button = new OptionButton
        {
            TooltipText = GameText.T("menu.skirmish.difficulty"),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        button.AddItem(GameText.T("menu.difficulty.easy"));
        button.AddItem(GameText.T("menu.difficulty.normal"));
        button.AddItem(GameText.T("menu.difficulty.hard"));
        button.Selected = (int)EnemyDifficulty.Normal;
        UiFontProfile.ApplyToControl(button, UiFontRole.Body, 12);
        return button;
    }

    private static OptionButton MakeFactionSelect(FactionId selected)
    {
        var button = new OptionButton
        {
            TooltipText = GameText.T("menu.skirmish.playerFaction"),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        button.AddItem(FactionLabel(FactionId.Dog), (int)FactionId.Dog);
        button.AddItem(FactionLabel(FactionId.Cat), (int)FactionId.Cat);
        button.AddItem(FactionLabel(FactionId.Corruption), (int)FactionId.Corruption);
        button.SetItemDisabled(2, true);
        button.Selected = selected == FactionId.Cat ? 1 : 0;
        UiFontProfile.ApplyToControl(button, UiFontRole.Body, 12);
        return button;
    }

    private static SpinBox MakeSpinBox(double min, double max, double step, double value)
    {
        var spinBox = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = value,
            Rounded = true,
            SelectAllOnFocus = true,
            MouseDefaultCursorShape = CursorShape.Ibeam,
        };
        UiFontProfile.ApplyToControl(spinBox, UiFontRole.Numeric, 12);
        return spinBox;
    }
}
