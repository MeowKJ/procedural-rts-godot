using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void RefreshProductionProviderLaneSummary()
    {
        if (_queueMiniStack is null)
        {
            return;
        }

        if (_selectedCatalogMode is not (CatalogModeKind.Build or CatalogModeKind.Train))
        {
            _queueMiniStack.Visible = false;
            _cancelProduction.Visible = false;
            return;
        }

        var state = CurrentProviderLaneState();
        _queueMiniStack.Visible = true;
        _cancelProduction.Visible = _selectedCatalogMode == CatalogModeKind.Train;
        _queueMiniStack.SetState(
            QueueMiniStackGlyph(),
            QueueMiniStackAccent(state),
            state?.QueueCount ?? 0,
            state?.ActiveProgress ?? 0,
            state?.Available ?? false);
    }

    private IconGlyph QueueMiniStackGlyph()
    {
        if (_selectedCatalogMode == CatalogModeKind.Build)
        {
            return IconGlyph.Building;
        }

        for (var index = 0; index < _commandCardStates.Count; index++)
        {
            var state = _commandCardStates[index];
            if (state.Category == _selectedProductionCategory
                && (state.ActiveProgress > 0 || state.QueuedCount > 0))
            {
                return state.RoleGlyph == IconGlyph.None ? state.Icon : state.RoleGlyph;
            }
        }

        return IconGlyph.Infantry;
    }

    private static Color QueueMiniStackAccent(ProductionProviderLaneState? state)
    {
        if (state is null || !state.Available)
        {
            return InkMuted;
        }

        return state.Scope switch
        {
            ProductionProviderLaneScope.All => Mint,
            ProductionProviderLaneScope.Specific => Amber,
            _ => Cyan,
        };
    }

    private string CurrentQueueDeckInspectorText()
    {
        if (_selectedCatalogMode is not (CatalogModeKind.Build or CatalogModeKind.Train))
        {
            return NonProviderLaneRailHintText();
        }

        return CurrentProviderLaneState() is { } state
            ? ProviderLaneSummaryText(state)
            : CurrentProviderLaneEmptyText();
    }

    private ProductionProviderLaneState? CurrentProviderLaneState()
    {
        return _selectedCatalogMode == CatalogModeKind.Build
            ? CurrentConstructionProviderLaneState()
            : CurrentProductionProviderLaneState();
    }

    private ProductionProviderLaneState? CurrentProductionProviderLaneState()
    {
        for (var index = 0; index < _productionProviderLaneStates.Count; index++)
        {
            var state = _productionProviderLaneStates[index];
            if (!ProviderLaneMatchesSelectedTrainCategory(state)
                || state.Scope != _selectedProductionProviderLaneScope)
            {
                continue;
            }

            if (state.Scope != ProductionProviderLaneScope.Specific
                || state.ProducerId == _selectedProductionProviderId)
            {
                return state;
            }
        }

        return null;
    }

    private ProductionProviderLaneState? CurrentConstructionProviderLaneState()
    {
        for (var index = 0; index < _constructionProviderLaneStates.Count; index++)
        {
            var state = _constructionProviderLaneStates[index];
            if (state.Scope != _selectedConstructionProviderLaneScope)
            {
                continue;
            }

            if (state.Scope != ProductionProviderLaneScope.Specific
                || state.ProducerId == _selectedConstructionProviderId)
            {
                return state;
            }
        }

        return null;
    }

    private string CurrentProviderLaneEmptyText()
    {
        return _selectedCatalogMode == CatalogModeKind.Build
            ? GameText.T("ui.constructionProviderLane.empty")
            : GameText.T("ui.providerLane.empty");
    }

    private string NonProviderLaneRailHintText()
    {
        return _selectedCatalogMode switch
        {
            CatalogModeKind.Upgrades => GameText.T("ui.providerLane.upgradesNone"),
            CatalogModeKind.Abilities => GameText.T("ui.providerLane.abilitiesNone"),
            _ => "",
        };
    }

    private static string ProviderLaneSummaryText(ProductionProviderLaneState state)
    {
        var availability = state.Available
            ? GameText.T("ui.providerLane.summaryOk")
            : ProviderLaneSummaryDisabledReason(state.DisabledReasonKey);
        var progress = state.ActiveProgress > 0 ? Mathf.RoundToInt(state.ActiveProgress * 100) : 0;
        return GameText.Format(
            "ui.providerLane.summary",
            state.ShortLabel,
            state.ProviderCount,
            state.QueueCount,
            progress,
            availability);
    }

    private static string ProviderLaneSummaryDisabledReason(string disabledReasonKey)
    {
        return disabledReasonKey switch
        {
            "ui.providerLane.offline" => GameText.T("ui.providerLane.summaryOffline"),
            "ui.providerLane.incomplete" => GameText.T("ui.providerLane.summaryIncomplete"),
            "ui.constructionProviderLane.offline" => GameText.T("ui.providerLane.summaryOffline"),
            "ui.constructionProviderLane.incomplete" => GameText.T("ui.providerLane.summaryIncomplete"),
            "ui.constructionProviderLane.none" => GameText.T("ui.providerLane.summaryNone"),
            "ui.producerUnavailable" => GameText.T("ui.providerLane.summaryNone"),
            _ => GameText.T("ui.providerLane.summaryLocked"),
        };
    }
}
