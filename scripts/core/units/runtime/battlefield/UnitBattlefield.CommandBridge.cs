using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private static bool IsHarvester(UnitInstance unit)
    {
        return unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
            && unit.Spec.HasAbility(AbilityKind.Harvest);
    }

    private static bool IsRepairer(UnitInstance unit)
    {
        return unit.Spec.HasAbility(AbilityKind.RepairField);
    }

    private bool IsRepairableTarget(PlayerSlotId playerSlotId, UnitInstance target)
    {
        return target.Hp > 0
            && target.Hp < target.Spec.Stats.MaxHp
            && Relations.Relation(playerSlotId, target.PlayerSlotId) is PlayerRelation.Self or PlayerRelation.Allied;
    }

    private bool IsRepairableBuildingTargetCore(PlayerSlotId playerSlotId, int buildingId)
    {
        return BuildingSnapshot(buildingId) is { } target
            && target.Hp > 0
            && target.Hp < BuildSpecCatalog.For(target.Kind).MaxHp
            && Relations.Relation(playerSlotId, target.PlayerSlotId) is PlayerRelation.Self or PlayerRelation.Allied;
    }

    private int NextInputCommandTick()
    {
        return ++_inputCommandTick;
    }

    private int SubmitSelectionCommand(PlayerSlotId playerSlotId, IEnumerable<EntityId> selectedEntityIds)
    {
        CollectSelectionCommandEntityIds(selectedEntityIds, _selectionCommandEntityBuffer);
        SubmitAndApplyInputCommand(new SetSelectionEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _selectionCommandEntityBuffer,
            NextInputCommandTick()));
        return SelectedCount(playerSlotId);
    }

    private static void CollectSelectionCommandEntityIds(IEnumerable<EntityId> selectedEntityIds, List<EntityId> result)
    {
        result.Clear();
        foreach (var entityId in selectedEntityIds)
        {
            if (entityId.IsValid && !ContainsEntityId(result, entityId))
            {
                result.Add(entityId);
            }
        }

        result.Sort(CompareEntityIds);
    }

    private static bool ContainsEntityId(IReadOnlyList<EntityId> entityIds, EntityId candidate)
    {
        foreach (var entityId in entityIds)
        {
            if (entityId == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static int CompareEntityIds(EntityId left, EntityId right)
    {
        return left.Value.CompareTo(right.Value);
    }

    private static int CompareBuildingIds(int left, int right)
    {
        return left.CompareTo(right);
    }

    private void SubmitAndApplyInputCommand(EntityCommand command)
    {
        SyncUnitEntities();
        SyncBuildingTargetEntities();
        _inputCommands.Enqueue(command);
        var due = _inputCommands.DrainUpToTick(command.Tick);
        if (due.Count == 0)
        {
            return;
        }

        var context = new SimContext(_entityWorld, command.Tick, 0, due);
        _inputCommandSystem.Step(context);
        _abilitySystem.Step(context);
        _entityWorld.FlushQueuedSpawns();
        _entityWorld.FlushQueuedRemovals();
        SyncUnitRuntimeStateFromEntities();
        AppliedInputCommandCount += due.Count;
        ApplyInputCommandResults(due);
    }

    private void SubmitProductionCommand(EntityCommand command)
    {
        _entityWorld.ResourceInventory(command.Issuer).Credits = Credits(command.Issuer.ToPlayerSlot());
        _productionSystem.Step(new SimContext(
            _entityWorld,
            command.Tick,
            0,
            [new SequencedCommandEnvelope(AppliedInputCommandCount + 1, command)]));
        AppliedInputCommandCount++;
        SyncCreditsFromEntityWorld(command.Issuer.ToPlayerSlot());
    }

    private void SubmitConstructionCommand(EntityCommand command)
    {
        _inputCommands.Enqueue(command);
        var due = _inputCommands.DrainUpToTick(command.Tick);
        if (due.Count == 0)
        {
            return;
        }

        _constructionSystem.Step(new SimContext(_entityWorld, command.Tick, 0, due));
        AppliedInputCommandCount += due.Count;
        SyncCreditsFromEntityWorld(command.Issuer.ToPlayerSlot());
    }

    private void SyncOwnerRelations()
    {
        CollectOwnerRelationSlots(_ownerRelationSlots);
        foreach (var first in _ownerRelationSlots)
        {
            foreach (var second in _ownerRelationSlots)
            {
                if (first == second)
                {
                    continue;
                }

                _entityWorld.Relations.Set(
                    OwnerId.FromPlayerSlot(first),
                    OwnerId.FromPlayerSlot(second),
                    Relations.Relation(first, second));
            }
        }
    }

    private void CollectOwnerRelationSlots(List<PlayerSlotId> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            AddOwnerRelationSlot(result, unit.PlayerSlotId);
        }

        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (entity.Components.TryGet<BuildingIdentityComponentState>(out var identity))
            {
                AddOwnerRelationSlot(result, identity.PlayerSlotId);
            }
        }

        foreach (var slot in ResourceInventories.Keys)
        {
            AddOwnerRelationSlot(result, slot);
        }

        AddOwnerRelationSlot(result, PlayerSlotId.One);
        AddOwnerRelationSlot(result, PlayerSlotId.Two);
        result.Sort(ComparePlayerSlotIds);
    }

    private static void AddOwnerRelationSlot(List<PlayerSlotId> result, PlayerSlotId slot)
    {
        if (slot.Value <= 0 || ContainsPlayerSlotId(result, slot))
        {
            return;
        }

        result.Add(slot);
    }

    private static bool ContainsPlayerSlotId(IReadOnlyList<PlayerSlotId> slots, PlayerSlotId candidate)
    {
        foreach (var slot in slots)
        {
            if (slot == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static int ComparePlayerSlotIds(PlayerSlotId left, PlayerSlotId right)
    {
        return left.Value.CompareTo(right.Value);
    }

    private IReadOnlyList<EntityId> ConstructionSubjectEntities(PlayerSlotId playerSlotId, BuildSpec spec)
    {
        CollectConstructionSubjectEntities(playerSlotId, spec, _constructionSubjectBuildingIds, _constructionSubjectEntityBuffer);
        return _constructionSubjectEntityBuffer;
    }

    private void CollectConstructionSubjectEntities(
        PlayerSlotId playerSlotId,
        BuildSpec spec,
        List<int> buildingIds,
        List<EntityId> result)
    {
        buildingIds.Clear();
        result.Clear();
        if (spec.RequiredProducer is not { } requiredProducer)
        {
            return;
        }

        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is { } building
                && building.PlayerSlotId == playerSlotId
                && building.Kind == requiredProducer
                && building.Hp > 0
                && BuildingBuildProgress(building.Id) >= 1)
            {
                buildingIds.Add(building.Id);
            }
        }

        buildingIds.Sort(CompareBuildingIds);
        foreach (var buildingId in buildingIds)
        {
            SyncBuildingTargetEntity(buildingId);
            result.Add(_buildingTargetEntityIds[buildingId]);
        }
    }

    private string? EntityBuildingSpecId(EntityInstance entity)
    {
        if (entity.Components.TryGet<BuildingIdentityComponentState>(out var buildingIdentity))
        {
            return buildingIdentity.Kind;
        }

        if (entity.Components.TryGet<ConstructionIdentityComponentState>(out var identity))
        {
            return identity.Kind;
        }

        return _entityWorld.TryGetSpec(entity.SpecId, out var spec)
            ? spec.Authoring.BuildingSpecId
            : null;
    }

    private void AdoptUnmappedConstructedBuildings()
    {
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (_buildingTargetIdsByEntityId.ContainsKey(entity.Id)
                || EntityBuildingSpecId(entity) is not { } kind
                || !_entityWorld.TryGetSpec(entity.SpecId, out var spec)
                || spec.Kind is not (EntityKind.Building or EntityKind.Turret))
            {
                continue;
            }

            AdoptConstructedBuildingId(entity, kind, entity.OwnerId.ToPlayerSlot(), FactionForSlot(entity.OwnerId.ToPlayerSlot()));
        }
    }

    private int AdoptConstructedBuildingId(
        EntityInstance entity,
        string kind,
        PlayerSlotId playerSlotId,
        UnitFactionId faction)
    {
        if (BuildingTargetIdByEntityId(entity.Id) is { } existingId)
        {
            if (BuildingIdentity(existingId) is null)
            {
                entity.Components.Set(new BuildingIdentityComponentState(existingId, kind, playerSlotId, faction));
            }

            EnsureProductionQueueComponent(existingId, entity);
            return existingId;
        }

        var spec = BuildSpecCatalog.For(kind);
        var target = new BuildingEntitySeed(
            NextBuildingTargetId(),
            kind,
            playerSlotId,
            faction,
            entity.Transform.Position,
            entity.Transform.Facing,
            entity.Components.TryGet<HealthComponentState>(out var health) ? health.Hp : spec.MaxHp);

        SetBuildingTargetEntityId(target.Id, entity.Id);
        entity.Components.Set(new BuildingIdentityComponentState(
            target.Id,
            target.Kind,
            target.PlayerSlotId,
            target.Faction));
        EnsureProductionQueueComponent(target.Id, entity);
        return target.Id;
    }

    private int NextBuildingTargetId()
    {
        while (BuildingTargetIdInUse(_nextBuildingTargetId))
        {
            _nextBuildingTargetId++;
        }

        return _nextBuildingTargetId++;
    }

    private bool BuildingTargetIdInUse(int buildingId)
    {
        return BuildingIdentity(buildingId) is not null
            || BuildingEntityByTargetId(buildingId) is not null;
    }

    private UnitFactionId FactionForSlot(PlayerSlotId playerSlotId)
    {
        CollectBuildingTargetIds(_buildingTargetIdSecondaryBuffer);
        foreach (var buildingId in _buildingTargetIdSecondaryBuffer)
        {
            if (BuildingIdentity(buildingId) is { PlayerSlotId: var slot } identity
                && slot == playerSlotId)
            {
                return identity.Faction;
            }
        }

        if (Units.FirstOrDefault(unit => unit.PlayerSlotId == playerSlotId) is { } unit)
        {
            return unit.Spec.Faction;
        }

        return playerSlotId == PlayerSlotId.One ? UnitFactionId.Dog : UnitFactionId.Cat;
    }

    private void SyncCreditsFromEntityWorld(PlayerSlotId playerSlotId)
    {
        ResourceInventory(playerSlotId).Credits = _entityWorld.ResourceInventory(OwnerId.FromPlayerSlot(playerSlotId)).Credits;
    }

    private void ApplyInputCommandResults(IReadOnlyList<SequencedCommandEnvelope> commands)
    {
        foreach (var envelope in commands)
        {
            if (envelope.Command is SetSelectionEntityCommand selection)
            {
                ApplySelectionCommandStateToUnits(selection);
                continue;
            }

            foreach (var entityId in envelope.Command.Subjects)
            {
                var unit = UnitByEntityId(entityId);
                if (unit is null || !_entityWorld.TryGet(entityId, out var entity))
                {
                    continue;
                }

                ApplyEntityCommandStateToUnit(unit, entity, envelope.Command);
            }
        }
    }

}
