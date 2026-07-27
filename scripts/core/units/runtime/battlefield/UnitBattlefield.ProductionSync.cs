namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void UpdateProductionQueues(float dt)
    {
        SyncBuildingTargetEntities();
        CollectActiveProducerIds(_productionActiveProducerIds);
        if (_productionActiveProducerIds.Count == 0)
        {
            return;
        }

        SyncUnitEntities();
        CollectKnownProductionEntityIds(_productionKnownEntityIds);
        CollectProductionCompletionCandidates(_productionActiveProducerIds, _productionCompletionCandidates);

        _productionSystem.Step(new SimContext(
            _entityWorld,
            NextInputCommandTick(),
            dt,
            Array.Empty<SequencedCommandEnvelope>()));

        CollectNewProductionUnitEntities(_productionKnownEntityIds, _productionNewUnitEntities);
        foreach (var entity in _productionNewUnitEntities)
        {
            if (!TryFindCompletedProduction(entity, out var completed))
            {
                continue;
            }

            var unit = AdoptUnitEntity(entity);
            unit.CommandPulse = 1;
            ProductionCompleted?.Invoke(completed.Snapshot, completed.Item, unit);
        }
    }

    private void CollectActiveProducerIds(List<int> result)
    {
        result.Clear();
        _productionBuildingIdSeen.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || !_productionBuildingIdSeen.Add(identity.BuildingId)
                || BuildingProductionQueue(identity.BuildingId).Count == 0)
            {
                continue;
            }

            result.Add(identity.BuildingId);
        }
    }

    private void CollectKnownProductionEntityIds(HashSet<int> result)
    {
        result.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            result.Add(entity.Id.Value);
        }
    }

    private void CollectProductionCompletionCandidates(
        IReadOnlyList<int> activeProducerIds,
        List<ProductionCompletionCandidate> result)
    {
        result.Clear();
        foreach (var buildingId in activeProducerIds)
        {
            if (BuildingSnapshot(buildingId) is not { } snapshot)
            {
                continue;
            }

            result.Add(new ProductionCompletionCandidate(
                buildingId,
                snapshot,
                BuildingProductionQueue(buildingId)[0]));
        }
    }

    private void CollectNewProductionUnitEntities(HashSet<int> knownEntityIds, List<EntityInstance> result)
    {
        result.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (knownEntityIds.Contains(entity.Id.Value)
                || !_entityWorld.TryGetSpec(entity.SpecId, out var spec)
                || spec.Kind != EntityKind.Unit)
            {
                continue;
            }

            result.Add(entity);
        }
    }

    private bool TryFindCompletedProduction(
        EntityInstance entity,
        out ProductionCompletionCandidate completed)
    {
        completed = default;
        var found = false;
        var bestDistance = 0f;
        foreach (var candidate in _productionCompletionCandidates)
        {
            if (candidate.Snapshot.PlayerSlotId != entity.OwnerId.ToPlayerSlot()
                || candidate.Item.DesignId != entity.SpecId)
            {
                continue;
            }

            var distance = candidate.Snapshot.Position.DistanceSquaredTo(entity.Transform.Position);
            if (!found || distance < bestDistance)
            {
                completed = candidate;
                bestDistance = distance;
                found = true;
            }
        }

        return found;
    }
}
