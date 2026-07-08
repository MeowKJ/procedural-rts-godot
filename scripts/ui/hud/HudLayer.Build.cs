using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void BuildResourceStrip(Control root)
    {
        var top = MakePanel("ResourceStrip", CurrentPalette.PanelStrongFill, CurrentPalette.PanelBorderStrong);
        top.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        top.OffsetLeft = -224;
        top.OffsetTop = 10;
        top.OffsetRight = 224;
        top.OffsetBottom = 56;
        top.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(top);

        var credits = MakeBlock(GameText.T("ui.top.credits"), "0", new Vector2(10, 5), new Vector2(150, 36));
        _creditsValue = credits.GetNode<Label>("Value");
        top.AddChild(credits);

        var power = MakeBlock(GameText.T("ui.top.power"), GameText.T("ui.top.powerStable"), new Vector2(170, 5), new Vector2(116, 36));
        top.AddChild(power);

        var status = MakeBlock(GameText.T("ui.top.status"), GameText.T("ui.status.systemsOnline"), new Vector2(296, 5), new Vector2(140, 36));
        _statusValue = status.GetNode<Label>("Value");
        top.AddChild(status);
    }

    private void BuildSandboxDeveloperPanel(Control root)
    {
        _sandboxDeveloperPanel = MakePanel("SandboxDeveloperPanel", CurrentPalette.PanelFill, CurrentPalette.PanelBorder);
        _sandboxDeveloperPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _sandboxDeveloperPanel.OffsetLeft = 86;
        _sandboxDeveloperPanel.OffsetTop = 182;
        _sandboxDeveloperPanel.OffsetRight = 330;
        _sandboxDeveloperPanel.OffsetBottom = 390;
        _sandboxDeveloperPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        _sandboxDeveloperPanel.Visible = false;
        root.AddChild(_sandboxDeveloperPanel);

        _sandboxDeveloperPanel.AddChild(MakeSizedLabel("Sandbox", new Vector2(12, 10), new Vector2(90, 18), FontSmall, Mint));
        _sandboxDeveloperStatus = MakeSizedLabel("", new Vector2(88, 10), new Vector2(140, 18), FontTiny, InkMuted);
        _sandboxDeveloperPanel.AddChild(_sandboxDeveloperStatus);

        _sandboxOwnerButton = AddSandboxDeveloperButton("SandboxOwner", "Cycle sandbox owner", new Vector2(12, 36), new Vector2(66, 28), Cyan, () =>
            SandboxDeveloperContextRequested?.Invoke(new SandboxDeveloperContextRequest(OwnerId: NextSandboxOwner())));
        _sandboxFactionButton = AddSandboxDeveloperButton("SandboxFaction", "Cycle sandbox faction", new Vector2(88, 36), new Vector2(66, 28), Mint, () =>
            SandboxDeveloperContextRequested?.Invoke(new SandboxDeveloperContextRequest(Faction: NextSandboxFaction())));
        _sandboxTeamButton = AddSandboxDeveloperButton("SandboxTeam", "Cycle sandbox team", new Vector2(164, 36), new Vector2(66, 28), Amber, () =>
            SandboxDeveloperContextRequested?.Invoke(new SandboxDeveloperContextRequest(TeamId: NextSandboxTeam())));

        _sandboxRelationButton = AddSandboxDeveloperButton("SandboxRelation", "Cycle relation to player 1", new Vector2(12, 74), new Vector2(66, 28), Danger, () =>
            SandboxDeveloperContextRequested?.Invoke(new SandboxDeveloperContextRequest(Relation: NextSandboxRelation())));
        _sandboxTimeButton = AddSandboxDeveloperButton("SandboxTime", "Cycle time scale", new Vector2(88, 74), new Vector2(66, 28), Cyan, () =>
            SandboxDeveloperContextRequested?.Invoke(new SandboxDeveloperContextRequest(TimeScale: NextSandboxTimeScale())));
        _sandboxAtmosphereButton = AddSandboxDeveloperButton("SandboxAtmosphere", "Cycle atmosphere", new Vector2(164, 74), new Vector2(66, 28), Mint, () =>
            SandboxDeveloperContextRequested?.Invoke(new SandboxDeveloperContextRequest(Environment: NextSandboxEnvironment())));

        _sandboxOverlayButton = AddSandboxDeveloperButton("SandboxOverlay", "Cycle debug overlay preset", new Vector2(12, 112), new Vector2(104, 28), Amber, () =>
            SandboxDeveloperContextRequested?.Invoke(new SandboxDeveloperContextRequest(DebugOverlayPreset: NextSandboxOverlayPreset())));
        _sandboxStressButton = AddSandboxDeveloperButton("SandboxStress", "Spawn a deterministic stress pack", new Vector2(126, 112), new Vector2(104, 28), Danger, () =>
            SandboxStressRequested?.Invoke());

        _sandboxStateHashValue = MakeSizedLabel("", new Vector2(12, 154), new Vector2(218, 18), FontTiny, InkMuted);
        _sandboxStateHashValue.Name = "SandboxStateHash";
        _sandboxStateHashValue.Visible = false;
        _sandboxDeveloperPanel.AddChild(_sandboxStateHashValue);

        SetSandboxDeveloperContext(_sandboxDeveloperContext);
    }

    private void BuildMinimapCluster(Control root)
    {
        var minimap = MakePanel("MinimapCluster", CurrentPalette.PanelStrongFill, CurrentPalette.PanelBorderStrong);
        minimap.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        minimap.OffsetLeft = -312;
        minimap.OffsetTop = 12;
        minimap.OffsetRight = -12;
        minimap.OffsetBottom = 178;
        minimap.MouseFilter = Control.MouseFilterEnum.Stop;
        root.AddChild(minimap);

        _minimapSurface = new MinimapSurface
        {
            Name = "Surface",
            Position = new Vector2(10, 10),
            CustomMinimumSize = new Vector2(280, 146),
            MouseFilter = Control.MouseFilterEnum.Stop,
            JumpRequested = worldPoint => MinimapJumpRequested?.Invoke(worldPoint),
        };
        minimap.AddChild(_minimapSurface);
    }

    private void BuildCommandRibbon(Control root)
    {
        _commandRibbon = MakePanel("CommandRibbon", CurrentPalette.PanelFill, CurrentPalette.PanelBorder);
        _commandRibbon.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _commandRibbon.OffsetLeft = 96;
        _commandRibbon.OffsetTop = -58;
        _commandRibbon.OffsetRight = -328;
        _commandRibbon.OffsetBottom = -12;
        _commandRibbon.MouseFilter = Control.MouseFilterEnum.Ignore;
        _commandRibbon.Visible = true;
        root.AddChild(_commandRibbon);

        AddStanceModeButton(_commandRibbon, UnitStance.Hold, IconGlyph.StanceHold, $"Z  {GameText.T("stance.hold")}", new Vector2(12, 6));
        AddStanceModeButton(_commandRibbon, UnitStance.Aggressive, IconGlyph.StanceAggressive, $"X  {GameText.T("stance.aggressive")}", new Vector2(58, 6));
        AddStanceModeButton(_commandRibbon, UnitStance.ReturnGuard, IconGlyph.StanceReturn, $"C  {GameText.T("stance.returnGuard")}", new Vector2(104, 6));
        AddStanceModeButton(_commandRibbon, UnitStance.PassiveRetaliate, IconGlyph.StancePassive, $"V  {GameText.T("stance.passive")}", new Vector2(150, 6));
        AddStanceModeButton(_commandRibbon, UnitStance.Ignore, IconGlyph.StanceIgnore, $"B  {GameText.T("stance.ignore")}", new Vector2(196, 6));

        AddSeparator(_commandRibbon, new Vector2(254, 8));
        AddMoveModeButton(_commandRibbon, MoveCommandMode.Direct, IconGlyph.Move, GameText.T("ui.tooltip.directMove"), new Vector2(274, 6));
        AddMoveModeButton(_commandRibbon, MoveCommandMode.Attack, IconGlyph.AttackMove, GameText.T("ui.tooltip.attackMove"), new Vector2(320, 6));
        AddMoveModeButton(_commandRibbon, MoveCommandMode.Ignore, IconGlyph.IgnoreMove, GameText.T("ui.tooltip.ignoreMove"), new Vector2(366, 6));

        AddSeparator(_commandRibbon, new Vector2(642, 8));
        var ribbonRepair = AddIconActionButton(_commandRibbon, IconGlyph.Repair, GameText.T("ui.context.repair"), new Vector2(662, 6), new Vector2(36, 34), Mint);
        ribbonRepair.Name = "RibbonRepair";
        ribbonRepair.Pressed += () => RepairRequested?.Invoke();
        var ribbonCancel = AddIconActionButton(_commandRibbon, IconGlyph.Cancel, GameText.T("ui.context.sell"), new Vector2(708, 6), new Vector2(36, 34), Danger);
        ribbonCancel.Name = "RibbonCancelProduction";
        ribbonCancel.Pressed += () => SellOrCancelRequested?.Invoke();
        _sellOrCancelAction = ribbonCancel;
        RefreshSellOrCancelAction();
        var ribbonRally = AddIconActionButton(_commandRibbon, IconGlyph.Building, GameText.T("ui.context.rally"), new Vector2(754, 6), new Vector2(36, 34), Amber);
        ribbonRally.Name = "RibbonSetRally";
        ribbonRally.Pressed += () => RallyRequested?.Invoke();
        _settingsButton = AddIconActionButton(_commandRibbon, IconGlyph.Settings, GameText.T("common.settings"), new Vector2(816, 6), new Vector2(36, 34), Cyan);
        _settingsButton.Pressed += () => SettingsRequested?.Invoke();
    }

    private void BuildAlertChips(Control root)
    {
        _alertValue = MakeLabel(GameText.T("ui.alert.none"), new Vector2(16, 66), 10, new Color("#ffb4c0", 0.0f));
        _alertValue.Visible = false;
        root.AddChild(_alertValue);
        for (var index = 0; index < 2; index++)
        {
            var row = new AlertRow
            {
                Position = new Vector2(16, 66 + index * 22),
                CustomMinimumSize = new Vector2(280, 20),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _alertRows.Add(row);
            root.AddChild(row);
        }
    }

    private void BuildRightRail(Control root)
    {
        _rightRail = MakePanel("RightRail", CurrentPalette.PanelFill, CurrentPalette.PanelBorder);
        _rightRail.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        _rightRail.OffsetLeft = -RailWidth;
        _rightRail.OffsetTop = HudLayoutMath.ProductionPanelTop;
        _rightRail.OffsetRight = 0;
        _rightRail.OffsetBottom = -12;
        _rightRail.MouseFilter = Control.MouseFilterEnum.Stop;
        root.AddChild(_rightRail);
        for (var index = 0; index < MaxProductionProviderLaneButtons; index++)
        {
            AddProductionProviderLaneButton(_rightRail, index);
        }

        _providerLaneSummaryValue = MakeSizedLabel(GameText.T("ui.providerLane.empty"), new Vector2(4, 284), new Vector2(40, 92), FontTiny, InkMuted);
        _providerLaneSummaryValue.Name = "ProviderLaneSummary";
        _providerLaneSummaryValue.VerticalAlignment = VerticalAlignment.Top;
        _rightRail.AddChild(_providerLaneSummaryValue);
    }

    private void BuildRightDrawer(Control root)
    {
        _rightProductionPanel = MakePanel("ProductionPanel", CurrentPalette.PanelStrongFill, CurrentPalette.PanelBorderStrong);
        _rightProductionPanel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _rightProductionPanel.OffsetLeft = -312;
        _rightProductionPanel.OffsetTop = HudLayoutMath.ProductionPanelTop;
        _rightProductionPanel.OffsetRight = -12;
        _rightProductionPanel.OffsetBottom = HudLayoutMath.ProductionPanelTop + HudLayoutMath.ProductionPanelHeight;
        _rightProductionPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        root.AddChild(_rightProductionPanel);

        AddCatalogModeButton(_rightProductionPanel, CatalogModeKind.Build, GameText.T("ui.catalog.build"), GameText.T("ui.catalog.buildDetail"), GameText.T("ui.catalog.buildHelp"), new Vector2(8, 8));
        AddCatalogModeButton(_rightProductionPanel, CatalogModeKind.Train, GameText.T("ui.catalog.train"), GameText.T("ui.catalog.trainDetail"), GameText.T("ui.catalog.trainHelp"), new Vector2(78, 8));
        AddCatalogModeButton(_rightProductionPanel, CatalogModeKind.Upgrades, GameText.T("ui.catalog.upgrades"), GameText.T("ui.catalog.upgradesDetail"), GameText.T("ui.catalog.upgradesHelp"), new Vector2(148, 8));
        AddCatalogModeButton(_rightProductionPanel, CatalogModeKind.Abilities, GameText.T("ui.catalog.abilities"), GameText.T("ui.catalog.abilitiesDetail"), GameText.T("ui.catalog.abilitiesHelp"), new Vector2(218, 8));

        AddProductionTab(_rightProductionPanel, IconGlyph.Building, GameText.T("ui.tabs.command"), new Vector2(10, 44), BuildCategory.Command, active: true);
        AddProductionTab(_rightProductionPanel, IconGlyph.Credits, GameText.T("ui.tabs.power"), new Vector2(45, 44), BuildCategory.Power, active: true);
        AddProductionTab(_rightProductionPanel, IconGlyph.Harvester, GameText.T("ui.tabs.economy"), new Vector2(80, 44), BuildCategory.Economy, active: true);
        AddProductionTab(_rightProductionPanel, IconGlyph.Infantry, GameText.T("ui.tabs.infantry"), new Vector2(115, 44), BuildCategory.Infantry, active: true);
        AddProductionTab(_rightProductionPanel, IconGlyph.Tank, GameText.T("ui.tabs.vehicle"), new Vector2(150, 44), BuildCategory.Vehicle, active: true);
        AddProductionTab(_rightProductionPanel, IconGlyph.StanceHold, GameText.T("ui.tabs.defense"), new Vector2(185, 44), BuildCategory.Defense, active: true);
        AddProductionTab(_rightProductionPanel, IconGlyph.Air, GameText.T("ui.tabs.air"), new Vector2(220, 44), BuildCategory.Air, active: true);
        AddProductionTab(_rightProductionPanel, IconGlyph.Naval, GameText.T("ui.tabs.naval"), new Vector2(255, 44), BuildCategory.Naval, active: false);

        AddTrainCategoryTab(_rightProductionPanel, IconGlyph.Infantry, GameText.T("ui.tabs.infantry"), new Vector2(10, 44), ProductionCategory.Infantry, active: true);
        AddTrainCategoryTab(_rightProductionPanel, IconGlyph.Tank, GameText.T("ui.tabs.vehicle"), new Vector2(45, 44), ProductionCategory.Vehicle, active: true);
        AddTrainCategoryTab(_rightProductionPanel, IconGlyph.Harvester, GameText.T("ui.tabs.economy"), new Vector2(80, 44), ProductionCategory.Economy, active: true);
        AddTrainCategoryTab(_rightProductionPanel, IconGlyph.StanceHold, GameText.T("ui.tabs.defense"), new Vector2(115, 44), ProductionCategory.Defense, active: true);
        AddTrainCategoryTab(_rightProductionPanel, IconGlyph.Air, GameText.T("ui.tabs.air"), new Vector2(150, 44), ProductionCategory.Air, active: true);
        AddTrainCategoryTab(_rightProductionPanel, IconGlyph.Naval, GameText.T("ui.tabs.naval"), new Vector2(185, 44), ProductionCategory.Naval, active: false);

        _catalogSurfaceLabel = MakeSizedLabel(GameText.T("ui.catalog.trainSurface"), new Vector2(14, 76), new Vector2(92, 14), FontTiny, InkMuted);
        _rightProductionPanel.AddChild(_catalogSurfaceLabel);
        _catalogOverviewValue = MakeSizedLabel("", new Vector2(112, 76), new Vector2(172, 14), FontTiny, InkMuted);
        _catalogOverviewValue.Name = "CatalogOverview";
        _catalogOverviewValue.HorizontalAlignment = HorizontalAlignment.Right;
        _rightProductionPanel.AddChild(_catalogOverviewValue);
        _productionValue = MakeSizedLabel(GameText.T("ui.status.ready"), new Vector2(70, 72), new Vector2(214, 28), FontSmall, Ink);
        _productionValue.Name = "CatalogInspector";
        _rightProductionPanel.AddChild(_productionValue);

        _queueValue = MakeSizedLabel(GameText.T("ui.queue.empty"), new Vector2(14, 334), new Vector2(150, 20), FontSmall, InkMuted);
        _rightProductionPanel.AddChild(_queueValue);

        _repeatProduction = AddIconActionButton(
            _rightProductionPanel,
            IconGlyph.StanceReturn,
            GameText.T("ui.repeat.needCard"),
            new Vector2(166, 326),
            new Vector2(28, 28),
            Cyan);
        _repeatProduction.Name = "RepeatProduction";
        _repeatProduction.ToggleMode = true;
        _repeatProduction.Disabled = true;
        _repeatProduction.Pressed += RequestFocusedProductionRepeat;

        _repeatProductionStateValue = MakeSizedLabel(GameText.T("ui.repeat.state.needCard"), new Vector2(156, 306), new Vector2(48, 14), FontTiny, InkMuted);
        _repeatProductionStateValue.Name = "RepeatProductionState";
        _repeatProductionStateValue.HorizontalAlignment = HorizontalAlignment.Center;
        _rightProductionPanel.AddChild(_repeatProductionStateValue);

        _cancelProduction = new Button
        {
            Name = "CancelProduction",
            Text = GameText.T("ui.cancel"),
            Position = new Vector2(202, 326),
            CustomMinimumSize = new Vector2(82, 28),
            FocusMode = Control.FocusModeEnum.Click,
            MouseFilter = Control.MouseFilterEnum.Stop,
            Disabled = true,
            TooltipText = GameText.T("ui.cancel.none"),
        };
        UiFactory.ApplyHudCancelButtonTheme(_cancelProduction, CurrentPalette, FontSmall);
        _cancelProduction.Pressed += () => CancelProductionRequested?.Invoke();
        _rightProductionPanel.AddChild(_cancelProduction);
        SelectCatalogMode(_selectedCatalogMode);

        _rightDetailPanel = MakePanel("UnitDetailPanel", CurrentPalette.PanelStrongFill, CurrentPalette.PanelBorder);
        _rightDetailPanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        _rightDetailPanel.OffsetLeft = -312;
        _rightDetailPanel.OffsetTop = -170;
        _rightDetailPanel.OffsetRight = -12;
        _rightDetailPanel.OffsetBottom = -12;
        _rightDetailPanel.CustomMinimumSize = new Vector2(300, 158);
        _rightDetailPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        root.AddChild(_rightDetailPanel);
        _rightDetailPanel.AddChild(MakeSizedLabel(GameText.T("ui.unitDetail"), new Vector2(14, 12), new Vector2(120, 18), FontBody, Mint));
        _drawerPortrait = new PortraitGlyph
        {
            Name = "DetailPortrait",
            Position = new Vector2(14, 42),
            CustomMinimumSize = new Vector2(72, 82),
        };
        _rightDetailPanel.AddChild(_drawerPortrait);
        _drawerIconSummary = new SelectionIconSummary
        {
            Name = "IconSummary",
            Position = new Vector2(12, 42),
            CustomMinimumSize = new Vector2(88, 92),
            Visible = false,
        };
        _drawerIconSummary.Size = _drawerIconSummary.CustomMinimumSize;
        _rightDetailPanel.AddChild(_drawerIconSummary);
        _drawerSelectedTitle = MakeSizedLabel(GameText.T("ui.noSelection.title"), new Vector2(104, 40), new Vector2(170, 20), FontMeta, Ink);
        _drawerSelectedMeta = MakeSizedLabel(GameText.T("ui.noSelection.meta"), new Vector2(104, 64), new Vector2(170, 18), FontSmall, Mint);
        _drawerSelectedStats = MakeSizedLabel(GameText.T("ui.noSelection.stats"), new Vector2(104, 86), new Vector2(170, 18), FontSmall, Ink);
        _drawerSelectedDetail = MakeSizedLabel(GameText.T("ui.noSelection.detail"), new Vector2(104, 108), new Vector2(170, 48), FontTiny, InkMuted);
        _rightDetailPanel.AddChild(_drawerSelectedTitle);
        _rightDetailPanel.AddChild(_drawerSelectedMeta);
        _rightDetailPanel.AddChild(_drawerSelectedStats);
        _rightDetailPanel.AddChild(_drawerSelectedDetail);
    }

    private void BuildCommandPreview(Control root)
    {
        _commandPreview = new CommandPreviewOverlay
        {
            Name = "CommandPreview",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _commandPreview.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_commandPreview);
    }

    private void BuildOutcomeBanner(Control root)
    {
        _outcomeBanner = MakePanel("OutcomeBanner", CurrentPalette.PanelStrongFill, new Color(Mint, 0.62f));
        _outcomeBanner.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _outcomeBanner.OffsetLeft = -190;
        _outcomeBanner.OffsetTop = 132;
        _outcomeBanner.OffsetRight = 190;
        _outcomeBanner.OffsetBottom = 220;
        _outcomeBanner.MouseFilter = Control.MouseFilterEnum.Ignore;
        _outcomeBanner.Visible = false;
        root.AddChild(_outcomeBanner);

        _outcomeTitle = MakeSizedLabel(GameText.T("ui.outcome.victory"), new Vector2(20, 14), new Vector2(340, 30), 24, Mint);
        _outcomeTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _outcomeBanner.AddChild(_outcomeTitle);

        _outcomeDetail = MakeSizedLabel(GameText.T("ui.outcome.enemyHqDestroyed"), new Vector2(20, 52), new Vector2(340, 22), FontBody, Ink);
        _outcomeDetail.HorizontalAlignment = HorizontalAlignment.Center;
        _outcomeBanner.AddChild(_outcomeDetail);
    }
}
