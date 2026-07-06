using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private void ApplyStartConstruction(EntityWorld world, StartConstructionEntityCommand command)
    {
        var legality = ValidateConstructionStart(world, command);
        if (!legality.IsValid)
        {
            world.Events.Raise(new ConstructionRejectedEvent(
                command.Tick,
                command.Issuer,
                command.BuildingSpecId,
                command.Position,
                legality.Reason));
            return;
        }

        var spec = BuildSpecCatalog.For(command.BuildingSpecId);
        if (command.ReadyTicket.IsValid)
        {
            PlaceReadyTicket(world, command, spec, legality);
            return;
        }

        var inventory = world.ResourceInventory(command.Issuer);
        if (inventory.Credits < spec.Cost)
        {
            world.Events.Raise(new ConstructionRejectedEvent(
                command.Tick,
                command.Issuer,
                command.BuildingSpecId,
                command.Position,
                "placement.needCredits"));
            return;
        }

        inventory.Credits -= spec.Cost;
        world.Spawn(
            spec.ToEntitySpec(),
            command.Issuer,
            EntityTransform.At(new Vector2(legality.X, legality.Y), command.Facing),
            InitialConstructionComponents(spec, command.Facing));
    }

    private static void PlaceReadyTicket(
        EntityWorld world,
        StartConstructionEntityCommand command,
        BuildSpec spec,
        PlacementResult legality)
    {
        if (!TryGetReadyTicket(world, command, spec, out var ticket, out _, out _))
        {
            world.Events.Raise(new ConstructionRejectedEvent(
                command.Tick,
                command.Issuer,
                command.BuildingSpecId,
                command.Position,
                "placement.invalidReadyTicket"));
            return;
        }

        world.Spawn(
            spec.ToEntitySpec(),
            command.Issuer,
            EntityTransform.At(new Vector2(legality.X, legality.Y), command.Facing),
            InitialConstructionComponents(spec, command.Facing, initialProgress: 1));
        world.Remove(ticket.Id);
    }

    private void ApplyQueueConstruction(EntityWorld world, QueueConstructionEntityCommand command)
    {
        var legality = ValidateConstructionQueueStart(world, command);
        if (!legality.IsValid)
        {
            world.Events.Raise(new ConstructionRejectedEvent(
                command.Tick,
                command.Issuer,
                command.BuildingSpecId,
                Vector2.Zero,
                legality.Reason));
            return;
        }

        var spec = BuildSpecCatalog.For(command.BuildingSpecId);
        var inventory = world.ResourceInventory(command.Issuer);
        if (inventory.Credits < spec.Cost)
        {
            world.Events.Raise(new ConstructionRejectedEvent(
                command.Tick,
                command.Issuer,
                command.BuildingSpecId,
                Vector2.Zero,
                "placement.needCredits"));
            return;
        }

        inventory.Credits -= spec.Cost;
        world.Spawn(
            QueuedConstructionSpec(spec),
            command.Issuer,
            EntityTransform.At(QueueTicketPosition(world, command)),
            QueuedConstructionComponents(spec));
    }

    private void ApplyCancelConstruction(EntityWorld world, CancelConstructionEntityCommand command)
    {
        CollectOrderedSubjects(command.Subjects, _constructionSubjectOrder);
        foreach (var entityId in _constructionSubjectOrder)
        {
            if (!world.TryGet(entityId, out var entity)
                || entity.OwnerId.Value != command.Issuer.Value
                || !entity.Components.TryGet<ConstructionComponentState>(out var construction)
                || (construction.Phase == ConstructionPhase.Building && construction.Progress >= 1)
                || BuildingSpecIdFor(world, entity) is not { } BuildingSpecId)
            {
                continue;
            }

            var refund = ConstructionRefund(construction);
            if (refund > 0)
            {
                world.ResourceInventory(command.Issuer).Credits += refund;
            }

            world.Events.Raise(new ConstructionCancelledEvent(
                command.Tick,
                entity.Id,
                command.Issuer,
                BuildingSpecId,
                entity.Transform.Position,
                refund,
                construction.Progress));
            world.QueueRemoval(entity.Id);
        }
    }

    private static int ConstructionRefund(ConstructionComponentState construction)
    {
        var ratio = Math.Clamp(construction.RefundRatio, 0, 1);
        if (construction.Phase is ConstructionPhase.Queued or ConstructionPhase.ReadyToPlace)
        {
            return Mathf.RoundToInt(construction.Cost * ratio);
        }

        var remaining = 1 - Mathf.Clamp(construction.Progress, 0, 1);
        return Mathf.RoundToInt(construction.Cost * ratio * remaining);
    }
}
