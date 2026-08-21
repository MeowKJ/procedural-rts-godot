using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public bool SetRallyPoint(int buildingId, Vector2 target, out string status)
    {
        return SetRallyPoint(buildingId, target, default, out status);
    }

    public bool SetRallyPoint(int buildingId, UnitBattlefieldResourceNodeProjection resource, out string status)
    {
        return SetRallyPoint(buildingId, resource.Position, resource.EntityId, out status);
    }

    public bool SetRallyPoint(int buildingId, UnitInstance unit, out string status)
    {
        if (BuildingIdentity(buildingId)?.PlayerSlotId is not { } playerSlotId
            || Relations.Relation(playerSlotId, unit.PlayerSlotId) is not (PlayerRelation.Self or PlayerRelation.Allied)
            || !_entityWorld.TryGet(unit.EntityId, out _))
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        return SetRallyPoint(buildingId, unit.Position, unit.EntityId, out status);
    }

    private bool SetRallyPoint(int buildingId, Vector2 target, EntityId rallyTargetEntity, out string status)
    {
        if (BuildingEntityByTargetId(buildingId) is not { } buildingEntity
            || !buildingEntity.Components.TryGet<BuildingIdentityComponentState>(out var identity))
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        if (!HasAnyProductionForCore(buildingId))
        {
            status = GameText.T("rally.unsupported");
            return false;
        }

        target = ClampInsideWorld(target, 80);
        SetBuildingRallyPulseCore(buildingId, 1);
        SubmitProductionCommand(new SetRallyPointEntityCommand(
            OwnerId.FromPlayerSlot(identity.PlayerSlotId),
            [buildingEntity.Id],
            NextInputCommandTick(),
            target,
            rallyTargetEntity));
        SetBuildingRallyPulseCore(buildingId, 1);
        status = GameText.T("rally.set");
        return true;
    }

    public bool SetSelectedBuildingRallyPoints(PlayerSlotId playerSlotId, Vector2 target, out string status)
    {
        var hasSelected = CollectSelectedBuildingRallyProducerIds(playerSlotId, _selectedBuildingRallyProducerIds);
        if (!hasSelected)
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        if (_selectedBuildingRallyProducerIds.Count == 0)
        {
            status = GameText.T("rally.unsupported");
            return false;
        }

        var clamped = ClampInsideWorld(target, 80);
        foreach (var producerId in _selectedBuildingRallyProducerIds)
        {
            SetRallyPoint(producerId, clamped, out _);
        }

        status = _selectedBuildingRallyProducerIds.Count == 1
            ? GameText.Format("rally.singleSet", BuildSpecCatalog.For(BuildingIdentity(_selectedBuildingRallyProducerIds[0])!.Kind).Label)
            : GameText.Format("rally.multiSet", _selectedBuildingRallyProducerIds.Count);
        return true;
    }

    public bool SetSelectedBuildingRallyPoints(PlayerSlotId playerSlotId, UnitBattlefieldResourceNodeProjection resource, out string status)
    {
        var hasSelected = CollectSelectedBuildingRallyProducerIds(playerSlotId, _selectedBuildingRallyProducerIds);
        if (!hasSelected)
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        if (_selectedBuildingRallyProducerIds.Count == 0)
        {
            status = GameText.T("rally.unsupported");
            return false;
        }

        var clamped = ClampInsideWorld(resource.Position, 80);
        foreach (var producerId in _selectedBuildingRallyProducerIds)
        {
            SetRallyPoint(producerId, clamped, resource.EntityId, out _);
        }

        status = _selectedBuildingRallyProducerIds.Count == 1
            ? GameText.Format("rally.singleSet", BuildSpecCatalog.For(BuildingIdentity(_selectedBuildingRallyProducerIds[0])!.Kind).Label)
            : GameText.Format("rally.multiSet", _selectedBuildingRallyProducerIds.Count);
        return true;
    }

    public bool SetSelectedBuildingRallyPoints(PlayerSlotId playerSlotId, UnitInstance unit, out string status)
    {
        if (Relations.Relation(playerSlotId, unit.PlayerSlotId) is not (PlayerRelation.Self or PlayerRelation.Allied)
            || !_entityWorld.TryGet(unit.EntityId, out _))
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        var hasSelected = CollectSelectedBuildingRallyProducerIds(playerSlotId, _selectedBuildingRallyProducerIds);
        if (!hasSelected)
        {
            status = GameText.T("rally.selectProducer");
            return false;
        }

        if (_selectedBuildingRallyProducerIds.Count == 0)
        {
            status = GameText.T("rally.unsupported");
            return false;
        }

        var clamped = ClampInsideWorld(unit.Position, 80);
        foreach (var producerId in _selectedBuildingRallyProducerIds)
        {
            SetRallyPoint(producerId, clamped, unit.EntityId, out _);
        }

        status = _selectedBuildingRallyProducerIds.Count == 1
            ? GameText.Format("rally.singleSet", BuildSpecCatalog.For(BuildingIdentity(_selectedBuildingRallyProducerIds[0])!.Kind).Label)
            : GameText.Format("rally.multiSet", _selectedBuildingRallyProducerIds.Count);
        return true;
    }

    public bool EnqueueProductionDesign(string designId, PlayerSlotId playerSlotId, out string status)
    {
        return CommandEnqueueProductionDesign(designId, playerSlotId, out status);
    }

    public bool ToggleRepeatProductionForProvider(
        string designId,
        PlayerSlotId playerSlotId,
        int producerBuildingId,
        out string status)
    {
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

        if (spec.Production is null
            || !ProducerSupportsSpec(producerBuildingId, playerSlotId, spec)
            || BuildingEntityByTargetId(producerBuildingId) is null)
        {
            status = GameText.Format(
                "production.needProducer",
                spec.Production is null ? GameText.T("ui.providerLane.specificFallback") : BuildSpecCatalog.For(spec.Production.ProducerKind).Label,
                spec.Label);
            return false;
        }

        if (BuildingSnapshot(producerBuildingId) is not { } producerSnapshot)
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(spec.Production.ProducerKind).Label, spec.Label);
            return false;
        }

        var currentRepeat = BuildingProductionRepeatOutputSpecId(producerBuildingId);
        var enable = !string.Equals(currentRepeat, spec.Id, StringComparison.Ordinal);
        if (enable && !ProducerCanQueueSpec(producerBuildingId, playerSlotId, spec))
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(spec.Production.ProducerKind).Label, spec.Label);
            return false;
        }

        SubmitProductionCommand(new SetRepeatProductionEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            [_buildingTargetEntityIds[producerBuildingId]],
            NextInputCommandTick(),
            enable,
            enable ? spec.Id : ""));

        var producerLabel = BuildSpecCatalog.For(producerSnapshot.Kind).Label;
        status = GameText.Format(
            enable ? "production.repeatEnabled" : "production.repeatDisabled",
            spec.Label,
            producerLabel);
        return true;
    }

    public bool CommandEnqueueProductionDesign(string designId, PlayerSlotId playerSlotId, out string status)
    {
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
        if (producerId is null)
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(spec.Production.ProducerKind).Label, spec.Label);
            return false;
        }

        var cost = spec.Stats.Cost;
        var inventory = ResourceInventory(playerSlotId);
        if (inventory.Credits < cost)
        {
            status = GameText.Format("production.needCredits", cost, spec.Label, inventory.Credits);
            return false;
        }

        if (BuildingEntityByTargetId(producerId.Value) is null)
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(spec.Production.ProducerKind).Label, spec.Label);
            return false;
        }

        var queueBefore = BuildingProductionQueue(producerId.Value).Count;
        SubmitProductionCommand(new ProduceEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            [_buildingTargetEntityIds[producerId.Value]],
            NextInputCommandTick(),
            spec.Id));
        var queueAfter = BuildingProductionQueue(producerId.Value);
        if (queueAfter.Count <= queueBefore)
        {
            status = GameText.Format("production.needCredits", cost, spec.Label, Credits(playerSlotId));
            return false;
        }

        var producerSnapshot = BuildingSnapshot(producerId.Value);
        if (producerSnapshot is null)
        {
            status = GameText.Format("production.needProducer", BuildSpecCatalog.For(spec.Production.ProducerKind).Label, spec.Label);
            return false;
        }

        var item = queueAfter[^1];
        ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
        ProductionQueued?.Invoke(producerSnapshot.Value, item);
        status = GameText.Format("production.queued", spec.Label, BuildSpecCatalog.For(producerSnapshot.Value.Kind).Label, cost, Credits(playerSlotId));
        return true;
    }

    private int? LeastQueuedProducerId(IReadOnlyList<int> producerIds)
    {
        int? bestProducerId = null;
        var bestQueueCount = 0;
        foreach (var producerId in producerIds)
        {
            var queueCount = BuildingProductionQueue(producerId).Count;
            if (bestProducerId is not null
                && (queueCount > bestQueueCount || (queueCount == bestQueueCount && producerId >= bestProducerId.Value)))
            {
                continue;
            }

            bestProducerId = producerId;
            bestQueueCount = queueCount;
        }

        return bestProducerId;
    }

}
