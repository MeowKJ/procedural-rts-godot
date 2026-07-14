using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    private BattleRootUnitDeathSpecReadPath UnitDeathSpecReadPathFor(UnitDeathInfo death)
    {
        if (!TryResolveBattleRootUnitSpecReadPath(death.DesignId, death.Owner, death.FactionId, out var style))
        {
            return new BattleRootUnitDeathSpecReadPath(death, HudMint);
        }

        return new BattleRootUnitDeathSpecReadPath(
            death with
            {
                Radius = style.Descriptor.Radius,
                WeightClass = style.Descriptor.WeightClass,
                MovementDomain = style.Descriptor.MovementDomain,
            },
            style.EffectAccent);
    }

    private BattleRootUnitSpecReadPath UnitSpecReadPathFor(UnitModel unit)
    {
        if (TryResolveBattleRootUnitSpecReadPath(unit.DesignId, unit.Owner, unit.FactionId, out var style))
        {
            return style;
        }

        throw new KeyNotFoundException($"Unit '{unit.Id}' does not have a UnitSpec read path for '{unit.DesignId}'.");
    }

    private bool TryResolveBattleRootUnitSpecReadPath(
        string designId,
        ProceduralRts.Core.Owner owner,
        FactionId factionId,
        out BattleRootUnitSpecReadPath style)
    {
        if (!UnitDesignCatalog.Designs.ContainsKey(designId)
            || !UnitDesignDefinitionCatalog.RuntimeDescriptors.TryGetValue(designId, out var descriptor))
        {
            style = default;
            return false;
        }

        var spec = UnitDesignCatalog.Spec(designId);
        var presentation = UnitPresentationCatalog.ForSpec(spec);
        var ownerColor = SoftOldCityPalette.PlayerColor(PlayerSlotForOwner(owner));
        var environmentTone = EnvironmentTonePalette.For(_state.VisualTheme);
        var palette = EntityRenderPalette.SoftOldCity(ownerColor, descriptor.Accent);
        var entityAccent = _state.VisualAccent(owner, factionId, presentation.Accent);
        var effectAccent = palette.Resolve(ColorRole.Effect, environmentTone, EnvironmentResponse.EffectReactive);

        style = new BattleRootUnitSpecReadPath(spec, descriptor, presentation, entityAccent, effectAccent);
        return true;
    }

    private void SetBuildingSelectionInfo(BuildingModel building)
    {
        var spec = BuildSpecCatalog.For(building.Kind);
        var entityAccent = _state.VisualAccent(building.Owner, building.FactionId, spec.Accent);
        var health = $"{Mathf.CeilToInt(building.Hp)}/{Mathf.CeilToInt(spec.MaxHp)}";
        var queue = building.ProductionQueue.Count == 0
            ? GameText.T("ui.queue.empty").ToUpperInvariant()
            : ProductionDetail(building);
        var rally = building.RallyPoint is null ? GameText.T("ui.rally.none") : GameText.T("ui.rally.set");
        var sellRefund = BuildingSellRefundPreview(spec);

        _hud.SetSelectionInfo(
            GameText.T(spec.NameKey).ToUpperInvariant(),
            BuildingAffiliationLabel(building),
            GameText.Format("ui.stat.building", health, spec.SightRange),
            GameText.Format("ui.detail.building", queue, rally, sellRefund),
            "building",
            spec.Icon,
            [],
            entityAccent);
    }

    private static string BuildingSellRefundPreview(BuildSpec spec)
    {
        var refund = Mathf.RoundToInt(spec.Cost * Math.Clamp(spec.RefundRatio, 0, 1));
        return GameText.Format("ui.detail.sellRefund", refund);
    }

    private static string UnitAffiliationLabel(UnitModel unit)
    {
        var owner = unit.Owner == ProceduralRts.Core.Owner.Player
            ? GameText.T("ui.owner.playerUnit")
            : GameText.T("ui.owner.enemyUnit");
        return $"{owner} / {FactionLabel(unit.FactionId)}";
    }

    private static string BuildingAffiliationLabel(BuildingModel building)
    {
        var owner = building.Owner == ProceduralRts.Core.Owner.Player
            ? GameText.T("ui.owner.playerStructure")
            : GameText.T("ui.owner.enemyStructure");
        return $"{owner} / {FactionLabel(building.FactionId)}";
    }

    private static string FactionLabel(FactionId factionId)
    {
        var definition = FactionCatalog.For(factionId);
        return GameText.T(definition.DisplayNameKey);
    }

    private static string UnitFactionLabel(UnitFactionId factionId)
    {
        return factionId switch
        {
            UnitFactionId.Dog => "Dog design",
            UnitFactionId.Cat => "Cat design",
            UnitFactionId.Corruption => "Corruption design",
            _ => factionId.ToString(),
        };
    }

    private static string PlayerSlotLabel(PlayerSlotId playerSlotId)
    {
        return playerSlotId.Value == 1 ? "Player 1" : $"Player {playerSlotId.Value}";
    }

    private static PlayerSlotId PlayerSlotForOwner(ProceduralRts.Core.Owner owner)
    {
        return owner == ProceduralRts.Core.Owner.Player ? PlayerSlotId.One : PlayerSlotId.Two;
    }

    private static ProceduralRts.Core.Owner? OwnerForPlayerSlot(PlayerSlotId playerSlotId)
    {
        if (playerSlotId == PlayerSlotId.One)
        {
            return ProceduralRts.Core.Owner.Player;
        }

        if (playerSlotId == PlayerSlotId.Two)
        {
            return ProceduralRts.Core.Owner.Enemy;
        }

        return null;
    }

    private static Color PlayerSlotAccent(PlayerSlotId playerSlotId)
    {
        return playerSlotId.Value switch
        {
            1 => new Color("#68a6c8"),
            2 => new Color("#c86c68"),
            3 => new Color("#8abf74"),
            4 => new Color("#c5a45d"),
            _ => new Color("#b7ad9c"),
        };
    }

    private static Color UnitFactionAccent(UnitFactionId factionId, PlayerSlotId playerSlotId)
    {
        var faction = factionId switch
        {
            UnitFactionId.Dog => new Color("#64c7c7"),
            UnitFactionId.Cat => new Color("#c98293"),
            UnitFactionId.Corruption => new Color("#9d4259"),
            _ => new Color("#d7b66a"),
        };

        return faction.Lerp(PlayerSlotAccent(playerSlotId), 0.36f);
    }

    private static bool IsEconomyUnit(UnitModel unit)
    {
        return unit.Spec.RoleTags.Contains(UnitRoleTag.Economy);
    }

    private static bool IsHarvestWorker(UnitModel unit)
    {
        var spec = unit.Spec;
        return (spec.RoleTags.Contains(UnitRoleTag.Economy) || spec.RoleTags.Contains(UnitRoleTag.Worker))
            && spec.HasAbility(AbilityKind.Harvest);
    }

    private static float UnitHealthRatioForSelection(UnitModel unit)
    {
        var descriptor = unit.RuntimeDescriptor;
        return descriptor.MaxHp > 0
            ? Mathf.Clamp(unit.Hp / descriptor.MaxHp, 0, 1)
            : 0;
    }

    private readonly record struct BattleRootUnitSpecReadPath(
        UnitSpec Spec,
        UnitSpecRuntimeDescriptor Descriptor,
        UnitSpecPresentationDescriptor Presentation,
        Color EntityAccent,
        Color EffectAccent);

    private readonly record struct BattleRootUnitDeathSpecReadPath(UnitDeathInfo Death, Color EffectAccent);

    private static UnitStance? SelectedUniformStance(IReadOnlyList<UnitModel> selectedUnits)
    {
        if (selectedUnits.Count == 0)
        {
            return null;
        }

        var stance = selectedUnits[0].Stance;
        for (var index = 1; index < selectedUnits.Count; index++)
        {
            if (selectedUnits[index].Stance != stance)
            {
                return null;
            }
        }

        return stance;
    }

    private static UnitStance? SelectedUniformStance(IReadOnlyList<UnitInstance> selectedUnits)
    {
        if (selectedUnits.Count == 0)
        {
            return null;
        }

        var stance = selectedUnits[0].Stance;
        for (var index = 1; index < selectedUnits.Count; index++)
        {
            if (selectedUnits[index].Stance != stance)
            {
                return null;
            }
        }

        return stance;
    }

    private static string HarvestModeLabel(HarvesterMode mode)
    {
        return mode switch
        {
            HarvesterMode.Idle => GameText.T("harvest.idle"),
            HarvesterMode.MovingToField => GameText.T("harvest.toField"),
            HarvesterMode.Gathering => GameText.T("harvest.gather"),
            HarvesterMode.ReturningToRefinery => GameText.T("harvest.return"),
            HarvesterMode.Unloading => GameText.T("harvest.unload"),
            _ => mode.ToString().ToUpperInvariant(),
        };
    }

    private static string ProductionDetail(BuildingModel building)
    {
        var item = building.ProductionQueue[0];
        var spec = UnitDesignCatalog.Spec(item.DesignId);
        var production = spec.Production
            ?? throw new InvalidOperationException($"UnitDesign '{item.DesignId}' must include ProductionSpec.");
        var progress = Mathf.RoundToInt(Mathf.Clamp(item.Progress / production.Duration, 0, 1) * 100);
        return GameText.Format("ui.production.detail", spec.Label.ToUpperInvariant(), progress, building.ProductionQueue.Count);
    }

    private static string ProductionDetail(IReadOnlyList<UnitProductionQueueItem> queue)
    {
        var item = queue[0];
        var spec = UnitDesignCatalog.Spec(item.DesignId);
        var production = spec.Production
            ?? throw new InvalidOperationException($"UnitDesign '{spec.Id}' must include ProductionSpec for queue details.");
        var progress = Mathf.RoundToInt(Mathf.Clamp(item.Progress / production.Duration, 0, 1) * 100);
        return GameText.Format("ui.production.detail", spec.Label.ToUpperInvariant(), progress, queue.Count);
    }
}
