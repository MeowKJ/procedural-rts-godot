using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class BattleRoot
{
    private Rect2 BuildingWorldRect(BuildingModel building)
    {
        var spec = BuildSpecCatalog.For(building.Kind);
        return new Rect2(building.Position - spec.Footprint / 2f, spec.Footprint);
    }

    private static Rect2 BuildingProjectionWorldRect(BuildingPresentationProjection building)
    {
        return new Rect2(building.Entity.Position - building.Footprint / 2f, building.Footprint);
    }

    private void RefreshControlGroups()
    {
        _hud.SetControlGroups(_controlGroups.Snapshots());
    }
    private void RefreshCommandCard()
    {
        _hud.SetBuildCardState(_state.BuildOptionSnapshots(ProceduralRts.Core.Owner.Player));
        if (UseUnitDesignRuntime)
        {
            CollectSelectedProductionBuildingIds();
            _hud.SetCommandCardState(RuntimeProductionCommandCardStates(_selectedProductionBuildingIdBuffer));
            _hud.SetProductionQueueSummary(
                RuntimeProductionQueueSummary(_selectedProductionBuildingIdBuffer, out var canCancel),
                canCancel);
            return;
        }

        _hud.SetCommandCardState(_unitBattlefield.ProductionOptionStates(PlayerSlotId.One));
        _hud.SetProductionQueueSummary(
            _unitBattlefield.ProductionQueueSummary(PlayerSlotId.One),
            _unitBattlefield.HasQueuedProduction(PlayerSlotId.One));
    }

    private void CollectSelectedProductionBuildingIds()
    {
        _selectedProductionBuildingIdBuffer.Clear();
        foreach (var building in _unitBattlefield.SelectedBuildingSelectionProjections(PlayerSlotId.One))
        {
            _selectedProductionBuildingIdBuffer.Add(building.Id);
        }
    }

    private IReadOnlyList<ProductionOptionState> RuntimeProductionCommandCardStates(IReadOnlyList<int> selectedBuildingIds)
    {
        if (selectedBuildingIds.Count > 0)
        {
            var selectedStates = _unitBattlefield.ProductionDesignOptionStatesForSelectedProducers(
                PlayerSlotId.One,
                selectedBuildingIds,
                out var hasSelectedProducers);
            if (hasSelectedProducers)
            {
                return selectedStates;
            }
        }

        return _unitBattlefield.ProductionDesignOptionStates(PlayerSlotId.One);
    }

    private string RuntimeProductionQueueSummary(IReadOnlyList<int> selectedBuildingIds, out bool canCancel)
    {
        if (selectedBuildingIds.Count > 0)
        {
            var selectedSummary = _unitBattlefield.ProductionQueueSummaryForSelectedProducers(
                PlayerSlotId.One,
                selectedBuildingIds,
                out var hasSelectedProducers,
                out var hasQueuedProduction);
            if (hasSelectedProducers)
            {
                canCancel = hasQueuedProduction;
                return selectedSummary;
            }
        }

        canCancel = _unitBattlefield.HasQueuedProduction(PlayerSlotId.One);
        return _unitBattlefield.ProductionQueueSummary(PlayerSlotId.One);
    }

    private void OnCancelProductionRequested()
    {
        CollectSelectedProductionBuildingIds();
        string status;
        if (_selectedProductionBuildingIdBuffer.Count > 0)
        {
            _unitBattlefield.CancelFirstProductionForSelectedProducers(
                PlayerSlotId.One,
                _selectedProductionBuildingIdBuffer,
                out var hasSelectedProducers,
                out status);
            if (hasSelectedProducers)
            {
                _hud.SetStatus(status);
                _hud.SetProductionStatus(status);
                RefreshCommandCard();
                return;
            }
        }

        _unitBattlefield.CancelFirstProduction(PlayerSlotId.One, out status);

        _hud.SetStatus(status);
        _hud.SetProductionStatus(status);
        RefreshCommandCard();
    }

    private void OnBuildKindRequested(string kind)
    {
        _buildPlacement.SelectBuildKind(kind);
    }

    private void OnRallyRequested()
    {
        _selection.ArmRallyCommand();
    }

    private void RefreshSelectionInfo()
    {
        CollectSelectedUnitInstances(PlayerSlotId.One, _selectedUnitInstanceBuffer);
        var selectedUnitInstances = _selectedUnitInstanceBuffer;
        if (selectedUnitInstances.Count > 0)
        {
            _hud.SetHudContext(true, false, _buildPlacement.IsActive);
            _hud.SetSelectedUnitStance(SelectedUniformStance(selectedUnitInstances));
            if (selectedUnitInstances.Count == 1)
            {
                SetUnitInstanceSelectionInfo(selectedUnitInstances[0]);
            }
            else
            {
                SetUnitInstanceGroupSelectionInfo(selectedUnitInstances);
            }

            return;
        }

        if (UseUnitDesignRuntime)
        {
            var selectedBuildingProjections = _unitBattlefield.SelectedBuildingSelectionProjections(PlayerSlotId.One);
            if (selectedBuildingProjections.Count > 0)
            {
                _hud.SetHudContext(true, true, _buildPlacement.IsActive);
                _hud.SetSelectedUnitStance(null);
                if (selectedBuildingProjections.Count == 1)
                {
                    SetUnitBattlefieldBuildingSelectionInfo(selectedBuildingProjections[0]);
                }
                else
                {
                    SetUnitBattlefieldBuildingGroupSelectionInfo(selectedBuildingProjections);
                }

                return;
            }
        }

        CollectSelectedLegacyUnits(_selectedLegacyUnitBuffer);
        CollectSelectedLegacyBuildings(_selectedLegacyBuildingBuffer);
        var selectedUnits = _selectedLegacyUnitBuffer;
        var selectedBuildings = _selectedLegacyBuildingBuffer;
        var total = selectedUnits.Count + selectedBuildings.Count;

        if (total == 0)
        {
            _hud.SetHudContext(false, false, _buildPlacement.IsActive);
            _hud.SetSelectedUnitStance(null);
            _hud.SetSelectedCount(0);
            return;
        }

        _hud.SetHudContext(true, selectedBuildings.Count > 0, _buildPlacement.IsActive);
        _hud.SetSelectedUnitStance(SelectedUniformStance(selectedUnits));

        if (total > 1)
        {
            var combatUnits = 0;
            var economyUnits = 0;
            var cargo = 0;
            var unitHealthTotal = 0f;
            foreach (var unit in selectedUnits)
            {
                if (IsEconomyUnit(unit))
                {
                    economyUnits++;
                }
                else
                {
                    combatUnits++;
                }

                cargo += unit.Cargo;
                unitHealthTotal += UnitHealthRatioForSelection(unit);
            }

            var buildingHealthTotal = 0f;
            foreach (var building in selectedBuildings)
            {
                var spec = BuildSpecCatalog.For(building.Kind);
                buildingHealthTotal += spec.MaxHp > 0 ? building.Hp / spec.MaxHp : 0;
            }

            var avgHealth = selectedUnits.Count == 0
                ? buildingHealthTotal / selectedBuildings.Count
                : unitHealthTotal / selectedUnits.Count;
            _hud.SetSelectionInfo(
                GameText.Format("ui.multi.title", total),
                GameText.Format("ui.multi.meta", combatUnits, economyUnits, selectedBuildings.Count),
                GameText.Format("ui.multi.stats", Mathf.RoundToInt(avgHealth * 100), cargo),
                GameText.T("ui.multi.detail"),
                "multi",
                IconGlyph.Group,
                SelectionIconSummary(selectedUnits, selectedBuildings),
                HudMint);
            return;
        }

        if (selectedUnits.Count == 1)
        {
            SetUnitSelectionInfo(selectedUnits[0]);
            return;
        }

        SetBuildingSelectionInfo(selectedBuildings[0]);
    }

    private void CollectSelectedLegacyUnits(List<UnitModel> result)
    {
        result.Clear();
        foreach (var unit in _state.Units)
        {
            if (unit.Owner == ProceduralRts.Core.Owner.Player && unit.Selected)
            {
                result.Add(unit);
            }
        }
    }

    private void CollectSelectedLegacyBuildings(List<BuildingModel> result)
    {
        result.Clear();
        foreach (var building in _state.Buildings)
        {
            if (building.Owner == ProceduralRts.Core.Owner.Player && building.Selected)
            {
                result.Add(building);
            }
        }
    }

    private void CollectSelectedUnitInstances(PlayerSlotId playerSlotId, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in _unitBattlefield.Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected)
            {
                result.Add(unit);
            }
        }
    }

    private void SetUnitInstanceSelectionInfo(UnitInstance unit)
    {
        var spec = unit.Spec;
        var weapon = WeaponCatalog.Weapons[spec.PrimaryWeapon.WeaponKind];
        var health = $"{Mathf.CeilToInt(unit.Hp)}/{Mathf.CeilToInt(spec.Stats.MaxHp)}";
        var role = GameText.T(spec.RoleKey);
        var detail = UnitInstanceDetail(unit, weapon);

        _hud.SetSelectionInfo(
            spec.Label.ToUpperInvariant(),
            $"{PlayerSlotLabel(unit.PlayerSlotId)} / {UnitFactionLabel(spec.Faction)}",
            GameText.Format("ui.stat.unit", health, role, weapon.Range),
            detail,
            "unit",
            spec.Icon,
            [],
            PlayerSlotAccent(unit.PlayerSlotId),
            spec.Id);
    }

    private static string UnitInstanceDetail(UnitInstance unit, WeaponDefinition weapon)
    {
        var spec = unit.Spec;
        if (spec.RoleTags.Contains(UnitRoleTag.Economy))
        {
            var cargo = GameText.Format("ui.detail.cargo", unit.Cargo, 700);
            return $"{HarvestModeLabel(unit.HarvesterMode)}   {cargo}";
        }

        if (spec.TryGetAbility(AbilityKind.ShieldField, out var shieldField))
        {
            return GameText.Format(
                "ui.detail.shieldField",
                Mathf.RoundToInt(shieldField.Radius),
                ShieldFieldAbsorbLabel(shieldField.Value));
        }

        return GameText.Format("ui.detail.cooldown", weapon.Label, weapon.Cooldown);
    }

    private static string ShieldFieldAbsorbLabel(float value)
    {
        if (value <= 0)
        {
            return "0";
        }

        return value <= 1
            ? $"{Mathf.RoundToInt(value * 100)}%"
            : Mathf.CeilToInt(value).ToString();
    }

    private void SetUnitInstanceGroupSelectionInfo(IReadOnlyList<UnitInstance> units)
    {
        var combatUnits = 0;
        var economyUnits = 0;
        var cargo = 0;
        var healthRatioTotal = 0f;
        foreach (var unit in units)
        {
            if (unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            {
                economyUnits++;
            }
            else
            {
                combatUnits++;
            }

            cargo += unit.Cargo;
            healthRatioTotal += unit.Spec.Stats.MaxHp > 0 ? unit.Hp / unit.Spec.Stats.MaxHp : 0;
        }

        var avgHealth = units.Count == 0 ? 0 : healthRatioTotal / units.Count;

        _hud.SetSelectionInfo(
            GameText.Format("ui.multi.title", units.Count),
            $"{PlayerSlotLabel(PlayerSlotId.One)} / UnitSpec runtime",
            GameText.Format("ui.multi.stats", Mathf.RoundToInt(avgHealth * 100), cargo),
            GameText.Format("ui.multi.meta", combatUnits, economyUnits, 0),
            "multi",
            IconGlyph.Group,
            UnitInstanceIconSummary(units),
            PlayerSlotAccent(PlayerSlotId.One));
    }

    private void SetUnitBattlefieldBuildingSelectionInfo(BuildingSelectionProjection building)
    {
        var health = $"{Mathf.CeilToInt(building.Hp)}/{Mathf.CeilToInt(building.MaxHp)}";
        var queue = building.ProductionQueue.Count == 0
            ? GameText.T("ui.queue.empty").ToUpperInvariant()
            : ProductionDetail(building.ProductionQueue);
        var rally = building.HasRallyPoint ? GameText.T("ui.rally.set") : GameText.T("ui.rally.none");

        _hud.SetSelectionInfo(
            building.Label.ToUpperInvariant(),
            $"{PlayerSlotLabel(building.PlayerSlotId)} / {UnitFactionLabel(building.Faction)}",
            GameText.Format("ui.stat.building", health, building.SightRange),
            GameText.Format("ui.detail.building", queue, rally),
            "building",
            building.Icon,
            [],
            building.Accent);
    }

    private void SetUnitBattlefieldBuildingGroupSelectionInfo(IReadOnlyList<BuildingSelectionProjection> buildings)
    {
        var healthRatioTotal = 0f;
        foreach (var building in buildings)
        {
            healthRatioTotal += building.MaxHp <= 0 ? 0 : building.Hp / building.MaxHp;
        }

        var avgHealth = buildings.Count == 0 ? 0 : healthRatioTotal / buildings.Count;
        _hud.SetSelectionInfo(
            GameText.Format("ui.multi.title", buildings.Count),
            $"{PlayerSlotLabel(PlayerSlotId.One)} / UnitSpec structures",
            GameText.Format("ui.multi.stats", Mathf.RoundToInt(avgHealth * 100), 0),
            GameText.Format("ui.multi.meta", 0, 0, buildings.Count),
            "multi",
            IconGlyph.Group,
            UnitBattlefieldBuildingIconSummary(buildings),
            PlayerSlotAccent(PlayerSlotId.One));
    }

    private void SetUnitSelectionInfo(UnitModel unit)
    {
        var style = UnitSpecReadPathFor(unit);
        var health = $"{Mathf.CeilToInt(unit.Hp)}/{Mathf.CeilToInt(style.Descriptor.MaxHp)}";
        var isEconomy = style.Spec.RoleTags.Contains(UnitRoleTag.Economy);
        var cargo = isEconomy
            ? GameText.Format("ui.detail.cargo", unit.Cargo, GameState.HarvesterCargoCapacity)
            : GameText.Format("ui.detail.damage", style.Descriptor.Damage);
        var detail = isEconomy
            ? $"{HarvestModeLabel(unit.HarvesterMode)}   {cargo}"
            : GameText.Format("ui.detail.cooldown", StanceLabel(unit.Stance), style.Descriptor.AttackCooldown);

        _hud.SetSelectionInfo(
            GameText.T(style.Presentation.NameKey).ToUpperInvariant(),
            UnitAffiliationLabel(unit),
            GameText.Format("ui.stat.unit", health, GameText.T(style.Presentation.RoleKey), style.Descriptor.AttackRange),
            detail,
            style.Presentation.PortraitMode,
            style.Presentation.Icon,
            [],
            style.EntityAccent,
            style.Spec.Id);
    }

}
