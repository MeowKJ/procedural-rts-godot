using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void RefreshBuildCards()
    {
        for (var index = 0; index < _visibleBuildCardStates.Count; index++)
        {
            var state = _visibleBuildCardStates[index];
            var optionId = BuildOptionId(state);
            if (!_commandButtons.TryGetValue(optionId, out var button))
            {
                button = AddCommandButton(_rightProductionPanel, optionId);
            }

            button.Hotkey = ProductionHotkey(index);
            button.Position = ProductionButtonPosition(index);

            var disabledReason = LocalizedDisabledReason(state.DisabledReasonKey, state.Cost);
            button.SetBuildState(state, disabledReason);
        }

        RefreshCatalogEmptyHint();
    }

    private void RefreshProductionCards()
    {
        for (var index = 0; index < _visibleCommandCardStates.Count; index++)
        {
            var state = _visibleCommandCardStates[index];
            var optionId = ProductionOptionId(state);
            if (!_commandButtons.TryGetValue(optionId, out var button))
            {
                button = AddCommandButton(_rightProductionPanel, optionId);
            }

            button.Hotkey = ProductionHotkey(index);
            button.Position = ProductionButtonPosition(index);
            button.Kind = state.Kind;
            button.UnitDesignId = state.UnitDesignId;

            var disabledReason = LocalizedDisabledReason(state.DisabledReasonKey, state.Cost);
            button.SetState(state, disabledReason);
        }

        RefreshCatalogEmptyHint();
    }

    private void RefreshCatalogEmptyHint()
    {
        if (_catalogEmptyHintValue is null)
        {
            return;
        }

        var text = CatalogEmptyHintText();
        _catalogEmptyHintValue.Visible = !string.IsNullOrWhiteSpace(text);
        _catalogEmptyHintValue.Text = CompactMultiline(text, 38);
        SetLabelColor(_catalogEmptyHintValue, new Color(CatalogModeAccent(_selectedCatalogMode), 0.82f));
    }

    private string CatalogEmptyHintText()
    {
        return _selectedCatalogMode switch
        {
            CatalogModeKind.Build => _visibleBuildCardStates.Count == 0
                ? GameText.T("ui.catalog.empty.build")
                : "",
            CatalogModeKind.Train => _visibleCommandCardStates.Count == 0
                ? GameText.T("ui.catalog.empty.train")
                : "",
            CatalogModeKind.Upgrades => DefaultUpgradeProjectShellStates.Length == 0
                ? GameText.T("ui.catalog.empty.upgrades")
                : "",
            CatalogModeKind.Abilities => _abilityCardStates.Count == 0
                ? GameText.T("ui.catalog.empty.abilities")
                : "",
            _ => "",
        };
    }

    private static string LocalizedDisabledReason(string disabledReasonKey, int cost)
    {
        return disabledReasonKey == "ui.needCredits"
            ? GameText.Format("ui.needCredits", cost)
            : string.IsNullOrWhiteSpace(disabledReasonKey) ? "" : GameText.T(disabledReasonKey);
    }
}
