using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    private static string BuildingSellRefundPreview(BuildSpec spec)
    {
        var refund = Mathf.RoundToInt(spec.Cost * Math.Clamp(spec.RefundRatio, 0, 1));
        return GameText.Format("ui.detail.sellRefund", refund);
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
