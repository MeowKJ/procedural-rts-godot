using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    public void SetCommandCardState(IReadOnlyList<ProductionOptionState> states)
    {
        _commandCardStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            var state = states[index];
            _commandCardStates.Add(state);
        }

        ValidateProductionProviderLaneSelection();
        RefreshCommandCards();
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshRepeatProductionControl();
    }

    public void SetProductionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)
    {
        _productionProviderLaneStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            _productionProviderLaneStates.Add(states[index]);
        }

        ValidateProductionProviderLaneSelection();
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshRepeatProductionControl();
    }

    public void SetConstructionProviderLaneState(IReadOnlyList<ProductionProviderLaneState> states)
    {
        _constructionProviderLaneStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            _constructionProviderLaneStates.Add(states[index]);
        }

        ValidateConstructionProviderLaneSelection();
        RefreshProductionProviderLaneSummary();
        RefreshProductionProviderLaneButtons();
        RefreshRepeatProductionControl();
    }

    public void SetBuildCardState(IReadOnlyList<BuildOptionSnapshot> states)
    {
        _buildCardStates.Clear();
        for (var index = 0; index < states.Count; index++)
        {
            _buildCardStates.Add(states[index]);
        }

        RefreshCommandCards();
    }
}
