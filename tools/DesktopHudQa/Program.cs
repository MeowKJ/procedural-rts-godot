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
    var hudSync = File.ReadAllText(Path.Combine(root, "scripts", "BattleRoot.HudSync.cs"));
    var uiFactory = File.ReadAllText(Path.Combine(root, "scripts", "ui", "UiFactory.cs"));
    var cursorCatalog = File.ReadAllText(Path.Combine(root, "scripts", "core", "presentation", "ui", "BattleCursorCatalog.cs"));
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
    RequireText(hudLayer, "BuildKindRequested?.Invoke(button.BuildKind)", "Build option cards must request build placement explicitly.");
    RequireText(hudLayer, "state.Category != _selectedBuildCategory", "Build option cards must filter by the selected build category.");
    RequireText(hudLayer, "_visibleBuildCardStates.Count >= 12", "Build option cards must keep the 12-slot fixed grid cap.");
    RequireText(hudLayer, "button.SetBuildState(state, disabledReason)", "Build option cards must render build snapshot state and disabled reasons.");
    ForbidText(hudLayer, "BuildCategoryRequested", "Build category tabs must filter cards without arming placement.");
    RequireText(hudLayer, "BuildCategory.Naval, active: false", "HUD must keep the naval build tab visibly disabled until naval build specs exist.");
    RequireText(hudLayer, "\"CatalogModeBuild\"", "Right command panel must expose a stable Build catalog mode node.");
    RequireText(hudLayer, "\"CatalogModeTrain\"", "Right command panel must expose a stable Train catalog mode node.");
    RequireText(hudLayer, "\"CatalogModeAbilities\"", "Right command panel must expose a stable Abilities catalog mode node.");
    RequireText(hudLayer, "Name = \"CatalogInspector\"", "Right command panel must expose a stable catalog inspector node.");
    RequireText(hudLayer, "new Vector2(70, 72), new Vector2(214, 28), FontSmall", "Catalog inspector must use the two-line compact status slot between tabs and cards.");
    RequireText(hudLayer, "CatalogModeButton", "Right command panel mode controls must be clickable buttons, not decorative labels.");
    RequireText(hudLayer, "button.Pressed += () => SelectCatalogMode(mode);", "Build/Train mode buttons must switch catalog pages.");
    RequireText(hudLayer, "tab.Pressed += () => SelectProductionCategory(category);", "Train category tabs must update the production category page.");
    RequireText(hudLayer, "tab.Visible = mode == CatalogModeKind.Build;", "Build category tabs must only be visible on the Build catalog page.");
    RequireText(hudLayer, "tab.Visible = mode == CatalogModeKind.Train;", "Train category tabs must only be visible on the Train catalog page.");
    RequireText(hudLayer, "state.Category != _selectedProductionCategory", "Train command grid must filter by the selected production category.");
    RequireText(hudLayer, "CatalogModeKind.Abilities => GameText.T(\"ui.catalog.abilitiesSurface\")", "Abilities mode must own a distinct right-panel surface label.");
    RequireText(hudLayer, "public void SetAbilityCardState(IReadOnlyList<AbilityCardState> states)", "HUD must expose selected-unit ability card state separately from production cards.");
    RequireText(hudLayer, "Dictionary<AbilityKind, AbilityCard> _abilityCards", "Ability cards must not reuse production command buttons.");
    RequireText(hudLayer, "private partial class AbilityCard : Button", "Abilities mode must render dedicated ability cards.");
    RequireText(hudLayer, "Name = $\"AbilityCard{kind}\"", "Ability cards must expose stable node names for structure/screenshot QA.");
    RequireText(hudLayer, "Action<AbilityKind>? AbilityRequested", "Ability cards must emit a typed request instead of routing through production cards.");
    RequireText(hudLayer, "button.MouseEntered += () => SetCatalogStatusText(button.InspectorText);", "Build and train cards must update the catalog inspector on hover.");
    RequireText(hudLayer, "card.MouseEntered += () => SetCatalogStatusText(card.InspectorText);", "Ability cards must update the catalog inspector on hover.");
    RequireText(hudLayer, "private void RestoreCatalogStatusText()", "Card hover exit must restore the catalog page status text.");
    RequireText(hudLayer, "List<ProductionProviderLaneState> _productionProviderLaneStates", "Train catalog provider lanes must keep reusable lane state storage.");
    RequireText(hudLayer, "private partial class ProductionProviderLaneButton : Button", "Train catalog provider lanes must render through stable lane buttons.");
    RequireText(hudLayer, "Name = $\"ProductionProviderLane{index}\"", "Train provider lane buttons must expose stable node names for QA.");
    RequireText(hudLayer, "AddProductionProviderLaneButton(_rightRail, index)", "Train provider lanes must live in the right rail instead of compressing the 12-slot card grid.");
    RequireText(hudLayer, "Name = \"ProviderLaneSummary\"", "Train provider lane summary must expose a stable node name for QA.");
    RequireText(hudLayer, "RefreshProductionProviderLaneSummary()", "Train provider lane selection/state changes must refresh the provider detail summary.");
    RequireText(hudLayer, "ProviderLaneSummaryText(state)", "Train provider lane summary must render selected provider count, queue count, progress, and availability.");
    RequireText(hudLayer, "ProviderLaneSummaryDisabledReason(state.DisabledReasonKey)", "Train provider lane summary must use rail-safe disabled reason codes.");
    RequireText(hudLayer, "ProductionDesignRequested?.Invoke(button.UnitDesignId, SelectedProductionProviderId(button.UnitDesignId))", "Train cards must pass the selected provider lane into production requests.");
    RequireText(hudLayer, "NextAllProductionProviderId(spec.Production.ProducerKind)", "All provider lane clicks must distribute repeated train commands across valid providers.");
    RequireText(hudSync, "_hud.SetProductionProviderLaneState(_unitBattlefield.ProductionProviderLaneStates(PlayerSlotId.One))", "BattleRoot must feed runtime production provider lane state into the HUD.");
    RequireText(battleRoot, "TryCreateProductionDesignPayloadForProvider", "BattleRoot must route specific provider lane production through a scoped payload helper.");
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
    RequireText(hudLayer, "Action? RallyRequested", "HUD must expose a rally request for the command-ribbon rally button.");
    RequireText(hudLayer, "Action? SellOrCancelRequested", "HUD must expose a sell-or-cancel request for the command-ribbon sell button.");
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
    RequireText(englishText, "[\"ui.catalog.buildSurface\"]", "English HUD catalog Build surface label must exist.");
    RequireText(englishText, "[\"ui.catalog.train\"]", "English HUD catalog Train label must exist.");
    RequireText(englishText, "[\"ui.catalog.abilities\"]", "English HUD catalog Abilities label must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectBuild\"]", "English HUD catalog build inspector text must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectTrain\"]", "English HUD catalog train inspector text must exist.");
    RequireText(englishText, "[\"ui.catalog.inspectAbility\"]", "English HUD catalog ability inspector text must exist.");
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
    RequireText(chineseText, "[\"ui.catalog.build\"]", "Chinese HUD catalog Build label must exist.");
    RequireText(chineseText, "[\"ui.catalog.buildSurface\"]", "Chinese HUD catalog Build surface label must exist.");
    RequireText(chineseText, "[\"ui.catalog.train\"]", "Chinese HUD catalog Train label must exist.");
    RequireText(chineseText, "[\"ui.catalog.abilities\"]", "Chinese HUD catalog Abilities label must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectBuild\"]", "Chinese HUD catalog build inspector text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectTrain\"]", "Chinese HUD catalog train inspector text must exist.");
    RequireText(chineseText, "[\"ui.catalog.inspectAbility\"]", "Chinese HUD catalog ability inspector text must exist.");
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
    RequireText(hudSync, "_hud.SetAbilityCardState(RuntimeSelectedAbilityCardStates())", "BattleRoot must feed selected-unit ability cards into the HUD.");
    RequireText(hudSync, "_unitBattlefield.UnitEntityByInstanceId(unit.Id)", "BattleRoot ability cards must read the selected unit runtime mirror for cooldown state.");
    RequireText(hudSync, "AbilityCooldownRemaining(entity, ability.Kind)", "BattleRoot ability cards must include runtime cooldown state.");

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
