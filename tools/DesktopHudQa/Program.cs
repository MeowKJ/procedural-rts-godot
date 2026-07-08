using ProceduralRts.Core;

var cases = new (int Width, int Height, float UiScale, string Name)[]
{
    (1280, 720, 1.0f, "desktop minimum"),
    (1600, 900, 1.0f, "desktop standard"),
    (1920, 1080, 1.0f, "desktop full hd"),
    (1600, 900, 1.25f, "high dpi 125"),
    (1920, 1080, 1.5f, "high dpi 150"),
};

var failures = new List<string>();
foreach (var testCase in cases)
{
    var snapshot = HudLayoutMath.Create(testCase.Width, testCase.Height, testCase.UiScale);
    var issues = HudLayoutMath.Validate(snapshot);
    if (issues.Count > 0)
    {
        failures.Add($"{testCase.Name} {testCase.Width}x{testCase.Height} scale {testCase.UiScale:0.##}: {string.Join("; ", issues)}");
    }
}

if (failures.Count > 0)
{
    throw new InvalidOperationException("HUD desktop QA failed:\n" + string.Join("\n", failures));
}

var repoRoot = FindRepoRoot();
AssertHudFactoryExtraction(repoRoot);

Console.WriteLine("Desktop HUD QA passed: 1280x720, 1600x900, 1920x1080, high-DPI layout constraints, and HUD UiFactory extraction");

static string FindRepoRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ProceduralRts.csproj"))
            && File.Exists(Path.Combine(current.FullName, "scripts", "ui", "HudLayer.cs")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find procedural-rts-godot repository root for HUD source checks.");
}

static void AssertHudFactoryExtraction(string root)
{
    var hudLayer = ReadSourceWithPartials(Path.Combine(root, "scripts", "ui", "HudLayer.cs"));
    var battleRoot = ReadSourceWithPartials(Path.Combine(root, "scripts", "BattleRoot.cs"));
    var selectionController = ReadSourceWithPartials(Path.Combine(root, "scripts", "controllers", "SelectionController.cs"));
    var hudSync = File.ReadAllText(Path.Combine(root, "scripts", "BattleRoot.HudSync.cs"));
    var uiFactory = File.ReadAllText(Path.Combine(root, "scripts", "ui", "UiFactory.cs"));
    var cursorCatalog = File.ReadAllText(Path.Combine(root, "scripts", "core", "presentation", "ui", "BattleCursorCatalog.cs"));
    var hotkeys = File.ReadAllText(Path.Combine(root, "scripts", "ui", "HotkeyLegendLayer.cs"));
    var englishText = File.ReadAllText(Path.Combine(root, "scripts", "core", "localization", "GameText.English.cs"));
    var chineseText = File.ReadAllText(Path.Combine(root, "scripts", "core", "localization", "GameText.ChineseSimplified.cs"));

    RequireText(cursorCatalog, "public enum BattleCursorState", "Battle cursor states must live in a central catalog.");
    RequireText(cursorCatalog, "BuildValid and BuildInvalid must share a hotspot", "Cursor catalog validation must guard build valid/invalid hotspot parity.");
    RequireText(cursorCatalog, "Kenney Cursor Pack CC0", "Cursor catalog texture entries must keep Kenney CC0 provenance near the data.");
    RequireText(hudLayer, "Input.SetCustomMouseCursor", "HudLayer should use custom cursor textures when catalog texture paths are present.");
    RequireText(hudLayer, "Input.SetDefaultCursorShape(shape)", "HudLayer cursor textures must preserve built-in cursor shape fallback.");
    RequireText(hudLayer, "ApplyCommandCursor(preview);", "HudLayer command preview updates must route cursor state through the cursor catalog.");
    RequireText(hudLayer, "BattleCursorCatalog.StateForPreview(preview)", "HudLayer must derive in-game cursor state from command preview state.");
    RequireText(uiFactory, "BattleCursorCatalog.DefinitionFor(BattleCursorState.UiHover)", "HUD/UI buttons must use the shared cursor catalog for hover cursors.");
    RequireText(battleRoot, "IsInsufficientCreditsStatus(status)", "BattleRoot status alerts must detect insufficient-credit failures through a focused helper.");
    RequireText(battleRoot, "TryUseAlertCooldown(\"status:insufficient-credits\", InsufficientCreditsAlertCooldown)", "Insufficient-credit HUD alerts must use one shared cooldown key instead of per-status spam.");
    RequireText(battleRoot, "AddAlert(AlertKind.Economy, GameText.T(\"ui.alert.insufficientCredits\"))", "Insufficient-credit failures must surface a localized economy alert.");
    RequireText(englishText, "[\"ui.alert.insufficientCredits\"]", "English insufficient-credit alert text must exist.");
    RequireText(chineseText, "[\"ui.alert.insufficientCredits\"]", "Chinese insufficient-credit alert text must exist.");
    RequireText(hudLayer, "UiFactory.MakeHudPanel", "HudLayer panel creation must use UiFactory.MakeHudPanel.");
    RequireText(hudLayer, "UiFactory.MakeHudSizedLabel", "HudLayer sized labels must use UiFactory.MakeHudSizedLabel.");
    RequireText(hudLayer, "UiFactory.ApplyNamedHudPanelTheme", "HudLayer panel refresh must use UiFactory.ApplyNamedHudPanelTheme.");
    RequireText(hudLayer, "UiFactory.ApplyHudActionButtonTheme", "HudLayer icon actions must use UiFactory.ApplyHudActionButtonTheme.");
    RequireText(hudLayer, "UiFactory.ApplyHudCommandButtonTheme", "HudLayer command buttons must use UiFactory.ApplyHudCommandButtonTheme.");
    RequireText(hudLayer, "UiFactory.GetHudControlGroupSlotStyle", "HudLayer control-group slot style must come from UiFactory.");
    RequireText(hudLayer, "List<ProductionTab> _productionTabs", "HUD must retain production tab controls so selected state can update after clicks.");
    RequireText(hudLayer, "private BuildCategory _selectedBuildCategory = BuildCategory.Command", "HUD production tabs must default to command selected before a tab click.");
    RequireText(hudLayer, "SelectProductionTab(category);", "Build category tab clicks must update the visible selected tab before rendering build cards.");
    RequireText(hudLayer, "tab.SetSelected(tab.Category == category);", "HUD production tabs must redraw selected state from the last requested build category.");
    RequireText(hudLayer, "BuildKindRequested?.Invoke(button.BuildKind, SelectedConstructionProviderId())", "Build option cards must request build placement with the selected construction provider lane.");
    RequireText(hudLayer, "state.Category != _selectedBuildCategory", "Build option cards must filter by the selected build category.");
    RequireText(hudLayer, "_visibleBuildCardStates.Count >= 12", "Build option cards must keep the 12-slot fixed grid cap.");
    RequireText(hudLayer, "button.SetBuildState(state, disabledReason)", "Build option cards must render build snapshot state and disabled reasons.");
    ForbidText(hudLayer, "BuildCategoryRequested", "Build category tabs must filter cards without arming placement.");
    RequireText(hudLayer, "BuildCategory.Naval, active: false", "HUD must keep the naval build tab visibly disabled until naval build specs exist.");
    RequireText(hudLayer, "\"CatalogModeBuild\"", "Right command panel must expose a stable Build catalog mode node.");
    RequireText(hudLayer, "\"CatalogModeTrain\"", "Right command panel must expose a stable Train catalog mode node.");
    RequireText(hudLayer, "\"CatalogModeUpgrades\"", "Right command panel must expose a stable Upgrades catalog mode node.");
    RequireText(hudLayer, "\"CatalogModeAbilities\"", "Right command panel must expose a stable Abilities catalog mode node.");
    RequireText(hudLayer, "Name = \"CatalogInspector\"", "Right command panel must expose a stable catalog inspector node.");
    RequireText(hudLayer, "Name = \"CatalogOverview\"", "Right command panel must expose a stable catalog overview node.");
    RequireText(hudLayer, "new Vector2(112, 76), new Vector2(172, 14), FontTiny", "Catalog overview must use a compact row above the fixed card grid.");
    RequireText(hudLayer, "new Vector2(70, 72), new Vector2(214, 28), FontSmall", "Catalog inspector must keep the two-line compact status slot above the cards.");
    RequireText(hudLayer, "RefreshCatalogOverview()", "Right command panel must refresh the catalog overview when page state changes.");
    RequireText(hudLayer, "SetAbilityCardState(IReadOnlyList<AbilityCardState> states)", "Ability state changes must feed the catalog overview path.");
    RequireText(hudLayer, "SetProductionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)", "Train provider lane state changes must feed the catalog overview path.");
    RequireText(hudLayer, "SetConstructionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)", "Build provider lane state changes must feed the catalog overview path.");
    RequireText(hudLayer, "CatalogOverviewProductionLaneCount()", "Train catalog overview must count provider lanes for the selected train category.");
    RequireText(hudLayer, "CatalogModeButton", "Right command panel mode controls must be clickable buttons, not decorative labels.");
    RequireText(hudLayer, "button.Pressed += () => SelectCatalogMode(mode);", "Build/Train mode buttons must switch catalog pages.");
    RequireText(hudLayer, "HelpText = helpText", "Catalog mode buttons must carry localized page help text.");
    RequireText(hudLayer, "button.MouseEntered += () => SetCatalogStatusText(button.HelpText);", "Catalog mode button hover must explain the page in the inspector.");
    RequireText(hudLayer, "button.FocusEntered += () => SetCatalogStatusText(button.HelpText);", "Catalog mode button focus must explain the page in the inspector.");
    RequireText(hudLayer, "button.Pressed += () => SetCatalogStatusText(button.HelpText);", "Catalog mode button press must explain the selected page in the inspector.");
    RequireText(hudLayer, "button.FocusEntered += () => SetCatalogStatusText(button.InspectorText);", "Build and Train card focus must show focused action details in the catalog inspector.");
    RequireText(hudLayer, "button.FocusEntered += () => FocusRepeatProductionDesign(button.UnitDesignId);", "Train card focus must establish repeat-production source context without mouse hover.");
    RequireText(hudLayer, "button.FocusExited += () => RestoreCatalogStatusText();", "Build and Train card focus exit must restore catalog page help/status.");
    RequireText(hudLayer, "card.FocusEntered += () => SetCatalogStatusText(card.InspectorText);", "Ability card focus must show focused ability details in the catalog inspector.");
    RequireText(hudLayer, "card.FocusExited += RestoreCatalogStatusText;", "Ability card focus exit must restore catalog ability page status.");
    RequireText(hudLayer, "tab.Pressed += () => SelectProductionCategory(category);", "Train category tabs must update the production category page.");
    RequireText(hudLayer, "tab.Visible = mode == CatalogModeKind.Build;", "Build category tabs must only be visible on the Build catalog page.");
    RequireText(hudLayer, "tab.Visible = mode == CatalogModeKind.Train;", "Train category tabs must only be visible on the Train catalog page.");
    RequireText(hudLayer, "state.Category != _selectedProductionCategory", "Train command grid must filter by the selected production category.");
    RequireText(hudLayer, "DrawStatusBadge(size)", "Build and Train command cards must render compact availability badges.");
    RequireText(hudLayer, "CommandCardStatusBadgeText(enabled, queued, _progress, disabledReasonKey)", "Command-card status badges must derive from enabled, queue, progress, and disabled-reason state.");
    RequireText(hudLayer, "state.DisabledReasonKey", "Build and Train cards must pass raw disabled reason keys into status badge selection.");
    RequireText(hudLayer, "CatalogModeKind.Abilities => GameText.T(\"ui.catalog.abilitiesSurface\")", "Abilities mode must own a distinct right-panel surface label.");
    RequireText(hudLayer, "CatalogModeKind.Upgrades => GameText.T(\"ui.catalog.upgradesSurface\")", "Upgrades mode must own a distinct right-panel surface label.");
    RequireText(hudLayer, "CatalogModeKind.Upgrades => GameText.T(\"ui.catalog.overview.upgrades\")", "Upgrades mode must expose a shell overview without provider lanes.");
    RequireText(hudLayer, "SetCatalogStatusText(GameText.T(\"ui.catalog.upgradesEmpty\"))", "Upgrades mode must show a localized shell empty state instead of production status.");
    RequireText(hudLayer, "_selectedCatalogMode == CatalogModeKind.Upgrades", "Catalog inspector restore must keep Upgrades on its shell empty state.");
    RequireText(hudLayer, "public void SetAbilityCardState(IReadOnlyList<AbilityCardState> states)", "HUD must expose selected-unit ability card state separately from production cards.");
    RequireText(hudLayer, "Dictionary<AbilityKind, AbilityCard> _abilityCards", "Ability cards must not reuse production command buttons.");
    RequireText(hudLayer, "private partial class AbilityCard : Button", "Abilities mode must render dedicated ability cards.");
    RequireText(hudLayer, "Name = $\"AbilityCard{kind}\"", "Ability cards must expose stable node names for structure/screenshot QA.");
    RequireText(hudLayer, "Action<AbilityKind>? AbilityRequested", "Ability cards must emit a typed request instead of routing through production cards.");
    RequireText(hudLayer, "button.MouseEntered += () => SetCatalogStatusText(button.InspectorText);", "Build and train cards must update the catalog inspector on hover.");
    RequireText(hudLayer, "card.MouseEntered += () => SetCatalogStatusText(card.InspectorText);", "Ability cards must update the catalog inspector on hover.");
    RequireText(hudLayer, "private void RestoreCatalogStatusText()", "Card hover exit must restore the catalog page status text.");
    RequireText(hudLayer, "List<ProductionProviderLaneState> _productionProviderLaneStates", "Train catalog provider lanes must keep reusable lane state storage.");
    RequireText(hudLayer, "List<ProductionProviderLaneState> _constructionProviderLaneStates", "Build catalog construction provider lanes must keep separate reusable lane state storage.");
    RequireText(hudLayer, "private partial class ProductionProviderLaneButton : Button", "Train catalog provider lanes must render through stable lane buttons.");
    RequireText(hudLayer, "Name = $\"ProductionProviderLane{index}\"", "Train provider lane buttons must expose stable node names for QA.");
    RequireText(hudLayer, "AddProductionProviderLaneButton(_rightRail, index)", "Train provider lanes must live in the right rail instead of compressing the 12-slot card grid.");
    RequireText(hudLayer, "Name = \"ProviderLaneSummary\"", "Train provider lane summary must expose a stable node name for QA.");
    RequireText(hudLayer, "RefreshProductionProviderLaneSummary()", "Train provider lane selection/state changes must refresh the provider detail summary.");
    RequireText(hudLayer, "SetConstructionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)", "HUD must accept construction provider lanes separately from Train lanes.");
    RequireText(hudLayer, "SelectConstructionProviderLane(state)", "Build provider lane clicks must update construction lane selection without changing Train provider selection.");
    RequireText(hudLayer, "button.SetState(state, IsConstructionProviderLaneSelected(state), state.Available, constructionMode: true)", "Build catalog mode must render construction provider lanes in the right rail.");
    RequireText(hudLayer, "ui.constructionProviderLane.tooltip", "Build provider lane tooltips must use construction-specific copy.");
    RequireText(hudLayer, "ProviderLaneSummaryText(state)", "Train provider lane summary must render selected provider count, queue count, progress, and availability.");
    RequireText(hudLayer, "ProviderLaneSummaryDisabledReason(state.DisabledReasonKey)", "Train provider lane summary must use rail-safe disabled reason codes.");
    RequireText(hudLayer, "ProductionDesignRequested?.Invoke(button.UnitDesignId, () => SelectedProductionProviderId(button.UnitDesignId), ProductionRequestCount())", "Shift-click train cards must pass a bounded production request count while preserving provider lane selection.");
    RequireText(hudLayer, "ProductionRequested?.Invoke(button.Kind, ProductionRequestCount())", "Legacy production card requests must pass the same Shift batch count path.");
    RequireText(hudLayer, "Input.IsKeyPressed(Key.Shift) ? ShiftProductionBatchCount : 1", "HUD train cards must keep single-click unchanged and Shift-click bounded.");
    RequireText(hudLayer, "Action<string, int>? ProductionRepeatRequested", "HUD must expose a selected-provider repeat-production request.");
    RequireText(hudLayer, "Name = \"RepeatProduction\"", "Right Train controls must expose a stable repeat-production toggle node.");
    RequireText(hudLayer, "Name = \"RepeatProductionState\"", "Right Train controls must expose a stable repeat-production state label.");
    RequireText(hudLayer, "FocusRepeatProductionDesign(button.UnitDesignId)", "Train card hover/click must establish the repeat-production target context.");
    RequireText(hudLayer, "CurrentProductionProviderLaneState() is not { Scope: ProductionProviderLaneScope.Specific", "Repeat production must require a specific provider lane.");
    RequireText(hudLayer, "_repeatProductionStateValue.Text = statusText;", "Repeat production must surface visible state text beside the toggle.");
    RequireText(hudLayer, "RepeatProductionStateText(laneState, hasDesign, hasSpecificProvider, providerSupportsDesign, active)", "Repeat production state text must derive from focus, provider lane, availability, and active state.");
    RequireText(hudLayer, "RefreshRepeatProductionControl();", "Theme refresh and provider-lane changes must reapply repeat-production visual state.");
    RequireText(hudLayer, "State.RepeatOutputSpecId", "Provider lane controls must surface the armed repeat unit state.");
    RequireText(battleRoot, "ProductionRepeatRequested = OnProductionRepeatRequested", "BattleRoot must wire the Train repeat toggle from HUD.");
    RequireText(battleRoot, "ToggleRepeatProductionForProvider", "BattleRoot must route repeat production through the UnitBattlefield provider helper.");
    RequireText(battleRoot, "internal const int ShiftProductionBatchCount = 5", "BattleRoot must expose the bounded Shift production batch count.");
    RequireText(battleRoot, "ProductionBatchStatus(queued, attempts, status)", "BattleRoot production requests must summarize Shift batches after existing command submission.");
    RequireText(battleRoot, "TrySubmitProductionDesignRequest(designId, providerIdSelector(), out status)", "Train-card Shift batches must resolve provider lanes for each queued attempt.");
    RequireText(englishText, "[\"production.batchQueued\"]", "English Shift production batch status text must exist.");
    RequireText(englishText, "[\"ui.repeat.state.available\"]", "English repeat-production available state badge text must exist.");
    RequireText(englishText, "[\"ui.repeat.state.active\"]", "English repeat-production active state badge text must exist.");
    RequireText(englishText, "[\"ui.repeat.state.blocked\"]", "English repeat-production blocked state badge text must exist.");
    RequireText(chineseText, "[\"production.batchQueued\"]", "Chinese Shift production batch status text must exist.");
    RequireText(chineseText, "[\"ui.repeat.state.available\"]", "Chinese repeat-production available state badge text must exist.");
    RequireText(chineseText, "[\"ui.repeat.state.active\"]", "Chinese repeat-production active state badge text must exist.");
    RequireText(chineseText, "[\"ui.repeat.state.blocked\"]", "Chinese repeat-production blocked state badge text must exist.");
    RequireText(hudLayer, "NextAllProductionProviderId(spec.Production.ProducerKind)", "All provider lane clicks must distribute repeated train commands across valid providers.");
    RequireText(hudSync, "_hud.SetProductionProviderLaneState(_unitBattlefield.ProductionProviderLaneStates(PlayerSlotId.One))", "BattleRoot must feed runtime production provider lane state into the HUD.");
    RequireText(hudSync, "_hud.SetConstructionProviderLaneState(_unitBattlefield.ConstructionProviderLaneStates(PlayerSlotId.One))", "BattleRoot must feed runtime construction provider lane state into the HUD.");
    RequireText(battleRoot, "TryCreateProductionDesignPayloadForProvider", "BattleRoot must route specific provider lane production through a scoped payload helper.");
    RequireText(englishText, "[\"production.repeatEnabled\"]", "English repeat-production status text must exist.");
    RequireText(chineseText, "[\"production.repeatEnabled\"]", "Chinese repeat-production status text must exist.");
    RequireText(hudLayer, "CompactMultiline(status, 34)", "Catalog inspector text must compact per line instead of single-line clipping.");
    RequireText(hudLayer, "BuildInspectorText(state, spec, disabledReason)", "Build cards must provide label/cost/time/disabled inspector text.");
    RequireText(hudLayer, "TrainInspectorText(state, ProducerLabel, disabledReason)", "Train cards must provide source/cost/time/queue/disabled inspector text.");
    RequireText(hudLayer, "AbilityInspectorText(state)", "Ability cards must provide target/cooldown/active inspector text.");
    RequireText(hudLayer, "AbilityRequested?.Invoke(kind);", "Ability cards must route clicks through the typed ability request path.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.build\")", "Right command panel Build mode label must be i18n-backed.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.buildSurface\")", "Right command panel Build surface label must be i18n-backed.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.train\")", "Right command panel Train mode label must be i18n-backed.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.trainSurface\")", "Right command panel train grid section label must be i18n-backed.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.abilities\")", "Right command panel Abilities mode label must be i18n-backed.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.abilitiesEmpty\")", "Abilities mode empty state must be i18n-backed.");
    RequireText(hudLayer, "96 + row * 58", "Right command panel production cells must keep fixed grid spacing below the catalog strip.");
    RequireText(hudLayer, "_visibleCommandCardStates.Count >= 12", "Right command panel must cap production cells to the 12-slot fixed grid.");
    RequireText(hudLayer, "Math.Min(_abilityCardStates.Count, 12)", "Abilities mode must use the same fixed 12-slot grid cap.");
    RequireText(hudLayer, "private int? SelectedConstructionProviderId()", "Build provider lane selection must expose a compact selected construction-provider helper.");
    RequireText(battleRoot, "_buildPlacement.SelectBuildKind(kind, constructionProviderId)", "BattleRoot must pass Build provider lane selection into build placement.");
    RequireText(selectionController, "public void ArmRepairCommand()", "SelectionController must expose one-shot repair command arming for the command ribbon.");
    RequireText(hudLayer, "Action? RallyRequested", "HUD must expose a rally request for the command-ribbon rally button.");
    RequireText(hudLayer, "Action? RepairRequested", "HUD must expose a repair request for the command-ribbon repair button.");
    RequireText(hudLayer, "Action? SellOrCancelRequested", "HUD must expose a sell-or-cancel request for the command-ribbon sell button.");
    RequireText(hudLayer, "Name = \"RibbonRepair\"", "Command ribbon repair action must expose a stable node.");
    RequireText(hudLayer, "ribbonRepair.Pressed += () => RepairRequested?.Invoke();", "Command ribbon repair action must route through the repair request path.");
    RequireText(battleRoot, "RepairRequested = OnRepairRequested", "BattleRoot must wire the command-ribbon repair request.");
    RequireText(battleRoot, "_selection.ArmRepairCommand();", "BattleRoot repair request must arm SelectionController repair targeting.");
    RequireText(selectionController, "FinishRuntimeRepairCommand(ScreenToWorld(screenPoint), acknowledgeInvalidAtTarget: true)", "Armed repair mode must finish through the runtime repair command path.");
    RequireText(hudLayer, "ribbonCancel.Pressed += () => SellOrCancelRequested?.Invoke();", "Command ribbon sell action must route through the sell-or-cancel request path.");
    RequireText(hudLayer, "_cancelProduction.Pressed += () => CancelProductionRequested?.Invoke();", "Right-side queue cancel must stay on the production-cancel request path.");
    RequireText(hudLayer, "Name = \"RibbonSetRally\"", "Command ribbon rally action must expose a stable node.");
    RequireText(hudLayer, "ribbonRally.Pressed += () => RallyRequested?.Invoke();", "Command ribbon rally action must route through the rally request path.");
    ForbidText(hudLayer, "Selected = category == BuildCategory.Command", "HUD production tab selected state must not be a fixed initialization-only value.");
    ForbidText(hudLayer, "BuildGlobalSkillPanel", "Normal HUD must not build placeholder global-skill controls.");
    ForbidText(hudLayer, "GlobalSkillPanel", "Normal HUD must not include an unwired global-skill panel.");
    ForbidText(hudLayer, "PlaceholderBuildSlot", "Normal HUD must not keep placeholder production slot controls.");
    ForbidText(hudLayer, "_selectionCluster", "Selection detail must be owned by the right detail drawer, not a permanently hidden duplicate panel.");

    RequireText(uiFactory, "ApplyHudLabelStyle", "UiFactory must own HUD label color, outline, and shadow styling.");
    RequireText(uiFactory, "ApplyHudMoveModeButtonTheme", "UiFactory must own HUD move-mode button styling.");
    RequireText(uiFactory, "ApplyHudStanceButtonTheme", "UiFactory must own HUD stance button styling.");
    RequireText(englishText, "[\"ui.catalog.build\"]", "English HUD catalog Build label must exist.");
    RequireText(englishText, "[\"ui.catalog.buildHelp\"]", "English HUD catalog Build help text must exist.");
    RequireText(englishText, "[\"ui.catalog.buildSurface\"]", "English HUD catalog Build surface label must exist.");
    RequireText(englishText, "[\"ui.catalog.train\"]", "English HUD catalog Train label must exist.");
    RequireText(englishText, "[\"ui.catalog.trainHelp\"]", "English HUD catalog Train help text must exist.");
    RequireText(englishText, "[\"ui.catalog.upgrades\"]", "English HUD catalog Upgrades label must exist.");
    RequireText(englishText, "[\"ui.catalog.upgradesHelp\"]", "English HUD catalog Upgrades help text must exist.");
    RequireText(englishText, "[\"ui.catalog.upgradesEmpty\"]", "English HUD catalog Upgrades empty text must exist.");
    RequireText(englishText, "[\"ui.catalog.abilities\"]", "English HUD catalog Abilities label must exist.");
    RequireText(englishText, "[\"ui.catalog.abilitiesHelp\"]", "English HUD catalog Abilities help text must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectBuild\"]", "English HUD catalog build inspector text must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectTrain\"]", "English HUD catalog train inspector text must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectAbility\"]", "English HUD catalog ability inspector text must exist.");
    RequireText(hotkeys, "GameText.T(\"hotkeys.catalog\")", "Hotkey legend must include a right-catalog control section.");
    RequireText(englishText, "[\"hotkeys.catalog\"] = \"CATALOG\"", "English hotkey legend must label the right catalog section.");
    RequireText(englishText, "[\"hotkeys.catalog.1\"] = \"Tab right catalog drawer\"", "English hotkey legend must expose the right catalog drawer toggle.");
    RequireText(englishText, "[\"hotkeys.catalog.2\"] = \"Build / Train / UPG / ABIL pages\"", "English hotkey legend must expose right catalog page switching.");
    RequireText(englishText, "[\"hotkeys.catalog.3\"] = \"Click cards / provider lanes\"", "English hotkey legend must expose right catalog card and provider interactions.");
    RequireText(englishText, "[\"ui.catalog.overview.build\"]", "English HUD catalog build overview text must exist.");
    RequireText(englishText, "[\"ui.catalog.overview.train\"]", "English HUD catalog train overview text must exist.");
    RequireText(englishText, "[\"ui.catalog.overview.abilities\"]", "English HUD catalog ability overview text must exist.");
    RequireText(englishText, "[\"ui.catalog.badge.ready\"]", "English HUD catalog ready badge text must exist.");
    RequireText(englishText, "[\"ui.catalog.badge.queued\"]", "English HUD catalog queued badge text must exist.");
    RequireText(englishText, "[\"ui.catalog.badge.active\"]", "English HUD catalog active badge text must exist.");
    RequireText(englishText, "[\"ui.catalog.badge.noCredits\"]", "English HUD catalog credit badge text must exist.");
    RequireText(englishText, "[\"ui.catalog.badge.noProvider\"]", "English HUD catalog provider badge text must exist.");
    RequireText(englishText, "[\"ui.catalog.badge.locked\"]", "English HUD catalog locked badge text must exist.");
    RequireText(englishText, "[\"ui.providerLane.auto\"]", "English HUD provider lane Auto label must exist.");
    RequireText(englishText, "[\"ui.providerLane.selected\"]", "English HUD provider lane selected text must exist.");
    RequireText(englishText, "[\"ui.providerLane.summary\"]", "English HUD provider lane summary text must exist.");
    RequireText(englishText, "[\"ui.providerLane.empty\"]", "English HUD provider lane empty text must exist.");
    RequireText(englishText, "[\"ui.providerLane.summaryOk\"] = \"OK\"", "English HUD provider summary must use a rail-safe OK code.");
    RequireText(englishText, "[\"ui.providerLane.summaryOffline\"] = \"OFF\"", "English HUD provider summary must use a rail-safe offline code.");
    ForbidText(englishText, "[\"ui.providerLane.available\"]", "Provider summary must not use long availability text in the narrow right rail.");
    RequireText(englishText, "\\n{2} cr  {3}s", "English catalog inspector strings must use a two-line metrics layout.");
    RequireText(englishText, "[\"ui.ability.shieldField\"]", "English ability-card ShieldField label must exist.");
    RequireText(englishText, "[\"ui.ability.armed\"]", "English ability armed status must exist.");
    RequireText(englishText, "[\"ui.constructionProviderLane.auto\"]", "English Build provider lane Auto label must exist.");
    RequireText(englishText, "[\"ui.constructionProviderLane.tooltip\"]", "English Build provider lane tooltip must use construction-specific copy.");
    RequireText(chineseText, "[\"ui.catalog.build\"]", "Chinese HUD catalog Build label must exist.");
    RequireText(chineseText, "[\"ui.catalog.buildHelp\"]", "Chinese HUD catalog Build help text must exist.");
    RequireText(chineseText, "[\"ui.catalog.buildSurface\"]", "Chinese HUD catalog Build surface label must exist.");
    RequireText(chineseText, "[\"ui.catalog.train\"]", "Chinese HUD catalog Train label must exist.");
    RequireText(chineseText, "[\"ui.catalog.trainHelp\"]", "Chinese HUD catalog Train help text must exist.");
    RequireText(chineseText, "[\"ui.catalog.upgrades\"]", "Chinese HUD catalog Upgrades label must exist.");
    RequireText(chineseText, "[\"ui.catalog.upgradesHelp\"]", "Chinese HUD catalog Upgrades help text must exist.");
    RequireText(chineseText, "[\"ui.catalog.upgradesEmpty\"]", "Chinese HUD catalog Upgrades empty text must exist.");
    RequireText(chineseText, "[\"ui.catalog.abilities\"]", "Chinese HUD catalog Abilities label must exist.");
    RequireText(chineseText, "[\"ui.catalog.abilitiesHelp\"]", "Chinese HUD catalog Abilities help text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectBuild\"]", "Chinese HUD catalog build inspector text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectTrain\"]", "Chinese HUD catalog train inspector text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectAbility\"]", "Chinese HUD catalog ability inspector text must exist.");
    RequireText(chineseText, "[\"hotkeys.catalog\"] = \"目录\"", "Chinese hotkey legend must label the right catalog section.");
    RequireText(chineseText, "[\"hotkeys.catalog.1\"] = \"Tab 右侧目录抽屉\"", "Chinese hotkey legend must expose the right catalog drawer toggle.");
    RequireText(chineseText, "[\"hotkeys.catalog.2\"] = \"建造/训练/升级/能力页\"", "Chinese hotkey legend must expose right catalog page switching.");
    RequireText(chineseText, "[\"hotkeys.catalog.3\"] = \"点击卡片/来源通道\"", "Chinese hotkey legend must expose right catalog card and provider interactions.");
    RequireText(chineseText, "[\"ui.catalog.overview.build\"]", "Chinese HUD catalog build overview text must exist.");
    RequireText(chineseText, "[\"ui.catalog.overview.train\"]", "Chinese HUD catalog train overview text must exist.");
    RequireText(chineseText, "[\"ui.catalog.overview.abilities\"]", "Chinese HUD catalog ability overview text must exist.");
    RequireText(chineseText, "[\"ui.catalog.badge.ready\"]", "Chinese HUD catalog ready badge text must exist.");
    RequireText(chineseText, "[\"ui.catalog.badge.queued\"]", "Chinese HUD catalog queued badge text must exist.");
    RequireText(chineseText, "[\"ui.catalog.badge.active\"]", "Chinese HUD catalog active badge text must exist.");
    RequireText(chineseText, "[\"ui.catalog.badge.noCredits\"]", "Chinese HUD catalog credit badge text must exist.");
    RequireText(chineseText, "[\"ui.catalog.badge.noProvider\"]", "Chinese HUD catalog provider badge text must exist.");
    RequireText(chineseText, "[\"ui.catalog.badge.locked\"]", "Chinese HUD catalog locked badge text must exist.");
    RequireText(chineseText, "[\"ui.providerLane.auto\"]", "Chinese HUD provider lane Auto label must exist.");
    RequireText(chineseText, "[\"ui.providerLane.selected\"]", "Chinese HUD provider lane selected text must exist.");
    RequireText(chineseText, "[\"ui.providerLane.summary\"]", "Chinese HUD provider lane summary text must exist.");
    RequireText(chineseText, "[\"ui.providerLane.empty\"]", "Chinese HUD provider lane empty text must exist.");
    RequireText(chineseText, "[\"ui.providerLane.summaryOk\"]", "Chinese HUD provider summary must use a rail-safe OK code.");
    RequireText(chineseText, "[\"ui.providerLane.summaryOffline\"]", "Chinese HUD provider summary must use a rail-safe offline code.");
    ForbidText(chineseText, "[\"ui.providerLane.available\"]", "Provider summary must not use long availability text in the narrow right rail.");
    RequireText(chineseText, "\\n{2} 资金", "Chinese catalog inspector strings must use a two-line metrics layout.");
    RequireText(chineseText, "[\"ui.ability.shieldField\"]", "Chinese ability-card ShieldField label must exist.");
    RequireText(chineseText, "[\"ui.ability.armed\"]", "Chinese ability armed status must exist.");
    RequireText(chineseText, "[\"ui.constructionProviderLane.auto\"]", "Chinese Build provider lane Auto label must exist.");
    RequireText(chineseText, "[\"ui.constructionProviderLane.tooltip\"]", "Chinese Build provider lane tooltip must use construction-specific copy.");
    RequireText(hudSync, "_hud.SetAbilityCardState(RuntimeSelectedAbilityCardStates())", "BattleRoot must feed selected-unit ability cards into the HUD.");
    RequireText(hudSync, "_unitBattlefield.UnitEntityByInstanceId(unit.Id)", "BattleRoot ability cards must read the selected unit runtime mirror for cooldown state.");
    RequireText(hudSync, "AbilityCooldownRemaining(entity, ability.Kind)", "BattleRoot ability cards must include runtime cooldown state.");
    RequireText(hudSync, "AddOrMergeSelectedAbilityCard(", "BattleRoot must aggregate HUD ability cards across multi-selected support units.");
    RequireText(hudSync, "MathF.Min(existing.CooldownRemaining, cooldownRemaining)", "Multi-selected ability cards must expose the shortest selected caster cooldown.");
    RequireText(hudSync, "MergedAbilityActiveState(ability.Kind, existing.IsActive, isActive)", "Multi-selected ability cards must merge active state through ability-specific semantics.");
    RequireText(hudSync, "kind == AbilityKind.Deploy", "Deploy ability cards must keep toggle-specific multi-selection active semantics.");
    RequireText(hudSync, "? existingActive && candidateActive", "Deploy ability cards must only show active when all selected deploy casters are active.");
    RequireText(hudSync, ": existingActive || candidateActive", "Non-toggle ability cards must stay active if any selected caster is active.");
    ForbidText(hudSync, "_selectedUnitInstanceBuffer.Count != 1", "Selected support ability cards must not disappear for multi-selection.");

    if (hudLayer.Contains("AddThemeStyleboxOverride(\"panel\"", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("HudLayer must not directly override panel styleboxes; use UiFactory panel helpers.");
    }
}

static void ForbidText(string source, string forbidden, string message)
{
    if (source.Contains(forbidden, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(message);
    }
}

static string ReadSourceWithPartials(string sourcePath)
{
    var parts = new List<string>();
    var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (File.Exists(sourcePath))
    {
        parts.Add(File.ReadAllText(sourcePath));
        addedPaths.Add(sourcePath);
    }

    var directory = Path.GetDirectoryName(sourcePath);
    var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
    if (directory is not null && Directory.Exists(directory))
    {
        foreach (var partialPath in Directory.EnumerateFiles(directory, $"{sourceName}.*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path))
        {
            if (addedPaths.Add(partialPath))
            {
                parts.Add(File.ReadAllText(partialPath));
            }
        }
    }

    return string.Join("\n\n", parts);
}

static void RequireText(string source, string required, string message)
{
    if (!source.Contains(required, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(message);
    }
}
