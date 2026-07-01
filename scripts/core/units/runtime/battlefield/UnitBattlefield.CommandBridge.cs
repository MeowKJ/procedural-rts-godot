using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private static bool IsHarvester(UnitInstance unit)
    {
        return unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
            && unit.Spec.Abilities.Any(ability => ability.Kind == AbilityKind.Harvest);
    }

    private static bool IsRepairer(UnitInstance unit)
    {
        return unit.Spec.Abilities.Any(ability => ability.Kind == AbilityKind.RepairField);
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
        SubmitAndApplyInputCommand(new SetSelectionEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            selectedEntityIds
                .Where(id => id.IsValid)
                .Distinct()
                .OrderBy(id => id.Value)
                .ToList(),
            NextInputCommandTick()));
        return SelectedCount(playerSlotId);
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

        _inputCommandSystem.Step(new SimContext(_entityWorld, command.Tick, 0, due));
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
        var slots = Units
            .Select(unit => unit.PlayerSlotId)
            .Concat(BuildingTargetIds()
                .Select(BuildingIdentity)
                .Where(identity => identity is not null)
                .Select(identity => identity!.PlayerSlotId))
            .Concat(ResourceInventories.Keys)
            .Concat([PlayerSlotId.One, PlayerSlotId.Two])
            .Where(slot => slot.Value > 0)
            .Distinct()
            .OrderBy(slot => slot.Value)
            .ToList();

        foreach (var first in slots)
        {
            foreach (var second in slots)
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

    private IReadOnlyList<EntityId> ConstructionSubjectEntities(PlayerSlotId playerSlotId, BuildSpec spec)
    {
        if (spec.RequiredProducer is not { } requiredProducer)
        {
            return [];
        }

        return BuildingTargetIds()
            .Select(BuildingSnapshot)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!.Value)
            .Where(building => building.PlayerSlotId == playerSlotId)
            .Where(building => building.Kind == requiredProducer)
            .Where(building => building.Hp > 0 && BuildingBuildProgress(building.Id) >= 1)
            .OrderBy(building => building.Id)
            .Select(building =>
            {
                SyncBuildingTargetEntity(building.Id);
                return _buildingTargetEntityIds[building.Id];
            })
            .ToList();
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
        if (BuildingTargetIds()
            .Select(BuildingIdentity)
            .FirstOrDefault(identity => identity?.PlayerSlotId == playerSlotId) is { } identity)
        {
            return identity.Faction;
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
