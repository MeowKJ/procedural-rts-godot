using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private readonly List<ProductionTab> _productionTabs = [];
    private BuildCategory _selectedBuildCategory = BuildCategory.Command;

    private void RegisterProductionTab(ProductionTab tab)
    {
        tab.SetSelected(tab.Category == _selectedBuildCategory);
        _productionTabs.Add(tab);
    }

    private void SelectProductionTab(BuildCategory category)
    {
        _selectedBuildCategory = category;
        for (var index = 0; index < _productionTabs.Count; index++)
        {
            var tab = _productionTabs[index];
            tab.SetSelected(tab.Category == category);
        }
    }
}
