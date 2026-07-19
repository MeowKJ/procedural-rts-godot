using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    public void ApplyBattleHudRuntimeProjection(BattleHudRuntimeProjection projection)
    {
        var selection = projection.Selection;
        var hasSelection = selection.Kind != BattleHudSelectionKind.None;
        var hasBuildingSelection = selection.Kind == BattleHudSelectionKind.ProductionBuilding;

        SetResourceCredits(projection.Credits);
        SetHudContext(hasSelection, hasBuildingSelection, buildModeActive: false);
        SetSelectionInfo(
            selection.Title,
            selection.Meta,
            selection.Stats,
            selection.Detail,
            selection.PortraitMode,
            selection.Icon);
        SetAbilityCardState([]);
        SetConstructionProviderLaneState([]);
        ApplyRuntimeProductionProjection(projection.Production);
        SetAlerts(projection.Alert is { } alert
            ? [new AlertLine(alert.Kind, null, alert.Text, alert.RemainingRatio)]
            : []);
        SetStatus(projection.Status);
        SetCommandDeckOpen(projection.CommandDeckOpen);
    }

    private void ApplyRuntimeProductionProjection(BattleHudProductionProjection projection)
    {
        if (!projection.Visible)
        {
            SetCommandCardState([]);
            SetProductionProviderLaneState([]);
            SetProductionQueueSummary(projection.QueueSummary, canCancel: false);
            return;
        }

        SetCommandCardState(
        [
            new ProductionOptionState(
                ProductionKind.InfantrySquad,
                ProductionCategory.Infantry,
                BuildingDesignIds.Barracks,
                "cat.basic",
                "CB",
                IconGlyph.Infantry,
                IconGlyph.Infantry,
                new Color("#62C9C4"),
                projection.Cost,
                5.2f,
                true,
                projection.EnoughCredits,
                projection.QueuedCount,
                projection.ActiveProgress,
                projection.DisabledReasonKey),
        ]);
        SetProductionProviderLaneState(
        [
            new ProductionProviderLaneState(
                ProductionProviderLaneScope.Auto,
                0,
                BuildingDesignIds.Barracks,
                "AUTO / BARRACKS",
                "A",
                1,
                projection.QueuedCount,
                projection.ActiveProgress,
                true,
                ""),
        ]);
        SetProductionQueueSummary(projection.QueueSummary, projection.CanCancel);
    }
}
