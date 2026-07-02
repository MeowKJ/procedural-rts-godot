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
    private bool _useSecondaryMinimapHudBuffers;

    private void RefreshMinimap()
    {
        _useSecondaryMinimapHudBuffers = !_useSecondaryMinimapHudBuffers;
        var units = _useSecondaryMinimapHudBuffers ? _minimapUnitSecondaryBuffer : _minimapUnitBuffer;
        var buildings = _useSecondaryMinimapHudBuffers ? _minimapBuildingSecondaryBuffer : _minimapBuildingBuffer;
        var resources = _useSecondaryMinimapHudBuffers ? _minimapResourceSecondaryBuffer : _minimapResourceBuffer;
        units.Clear();
        buildings.Clear();
        resources.Clear();

        FillMinimapUnits(units);
        if (UseUnitDesignRuntime)
        {
            FillUnitBattlefieldMinimapBuildings(buildings);
        }
        else
        {
            FillLegacyMinimapBuildings(buildings);
        }

        FillMinimapResources(resources);

        _hud.SetMinimapState(
            _state.WorldSize,
            _camera.VisibleWorldRect(),
            units,
            buildings,
            resources,
            _state.FogOfWar.MaskTexture(),
            _unitBattlefield.MinimapPips(PlayerSlotId.One));
    }

    private void FillMinimapUnits(List<HudLayer.MinimapUnit> result)
    {
        foreach (var unit in _state.Units)
        {
            if (unit.Hp <= 0 || !_state.IsVisibleToPlayer(unit))
            {
                continue;
            }

            result.Add(new HudLayer.MinimapUnit(
                unit.Position,
                unit.Owner,
                unit.FactionId,
                unit.Selected,
                unit.AlertPulse));
        }
    }

    private void FillLegacyMinimapBuildings(List<HudLayer.MinimapBuilding> result)
    {
        foreach (var building in _state.Buildings)
        {
            if (building.Hp <= 0 || !_state.IsExploredByPlayer(building))
            {
                continue;
            }

            var spec = BuildSpecCatalog.For(building.Kind);
            result.Add(new HudLayer.MinimapBuilding(
                building.Position,
                spec.Footprint,
                building.Owner,
                building.FactionId,
                building.Selected,
                Mathf.Max(building.HitPulse, building.DeliveryPulse * 0.45f)));
        }
    }

    private void FillUnitBattlefieldMinimapBuildings(List<HudLayer.MinimapBuilding> result)
    {
        var projections = _unitBattlefield.BuildingMinimapProjections(PlayerSlotId.One, rect => _state.FogOfWar.AnyExplored(rect));
        foreach (var building in projections)
        {
            result.Add(new HudLayer.MinimapBuilding(
                building.Position,
                building.Footprint,
                OwnerForPlayerSlot(building.PlayerSlotId) ?? ProceduralRts.Core.Owner.Enemy,
                ToLegacyFaction(building.Faction),
                building.Selected,
                building.AlertPulse));
        }
    }

    private void FillMinimapResources(List<HudLayer.MinimapResource> result)
    {
        var pips = _unitBattlefield.ResourcePips(_state.IsExploredByPlayer);
        foreach (var resource in pips)
        {
            result.Add(new HudLayer.MinimapResource(
                resource.Position,
                resource.Radius,
                resource.RemainingRatio));
        }
    }
}
