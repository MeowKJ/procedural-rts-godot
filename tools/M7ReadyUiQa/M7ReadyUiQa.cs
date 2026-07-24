static class M7ReadyUiQa
{
    public static void AssertSource(string root)
    {
        var hudRoot = Path.Combine(root, "scripts", "ui");
        var hud = string.Join("\n", Directory.EnumerateFiles(hudRoot, "HudLayer*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));
        var settings = Read(root, "scripts", "ui", "SettingsOverlayLayer.cs");
        var hudSync = Read(root, "scripts", "battle-root", "BattleRoot.HudSync.cs");
        var english = Read(root, "scripts", "core", "localization", "GameText.English.cs");
        var chinese = Read(root, "scripts", "core", "localization", "GameText.ChineseSimplified.cs");

        Require(hud, "tab.Visible = mode == CatalogModeKind.Upgrades;", "Upgrade category tabs must only be visible on Upgrades.");
        Require(hud, "List<UpgradeCategoryTab> _upgradeCategoryTabs", "Upgrades must retain category tabs.");
        Require(hud, "_selectedUpgradeCategory = UpgradeProjectAccentKind.Combat", "Upgrades must default to Combat.");
        Require(hud, "ui.upgrade.category.combat", "Upgrades must expose Combat category copy.");
        Require(hud, "ui.upgrade.category.vision", "Upgrades must expose Vision category copy.");
        Require(hud, "ui.upgrade.category.support", "Upgrades must expose Support category copy.");
        Require(hud, "Name = $\"UpgradeCategory{category}\"", "Upgrade tabs must expose stable node names.");
        Require(hud, "SelectUpgradeCategory(category)", "Upgrade tabs must update selected category.");
        Require(hud, "state.Accent != _selectedUpgradeCategory", "Upgrade cards must filter by category.");
        Require(hud, "VisibleUpgradeProjectShellCount()", "Upgrade status must count the selected category.");
        Require(hud, "BindFixedHoverText(tab, $\"upgrade-tab.{category}\"", "Upgrade tabs must use fixed hover text.");
        Require(english, "[\"ui.upgrade.category.combat\"]", "English upgrade category copy must exist.");
        Require(english, "[\"ui.upgrade.category.vision\"]", "English Vision category copy must exist.");
        Require(english, "[\"ui.upgrade.category.support\"]", "English Support category copy must exist.");
        Require(chinese, "[\"ui.upgrade.category.combat\"]", "Chinese upgrade category copy must exist.");
        Require(chinese, "[\"ui.upgrade.category.vision\"]", "Chinese Vision category copy must exist.");
        Require(chinese, "[\"ui.upgrade.category.support\"]", "Chinese Support category copy must exist.");

        Require(hud, "SetAbilityCardState(IReadOnlyList<AbilityCardState> states, int sourceUnitCount)", "Abilities must accept source counts.");
        Require(hud, "_abilitySourceUnitCount = Math.Max(0, sourceUnitCount)", "Ability source counts must be clamped.");
        Require(hud, "AbilityCatalogSourceContextText(visibleCount)", "Ability status must expose source context.");
        Require(hud, "AbilityRailSourceContextText()", "Ability rail must expose source context.");
        Require(hud, "GameText.T(\"ui.catalog.abilitiesSourceNone\")", "Ability empty-source copy must be localized.");
        Require(english, "[\"ui.providerLane.abilitiesSourceNone\"]", "English ability no-source rail copy must exist.");
        Require(english, "[\"ui.providerLane.abilitiesSourceSelected\"]", "English selected-source rail copy must exist.");
        Require(english, "[\"ui.providerLane.abilitiesSourceMixed\"]", "English mixed-source rail copy must exist.");
        Require(english, "[\"ui.catalog.abilitiesSourceSelected\"]", "English selected-source status must exist.");
        Require(english, "[\"ui.catalog.abilitiesSourceMixed\"]", "English mixed-source status must exist.");
        Require(chinese, "[\"ui.providerLane.abilitiesSourceNone\"]", "Chinese ability no-source rail copy must exist.");
        Require(chinese, "[\"ui.providerLane.abilitiesSourceSelected\"]", "Chinese selected-source rail copy must exist.");
        Require(chinese, "[\"ui.providerLane.abilitiesSourceMixed\"]", "Chinese mixed-source rail copy must exist.");
        Require(chinese, "[\"ui.catalog.abilitiesSourceSelected\"]", "Chinese selected-source status must exist.");
        Require(chinese, "[\"ui.catalog.abilitiesSourceMixed\"]", "Chinese mixed-source status must exist.");
        Require(hudSync, "RuntimeSelectedAbilityCardStates(out var abilitySourceUnitCount)", "BattleRoot must collect ability source counts.");
        Require(hudSync, "SetAbilityCardState(selectedAbilityCards, abilitySourceUnitCount)", "BattleRoot must feed ability source context.");
        Require(hudSync, "unitContributedAbility = true", "Ability source counts must share the card filter.");
        Require(hudSync, "abilitySourceUnitCount++", "BattleRoot must count contributing selected units.");

        Require(settings, "_status.Text = SettingsControlsSectionStatusText(_selectedControlsSectionIndex)", "Settings selection must preview remap status.");
        Require(settings, "SettingsControlsSectionStatusText(int sectionIndex)", "Settings remap status must use a helper.");
        Require(english, "[\"settings.controls.sectionStatus\"]", "English remap status copy must exist.");
        Require(chinese, "[\"settings.controls.sectionStatus\"]", "Chinese remap status copy must exist.");

        Require(english, "[\"preview.repair.needSupport\"]", "English repair support feedback must exist.");
        Require(english, "[\"repair.stalled.noCredits\"]", "English repair funding feedback must exist.");
        Require(chinese, "[\"preview.repair.needSupport\"]", "Chinese repair support feedback must exist.");
        Require(chinese, "[\"repair.stalled.noCredits\"]", "Chinese repair funding feedback must exist.");
    }

    private static string Read(string root, params string[] parts) => File.ReadAllText(Path.Combine([root, .. parts]));

    private static void Require(string source, string expected, string message)
    {
        if (!source.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }
}
