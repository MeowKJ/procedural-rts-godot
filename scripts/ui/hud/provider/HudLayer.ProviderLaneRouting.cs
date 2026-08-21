using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private int? SelectedConstructionProviderId(string? buildKind)
    {
        if (_selectedConstructionProviderLaneScope == ProductionProviderLaneScope.Specific
            && _selectedConstructionProviderId > 0)
        {
            return _selectedConstructionProviderId;
        }

        if (_selectedConstructionProviderLaneScope != ProductionProviderLaneScope.All
            || string.IsNullOrWhiteSpace(buildKind))
        {
            return null;
        }

        var requiredProducer = BuildSpecCatalog.For(buildKind).RequiredProducer;
        return string.IsNullOrWhiteSpace(requiredProducer)
            ? null
            : NextAllConstructionProviderId(requiredProducer);
    }

    private int? NextAllConstructionProviderId(string providerKind)
    {
        var matchingCount = 0;
        for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
        {
            if (IsAvailableSpecificProviderLane(_constructionProviderLaneStates[index], providerKind))
            {
                matchingCount++;
            }
        }

        if (matchingCount == 0)
        {
            return null;
        }

        _allConstructionProviderCursorByKind.TryGetValue(providerKind, out var cursor);
        var targetOrdinal = cursor % matchingCount;
        var ordinal = 0;
        for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
        {
            var state = _constructionProviderLaneStates[index];
            if (!IsAvailableSpecificProviderLane(state, providerKind))
            {
                continue;
            }

            if (ordinal == targetOrdinal)
            {
                _allConstructionProviderCursorByKind[providerKind] = targetOrdinal + 1;
                return state.ProducerId;
            }

            ordinal++;
        }

        return null;
    }

    private int? NextAllProductionProviderId(string producerKind)
    {
        var matchingCount = 0;
        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            if (IsAvailableSpecificProviderLane(_productionProviderLaneStates[index], producerKind))
            {
                matchingCount++;
            }
        }

        if (matchingCount == 0)
        {
            return null;
        }

        _allProductionProviderCursorByKind.TryGetValue(producerKind, out var cursor);
        var targetOrdinal = cursor % matchingCount;
        var ordinal = 0;
        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            var state = _productionProviderLaneStates[index];
            if (!IsAvailableSpecificProviderLane(state, producerKind))
            {
                continue;
            }

            if (ordinal == targetOrdinal)
            {
                _allProductionProviderCursorByKind[producerKind] = targetOrdinal + 1;
                return state.ProducerId;
            }

            ordinal++;
        }

        return null;
    }

    private static bool IsAvailableSpecificProviderLane(ProductionProviderLaneState state, string producerKind)
    {
        return state.Scope == ProductionProviderLaneScope.Specific
            && state.Available
            && state.ProducerKind == producerKind;
    }
}
