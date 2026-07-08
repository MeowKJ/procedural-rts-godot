using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield : ICommandGatewayEntityCommandSink
{
    private readonly CommandGateway _livePlayerCommandGateway = new();
    private readonly Dictionary<PlayerControllerId, int> _livePlayerCommandSequences = [];
    private static readonly PlayerControllerId LocalHumanControllerId = new("local-human");
    private static readonly PlayerSlotId[] LocalHumanControlledSlots = [PlayerSlotId.One];

    public CommandGatewayResult SubmitLiveLocalPlayerCommand(
        PlayerSlotId issuerSlotId,
        PlayerCommandKind kind,
        PlayerCommandPayload payload)
    {
        return SubmitLivePlayerCommand(
            LocalHumanControllerId,
            PlayerControllerKind.LocalHuman,
            LocalHumanControlledSlots,
            issuerSlotId,
            kind,
            payload);
    }

    public CommandGatewayResult SubmitLivePlayerCommand(
        PlayerControllerId controllerId,
        PlayerControllerKind controllerKind,
        IReadOnlyList<PlayerSlotId> controlledSlots,
        PlayerSlotId issuerSlotId,
        PlayerCommandKind kind,
        PlayerCommandPayload payload)
    {
        var sequence = NextLivePlayerCommandSequence(controllerId);
        var command = new PlayerCommand(
            issuerSlotId,
            sequence,
            _inputCommandTick + 1,
            kind,
            payload);
        var submission = new CommandGatewaySubmission(
            controllerId,
            controllerKind,
            controlledSlots,
            _inputCommandTick);
        return _livePlayerCommandGateway.Submit(submission, [command], this);
    }

    public CommandGatewayResult SubmitPlayerController(
        CommandGateway gateway,
        IPlayerController controller,
        PlayerSlotId viewerSlotId,
        int tick)
    {
        var observation = CreateObservationView(viewerSlotId, tick);
        return PlayerControllerGatewayDriver.PollAndSubmit(
            gateway,
            controller,
            new PlayerControllerContext(tick, observation),
            this);
    }

    public bool TryEnqueue(
        PlayerCommand command,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message)
    {
        envelope = null;
        error = CommandGatewayValidationError.None;
        message = string.Empty;

        return command.Kind switch
        {
            PlayerCommandKind.Select => ApplyInputEntityCommand(
                new SetSelectionEntityCommand(OwnerId.FromPlayerSlot(command.IssuerSlotId), command.Payload.SubjectIds, NextInputCommandTick()),
                out envelope),
            PlayerCommandKind.Move => ApplyInputEntityCommand(
                new GroupMoveEntityCommand(OwnerId.FromPlayerSlot(command.IssuerSlotId), command.Payload.SubjectIds, NextInputCommandTick(), ToVector2(command.Payload.TargetPoint), command.Payload.MoveMode, command.Payload.QueueMode),
                out envelope),
            PlayerCommandKind.AttackMove => ApplyInputEntityCommand(
                new GroupMoveEntityCommand(OwnerId.FromPlayerSlot(command.IssuerSlotId), command.Payload.SubjectIds, NextInputCommandTick(), ToVector2(command.Payload.TargetPoint), MoveCommandMode.Attack, command.Payload.QueueMode),
                out envelope),
            PlayerCommandKind.AttackGround => ApplyInputEntityCommand(
                new AttackGroundEntityCommand(OwnerId.FromPlayerSlot(command.IssuerSlotId), command.Payload.SubjectIds, NextInputCommandTick(), ToVector2(command.Payload.TargetPoint)),
                out envelope),
            PlayerCommandKind.Attack => TryApplyTargetedCommand(command, ApplyAttackCommand, out envelope, out error, out message),
            PlayerCommandKind.Stop => ApplyInputEntityCommand(
                new StopEntityCommand(OwnerId.FromPlayerSlot(command.IssuerSlotId), command.Payload.SubjectIds, NextInputCommandTick()),
                out envelope),
            PlayerCommandKind.HoldPosition => ApplyInputEntityCommand(
                new HoldPositionEntityCommand(OwnerId.FromPlayerSlot(command.IssuerSlotId), command.Payload.SubjectIds, NextInputCommandTick()),
                out envelope),
            PlayerCommandKind.Harvest => TryApplyResourceCommand(command, out envelope, out error, out message),
            PlayerCommandKind.Repair => TryApplyTargetedCommand(command, ApplyRepairCommand, out envelope, out error, out message),
            PlayerCommandKind.Rally => TryApplyRallyCommand(command, out envelope, out error, out message),
            PlayerCommandKind.Produce => TryApplyProduceCommand(command, out envelope, out error, out message),
            PlayerCommandKind.Build => TryApplyBuildCommand(command, out envelope, out error, out message),
            PlayerCommandKind.Ability => TryApplyAbilityCommand(command, out envelope, out error, out message),
            PlayerCommandKind.SetStance => ApplyInputEntityCommand(
                new SetStanceEntityCommand(OwnerId.FromPlayerSlot(command.IssuerSlotId), command.Payload.SubjectIds, NextInputCommandTick(), command.Payload.Stance),
                out envelope),
            _ => RejectGatewaySink(CommandGatewayValidationError.InvalidCommandKind, "Live battlefield sink does not support this command kind.", out error, out message),
        };
    }

    private int NextLivePlayerCommandSequence(PlayerControllerId controllerId)
    {
        var next = _livePlayerCommandSequences.GetValueOrDefault(controllerId) + 1;
        _livePlayerCommandSequences[controllerId] = next;
        return next;
    }

    private bool TryApplyTargetedCommand(
        PlayerCommand command,
        Func<PlayerCommand, EntityCommand> create,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message)
    {
        if (!_entityWorld.TryGet(command.Payload.TargetEntity, out _))
        {
            envelope = null;
            return RejectGatewaySink(CommandGatewayValidationError.InvalidTarget, "Target entity is not present in the live battlefield.", out error, out message);
        }

        error = CommandGatewayValidationError.None;
        message = string.Empty;
        return ApplyInputEntityCommand(create(command), out envelope);
    }

    private EntityCommand ApplyAttackCommand(PlayerCommand command)
    {
        return new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(command.IssuerSlotId),
            command.Payload.SubjectIds,
            NextInputCommandTick(),
            command.Payload.TargetEntity,
            command.Payload.TargetKind);
    }

    private EntityCommand ApplyRepairCommand(PlayerCommand command)
    {
        return new RepairEntityCommand(
            OwnerId.FromPlayerSlot(command.IssuerSlotId),
            command.Payload.SubjectIds,
            NextInputCommandTick(),
            command.Payload.TargetEntity);
    }

    private bool TryApplyAbilityCommand(
        PlayerCommand command,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message)
    {
        var targetEntity = command.Payload.TargetEntity;
        if (targetEntity.IsValid && !_entityWorld.TryGet(targetEntity, out _))
        {
            envelope = null;
            return RejectGatewaySink(CommandGatewayValidationError.InvalidTarget, "Ability target entity is not present in the live battlefield.", out error, out message);
        }

        if (command.Payload.HasTargetPoint && !command.Payload.TargetPoint.IsFinite)
        {
            envelope = null;
            return RejectGatewaySink(CommandGatewayValidationError.InvalidPayloadShape, "Ability target point must be finite.", out error, out message);
        }

        error = CommandGatewayValidationError.None;
        message = string.Empty;
        var targetPoint = command.Payload.HasTargetPoint ? ToVector2(command.Payload.TargetPoint) : (Vector2?)null;
        return ApplyInputEntityCommand(
            new AbilityEntityCommand(
                OwnerId.FromPlayerSlot(command.IssuerSlotId),
                command.Payload.SubjectIds,
                NextInputCommandTick(),
                command.Payload.Ability,
                targetEntity,
                targetPoint),
            out envelope);
    }

    private bool TryApplyResourceCommand(
        PlayerCommand command,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message)
    {
        if (!_entityWorld.TryGet(command.Payload.TargetEntity, out var resource)
            || !resource.Components.Has<ResourceNodeComponentState>())
        {
            envelope = null;
            return RejectGatewaySink(CommandGatewayValidationError.InvalidTarget, "Harvest target must be a live resource entity.", out error, out message);
        }

        error = CommandGatewayValidationError.None;
        message = string.Empty;
        return ApplyInputEntityCommand(
            new HarvestEntityCommand(
                OwnerId.FromPlayerSlot(command.IssuerSlotId),
                command.Payload.SubjectIds,
                NextInputCommandTick(),
                command.Payload.TargetEntity),
            out envelope);
    }

    private bool TryApplyRallyCommand(
        PlayerCommand command,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message)
    {
        var targetEntity = command.Payload.TargetEntity;
        if (targetEntity.IsValid)
        {
            if (!_entityWorld.TryGet(targetEntity, out var target))
            {
                envelope = null;
                return RejectGatewaySink(CommandGatewayValidationError.InvalidTarget, "Rally target entity is not present in the live battlefield.", out error, out message);
            }

            if (!target.Components.Has<ResourceNodeComponentState>()
                && _entityWorld.Relations.Relation(OwnerId.FromPlayerSlot(command.IssuerSlotId), target.OwnerId) is not (PlayerRelation.Self or PlayerRelation.Allied))
            {
                envelope = null;
                return RejectGatewaySink(CommandGatewayValidationError.InvalidTarget, "Rally target entity must be friendly or a resource.", out error, out message);
            }
        }

        error = CommandGatewayValidationError.None;
        message = string.Empty;
        return ApplyProductionEntityCommand(
            new SetRallyPointEntityCommand(
                OwnerId.FromPlayerSlot(command.IssuerSlotId),
                command.Payload.SubjectIds,
                NextInputCommandTick(),
                ToVector2(command.Payload.TargetPoint),
                targetEntity),
            out envelope);
    }

    private bool TryApplyProduceCommand(
        PlayerCommand command,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message)
    {
        if (!TryGetProductionSpec(command.Payload.SpecId, out _))
        {
            envelope = null;
            return RejectGatewaySink(CommandGatewayValidationError.InvalidSpecId, "Produce command requires a registered production spec.", out error, out message);
        }

        error = CommandGatewayValidationError.None;
        message = string.Empty;
        return ApplyProductionEntityCommand(
            new ProduceEntityCommand(
                OwnerId.FromPlayerSlot(command.IssuerSlotId),
                command.Payload.SubjectIds,
                NextInputCommandTick(),
                command.Payload.SpecId),
            out envelope);
    }

    private bool TryApplyBuildCommand(
        PlayerCommand command,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message)
    {
        if (!TryGetBuildSpec(command.Payload.SpecId, out var spec))
        {
            envelope = null;
            return RejectGatewaySink(CommandGatewayValidationError.InvalidSpecId, "Build command requires a registered building spec.", out error, out message);
        }

        var entityCommand = new StartConstructionEntityCommand(
            OwnerId.FromPlayerSlot(command.IssuerSlotId),
            ConstructionSubjectEntities(command.IssuerSlotId, spec),
            NextInputCommandTick(),
            command.Payload.SpecId,
            ClampInsideWorld(ToVector2(command.Payload.TargetPoint), MathF.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f + 8));

        error = CommandGatewayValidationError.None;
        message = string.Empty;
        return ApplyConstructionEntityCommand(entityCommand, out envelope);
    }

    private static bool TryGetProductionSpec(string specId, out UnitSpec spec)
    {
        try
        {
            spec = UnitDesignCatalog.Spec(specId);
            return spec.Production is not null;
        }
        catch (InvalidOperationException)
        {
            spec = null!;
            return false;
        }
    }

    private static bool TryGetBuildSpec(string specId, out BuildSpec spec)
    {
        try
        {
            spec = BuildSpecCatalog.For(specId);
            return true;
        }
        catch (InvalidOperationException)
        {
            spec = null!;
            return false;
        }
    }

    private bool ApplyInputEntityCommand(EntityCommand command, out SequencedCommandEnvelope? envelope)
    {
        envelope = new SequencedCommandEnvelope(AppliedInputCommandCount + 1, command);
        SubmitAndApplyInputCommand(command);
        return true;
    }

    private bool ApplyProductionEntityCommand(EntityCommand command, out SequencedCommandEnvelope? envelope)
    {
        envelope = new SequencedCommandEnvelope(AppliedInputCommandCount + 1, command);
        SubmitProductionCommand(command);
        return true;
    }

    private bool ApplyConstructionEntityCommand(EntityCommand command, out SequencedCommandEnvelope? envelope)
    {
        envelope = new SequencedCommandEnvelope(AppliedInputCommandCount + 1, command);
        SubmitConstructionCommand(command);
        return true;
    }

    private static Vector2 ToVector2(PlayerCommandPoint point)
    {
        return new Vector2(point.X, point.Y);
    }

    private static bool RejectGatewaySink(
        CommandGatewayValidationError validationError,
        string validationMessage,
        out CommandGatewayValidationError error,
        out string message)
    {
        error = validationError;
        message = validationMessage;
        return false;
    }
}
