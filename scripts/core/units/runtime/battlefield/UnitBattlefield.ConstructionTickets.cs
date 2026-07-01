using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public UnitBattlefieldConstructionTicketSnapshot? QueueConstructionTicket(
        PlayerSlotId playerSlotId,
        string kind,
        out string status)
    {
        var owner = OwnerId.FromPlayerSlot(playerSlotId);
        var spec = BuildSpecCatalog.For(kind);
        SyncOwnerRelations();
        SyncBuildingTargetEntities();
        _entityWorld.WorldWidth = WorldSize.X;
        _entityWorld.WorldHeight = WorldSize.Y;
        _entityWorld.ResourceInventory(owner).Credits = Credits(playerSlotId);

        var before = _entityWorld.OrderedEntities.Select(entity => entity.Id.Value).ToHashSet();
        var command = new QueueConstructionEntityCommand(
            owner,
            ConstructionSubjectEntities(playerSlotId, spec),
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

        var ticket = ReadyConstructionTicketsCore(playerSlotId, includeQueued: true)
            .Where(ticket => !before.Contains(ticket.EntityId.Value))
            .Where(ticket => ticket.Kind == kind)
            .OrderBy(ticket => ticket.EntityId.Value)
            .LastOrDefault();
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
        return ReadyConstructionTicketsCore(playerSlotId, includeQueued: false);
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
        SyncBuildingTargetEntities();
        _entityWorld.WorldWidth = WorldSize.X;
        _entityWorld.WorldHeight = WorldSize.Y;
        _entityWorld.ResourceInventory(owner).Credits = Credits(playerSlotId);

        var before = _entityWorld.OrderedEntities.Select(entity => entity.Id.Value).ToHashSet();
        var command = new StartConstructionEntityCommand(
            owner,
            ConstructionSubjectEntities(playerSlotId, spec),
            NextInputCommandTick(),
            ticket.Kind,
            ClampInsideWorld(position, MathF.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f + 8),
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

        var entity = _entityWorld.OrderedEntities
            .Where(entity => !before.Contains(entity.Id.Value))
            .Where(entity => entity.OwnerId == owner)
            .Where(entity => EntityBuildingSpecId(entity) == ticket.Kind)
            .OrderBy(entity => entity.Id.Value)
            .LastOrDefault();
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

    private IReadOnlyList<UnitBattlefieldConstructionTicketSnapshot> ReadyConstructionTicketsCore(
        PlayerSlotId playerSlotId,
        bool includeQueued)
    {
        return _entityWorld.OrderedEntities
            .Select(entity => ConstructionTicketSnapshot(entity, playerSlotId))
            .Where(ticket => ticket is not null)
            .Select(ticket => ticket!.Value)
            .Where(ticket => includeQueued || ticket.ReadyToPlace)
            .OrderBy(ticket => ticket.EntityId.Value)
            .ToArray();
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
        return _entityWorld.Events.Drain()
            .OfType<ConstructionRejectedEvent>()
            .LastOrDefault(rejection => rejection.Tick == tick
                && rejection.Owner == owner
                && rejection.BuildingSpecId == kind);
    }

    private void NotifyCreditsChanged(PlayerSlotId playerSlotId)
    {
        SyncCreditsFromEntityWorld(playerSlotId);
        ResourceInventoryChanged?.Invoke(playerSlotId, ResourceInventory(playerSlotId));
    }
}
