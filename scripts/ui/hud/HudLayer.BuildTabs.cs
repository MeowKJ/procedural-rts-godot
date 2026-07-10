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

    private static string CatalogModePageSelectedText(CatalogModeButton button)
    {
        return GameText.Format("ui.catalog.modeSelected", button.Label, CatalogModeSurfaceText(button.Mode));
    }

    private static string CatalogModeFocusText(CatalogModeButton button)
    {
        return GameText.Format("ui.catalog.modeFocus", button.Label);
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
        SetCatalogStatusText(GameText.Format("ui.catalog.modeSelected", CatalogModeLabelText(next), CatalogModeSurfaceText(next)));
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
    }

    private void RefreshCatalogOverview()
    {
        RefreshCatalogBreadcrumb();
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

    private void RefreshCatalogBreadcrumb()
    {
        if (_catalogBreadcrumbValue is null)
        {
            return;
        }

        _catalogBreadcrumbValue.Text = CompactText(CatalogBreadcrumbText(), 24);
    }

    private string CatalogBreadcrumbText()
    {
        return _selectedCatalogMode switch
        {
            CatalogModeKind.Build => GameText.Format(
                "ui.catalog.breadcrumb.build",
                CatalogBreadcrumbBuildCategoryText(_selectedBuildCategory),
                CatalogBreadcrumbProviderText(CurrentConstructionProviderLaneState(), _selectedConstructionProviderLaneScope)),
            CatalogModeKind.Train => GameText.Format(
                "ui.catalog.breadcrumb.train",
                CatalogBreadcrumbProductionCategoryText(_selectedProductionCategory),
                CatalogBreadcrumbProviderText(CurrentProductionProviderLaneState(), _selectedProductionProviderLaneScope)),
            CatalogModeKind.Upgrades => GameText.Format(
                "ui.catalog.breadcrumb.upgrades",
                GameText.T("ui.catalog.breadcrumb.category.projects"),
                GameText.T("ui.catalog.breadcrumb.noLane")),
            CatalogModeKind.Abilities => GameText.Format(
                "ui.catalog.breadcrumb.abilities",
                _abilityCardStates.Count == 0
                    ? GameText.T("ui.catalog.breadcrumb.abilities.none")
                    : GameText.T("ui.catalog.breadcrumb.abilities.selected")),
            _ => "",
        };
    }

    private static string CatalogBreadcrumbProviderText(ProductionProviderLaneState? state, ProductionProviderLaneScope scope)
    {
        if (scope == ProductionProviderLaneScope.Specific && state is not null && !string.IsNullOrWhiteSpace(state.ShortLabel))
        {
            return state.ShortLabel;
        }

        return CatalogOverviewProviderScopeText(scope);
    }

    private static string CatalogBreadcrumbBuildCategoryText(BuildCategory category)
    {
        return category switch
        {
            BuildCategory.Command => GameText.T("ui.catalog.breadcrumb.category.command"),
            BuildCategory.Power => GameText.T("ui.catalog.breadcrumb.category.power"),
            BuildCategory.Economy => GameText.T("ui.catalog.breadcrumb.category.economy"),
            BuildCategory.Infantry => GameText.T("ui.catalog.breadcrumb.category.infantry"),
            BuildCategory.Vehicle => GameText.T("ui.catalog.breadcrumb.category.vehicle"),
            BuildCategory.Defense => GameText.T("ui.catalog.breadcrumb.category.defense"),
            BuildCategory.Air => GameText.T("ui.catalog.breadcrumb.category.air"),
            BuildCategory.Naval => GameText.T("ui.catalog.breadcrumb.category.naval"),
            _ => "",
        };
    }

    private static string CatalogBreadcrumbProductionCategoryText(ProductionCategory category)
    {
        return category switch
        {
            ProductionCategory.Infantry => GameText.T("ui.catalog.breadcrumb.category.infantry"),
            ProductionCategory.Vehicle => GameText.T("ui.catalog.breadcrumb.category.vehicle"),
            ProductionCategory.Economy => GameText.T("ui.catalog.breadcrumb.category.economy"),
            ProductionCategory.Defense => GameText.T("ui.catalog.breadcrumb.category.defense"),
            ProductionCategory.Air => GameText.T("ui.catalog.breadcrumb.category.air"),
            ProductionCategory.Naval => GameText.T("ui.catalog.breadcrumb.category.naval"),
            _ => "",
        };
    }
}
