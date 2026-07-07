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
            _catalogSurfaceLabel.Text = mode switch
            {
                CatalogModeKind.Build => GameText.T("ui.catalog.buildSurface"),
                CatalogModeKind.Train => GameText.T("ui.catalog.trainSurface"),
                CatalogModeKind.Abilities => GameText.T("ui.catalog.abilitiesSurface"),
                _ => "",
            };
        }

        if (mode != CatalogModeKind.Abilities && _productionValue is not null)
        {
            SetCatalogStatusText(string.IsNullOrWhiteSpace(_lastProductionStatus)
                ? GameText.T("ui.status.ready")
                : _lastProductionStatus);
        }

        RefreshCommandCards();
        RefreshProductionProviderLaneButtons();
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
    }
}
