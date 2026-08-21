using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    public void SetStatus(string status)
    {
        _commandFailureVisible = CommandFailurePresentation.IsFailureStatus(status);
        SetLabelTextAndResetSizeWhenChanged(
            _statusValue,
            CompactText(CommandFailurePresentation.InlineText(status), 42));
    }

    public void SetProductionStatus(string status)
    {
        _productionCommandFailureVisible = CommandFailurePresentation.IsFailureStatus(status);
        if (!string.Equals(_lastProductionStatus, status, StringComparison.Ordinal))
        {
            _productionStatusPulse = 1f;
            _lastProductionStatus = status;
        }

        if (_selectedCatalogMode != CatalogModeKind.Abilities)
        {
            SetCommandPanelResult(status);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != GameText.T("ui.status.ready"))
        {
            SetCommandDeckOpen(true);
        }
    }

    public void ClearCommandFailureFeedback()
    {
        if (_commandFailureVisible)
        {
            _commandFailureVisible = false;
            SetLabelTextAndResetSizeWhenChanged(
                _statusValue,
                CompactText(GameText.T("ui.status.ready"), 42));
        }

        if (!_productionCommandFailureVisible)
        {
            return;
        }

        _productionCommandFailureVisible = false;
        _lastProductionStatus = "";
        if (_selectedCatalogMode != CatalogModeKind.Abilities)
        {
            ClearCatalogInspectorCommandFeedback();
            SetCatalogInspectorDefault(DefaultCatalogInspectorText());
        }
    }

    public void SetProductionQueueSummary(string summary, bool canCancel)
    {
        if (!string.Equals(_lastQueueSummary, summary, StringComparison.Ordinal)
            || _lastCanCancelProduction != canCancel)
        {
            _queueStatusPulse = 1f;
            _lastQueueSummary = summary;
            _lastCanCancelProduction = canCancel;
            var lineBreak = summary.IndexOf('\n');
            var surfaceSummary = lineBreak >= 0 ? summary[..lineBreak] : summary;
            SetLabelTextAndResetSizeWhenChanged(_queueValue, CompactText(surfaceSummary, 28));
            _cancelProduction.FixedHoverText = canCancel ? summary : GameText.T("ui.cancel.none");
        }

        _cancelProduction.Disabled = !canCancel;
        RefreshProductionProviderLaneSummary();
    }

    public void SetResourceCredits(int credits)
    {
        _creditsValue.Text = credits.ToString("N0");
    }

    public void SetMinimapState(
        Vector2 worldSize,
        Rect2 cameraWorldRect,
        IReadOnlyList<MinimapUnit> units,
        IReadOnlyList<MinimapBuilding> buildings,
        IReadOnlyList<MinimapResource> resources,
        Texture2D? fogMask,
        IReadOnlyList<UnitMinimapPip>? unitDesignPips = null,
        IReadOnlyList<MinimapAlertPing>? alertPings = null)
    {
        _minimapSurface.WorldSize = worldSize;
        _minimapSurface.ViewerFaction = ViewerFaction;
        _minimapSurface.CameraWorldRect = cameraWorldRect;
        _minimapSurface.Units = units;
        _minimapSurface.UnitDesignPips = unitDesignPips ?? [];
        _minimapSurface.Buildings = buildings;
        _minimapSurface.Resources = resources;
        _minimapSurface.AlertPings = alertPings ?? [];
        _minimapSurface.FogMask = fogMask;
        _minimapSurface.QueueRedraw();
    }

    public void SetControlGroups(IReadOnlyList<ControlGroupSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (snapshot.Number < 1 || snapshot.Number > _controlGroupSlots.Count)
            {
                continue;
            }

            _controlGroupSlots[snapshot.Number - 1].SetSnapshot(snapshot);
        }
    }

    public void SetAlerts(IReadOnlyList<AlertLine> alerts)
    {
        _alertValue.Visible = false;
        for (var index = 0; index < _alertRows.Count; index++)
        {
            _alertRows[index].SetAlert(index < alerts.Count ? alerts[index] : null);
        }
    }

    public void SetCommandPreview(CommandPreviewState preview)
    {
        _commandPreview.Preview = preview;
        ApplyCommandCursor(preview);
        _commandPreview.QueueRedraw();
        RefreshCommandRibbonContext();
    }

    public void SetOutcomeBanner(GameOutcome outcome, string detail)
    {
        if (outcome == GameOutcome.InProgress)
        {
            _outcomeBanner.Visible = false;
            return;
        }

        _outcomeBanner.Visible = true;
        _outcomeTitle.Text = outcome == GameOutcome.Victory ? GameText.T("ui.outcome.victory") : GameText.T("ui.outcome.defeat");
        SetLabelColor(_outcomeTitle, outcome == GameOutcome.Victory ? Mint : Danger);
        _outcomeDetail.Text = CompactText(detail, 54);
    }
}
