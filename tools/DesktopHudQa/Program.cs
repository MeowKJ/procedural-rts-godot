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
    var controlBindingCatalog = File.ReadAllText(Path.Combine(root, "scripts", "core", "presentation", "ui", "ControlBindingCatalog.cs"));
    var hotkeys = File.ReadAllText(Path.Combine(root, "scripts", "ui", "HotkeyLegendLayer.cs"));
    var settingsOverlay = File.ReadAllText(Path.Combine(root, "scripts", "ui", "SettingsOverlayLayer.cs"));
    var englishText = File.ReadAllText(Path.Combine(root, "scripts", "core", "localization", "GameText.English.cs"));
    var chineseText = File.ReadAllText(Path.Combine(root, "scripts", "core", "localization", "GameText.ChineseSimplified.cs"));

    RequireText(cursorCatalog, "public enum BattleCursorState", "Battle cursor states must live in a central catalog.");
    RequireText(cursorCatalog, "BuildValid and BuildInvalid must share a hotspot", "Cursor catalog validation must guard build valid/invalid hotspot parity.");
    RequireText(cursorCatalog, "Kenney Cursor Pack CC0", "Cursor catalog texture entries must keep Kenney CC0 provenance near the data.");
    RequireText(hudLayer, "Input.SetCustomMouseCursor", "HudLayer should use custom cursor textures when catalog texture paths are present.");
    RequireText(hudLayer, "Input.SetDefaultCursorShape(shape)", "HudLayer cursor textures must preserve built-in cursor shape fallback.");
    RequireText(hudLayer, "image.Load(absolutePath) == Error.Ok", "HudLayer cursor textures must load source PNGs before ResourceLoader to avoid CI import-loader noise.");
    RequireText(hudLayer, "else if (ResourceLoader.Exists(texturePath))", "HudLayer cursor ResourceLoader fallback must be guarded to avoid missing-loader log spam.");
    RequireText(hudLayer, "ApplyCommandCursor(preview);", "HudLayer command preview updates must route cursor state through the cursor catalog.");
    RequireText(hudLayer, "BattleCursorCatalog.StateForPreview(preview)", "HudLayer must derive in-game cursor state from command preview state.");
    RequireText(uiFactory, "BattleCursorCatalog.DefinitionFor(BattleCursorState.UiHover)", "HUD/UI buttons must use the shared cursor catalog for hover cursors.");
    RequireText(battleRoot, "IsInsufficientCreditsStatus(status)", "BattleRoot status alerts must detect insufficient-credit failures through a focused helper.");
    RequireText(battleRoot, "TryUseAlertCooldown(\"status:insufficient-credits\", InsufficientCreditsAlertCooldown)", "Insufficient-credit HUD alerts must use one shared cooldown key instead of per-status spam.");
    RequireText(battleRoot, "AddAlert(AlertKind.Economy, GameText.T(\"ui.alert.insufficientCredits\"))", "Insufficient-credit failures must surface a localized economy alert.");
    RequireText(englishText, "[\"ui.alert.insufficientCredits\"]", "English insufficient-credit alert text must exist.");
    RequireText(chineseText, "[\"ui.alert.insufficientCredits\"]", "Chinese insufficient-credit alert text must exist.");
    RequireText(englishText, "[\"ui.commandFailure.reason\"] = \"BLOCKED\\n{0}\"", "English blocked-command catalog reason text must exist.");
    RequireText(chineseText, "[\"ui.commandFailure.reason\"] = \"受阻\\n{0}\"", "Chinese blocked-command catalog reason text must exist.");
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
    RequireText(hudLayer, "BuildKindRequested?.Invoke(button.BuildKind, SelectedConstructionProviderId(button.BuildKind))", "Build option cards must request build placement with the selected construction provider lane.");
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
    RequireText(hudLayer, "_statusValue.Text = CompactText(CommandFailureInlineStatusText(status), 42);", "Top status must expose compact blocked-command feedback without changing command handling.");
    RequireText(hudLayer, "SetCatalogStatusText(CatalogCommandStatusText(status));", "Catalog inspector must format blocked command statuses with a compact reason.");
    RequireText(hudLayer, "SetCatalogStatusText(LastProductionCatalogStatusText());", "Catalog mode changes must restore blocked command status through the same compact formatter.");
    RequireText(hudLayer, "private static bool IsCatalogCommandFailureStatus(string status)", "HUD blocked-command feedback must use a focused status classifier.");
    RequireText(hudLayer, "MatchesLocalizedStatusPattern(status, \"production.needCredits\")", "HUD blocked-command feedback must recognize localized production credit failures.");
    RequireText(hudLayer, "private static bool IsLocalizedAbilityUnavailableStatus(string status)", "HUD blocked-command feedback must recognize only the known localized ability failures.");
    RequireText(hudLayer, "GameText.Format(\"ui.ability.unavailable\", GameText.T(\"ui.ability.deploy\"))", "HUD blocked-command feedback must include the localized deploy failure.");
    RequireText(hudLayer, "RefreshCatalogOverview()", "Right command panel must refresh the catalog overview when page state changes.");
    RequireText(hudLayer, "SetAbilityCardState(IReadOnlyList<AbilityCardState> states)", "Ability state changes must feed the catalog overview path.");
    RequireText(hudLayer, "SetProductionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)", "Train provider lane state changes must feed the catalog overview path.");
    RequireText(hudLayer, "SetConstructionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)", "Build provider lane state changes must feed the catalog overview path.");
    RequireText(hudLayer, "CatalogOverviewBuildStartableCount()", "Build catalog overview must summarize startable cards for the selected build category.");
    RequireText(hudLayer, "CatalogOverviewTrainQueueableCount()", "Train catalog overview must summarize queueable cards for the selected train category.");
    RequireText(hudLayer, "CatalogOverviewReadyAbilityCount(visibleCount)", "Abilities catalog overview must summarize ready or active selected-unit abilities.");
    RequireText(hudLayer, "CatalogOverviewProviderScopeText(_selectedProductionProviderLaneScope)", "Train catalog overview must expose the selected provider scope.");
    RequireText(hudLayer, "RefreshProductionProviderLaneButtons();\n        RefreshCatalogOverview();\n        RefreshRepeatProductionControl();", "Train provider lane clicks must immediately refresh provider-scope overview text.");
    RequireText(hudLayer, "RefreshProductionProviderLaneButtons();\n        RefreshCatalogOverview();\n    }\n\n    private void ValidateProductionProviderLaneSelection()", "Build provider lane clicks must immediately refresh provider-scope overview text.");
    RequireText(hudLayer, "CatalogOverviewProductionLaneCount()", "Train catalog overview must count provider lanes for the selected train category.");
    RequireText(hudLayer, "CatalogModeButton", "Right command panel mode controls must be clickable buttons, not decorative labels.");
    RequireText(hudLayer, "button.Pressed += () => SelectCatalogMode(mode);", "Build/Train mode buttons must switch catalog pages.");
    RequireText(hudLayer, "private void CycleCatalogMode(int direction)", "Right catalog pages must expose keyboard cycling through the same mode selection path.");
    RequireText(hudLayer, "key.Keycode == Key.Pageup", "Right catalog keyboard cycling must support PageUp.");
    RequireText(hudLayer, "key.Keycode == Key.Pagedown", "Right catalog keyboard cycling must support PageDown.");
    RequireText(hudLayer, "CycleCatalogMode(-1)", "PageUp must cycle the right catalog to the previous page.");
    RequireText(hudLayer, "CycleCatalogMode(1)", "PageDown must cycle the right catalog to the next page.");
    RequireText(hudLayer, "HelpText = helpText", "Catalog mode buttons must carry localized page help text.");
    RequireText(hudLayer, "button.MouseEntered += () => SetCatalogStatusText(button.HelpText);", "Catalog mode button hover must explain the page in the inspector.");
    RequireText(hudLayer, "button.FocusEntered += () => SetCatalogStatusText(CatalogModeFocusText(button));", "Catalog mode button focus must show keyboard-style focus feedback in the inspector.");
    RequireText(hudLayer, "button.Pressed += () => SetCatalogStatusText(CatalogModePageSelectedText(button));", "Catalog mode button press must confirm the selected page in the inspector.");
    RequireText(hudLayer, "var focused = HasFocus();", "Catalog mode buttons must draw an explicit keyboard/gamepad focus state.");
    RequireText(hudLayer, "DrawRect(rect.Grow(-4), new Color(Ink, 0.24f), false, 1, true);", "Catalog mode buttons must draw a visible inner focus ring.");
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
    RequireText(hudLayer, "CatalogOverviewUpgradeProjectCount()", "Upgrades mode must expose a shell overview with project count and selected-source requirement.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.overview.abilitiesEmpty\")", "Abilities mode overview must expose a no-source state before support units are selected.");
    RequireText(hudLayer, "UpgradeProjectCatalogStatusText()", "Catalog inspector restore must keep Upgrades on its project-card shell status.");
    RequireText(hudLayer, "GameText.T(\"ui.upgrade.source.researchBuilding\")", "Upgrades project-shell status must show the selected-source class without adding provider lanes.");
    RequireText(hudLayer, "UpgradeProjectCardMetricText(state)", "Upgrades project-shell cards must visibly expose compact cost/time metrics.");
    RequireText(hudLayer, "private partial class UpgradeProjectCard : Button", "Upgrades mode must render dedicated project-shell cards instead of production cards.");
    RequireText(hudLayer, "Name = $\"UpgradeProjectCard{id}\"", "Upgrades project-shell cards must expose stable node names for QA.");
    RequireText(hudLayer, "RefreshUpgradeProjectCards()", "Upgrades mode must refresh project-shell cards when the catalog page is selected.");
    RequireText(hudLayer, "card.Pressed += () => SetCatalogStatusText(card.InspectorText);", "Upgrades project-shell cards must update the inspector without emitting commands.");
    ForbidText(hudLayer, "ResearchRequested?.Invoke", "Upgrades project-shell cards must stay read-only and not emit research commands.");
    ForbidText(hudLayer, "UpgradeRequested?.Invoke", "Upgrades project-shell cards must stay read-only and not emit upgrade commands.");
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
    RequireText(hudLayer, "NonProviderLaneRailHintText()", "Non-provider catalog pages must render explicit rail hints instead of blank provider-lane state.");
    RequireText(hudLayer, "CatalogModeKind.Upgrades => GameText.T(\"ui.providerLane.upgradesNone\")", "Upgrades catalog mode must reject provider lanes in the right rail.");
    RequireText(hudLayer, "CatalogModeKind.Abilities => GameText.T(\"ui.providerLane.abilitiesNone\")", "Abilities catalog mode must explain selected-unit ability context in the right rail.");
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
    RequireText(hudLayer, "NextAllConstructionProviderId(requiredProducer)", "Build All provider lane clicks must rotate across valid construction providers.");
    RequireText(hudLayer, "BuildSpecCatalog.For(buildKind).RequiredProducer", "Build All provider routing must derive the required construction provider from the selected build kind.");
    RequireText(hudLayer, "_allConstructionProviderCursorByKind[providerKind]", "Build All provider routing must keep a cursor per construction provider kind.");
    RequireText(hudSync, "_hud.SetProductionProviderLaneState(_unitBattlefield.ProductionProviderLaneStates(PlayerSlotId.One))", "BattleRoot must feed runtime production provider lane state into the HUD.");
    RequireText(hudSync, "_hud.SetConstructionProviderLaneState(_unitBattlefield.ConstructionProviderLaneStates(PlayerSlotId.One))", "BattleRoot must feed runtime construction provider lane state into the HUD.");
    RequireText(battleRoot, "TryCreateProductionDesignPayloadForProvider", "BattleRoot must route specific provider lane production through a scoped payload helper.");
    RequireText(englishText, "[\"production.repeatEnabled\"]", "English repeat-production status text must exist.");
    RequireText(chineseText, "[\"production.repeatEnabled\"]", "Chinese repeat-production status text must exist.");
    RequireText(hudLayer, "CompactMultiline(status, 34)", "Catalog inspector text must compact per line instead of single-line clipping.");
    RequireText(hudLayer, "BuildInspectorText(state, spec, disabledReason)", "Build cards must provide label/cost/time/disabled inspector text.");
    RequireText(hudLayer, "TrainInspectorText(state, ProducerLabel, disabledReason)", "Train cards must provide source/cost/time/queue/disabled inspector text.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.inputHint.build\")", "Build cards must append compact input hints to inspector text.");
    RequireText(hudLayer, "GameText.T(\"ui.catalog.inputHint.train\")", "Train cards must append compact input hints to inspector text.");
    RequireText(hudLayer, "AbilityInspectorText(state)", "Ability cards must provide target/cooldown/active inspector text.");
    RequireText(hudLayer, "AbilityCommandGrammar(state.Ability)", "Ability cards must expose compact command grammar on the card and inspector.");
    RequireText(hudLayer, "AbilityTargetRuleFor(AbilitySpec ability)", "Ability cards must derive target grammar from ability target rules.");
    RequireText(hudLayer, "AbilityStateCode(state)", "Ability cards must expose compact ready/cooldown/active state on the card.");
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
    RequireText(hudLayer, "private int? SelectedConstructionProviderId(string? buildKind)", "Build provider lane selection must expose a compact selected construction-provider helper.");
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
    RequireText(selectionController, "RepairUnitPreviewLabel()", "Repair preview must distinguish damaged friendly units from generic repair.");
    RequireText(selectionController, "RepairStructurePreviewLabel()", "Repair preview must distinguish damaged friendly structures from generic repair.");
    RequireText(selectionController, "RepairInvalidPreviewLabel()", "Armed repair preview must label invalid repair targets distinctly.");
    RequireText(selectionController, "GameText.T(\"preview.rally.point\")", "Armed rally preview must distinguish point targets.");
    RequireText(selectionController, "GameText.T(\"preview.rally.resource\")", "Armed rally preview must distinguish resource targets.");
    RequireText(selectionController, "GameText.T(\"preview.rally.friendly\")", "Armed rally preview must distinguish friendly-unit targets.");
    RequireText(hudLayer, "ribbonCancel.Pressed += () => SellOrCancelRequested?.Invoke();", "Command ribbon sell action must route through the sell-or-cancel request path.");
    RequireText(hudLayer, "_sellOrCancelAction = ribbonCancel;", "Command ribbon sell action must keep a stable control reference for context affordance.");
    RequireText(hudLayer, "RefreshSellOrCancelAction()", "Command ribbon sell action must refresh context from selection and queue state.");
    RequireText(hudLayer, "GameText.T(\"ui.sellOrCancel.sellTooltip\")", "Command ribbon sell action must explain selected-building sell context.");
    RequireText(hudLayer, "GameText.T(\"ui.sellOrCancel.cancelTooltip\")", "Command ribbon sell action must explain production-cancel fallback context.");
    RequireText(hudLayer, "GameText.T(\"ui.sellOrCancel.noneTooltip\")", "Command ribbon sell action must explain empty sell/cancel context.");
    RequireText(hudLayer, "_cancelProduction.Pressed += () => CancelProductionRequested?.Invoke();", "Right-side queue cancel must stay on the production-cancel request path.");
    RequireText(battleRoot, "BuildingSellRefundPreview(spec)", "Selected building details must preview sell refund from the build spec.");
    RequireText(battleRoot, "Mathf.RoundToInt(spec.Cost * Math.Clamp(spec.RefundRatio, 0, 1))", "Selected building sell preview must use the same cost/refund-ratio formula as selling.");
    RequireText(battleRoot, "GameText.Format(\"ui.detail.building\", queue, rally, sellRefund)", "Selected building detail text must include sell refund before teardown.");
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
    RequireText(englishText, "[\"ui.catalog.upgradesHelp\"] = \"Research projects\\nselect research building\"", "English HUD catalog Upgrades help must frame research as selected-building contextual.");
    RequireText(englishText, "[\"ui.catalog.upgradesEmpty\"] = \"Select research building\\nprojects only, no lanes\"", "English HUD catalog Upgrades empty text must reject provider lanes.");
    RequireText(englishText, "[\"ui.catalog.overview.build\"] = \"{0}/{1} OK | {2} lanes | {3}\"", "English Build overview must summarize actionable cards, lanes, and provider scope.");
    RequireText(englishText, "[\"ui.catalog.overview.train\"] = \"{0}/{1} OK | {2} lanes | {3}\"", "English Train overview must summarize queueable cards, lanes, and provider scope.");
    RequireText(englishText, "[\"ui.catalog.overview.abilities\"] = \"{0}/{1} RDY\"", "English Abilities overview must summarize ready selected-unit ability cards.");
    RequireText(englishText, "[\"ui.catalog.overview.abilitiesEmpty\"] = \"no ability source\"", "English Abilities overview must expose a no-source state.");
    RequireText(englishText, "[\"ui.catalog.overview.upgrades\"] = \"{0} shells | no lanes\"", "English HUD catalog Upgrades overview must stay a non-provider project shell.");
    RequireText(englishText, "[\"ui.catalog.overview.scope.auto\"] = \"AUTO\"", "English catalog overview provider scope must expose Auto compactly.");
    RequireText(englishText, "[\"ui.catalog.upgradesCount\"] = \"{0} project shells\\nsource: {1}; read-only\"", "English HUD catalog Upgrades count must explain read-only selected-source project shells.");
    RequireText(englishText, "[\"ui.catalog.upgradeCardMetric\"] = \"{0}cr {1}s\"", "English upgrade shell cards must expose compact visible cost/time metrics.");
    RequireText(englishText, "[\"ui.catalog.inspectUpgrade\"] = \"{0} [{1}] via {2}", "English HUD catalog upgrade inspector text must include source context.");
    RequireText(englishText, "[\"ui.upgrade.badge.sourceNeeded\"] = \"SRC\"", "English upgrade shell card must use a compact source-required badge.");
    RequireText(englishText, "[\"ui.catalog.abilities\"]", "English HUD catalog Abilities label must exist.");
    RequireText(englishText, "[\"ui.catalog.abilitiesHelp\"]", "English HUD catalog Abilities help text must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectBuild\"]", "English HUD catalog build inspector text must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectTrain\"]", "English HUD catalog train inspector text must exist.");
    RequireText(englishText, "[\"ui.catalog.modeSelected\"] = \"PAGE: {0}\\n{1}\"", "English catalog mode selected feedback text must exist.");
    RequireText(englishText, "[\"ui.catalog.modeFocus\"] = \"FOCUS: {0}\\npress to switch page\"", "English catalog mode focus feedback text must exist.");
    RequireText(englishText, "[\"ui.catalog.inputHint.build\"] = \"Click: place | lane: auto/specific\"", "English Build card input hint must exist.");
    RequireText(englishText, "[\"ui.catalog.inputHint.train\"] = \"Click: queue | Shift x5 | lane\"", "English Train card input hint must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectAbility\"]", "English HUD catalog ability inspector text must exist.");
    RequireText(englishText, "[\"ui.catalog.abilitiesEmpty\"] = \"Select support unit\\nno ability source\"", "English Abilities empty state must explain the missing selected-unit ability source.");
    RequireText(englishText, "[\"ui.ability.grammar.self\"] = \"SELF\"", "English ability cards must expose self-target grammar.");
    RequireText(englishText, "[\"ui.ability.grammar.point\"] = \"POINT\"", "English ability cards must expose point-target grammar.");
    RequireText(englishText, "[\"ui.ability.grammar.target\"] = \"TARGET\"", "English ability cards must expose generic target grammar.");
    RequireText(englishText, "[\"ui.ability.grammar.friendly\"] = \"ALLY\"", "English ability cards must expose friendly-target grammar.");
    RequireText(englishText, "[\"ui.ability.grammar.hostile\"] = \"HOSTILE\"", "English ability cards must expose hostile-target grammar.");
    RequireText(englishText, "[\"ui.ability.state.ready\"] = \"RDY\"", "English ability cards must expose compact ready state.");
    RequireText(englishText, "[\"ui.ability.state.cooldown\"] = \"CD\"", "English ability cards must expose compact cooldown state.");
    RequireText(englishText, "[\"ui.ability.state.active\"] = \"ON\"", "English ability cards must expose compact active state.");
    RequireText(controlBindingCatalog, "public static IReadOnlyList<ControlBindingSection> Sections", "Control binding sections must live in a shared catalog.");
    RequireText(controlBindingCatalog, "public static IReadOnlyList<string> SettingsOverviewRowKeys", "Settings controls overview must draw from the shared binding catalog.");
    RequireText(controlBindingCatalog, "\"hotkeys.build.4\"", "Shared binding catalog must include batch production controls.");
    RequireText(hotkeys, "ControlBindingCatalog.Sections", "Hotkey legend must draw rows from the shared binding catalog.");
    RequireText(settingsOverlay, "Name = \"ControlsBindingOverview\"", "Settings overlay must expose a stable controls binding overview node.");
    RequireText(settingsOverlay, "Name = \"ControlsBindingSectionSelect\"", "Settings overlay must expose a stable controls section selector node.");
    RequireText(settingsOverlay, "Name = \"ControlsBindingSectionRows\"", "Settings overlay must expose stable controls section rows.");
    RequireText(settingsOverlay, "SettingsControlsOverviewText()", "Settings overlay controls overview must use shared binding catalog rows.");
    RequireText(settingsOverlay, "SettingsControlsSectionText(_selectedControlsSectionIndex)", "Settings overlay controls section rows must refresh from the selected shared binding section.");
    RequireText(settingsOverlay, "ControlBindingCatalog.Sections[index].TitleKey", "Settings overlay controls section selector must read titles from the shared binding catalog.");
    RequireText(settingsOverlay, "GameText.T(\"settings.controls.tooltip\")", "Settings overlay controls overview must explain its read-only remap staging state.");
    RequireText(settingsOverlay, "_controlsOverview.Text = SettingsControlsOverviewText()", "Settings overlay language refresh must update shared binding catalog rows.");
    ForbidText(settingsOverlay, "\"hotkeys.camera.1\"", "Settings overlay must not duplicate binding row keys outside ControlBindingCatalog.");
    RequireText(englishText, "[\"hotkeys.catalog\"] = \"CATALOG\"", "English hotkey legend must label the right catalog section.");
    RequireText(englishText, "[\"hotkeys.catalog.1\"] = \"Tab right catalog drawer\"", "English hotkey legend must expose the right catalog drawer toggle.");
    RequireText(englishText, "[\"hotkeys.catalog.2\"] = \"PageUp/PageDown cycle pages\"", "English hotkey legend must expose right catalog page cycling.");
    RequireText(englishText, "[\"hotkeys.catalog.3\"] = \"Click cards / provider lanes\"", "English hotkey legend must expose right catalog card and provider interactions.");
    RequireText(englishText, "[\"hotkeys.build.4\"] = \"Shift-click trains x5\"", "English hotkey legend must expose batch production controls.");
    RequireText(englishText, "[\"settings.controls\"] = \"CONTROLS\"", "English settings controls label must exist.");
    RequireText(englishText, "[\"settings.controls.tooltip\"]", "English settings controls tooltip must exist.");
    ForbidText(englishText, "[\"settings.controlsOverview\"]", "Settings controls overview must not drift from the shared binding catalog.");
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
    RequireText(englishText, "[\"ui.providerLane.upgradesNone\"] = \"NO\\nTECH\\nLANE\"", "English Upgrades rail hint must make the no-provider-lane state explicit.");
    RequireText(englishText, "[\"ui.providerLane.abilitiesNone\"] = \"UNIT\\nABIL\"", "English Abilities rail hint must make selected-unit ability context explicit.");
    ForbidText(englishText, "[\"ui.providerLane.available\"]", "Provider summary must not use long availability text in the narrow right rail.");
    RequireText(englishText, "\\n{2} cr  {3}s", "English catalog inspector strings must use a two-line metrics layout.");
    RequireText(englishText, "[\"ui.ability.shieldField\"]", "English ability-card ShieldField label must exist.");
    RequireText(englishText, "[\"ui.ability.armed\"]", "English ability armed status must exist.");
    RequireText(englishText, "[\"ui.constructionProviderLane.auto\"]", "English Build provider lane Auto label must exist.");
    RequireText(englishText, "[\"ui.constructionProviderLane.tooltip\"]", "English Build provider lane tooltip must use construction-specific copy.");
    RequireText(englishText, "[\"preview.repair.unit\"] = \"REPAIR UNIT\"", "English repair preview must distinguish unit repair targets.");
    RequireText(englishText, "[\"preview.repair.structure\"] = \"REPAIR STRUCTURE\"", "English repair preview must distinguish structure repair targets.");
    RequireText(englishText, "[\"preview.repair.invalid\"] = \"NO REPAIR TARGET\"", "English repair preview must label invalid repair targets.");
    RequireText(englishText, "[\"preview.rally.point\"] = \"RALLY POINT\"", "English rally point preview text must exist.");
    RequireText(englishText, "[\"preview.rally.resource\"] = \"RALLY RESOURCE\"", "English rally resource preview text must exist.");
    RequireText(englishText, "[\"preview.rally.friendly\"] = \"RALLY FOLLOW\"", "English rally friendly-unit preview text must exist.");
    RequireText(englishText, "[\"ui.sellOrCancel.sellTooltip\"]", "English sell/cancel ribbon context tooltip must exist.");
    RequireText(englishText, "[\"ui.detail.building\"] = \"{0}   {1}\\n{2}\"", "English selected-building detail must reserve a sell-refund row.");
    RequireText(englishText, "[\"ui.detail.sellRefund\"] = \"SELL refund {0} credits\"", "English selected-building detail must preview sell refund.");
    RequireText(chineseText, "[\"ui.sellOrCancel.sellTooltip\"]", "Chinese sell/cancel ribbon context tooltip must exist.");
    RequireText(chineseText, "[\"ui.detail.building\"] = \"{0}   {1}\\n{2}\"", "Chinese selected-building detail must reserve a sell-refund row.");
    RequireText(chineseText, "[\"ui.detail.sellRefund\"] = \"出售返还 {0} 资金\"", "Chinese selected-building detail must preview sell refund.");
    RequireText(chineseText, "[\"ui.catalog.build\"]", "Chinese HUD catalog Build label must exist.");
    RequireText(chineseText, "[\"ui.catalog.buildHelp\"]", "Chinese HUD catalog Build help text must exist.");
    RequireText(chineseText, "[\"ui.catalog.buildSurface\"]", "Chinese HUD catalog Build surface label must exist.");
    RequireText(chineseText, "[\"ui.catalog.train\"]", "Chinese HUD catalog Train label must exist.");
    RequireText(chineseText, "[\"ui.catalog.trainHelp\"]", "Chinese HUD catalog Train help text must exist.");
    RequireText(chineseText, "[\"ui.catalog.upgrades\"]", "Chinese HUD catalog Upgrades label must exist.");
    RequireText(chineseText, "[\"ui.catalog.upgradesHelp\"] = \"研究项目\\n选择科研建筑\"", "Chinese HUD catalog Upgrades help must frame research as selected-building contextual.");
    RequireText(chineseText, "[\"ui.catalog.upgradesCount\"] = \"{0} 张项目壳\\n来源: {1}; 只读\"", "Chinese HUD catalog Upgrades count must explain read-only selected-source project shells.");
    RequireText(chineseText, "[\"ui.catalog.upgradesEmpty\"] = \"选择科研建筑\\n项目无生产通道\"", "Chinese HUD catalog Upgrades empty text must reject provider lanes.");
    RequireText(chineseText, "[\"ui.catalog.overview.build\"] = \"{0}/{1}可 | {2}线 | {3}\"", "Chinese Build overview must summarize actionable cards, lanes, and provider scope.");
    RequireText(chineseText, "[\"ui.catalog.overview.train\"] = \"{0}/{1}可 | {2}线 | {3}\"", "Chinese Train overview must summarize queueable cards, lanes, and provider scope.");
    RequireText(chineseText, "[\"ui.catalog.overview.abilities\"] = \"{0}/{1}就绪\"", "Chinese Abilities overview must summarize ready selected-unit ability cards.");
    RequireText(chineseText, "[\"ui.catalog.overview.abilitiesEmpty\"] = \"无能力来源\"", "Chinese Abilities overview must expose a no-source state.");
    RequireText(chineseText, "[\"ui.catalog.overview.upgrades\"] = \"{0}壳 | 无通道\"", "Chinese HUD catalog Upgrades overview must stay a non-provider project shell.");
    RequireText(chineseText, "[\"ui.catalog.overview.scope.auto\"] = \"自动\"", "Chinese catalog overview provider scope must expose Auto compactly.");
    RequireText(chineseText, "[\"ui.catalog.abilities\"]", "Chinese HUD catalog Abilities label must exist.");
    RequireText(chineseText, "[\"ui.catalog.abilitiesHelp\"]", "Chinese HUD catalog Abilities help text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectBuild\"]", "Chinese HUD catalog build inspector text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectTrain\"]", "Chinese HUD catalog train inspector text must exist.");
    RequireText(chineseText, "[\"ui.catalog.modeSelected\"] = \"页面: {0}\\n{1}\"", "Chinese catalog mode selected feedback text must exist.");
    RequireText(chineseText, "[\"ui.catalog.modeFocus\"] = \"焦点: {0}\\n确认切换页面\"", "Chinese catalog mode focus feedback text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inputHint.build\"] = \"点击放置 | 通道: 自动/指定\"", "Chinese Build card input hint must exist.");
    RequireText(chineseText, "[\"ui.catalog.inputHint.train\"] = \"点击排队 | Shift x5 | 通道\"", "Chinese Train card input hint must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectAbility\"]", "Chinese HUD catalog ability inspector text must exist.");
    RequireText(chineseText, "[\"ui.catalog.upgradeCardMetric\"] = \"{0}资 {1}s\"", "Chinese upgrade shell cards must expose compact visible cost/time metrics.");
    RequireText(chineseText, "[\"ui.catalog.inspectUpgrade\"] = \"{0} [{1}] 来源 {2}", "Chinese HUD catalog upgrade inspector text must include source context.");
    RequireText(chineseText, "[\"ui.catalog.abilitiesEmpty\"] = \"选择支援单位\\n无能力来源\"", "Chinese Abilities empty state must explain the missing selected-unit ability source.");
    RequireText(chineseText, "[\"ui.ability.grammar.self\"] = \"自身\"", "Chinese ability cards must expose self-target grammar.");
    RequireText(chineseText, "[\"ui.ability.grammar.point\"] = \"地点\"", "Chinese ability cards must expose point-target grammar.");
    RequireText(chineseText, "[\"ui.ability.grammar.target\"] = \"目标\"", "Chinese ability cards must expose generic target grammar.");
    RequireText(chineseText, "[\"ui.ability.grammar.friendly\"] = \"友方\"", "Chinese ability cards must expose friendly-target grammar.");
    RequireText(chineseText, "[\"ui.ability.grammar.hostile\"] = \"敌方\"", "Chinese ability cards must expose hostile-target grammar.");
    RequireText(chineseText, "[\"ui.ability.state.ready\"] = \"就绪\"", "Chinese ability cards must expose compact ready state.");
    RequireText(chineseText, "[\"ui.ability.state.cooldown\"] = \"冷却\"", "Chinese ability cards must expose compact cooldown state.");
    RequireText(chineseText, "[\"ui.ability.state.active\"] = \"开启\"", "Chinese ability cards must expose compact active state.");
    RequireText(chineseText, "[\"hotkeys.catalog\"] = \"目录\"", "Chinese hotkey legend must label the right catalog section.");
    RequireText(chineseText, "[\"hotkeys.catalog.1\"] = \"Tab 右侧目录抽屉\"", "Chinese hotkey legend must expose the right catalog drawer toggle.");
    RequireText(chineseText, "[\"hotkeys.catalog.2\"] = \"PageUp/PageDown 切换页面\"", "Chinese hotkey legend must expose right catalog page cycling.");
    RequireText(chineseText, "[\"hotkeys.catalog.3\"] = \"点击卡片/来源通道\"", "Chinese hotkey legend must expose right catalog card and provider interactions.");
    RequireText(chineseText, "[\"hotkeys.build.4\"] = \"Shift 点击训练 x5\"", "Chinese hotkey legend must expose batch production controls.");
    RequireText(chineseText, "[\"settings.controls\"] = \"控制\"", "Chinese settings controls label must exist.");
    RequireText(chineseText, "[\"settings.controls.tooltip\"]", "Chinese settings controls tooltip must exist.");
    ForbidText(chineseText, "[\"settings.controlsOverview\"]", "Settings controls overview must not drift from the shared binding catalog.");
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
    RequireText(chineseText, "[\"ui.providerLane.upgradesNone\"] = \"无\\n科技\\n通道\"", "Chinese Upgrades rail hint must make the no-provider-lane state explicit.");
    RequireText(chineseText, "[\"ui.providerLane.abilitiesNone\"] = \"单位\\n能力\"", "Chinese Abilities rail hint must make selected-unit ability context explicit.");
    ForbidText(chineseText, "[\"ui.providerLane.available\"]", "Provider summary must not use long availability text in the narrow right rail.");
    RequireText(chineseText, "\\n{2} 资金", "Chinese catalog inspector strings must use a two-line metrics layout.");
    RequireText(chineseText, "[\"ui.ability.shieldField\"]", "Chinese ability-card ShieldField label must exist.");
    RequireText(chineseText, "[\"ui.ability.armed\"]", "Chinese ability armed status must exist.");
    RequireText(chineseText, "[\"ui.constructionProviderLane.auto\"]", "Chinese Build provider lane Auto label must exist.");
    RequireText(chineseText, "[\"ui.constructionProviderLane.tooltip\"]", "Chinese Build provider lane tooltip must use construction-specific copy.");
    RequireText(chineseText, "[\"preview.repair.unit\"] = \"修理单位\"", "Chinese repair preview must distinguish unit repair targets.");
    RequireText(chineseText, "[\"preview.repair.structure\"] = \"修理建筑\"", "Chinese repair preview must distinguish structure repair targets.");
    RequireText(chineseText, "[\"preview.repair.invalid\"] = \"无法修理\"", "Chinese repair preview must label invalid repair targets.");
    RequireText(chineseText, "[\"preview.rally.point\"] = \"集结到地点\"", "Chinese rally point preview text must exist.");
    RequireText(chineseText, "[\"preview.rally.resource\"] = \"集结到资源\"", "Chinese rally resource preview text must exist.");
    RequireText(chineseText, "[\"preview.rally.friendly\"] = \"跟随友军集结\"", "Chinese rally friendly-unit preview text must exist.");
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
