using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class BattleRoot
{
    private static Rect2 BuildingProjectionWorldRect(BuildingPresentationProjection building)
    {
        return new Rect2(building.Entity.Position - building.Footprint / 2f, building.Footprint);
    }

    private void RefreshControlGroups()
    {
        _hud.SetControlGroups(_controlGroups.Snapshots());
    }

    private void OnRepairRequested()
    {
        _selection.ArmRepairCommand();
    }

    private void RefreshCommandCard()
    {
        _hud.SetBuildCardState(_unitBattlefield.BuildOptionSnapshots(PlayerSlotId.One));
        CollectSelectedProductionBuildingIds();
        _hud.SetCommandCardState(RuntimeProductionCommandCardStates(_selectedProductionBuildingIdBuffer));
        _hud.SetProductionProviderLaneState(_unitBattlefield.ProductionProviderLaneStates(PlayerSlotId.One));
        _hud.SetConstructionProviderLaneState(_unitBattlefield.ConstructionProviderLaneStates(PlayerSlotId.One));
        _hud.SetProductionQueueSummary(
            RuntimeProductionQueueSummary(_selectedProductionBuildingIdBuffer, out var canCancel),
            canCancel);
        var selectedAbilityCards = RuntimeSelectedAbilityCardStates(out var abilitySourceUnitCount);
        _hud.SetAbilityCardState(selectedAbilityCards, abilitySourceUnitCount);
    }

    private void CollectSelectedProductionBuildingIds()
    {
        _selectedProductionBuildingIdBuffer.Clear();
        foreach (var building in _unitBattlefield.SelectedBuildingSelectionProjections(PlayerSlotId.One))
        {
            _selectedProductionBuildingIdBuffer.Add(building.Id);
        }
    }

    private IReadOnlyList<HudLayer.AbilityCardState> RuntimeSelectedAbilityCardStates(out int abilitySourceUnitCount)
    {
        _selectedAbilityCardBuffer.Clear();
        abilitySourceUnitCount = 0;
        CollectSelectedUnitInstances(PlayerSlotId.One, _selectedUnitInstanceBuffer);
        foreach (var unit in _selectedUnitInstanceBuffer)
        {
            if (unit.Hp <= 0)
            {
                continue;
            }

            var entity = _unitBattlefield.UnitEntityByInstanceId(unit.Id);
            var unitContributedAbility = false;
            foreach (var ability in unit.Spec.Abilities)
            {
                if (!IsHudAbility(ability.Kind))
                {
                    continue;
                }

                unitContributedAbility = true;
                AddOrMergeSelectedAbilityCard(
                    ability,
                    AbilityCooldownRemaining(entity, ability.Kind),
                    IsAbilityActive(entity, ability.Kind));
            }

            if (unitContributedAbility)
            {
                abilitySourceUnitCount++;
            }
        }

        return _selectedAbilityCardBuffer;
    }

    private void AddOrMergeSelectedAbilityCard(AbilitySpec ability, float cooldownRemaining, bool isActive)
    {
        var existingIndex = SelectedAbilityCardIndex(ability.Kind);
        if (existingIndex < 0)
        {
            _selectedAbilityCardBuffer.Add(new HudLayer.AbilityCardState(ability, cooldownRemaining, isActive));
            return;
        }

        var existing = _selectedAbilityCardBuffer[existingIndex];
        _selectedAbilityCardBuffer[existingIndex] = new HudLayer.AbilityCardState(
            cooldownRemaining < existing.CooldownRemaining ? ability : existing.Ability,
            MathF.Min(existing.CooldownRemaining, cooldownRemaining),
            MergedAbilityActiveState(ability.Kind, existing.IsActive, isActive));
    }

    private int SelectedAbilityCardIndex(AbilityKind kind)
    {
        for (var index = 0; index < _selectedAbilityCardBuffer.Count; index++)
        {
            if (_selectedAbilityCardBuffer[index].Ability.Kind == kind)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool MergedAbilityActiveState(AbilityKind kind, bool existingActive, bool candidateActive)
    {
        return kind == AbilityKind.Deploy
            ? existingActive && candidateActive
            : existingActive || candidateActive;
    }

    private static bool IsHudAbility(AbilityKind kind)
    {
        return kind is AbilityKind.Deploy
            or AbilityKind.RepairField
            or AbilityKind.ShieldField
            or AbilityKind.Scan;
    }

    private static float AbilityCooldownRemaining(EntityInstance? entity, AbilityKind kind)
    {
        if (entity is null || !entity.Components.TryGet<AbilityRuntimeComponentState>(out var runtime))
        {
            return 0;
        }

        foreach (var cooldown in runtime.Cooldowns)
        {
            if (cooldown.Kind == kind)
            {
                return MathF.Max(0, cooldown.CooldownRemaining);
            }
        }

        return 0;
    }

    private static bool IsAbilityActive(EntityInstance? entity, AbilityKind kind)
    {
        return kind == AbilityKind.Deploy
            && entity is not null
            && entity.Components.TryGet<DeployComponentState>(out var deploy)
            && deploy.IsDeployed;
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

    private void OnBuildKindRequested(string kind, int? constructionProviderId)
    {
        _buildPlacement.SelectBuildKind(kind, constructionProviderId);
    }

    private void OnRallyRequested()
    {
        _selection.ArmRallyCommand();
    }

    private void OnAbilityRequested(AbilityKind ability)
    {
        _selection.ArmAbilityCommand(ability);
    }

    private void RefreshSelectionInfo()
    {
        CollectSelectedUnitInstances(PlayerSlotId.One, _selectedUnitInstanceBuffer);
        var selectedUnitInstances = _selectedUnitInstanceBuffer;
        if (selectedUnitInstances.Count > 0)
        {
            _hud.SetHudContext(true, false, _buildPlacement.IsActive);
            _hud.SetSelectedUnitStance(SelectedUniformStance(selectedUnitInstances), selectedUnitInstances.Count);
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

        var selectedBuildingProjections = _unitBattlefield.SelectedBuildingSelectionProjections(PlayerSlotId.One);
        if (selectedBuildingProjections.Count > 0)
        {
            _hud.SetHudContext(true, true, _buildPlacement.IsActive);
            _hud.SetSelectedUnitStance(null, 0);
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

        _hud.SetHudContext(false, false, _buildPlacement.IsActive);
        _hud.SetSelectedUnitStance(null, 0);
        _hud.SetSelectedCount(0);
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
        var spec = BuildSpecCatalog.For(building.Kind);
        var sellRefund = BuildingSellRefundPreview(spec);

        _hud.SetSelectionInfo(
            building.Label.ToUpperInvariant(),
            $"{PlayerSlotLabel(building.PlayerSlotId)} / {UnitFactionLabel(building.Faction)}",
            GameText.Format("ui.stat.building", health, building.SightRange),
            GameText.Format("ui.detail.building", queue, rally, sellRefund),
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

}
