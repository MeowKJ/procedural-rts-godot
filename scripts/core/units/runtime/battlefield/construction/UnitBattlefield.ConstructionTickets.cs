using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public UnitBattlefieldConstructionTicketSnapshot? QueueConstructionTicket(
        PlayerSlotId playerSlotId,
        string kind,
        out string status)
    {
        return QueueConstructionTicket(playerSlotId, kind, constructionProviderId: null, out status);
    }

    public UnitBattlefieldConstructionTicketSnapshot? QueueConstructionTicket(
        PlayerSlotId playerSlotId,
        string kind,
        int? constructionProviderId,
        out string status)
    {
        var owner = OwnerId.FromPlayerSlot(playerSlotId);
        var spec = BuildSpecCatalog.For(kind);
        SyncOwnerRelations();
        var subjects = ConstructionSubjectEntities(playerSlotId, spec, constructionProviderId);
        if (constructionProviderId is not null && spec.RequiredProducer is not null && subjects.Count == 0)
        {
            status = "placement.missingProducer";
            NotifyCreditsChanged(playerSlotId);
            return null;
        }

        CollectEntityIds(_constructionEntityIdsBefore);
        var command = new QueueConstructionEntityCommand(
            owner,
            subjects,
            NextInputCommandTick(),
            kind);
        SubmitConstructionCommand(command);

        var rejection = DrainConstructionRejection(command.Tick, owner, kind);
        if (rejection is not null)
        {
            status = rejection.Reason;
            NotifyCreditsChanged(playerSlotId);
            return null;
        }

        var ticket = LastNewConstructionTicket(playerSlotId, kind, _constructionEntityIdsBefore);
        if (!ticket.EntityId.IsValid)
        {
            status = "placement.queueRejected";
            NotifyCreditsChanged(playerSlotId);
            return null;
        }

        status = $"queued.{kind}";
        NotifyCreditsChanged(playerSlotId);
        return ticket;
    }

    public IReadOnlyList<UnitBattlefieldConstructionTicketSnapshot> ReadyConstructionTickets(PlayerSlotId playerSlotId)
    {
        CollectReadyConstructionTickets(playerSlotId, includeQueued: false, _constructionTicketBuffer);
        return _constructionTicketBuffer;
    }

    public bool PlaceReadyConstructionTicket(
        PlayerSlotId playerSlotId,
        UnitFactionId faction,
        EntityId ticketId,
        Vector2 position,
        out UnitBattlefieldBuildingSnapshot? building,
        out string status,
        float facing = 0)
    {
        building = null;
        if (ConstructionTicketSnapshot(ticketId, playerSlotId) is not { } ticket)
        {
            status = "placement.invalidReadyTicket";
            return false;
        }

        var owner = OwnerId.FromPlayerSlot(playerSlotId);
        var spec = BuildSpecCatalog.For(ticket.Kind);
        SyncOwnerRelations();
        CollectEntityIds(_constructionEntityIdsBefore);
        var command = new StartConstructionEntityCommand(
            owner,
            ConstructionSubjectEntities(playerSlotId, spec),
            NextInputCommandTick(),
            ticket.Kind,
            position,
            facing,
            ticketId);
        SubmitConstructionCommand(command);

        var rejection = DrainConstructionRejection(command.Tick, owner, ticket.Kind);
        if (rejection is not null)
        {
            status = rejection.Reason;
            NotifyCreditsChanged(playerSlotId);
            return false;
        }

        var entity = LastNewConstructedEntity(owner, ticket.Kind, _constructionEntityIdsBefore);
        if (entity is null)
        {
            status = "placement.rejected";
            NotifyCreditsChanged(playerSlotId);
            return false;
        }

        if (entity.Components.TryGet<PowerComponentState>(out var power))
        {
            entity.Components.Set(power with { Powered = true });
        }

        var adoptedId = AdoptConstructedBuildingId(entity, ticket.Kind, playerSlotId, faction);
        building = RequiredBuildingSnapshot(adoptedId);
        status = GameText.Format("build.placed", spec.Label);
        NotifyCreditsChanged(playerSlotId);
        return true;
    }

    public bool CancelConstructionTicket(PlayerSlotId playerSlotId, EntityId ticketId, out string status)
    {
        if (ConstructionTicketSnapshot(ticketId, playerSlotId) is not { } ticket)
        {
            status = GameText.T("build.noTicket");
            return false;
        }

        var owner = OwnerId.FromPlayerSlot(playerSlotId);
        SyncOwnerRelations();
        _constructionSubjectEntityBuffer.Clear();
        _constructionSubjectEntityBuffer.Add(ticketId);
        var command = new CancelConstructionEntityCommand(
            owner,
            _constructionSubjectEntityBuffer,
            NextInputCommandTick());
        SubmitConstructionCommand(command);

        if (DrainConstructionCancellation(command.Tick, owner, ticketId) is not { } cancellation)
        {
            status = GameText.T("build.noTicket");
            NotifyCreditsChanged(playerSlotId);
            return false;
        }

        _entityWorld.FlushQueuedRemovals();
        status = GameText.Format(
            "build.ticketCancelled",
            BuildSpecCatalog.For(ticket.Kind).Label,
            cancellation.Refund);
        NotifyCreditsChanged(playerSlotId);
        return true;
    }

    private void CollectReadyConstructionTickets(
        PlayerSlotId playerSlotId,
        bool includeQueued,
        List<UnitBattlefieldConstructionTicketSnapshot> result)
    {
        result.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (ConstructionTicketSnapshot(entity, playerSlotId) is { } ticket
                && (includeQueued || ticket.ReadyToPlace))
            {
                result.Add(ticket);
            }
        }
    }

    private UnitBattlefieldConstructionTicketSnapshot LastNewConstructionTicket(
        PlayerSlotId playerSlotId,
        string kind,
        HashSet<int> before)
    {
        CollectReadyConstructionTickets(playerSlotId, includeQueued: true, _constructionTicketBuffer);
        var found = default(UnitBattlefieldConstructionTicketSnapshot);
        foreach (var ticket in _constructionTicketBuffer)
        {
            if (!before.Contains(ticket.EntityId.Value) && ticket.Kind == kind)
            {
                found = ticket;
            }
        }

        return found;
    }

    private EntityInstance? LastNewConstructedEntity(OwnerId owner, string kind, HashSet<int> before)
    {
        EntityInstance? found = null;
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!before.Contains(entity.Id.Value)
                && entity.OwnerId == owner
                && EntityBuildingSpecId(entity) == kind)
            {
                found = entity;
            }
        }

        return found;
    }

    private void CollectEntityIds(HashSet<int> result)
    {
        result.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            result.Add(entity.Id.Value);
        }
    }

    private UnitBattlefieldConstructionTicketSnapshot? ConstructionTicketSnapshot(
        EntityId ticketId,
        PlayerSlotId playerSlotId)
    {
        return _entityWorld.TryGet(ticketId, out var entity)
            ? ConstructionTicketSnapshot(entity, playerSlotId)
            : null;
    }

    private UnitBattlefieldConstructionTicketSnapshot? ConstructionTicketSnapshot(
        EntityInstance entity,
        PlayerSlotId playerSlotId)
    {
        if (entity.OwnerId.ToPlayerSlot() != playerSlotId
            || !entity.Components.TryGet<ConstructionComponentState>(out var construction)
            || construction.Phase is not (ConstructionPhase.Queued or ConstructionPhase.ReadyToPlace)
            || EntityBuildingSpecId(entity) is not { } kind)
        {
            return null;
        }

        return new UnitBattlefieldConstructionTicketSnapshot(
            entity.Id,
            kind,
            playerSlotId,
            entity.Transform.Position,
            construction.Progress,
            construction.ReadyToPlace,
            construction.Cost);
    }

    private ConstructionRejectedEvent? DrainConstructionRejection(int tick, OwnerId owner, string kind)
    {
        _entityWorld.Events.DrainInto(_simEventDrainBuffer);
        for (var index = _simEventDrainBuffer.Count - 1; index >= 0; index--)
        {
            if (_simEventDrainBuffer[index] is ConstructionRejectedEvent rejection
                && rejection.Tick == tick
                && rejection.Owner == owner
                && rejection.BuildingSpecId == kind)
            {
                _simEventDrainBuffer.Clear();
                return rejection;
            }
        }

        _simEventDrainBuffer.Clear();
        return null;
    }

    private ConstructionCancelledEvent? DrainConstructionCancellation(int tick, OwnerId owner, EntityId ticketId)
    {
        _entityWorld.Events.DrainInto(_simEventDrainBuffer);
        for (var index = _simEventDrainBuffer.Count - 1; index >= 0; index--)
        {
            if (_simEventDrainBuffer[index] is ConstructionCancelledEvent cancellation
                && cancellation.Tick == tick
                && cancellation.Owner == owner
                && cancellation.Entity == ticketId)
            {
                _simEventDrainBuffer.Clear();
                return cancellation;
            }
        }

        _simEventDrainBuffer.Clear();
        return null;
    }

    private void NotifyCreditsChanged(PlayerSlotId playerSlotId)
    {
        ResourceInventoryChanged?.Invoke(playerSlotId, ResourceInventory(playerSlotId));
    }
}
