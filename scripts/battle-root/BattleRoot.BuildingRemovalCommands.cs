using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void OnSellOrCancelRequested()
    {
        if (_unitBattlefield.HasSelectedBuildings(PlayerSlotId.One))
        {
            var soldCount = _unitBattlefield.SellSelectedBuildings(PlayerSlotId.One, out var status);
            _hud.SetStatus(status);
            _hud.SetProductionStatus(status);
            if (soldCount > 0)
            {
                RefreshSelectionInfo();
                RefreshCommandCard();
                RefreshMinimap();
                return;
            }
        }

        OnCancelProductionRequested();
    }

    private void OnUnitBattlefieldBuildingsRemoved(IReadOnlyList<UnitBattlefieldBuildingDeathInfo> deaths)
    {
        var shouldPlayDeathCue = false;
        foreach (var death in deaths)
        {
            var building = _state.BuildingById(death.Id);
            if (building is not null)
            {
                building.Hp = 0;
            }

            if (!_buildingViews.Remove(death.Id, out var view))
            {
                continue;
            }

            if (death.RemovalCause == UnitBattlefieldBuildingRemovalCause.Destroyed)
            {
                shouldPlayDeathCue = true;
                if (death.PlayerSlotId == PlayerSlotId.One)
                {
                    AddAlert(AlertKind.Building, GameText.Format("ui.building.destroyed", BuildSpecCatalog.For(death.Kind).Label), death.Position);
                }
            }

            if (death.PlayerSlotId == PlayerSlotId.One && death.Kind == BuildingDesignIds.PowerPlant)
            {
                UpdatePowerAlert(true);
            }

            view.QueueFree();
        }

        if (shouldPlayDeathCue)
        {
            PlayDeathCue(deaths);
        }
    }
}
