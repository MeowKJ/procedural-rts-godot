using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private readonly List<CatalogModeButton> _catalogModeButtons = [];
    private readonly List<ProductionTab> _productionTabs = [];
    private readonly List<ProductionCategoryTab> _trainCategoryTabs = [];
    private CatalogModeKind _selectedCatalogMode = CatalogModeKind.Train;
    private BuildCategory _selectedBuildCategory = BuildCategory.Command;
    private ProductionCategory _selectedProductionCategory = ProductionCategory.Infantry;

    private void RegisterCatalogModeButton(CatalogModeButton button)
    {
        button.SetSelected(button.Mode == _selectedCatalogMode);
        _catalogModeButtons.Add(button);
    }

    private void RegisterProductionTab(ProductionTab tab)
    {
        tab.SetSelected(tab.Category == _selectedBuildCategory);
        tab.Visible = _selectedCatalogMode == CatalogModeKind.Build;
        _productionTabs.Add(tab);
    }

    private void RegisterTrainCategoryTab(ProductionCategoryTab tab)
    {
        tab.SetSelected(tab.Category == _selectedProductionCategory);
        tab.Visible = _selectedCatalogMode == CatalogModeKind.Train;
        _trainCategoryTabs.Add(tab);
    }

    private void SelectCatalogMode(CatalogModeKind mode)
    {
        _selectedCatalogMode = mode;
        for (var index = 0; index < _catalogModeButtons.Count; index++)
        {
            var button = _catalogModeButtons[index];
            button.SetSelected(button.Mode == mode);
        }

        for (var index = 0; index < _productionTabs.Count; index++)
        {
            var tab = _productionTabs[index];
            tab.Visible = mode == CatalogModeKind.Build;
        }

        for (var index = 0; index < _trainCategoryTabs.Count; index++)
        {
            var tab = _trainCategoryTabs[index];
            tab.Visible = mode == CatalogModeKind.Train;
        }

        if (_catalogSurfaceLabel is not null)
        {
            _catalogSurfaceLabel.Text = CatalogModeSurfaceText(mode);
        }

        if (mode != CatalogModeKind.Abilities && _productionValue is not null)
        {
            SetCatalogStatusText(string.IsNullOrWhiteSpace(_lastProductionStatus)
                ? GameText.T("ui.status.ready")
                : _lastProductionStatus);
        }

        RefreshCommandCards();
        RefreshProductionProviderLaneButtons();
        RefreshCatalogOverview();
    }

    private void SelectProductionTab(BuildCategory category)
    {
        _selectedBuildCategory = category;
        for (var index = 0; index < _productionTabs.Count; index++)
        {
            var tab = _productionTabs[index];
            tab.SetSelected(tab.Category == category);
        }

        RefreshCommandCards();
        RefreshCatalogOverview();
        SetCatalogContextResetText();
    }

    private static string CatalogModeSurfaceText(CatalogModeKind mode)
    {
        return mode switch
        {
            CatalogModeKind.Build => GameText.T("ui.catalog.buildSurface"),
            CatalogModeKind.Train => GameText.T("ui.catalog.trainSurface"),
            CatalogModeKind.Upgrades => GameText.T("ui.catalog.upgradesSurface"),
            CatalogModeKind.Abilities => GameText.T("ui.catalog.abilitiesSurface"),
            _ => "",
        };
    }

    private string CatalogModePageSelectedText(CatalogModeButton button)
    {
        return GameText.Format("ui.catalog.modeSelected", button.Label, CatalogContextResetText());
    }

    private static string CatalogModeFocusText(CatalogModeButton button)
    {
        return GameText.Format("ui.catalog.modeFocus", button.Label);
    }

    private void SetCatalogContextResetText()
    {
        SetCatalogStatusText(CatalogContextResetText());
    }

    private string CatalogContextResetText()
    {
        return _selectedCatalogMode switch
        {
            CatalogModeKind.Build => GameText.Format(
                "ui.catalog.reset.build",
                CatalogResetBuildCategoryText(_selectedBuildCategory),
                CatalogContextProviderText(CurrentConstructionProviderLaneState(), _selectedConstructionProviderLaneScope)),
            CatalogModeKind.Train => GameText.Format(
                "ui.catalog.reset.train",
                CatalogResetProductionCategoryText(_selectedProductionCategory),
                CatalogContextProviderText(CurrentProductionProviderLaneState(), _selectedProductionProviderLaneScope)),
            CatalogModeKind.Upgrades => GameText.T("ui.catalog.reset.upgrades"),
            CatalogModeKind.Abilities => GameText.T("ui.catalog.reset.abilities"),
            _ => "",
        };
    }

    private static string CatalogContextProviderText(ProductionProviderLaneState? state, ProductionProviderLaneScope scope)
    {
        if (scope == ProductionProviderLaneScope.Specific && state is not null && !string.IsNullOrWhiteSpace(state.ShortLabel))
        {
            return state.ShortLabel;
        }

        return CatalogOverviewProviderScopeText(scope);
    }

    private static string CatalogResetBuildCategoryText(BuildCategory category)
    {
        return category switch
        {
            BuildCategory.Command => GameText.T("ui.tabs.command"),
            BuildCategory.Power => GameText.T("ui.tabs.power"),
            BuildCategory.Economy => GameText.T("ui.tabs.economy"),
            BuildCategory.Infantry => GameText.T("ui.tabs.infantry"),
            BuildCategory.Vehicle => GameText.T("ui.tabs.vehicle"),
            BuildCategory.Defense => GameText.T("ui.tabs.defense"),
            BuildCategory.Air => GameText.T("ui.tabs.air"),
            BuildCategory.Naval => GameText.T("ui.tabs.naval"),
            _ => "",
        };
    }

    private static string CatalogResetProductionCategoryText(ProductionCategory category)
    {
        return category switch
        {
            ProductionCategory.Infantry => GameText.T("ui.tabs.infantry"),
            ProductionCategory.Vehicle => GameText.T("ui.tabs.vehicle"),
            ProductionCategory.Economy => GameText.T("ui.tabs.economy"),
            ProductionCategory.Defense => GameText.T("ui.tabs.defense"),
            ProductionCategory.Air => GameText.T("ui.tabs.air"),
            ProductionCategory.Naval => GameText.T("ui.tabs.naval"),
            _ => "",
        };
    }

    private void CycleCatalogMode(int direction)
    {
        var next = _selectedCatalogMode switch
        {
            CatalogModeKind.Build => direction >= 0 ? CatalogModeKind.Train : CatalogModeKind.Abilities,
            CatalogModeKind.Train => direction >= 0 ? CatalogModeKind.Upgrades : CatalogModeKind.Build,
            CatalogModeKind.Upgrades => direction >= 0 ? CatalogModeKind.Abilities : CatalogModeKind.Train,
            CatalogModeKind.Abilities => direction >= 0 ? CatalogModeKind.Build : CatalogModeKind.Upgrades,
            _ => CatalogModeKind.Train,
        };
        SelectCatalogMode(next);
        _manualDrawerOpen = true;
        _drawerInactivity = 0;
        SetCatalogStatusText(GameText.Format("ui.catalog.modeSelected", CatalogModeLabelText(next), CatalogContextResetText()));
    }

    private static string CatalogModeLabelText(CatalogModeKind mode)
    {
        return mode switch
        {
            CatalogModeKind.Build => GameText.T("ui.catalog.build"),
            CatalogModeKind.Train => GameText.T("ui.catalog.train"),
            CatalogModeKind.Upgrades => GameText.T("ui.catalog.upgrades"),
            CatalogModeKind.Abilities => GameText.T("ui.catalog.abilities"),
            _ => "",
        };
    }

    private void SelectProductionCategory(ProductionCategory category)
    {
        _selectedProductionCategory = category;
        for (var index = 0; index < _trainCategoryTabs.Count; index++)
        {
            var tab = _trainCategoryTabs[index];
            tab.SetSelected(tab.Category == category);
        }

        ValidateProductionProviderLaneSelection();
        RefreshCommandCards();
        RefreshProductionProviderLaneButtons();
        RefreshCatalogOverview();
        SetCatalogContextResetText();
    }

    private void RefreshCatalogOverview()
    {
        if (_catalogOverviewValue is null)
        {
            return;
        }

        _catalogOverviewValue.Text = _selectedCatalogMode switch
        {
            CatalogModeKind.Build => GameText.Format(
                "ui.catalog.overview.build",
                CatalogOverviewBuildStartableCount(),
                _visibleBuildCardStates.Count,
                CatalogOverviewConstructionLaneCount(),
                CatalogOverviewProviderScopeText(_selectedConstructionProviderLaneScope)),
            CatalogModeKind.Train => GameText.Format(
                "ui.catalog.overview.train",
                CatalogOverviewTrainQueueableCount(),
                _visibleCommandCardStates.Count,
                CatalogOverviewProductionLaneCount(),
                CatalogOverviewProviderScopeText(_selectedProductionProviderLaneScope)),
            CatalogModeKind.Abilities => CatalogOverviewAbilitiesText(),
            CatalogModeKind.Upgrades => GameText.Format(
                "ui.catalog.overview.upgrades",
                CatalogOverviewUpgradeProjectCount()),
            _ => "",
        };
    }

    private int CatalogOverviewBuildStartableCount()
    {
        var count = 0;
        for (var index = 0; index < _visibleBuildCardStates.Count; index++)
        {
            if (_visibleBuildCardStates[index].CanStart)
            {
                count++;
            }
        }

        return count;
    }

    private int CatalogOverviewTrainQueueableCount()
    {
        var count = 0;
        for (var index = 0; index < _visibleCommandCardStates.Count; index++)
        {
            if (_visibleCommandCardStates[index].CanQueue)
            {
                count++;
            }
        }

        return count;
    }

    private string CatalogOverviewAbilitiesText()
    {
        var visibleCount = Math.Min(_abilityCardStates.Count, 12);
        if (visibleCount == 0)
        {
            return GameText.T("ui.catalog.overview.abilitiesEmpty");
        }

        return GameText.Format(
            "ui.catalog.overview.abilities",
            CatalogOverviewReadyAbilityCount(visibleCount),
            visibleCount);
    }

    private int CatalogOverviewReadyAbilityCount(int visibleCount)
    {
        var count = 0;
        for (var index = 0; index < visibleCount; index++)
        {
            var state = _abilityCardStates[index];
            if (state.CooldownRemaining <= 0.01f || state.IsActive)
            {
                count++;
            }
        }

        return count;
    }

    private static int CatalogOverviewUpgradeProjectCount()
    {
        return Math.Min(DefaultUpgradeProjectShellStates.Length, 12);
    }

    private static string CatalogOverviewProviderScopeText(ProductionProviderLaneScope scope)
    {
        return scope switch
        {
            ProductionProviderLaneScope.All => GameText.T("ui.catalog.overview.scope.all"),
            ProductionProviderLaneScope.Specific => GameText.T("ui.catalog.overview.scope.specific"),
            _ => GameText.T("ui.catalog.overview.scope.auto"),
        };
    }

    private int CatalogOverviewConstructionLaneCount()
    {
        return Math.Min(_constructionProviderLaneStates.Count, MaxProductionProviderLaneButtons);
    }

    private int CatalogOverviewProductionLaneCount()
    {
        var count = 0;
        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            if (ProviderLaneMatchesSelectedTrainCategory(_productionProviderLaneStates[index]))
            {
                count++;
            }
        }

        return Math.Min(count, MaxProductionProviderLaneButtons);
    }
}
