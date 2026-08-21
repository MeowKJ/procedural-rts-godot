static class M7ReadyUiReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var theme = Read(root, "scripts", "ui", "SoftOldCityTheme.cs");
        var abilityCards = Read(root, "scripts", "ui", "hud", "HudLayer.AbilityCards.cs");
        var providerSummary = Read(root, "scripts", "ui", "hud", "HudLayer.ProviderLaneSummary.cs");
        var hudSync = Read(root, "scripts", "battle-root", "BattleRoot.HudSync.cs");
        var settings = Read(root, "scripts", "ui", "SettingsOverlayLayer.cs");
        var preview = Read(root, "scripts", "controllers", "selection", "Preview.cs");
        var repairCommand = Read(root, "scripts", "controllers", "selection", "RepairCommand.cs");
        var harvestRepair = Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.HarvestRepair.cs");
        var repairProjection = Read(root, "scripts", "core", "sim", "RepairOrderProjection.cs");
        var english = Read(root, "scripts", "core", "localization", "GameText.English.cs");
        var chinese = Read(root, "scripts", "core", "localization", "GameText.ChineseSimplified.cs");

        RequireText(theme, "public static readonly SoftOldCityHudPalette NightRadar", "NightRadar must have a dedicated HUD palette.", result);
        RequireText(theme, "PanelBorderStrong: new Color(SoftOldCityPalette.NightRadar", "NightRadar must use its radar border accent.", result);
        RequireText(theme, "Text: new Color(SoftOldCityPalette.NightRadarSoft", "NightRadar must use its soft radar text.", result);
        RequireText(theme, "WorldVisualTheme.NightRadar => NightRadar", "NightRadar must map to its dedicated HUD palette.", result);
        ForbidText(theme, "WorldVisualTheme.DuskDefense or WorldVisualTheme.NightRadar => Dusk", "NightRadar must not reuse DuskDefense styling.", result);

        RequireText(abilityCards, "SetAbilityCardState(IReadOnlyList<AbilityCardState> states, int sourceUnitCount)", "Ability cards must accept selected source counts.", result);
        RequireText(abilityCards, "AbilityCatalogSourceContextText", "Ability catalog must expose selected source context.", result);
        RequireText(providerSummary, "CatalogModeKind.Abilities => AbilityRailSourceContextText()", "Ability rail must expose selected source context.", result);
        RequireText(hudSync, "RuntimeSelectedAbilityCardStates(out var abilitySourceUnitCount)", "BattleRoot must collect ability source counts.", result);
        RequireText(hudSync, "SetAbilityCardState(selectedAbilityCards, abilitySourceUnitCount)", "BattleRoot must feed ability source context.", result);
        RequireText(hudSync, "unitContributedAbility = true", "Ability source counts must use the same card filter.", result);
        RequireText(hudSync, "abilitySourceUnitCount++", "BattleRoot must count contributing units.", result);
        RequireText(english, "[\"ui.catalog.abilitiesSourceSelected\"]", "English ability source status must exist.", result);
        RequireText(english, "[\"ui.providerLane.abilitiesSourceMixed\"]", "English mixed ability rail status must exist.", result);
        RequireText(chinese, "[\"ui.catalog.abilitiesSourceSelected\"]", "Chinese ability source status must exist.", result);
        RequireText(chinese, "[\"ui.providerLane.abilitiesSourceMixed\"]", "Chinese mixed ability rail status must exist.", result);

        RequireText(settings, "_status.Text = SettingsControlsSectionStatusText(_selectedControlsSectionIndex)", "Settings selection must preview remap status.", result);
        RequireText(settings, "SettingsControlsSectionStatusText(int sectionIndex)", "Settings remap status must use localized helper text.", result);
        RequireText(english, "[\"settings.controls.sectionStatus\"]", "English remap status must exist.", result);
        RequireText(chinese, "[\"settings.controls.sectionStatus\"]", "Chinese remap status must exist.", result);

        RequireText(preview, "RepairNeedsSupportPreviewLabel()", "Repair hover must expose missing support.", result);
        RequireText(repairCommand, "RepairCommandStatusText(hoveredUnit.EntityId)", "Unit repair commands must expose scoped stall feedback.", result);
        RequireText(repairCommand, "RepairCommandStatusText(acceptedTarget)", "Structure repair commands must expose scoped stall feedback.", result);
        RequireText(repairCommand, "projection.Target == targetEntity", "Old repair stalls must not poison new commands.", result);
        RequireText(harvestRepair, "NeedsRepairSupport(PlayerSlotId playerSlotId, UnitInstance target)", "Unit repair support must be queryable.", result);
        RequireText(harvestRepair, "NeedsRepairSupportBuilding(PlayerSlotId playerSlotId, int buildingId)", "Building repair support must be queryable.", result);
        RequireText(repairProjection, "public bool IsStalled", "Repair projection must expose a stalled predicate.", result);
        RequireText(repairProjection, "InsufficientCredits", "Repair projection must expose credit stalls.", result);
        RequireText(harvestRepair, "RepairOrderProjections(PlayerSlotId playerSlotId)", "Repair projections must be presentation-readable.", result);
        RequireText(english, "[\"preview.repair.needSupport\"]", "English repair support copy must exist.", result);
        RequireText(english, "[\"repair.stalled.noCredits\"]", "English repair credit-stall copy must exist.", result);
        RequireText(chinese, "[\"preview.repair.needSupport\"]", "Chinese repair support copy must exist.", result);
        RequireText(chinese, "[\"repair.stalled.noCredits\"]", "Chinese repair credit-stall copy must exist.", result);
    }

    private static string Read(string root, params string[] parts) => ReviewGateSource.Read(root, parts);
}
