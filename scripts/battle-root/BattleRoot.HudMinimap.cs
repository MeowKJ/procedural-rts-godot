using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class BattleRoot
{
    private readonly List<HudLayer.MinimapUnit> _minimapUnitBuffer = [];
    private readonly List<HudLayer.MinimapUnit> _minimapUnitSecondaryBuffer = [];
    private readonly List<HudLayer.MinimapBuilding> _minimapBuildingBuffer = [];
    private readonly List<HudLayer.MinimapBuilding> _minimapBuildingSecondaryBuffer = [];
    private readonly List<HudLayer.MinimapResource> _minimapResourceBuffer = [];
    private readonly List<HudLayer.MinimapResource> _minimapResourceSecondaryBuffer = [];
    private readonly List<HudLayer.MinimapAlertPing> _minimapAlertPingBuffer = [];
    private readonly List<HudLayer.MinimapAlertPing> _minimapAlertPingSecondaryBuffer = [];
    private bool _useSecondaryMinimapHudBuffers;

    private void RefreshMinimap()
    {
        _useSecondaryMinimapHudBuffers = !_useSecondaryMinimapHudBuffers;
        var units = _useSecondaryMinimapHudBuffers ? _minimapUnitSecondaryBuffer : _minimapUnitBuffer;
        var buildings = _useSecondaryMinimapHudBuffers ? _minimapBuildingSecondaryBuffer : _minimapBuildingBuffer;
        var resources = _useSecondaryMinimapHudBuffers ? _minimapResourceSecondaryBuffer : _minimapResourceBuffer;
        var alertPings = _useSecondaryMinimapHudBuffers ? _minimapAlertPingSecondaryBuffer : _minimapAlertPingBuffer;
        units.Clear();
        buildings.Clear();
        resources.Clear();
        alertPings.Clear();

        FillMinimapUnits(units);
        FillUnitBattlefieldMinimapBuildings(buildings);

        FillMinimapResources(resources);
        FillMinimapAlertPings(alertPings);

        _hud.SetMinimapState(
            _worldSize,
            _camera.VisibleWorldRect(),
            units,
            buildings,
            resources,
            _presentationEnvironment.FogOfWar.MaskTexture(),
            _unitBattlefield.MinimapPips(PlayerSlotId.One),
            alertPings);
    }

    private void FillMinimapUnits(List<HudLayer.MinimapUnit> result)
    {
        foreach (var unit in _unitBattlefield.MinimapPips(PlayerSlotId.One))
        {
            if (unit.Relation == PlayerRelation.Hostile && !unit.IsVisible)
            {
                continue;
            }

            result.Add(new HudLayer.MinimapUnit(
                unit.Position,
                OwnerForPlayerSlot(unit.PlayerSlotId) ?? ProceduralRts.Core.Owner.Enemy,
                ToFactionId(unit.Faction),
                unit.Selected,
                unit.AlertPulse));
        }
    }

    private void FillUnitBattlefieldMinimapBuildings(List<HudLayer.MinimapBuilding> result)
    {
        var projections = _unitBattlefield.BuildingMinimapProjections(PlayerSlotId.One, _presentationEnvironment.FogOfWar.AnyExplored);
        foreach (var building in projections)
        {
            result.Add(new HudLayer.MinimapBuilding(
                building.Position,
                building.Footprint,
                OwnerForPlayerSlot(building.PlayerSlotId) ?? ProceduralRts.Core.Owner.Enemy,
                ToFactionId(building.Faction),
                building.Selected,
                building.AlertPulse));
        }
    }

    private void FillMinimapResources(List<HudLayer.MinimapResource> result)
    {
        var pips = _unitBattlefield.ResourcePips(_presentationEnvironment.IsExplored);
        foreach (var resource in pips)
        {
            result.Add(new HudLayer.MinimapResource(
                resource.Position,
                resource.Radius,
                resource.RemainingRatio));
        }
    }

    private void FillMinimapAlertPings(List<HudLayer.MinimapAlertPing> result)
    {
        for (var index = 0; index < _alerts.Count; index++)
        {
            var alert = _alerts[index];
            if (alert.WorldPosition is not { } position)
            {
                continue;
            }

            var remainingRatio = 1 - Mathf.Clamp(alert.Age / alert.Lifetime, 0, 1);
            if (remainingRatio <= 0.01f)
            {
                continue;
            }

            result.Add(new HudLayer.MinimapAlertPing(position, alert.Kind, remainingRatio));
        }
    }
}
