namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IReadOnlyList<EntityId> SelectedUnitEntityIds(PlayerSlotId playerSlotId)
    {
        var result = new List<EntityId>();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected && unit.Hp > 0)
            {
                result.Add(unit.EntityId);
            }
        }

        result.Sort(CompareEntityIds);
        return result;
    }

    public IReadOnlyList<EntityId> SelectedBuildingEntityIds(PlayerSlotId playerSlotId)
    {
        CollectSelectedBuildingEntityIds(playerSlotId, _selectedBuildingEntityIdBuffer);
        return _selectedBuildingEntityIdBuffer.ToArray();
    }

    public bool TryGetResourceEntityId(ResourceFieldModel field, out EntityId entityId)
    {
        SyncResourceFieldEntity(field);
        return _resourceFieldEntityIds.TryGetValue(field.Id, out entityId);
    }

    public bool TryCreateProductionPayload(
        ProductionKind productionKind,
        PlayerSlotId playerSlotId,
        out PlayerCommandPayload payload,
        out string status)
    {
        payload = PlayerCommandPayload.Empty;
        CollectCandidateProducerIds(productionKind, playerSlotId, _productionCandidateProducerIds);
        var producerId = LeastQueuedProducerId(_productionCandidateProducerIds);
        var designId = producerId is null ? FirstDesignIdFor(productionKind, playerSlotId) : ProductionDesignIdCore(producerId.Value, productionKind);
        var spec = designId is null ? null : UnitDesignCatalog.Spec(designId);
        if (producerId is null || spec is null || !SyncBuildingTargetEntity(producerId.Value))
        {
            status = GameText.Format("production.needProducer", ProducerLabelFor(spec), ProductionLabel(productionKind, spec));
            return false;
        }

        var inventory = ResourceInventory(playerSlotId);
        if (inventory.Credits < spec.Stats.Cost)
        {
            status = GameText.Format("production.needCredits", spec.Stats.Cost, spec.Label, inventory.Credits);
            return false;
        }

        if (BuildingSnapshot(producerId.Value) is not { } producerSnapshot)
        {
            status = GameText.Format("production.needProducer", ProducerLabelFor(spec), ProductionLabel(productionKind, spec));
            return false;
        }

        payload = PlayerCommandPayload.ForSpec(spec.Id, [_buildingTargetEntityIds[producerId.Value]]);
        status = GameText.Format(
            "production.queued",
            spec.Label,
            BuildSpecCatalog.For(producerSnapshot.Kind).Label,
            spec.Stats.Cost,
            Math.Max(0, inventory.Credits - spec.Stats.Cost));
        return true;
    }

    public bool TryCreateProductionDesignPayload(
        string designId,
        PlayerSlotId playerSlotId,
        out PlayerCommandPayload payload,
        out string status)
    {
        payload = PlayerCommandPayload.Empty;
        UnitSpec spec;
        try
        {
            spec = UnitDesignCatalog.Spec(designId);
        }
        catch (InvalidOperationException)
        {
            status = GameText.T("ui.producerUnavailable");
            return false;
        }

        if (spec.Production is null)
        {
            status = GameText.T("ui.producerUnavailable");
            return false;
        }

        CollectCandidateProducerIds(spec, playerSlotId, _productionCandidateProducerIds);
        var producerId = LeastQueuedProducerId(_productionCandidateProducerIds);
        if (producerId is null || !SyncBuildingTargetEntity(producerId.Value))
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(spec.Production.ProducerKind).Label, spec.Label);
            return false;
        }

        var inventory = ResourceInventory(playerSlotId);
        if (inventory.Credits < spec.Stats.Cost)
        {
            status = GameText.Format("production.needCredits", spec.Stats.Cost, spec.Label, inventory.Credits);
            return false;
        }

        if (BuildingSnapshot(producerId.Value) is not { } producerSnapshot)
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(spec.Production.ProducerKind).Label, spec.Label);
            return false;
        }

        payload = PlayerCommandPayload.ForSpec(spec.Id, [_buildingTargetEntityIds[producerId.Value]]);
        status = GameText.Format(
            "production.queued",
            spec.Label,
            BuildSpecCatalog.For(producerSnapshot.Kind).Label,
            spec.Stats.Cost,
            Math.Max(0, inventory.Credits - spec.Stats.Cost));
        return true;
    }
}
