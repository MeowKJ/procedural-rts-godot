using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public bool SetRallyPoint(int buildingId, Vector2 target, out string status)
    {
        return SetRallyPoint(buildingId, target, default, out status);
    }

    public bool SetRallyPoint(int buildingId, ResourceFieldModel field, out string status)
    {
        SyncResourceFieldEntity(field);
        return SetRallyPoint(buildingId, field.Position, _resourceFieldEntityIds[field.Id], out status);
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

    public bool SetSelectedBuildingRallyPoints(PlayerSlotId playerSlotId, ResourceFieldModel field, out string status)
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

        SyncResourceFieldEntity(field);
        var clamped = ClampInsideWorld(field.Position, 80);
        var targetEntity = _resourceFieldEntityIds[field.Id];
        foreach (var producerId in _selectedBuildingRallyProducerIds)
        {
            SetRallyPoint(producerId, clamped, targetEntity, out _);
        }

        status = _selectedBuildingRallyProducerIds.Count == 1
            ? GameText.Format("rally.singleSet", BuildSpecCatalog.For(BuildingIdentity(_selectedBuildingRallyProducerIds[0])!.Kind).Label)
            : GameText.Format("rally.multiSet", _selectedBuildingRallyProducerIds.Count);
        return true;
    }

    public bool EnqueueProduction(ProductionKind productionKind, PlayerSlotId playerSlotId, out string status)
    {
        var producerId = CandidateProducerIds(productionKind, playerSlotId)
            .OrderBy(buildingId => BuildingProductionQueue(buildingId).Count)
            .ThenBy(buildingId => buildingId)
            .Select(buildingId => (int?)buildingId)
            .FirstOrDefault();
        var designId = producerId is null ? FirstDesignIdFor(productionKind, playerSlotId) : ProductionDesignIdCore(producerId.Value, productionKind);
        var spec = designId is null ? null : UnitDesignCatalog.Spec(designId);
        if (producerId is null || spec is null)
        {
            status = GameText.Format("production.needProducer", ProducerLabelFor(spec), ProductionLabel(productionKind, spec));
            return false;
        }

        var cost = spec.Stats.Cost;
        var inventory = ResourceInventory(playerSlotId);
        if (inventory.Credits < cost)
        {
            status = GameText.Format("production.needCredits", cost, spec.Label, inventory.Credits);
            return false;
        }

        if (!SyncBuildingTargetEntity(producerId.Value))
        {
            status = GameText.Format("production.needProducer", ProducerLabelFor(spec), ProductionLabel(productionKind, spec));
            return false;
        }

        var queueBefore = BuildingProductionQueue(producerId.Value).Count;
        SubmitProductionCommand(new ProduceEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            [_buildingTargetEntityIds[producerId.Value]],
            NextInputCommandTick(),
            spec.Id));
        SyncCreditsFromEntityWorld(playerSlotId);
        var queueAfter = BuildingProductionQueue(producerId.Value);
        if (queueAfter.Count <= queueBefore)
        {
            status = GameText.Format("production.needCredits", cost, spec.Label, Credits(playerSlotId));
            return false;
        }

        var producerSnapshot = BuildingSnapshot(producerId.Value);
        if (producerSnapshot is null)
        {
            status = GameText.Format("production.needProducer", ProducerLabelFor(spec), ProductionLabel(productionKind, spec));
            return false;
        }

        var item = queueAfter[^1];
        ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
        ProductionQueued?.Invoke(producerSnapshot.Value, item);
        status = GameText.Format("production.queued", spec.Label, BuildSpecCatalog.For(producerSnapshot.Value.Kind).Label, cost, Credits(playerSlotId));
        return true;
    }

    public bool EnqueueProductionDesign(string designId, PlayerSlotId playerSlotId, out string status)
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

        var producerId = CandidateProducerIds(spec, playerSlotId)
            .OrderBy(buildingId => BuildingProductionQueue(buildingId).Count)
            .ThenBy(buildingId => buildingId)
            .Select(buildingId => (int?)buildingId)
            .FirstOrDefault();
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

        if (!SyncBuildingTargetEntity(producerId.Value))
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
        SyncCreditsFromEntityWorld(playerSlotId);
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

    public bool CancelFirstProduction(PlayerSlotId playerSlotId, out string status)
    {
        var producerId = BuildingTargetIds()
            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)
            .Where(buildingId => BuildingProductionQueue(buildingId).Count > 0)
            .OrderBy(buildingId => BuildingProductionQueue(buildingId)[0].Id)
            .Select(buildingId => (int?)buildingId)
            .FirstOrDefault();
        if (producerId is null)
        {
            status = GameText.T("production.noneQueued");
            return false;
        }

        var item = BuildingProductionQueue(producerId.Value)[0];
        var spec = UnitDesignCatalog.Spec(item.DesignId);
        var refund = Mathf.RoundToInt(spec.Stats.Cost * 0.5f);
        SyncBuildingTargetEntity(producerId.Value);
        SubmitProductionCommand(new CancelProductionEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            [_buildingTargetEntityIds[producerId.Value]],
            NextInputCommandTick()));
        SyncCreditsFromEntityWorld(playerSlotId);
        ResourceInventoryChanged?.Invoke(playerSlotId, ResourceInventory(playerSlotId));
        status = GameText.Format("production.cancelled", spec.Label, refund);
        return true;
    }

    public IReadOnlyList<ProductionOptionState> ProductionOptionStates(PlayerSlotId playerSlotId)
    {
        var credits = Credits(playerSlotId);
        return Enum.GetValues<ProductionKind>()
            .Select(kind =>
            {
                var designId = FirstDesignIdFor(kind, playerSlotId);
                var spec = designId is null ? null : UnitDesignCatalog.Spec(designId);
                var production = spec?.Production;
                var presentation = spec is null ? null : UnitPresentationCatalog.ForProductionSpec(kind, spec);
                var producers = CandidateProducerIds(kind, playerSlotId).ToList();
                var queued = producers.Sum(buildingId => BuildingProductionQueue(buildingId).Count(item => item.Kind == kind));
                var progress = producers
                    .Select(buildingId => BuildingProductionQueue(buildingId).FirstOrDefault())
                    .Where(item => item is not null && item.Kind == kind && spec is not null && spec.Production is not null)
                    .Select(item => Mathf.Clamp(item!.Progress / UnitDesignCatalog.Spec(item.DesignId).Production!.Duration, 0, 1))
                    .DefaultIfEmpty(0)
                    .Max();
                var cost = spec?.Stats.Cost ?? 0;
                var hasProducer = producers.Count > 0;
                var enoughCredits = credits >= cost;
                var disabledReason = hasProducer
                    ? enoughCredits ? "" : "ui.needCredits"
                    : "ui.producerUnavailable";
                return new ProductionOptionState(
                    kind,
                    production?.Category ?? ProductionCategory.Infantry,
                    production?.ProducerKind ?? BuildingDesignIds.Barracks,
                    spec?.Id,
                    presentation?.ShortCode ?? kind.ToString(),
                    presentation?.Icon ?? production?.CategoryIcon ?? IconGlyph.Infantry,
                    presentation?.RoleGlyph ?? spec?.Icon ?? IconGlyph.None,
                    presentation?.Accent ?? new Color("#8fffe1"),
                    cost,
                    production?.Duration ?? 0,
                    hasProducer,
                    enoughCredits,
                    queued,
                    progress,
                    disabledReason);
            })
            .OrderBy(state => state.Category)
            .ThenBy(state => state.Kind)
            .ToList();
    }

    public IReadOnlyList<ProductionOptionState> ProductionDesignOptionStates(PlayerSlotId playerSlotId)
    {
        var credits = Credits(playerSlotId);
        return ProductionDesignSpecs(playerSlotId)
            .Select(spec =>
            {
                var production = spec.Production!;
                var producers = CandidateProducerIds(spec, playerSlotId).ToList();
                var queued = producers.Sum(buildingId => BuildingProductionQueue(buildingId).Count(item => item.DesignId == spec.Id));
                var progress = producers
                    .Select(buildingId => BuildingProductionQueue(buildingId).FirstOrDefault())
                    .Where(item => item is not null && item.DesignId == spec.Id)
                    .Select(item => Mathf.Clamp(item!.Progress / production.Duration, 0, 1))
                    .DefaultIfEmpty(0)
                    .Max();
                var presentation = UnitPresentationCatalog.ForProductionSpec(ProductionKindFor(spec), spec);
                var hasProducer = producers.Count > 0;
                var enoughCredits = credits >= spec.Stats.Cost;
                var disabledReason = hasProducer
                    ? enoughCredits ? "" : "ui.needCredits"
                    : "ui.producerUnavailable";
                return new ProductionOptionState(
                    ProductionKindFor(spec),
                    production.Category,
                    production.ProducerKind,
                    spec.Id,
                    presentation.ShortCode,
                    presentation.Icon,
                    presentation.RoleGlyph,
                    presentation.Accent,
                    spec.Stats.Cost,
                    production.Duration,
                    hasProducer,
                    enoughCredits,
                    queued,
                    progress,
                    disabledReason);
            })
            .OrderBy(state => state.Category)
            .ThenBy(state => UnitDesignCatalog.Spec(state.UnitDesignId!).Stats.TechTier)
            .ThenBy(state => state.ProducerKind)
            .ThenBy(state => UnitDesignCatalog.Spec(state.UnitDesignId!).Production!.LaneIndex)
            .ThenBy(state => state.UnitDesignId)
            .ToList();
    }

    public bool HasQueuedProduction(PlayerSlotId playerSlotId)
    {
        return BuildingTargetIds()
            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)
            .Any(buildingId => BuildingProductionQueue(buildingId).Count > 0);
    }

    public string ProductionQueueSummary(PlayerSlotId playerSlotId)
    {
        var queued = BuildingTargetIds()
            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)
            .SelectMany(buildingId => BuildingProductionQueue(buildingId).Select(item => new
            {
                BuildingId = buildingId,
                Item = item,
            }))
            .OrderBy(entry => entry.Item.Id)
            .ToList();
        if (queued.Count == 0)
        {
            return GameText.T("ui.queue.empty");
        }

        var first = queued[0];
        var spec = UnitDesignCatalog.Spec(first.Item.DesignId);
        var progress = spec.Production is null ? 0 : Mathf.RoundToInt(Mathf.Clamp(first.Item.Progress / spec.Production.Duration, 0, 1) * 100);
        var refund = Mathf.RoundToInt(spec.Stats.Cost * 0.5f);
        return GameText.Format("ui.queue.summary", spec.Label.ToUpperInvariant(), progress, queued.Count, refund);
    }

}
