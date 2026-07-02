namespace ProceduralRts.Core;

public sealed record CommandGatewayOptions(
    bool SandboxCommandsEnabled = false,
    int MaxSubjectsPerCommand = 256,
    int MaxSpecIdLength = 128);

public readonly record struct CommandGatewaySubmission(
    PlayerControllerId ControllerId,
    PlayerControllerKind ControllerKind,
    IReadOnlyList<PlayerSlotId> ControlledSlots,
    int CurrentTick);

public interface ICommandGatewayEntityCommandSink
{
    bool TryEnqueue(
        PlayerCommand command,
        out SequencedCommandEnvelope? envelope,
        out CommandGatewayValidationError error,
        out string message);
}

public sealed partial class CommandGateway
{
    private readonly CommandGatewayOptions _options;
    private readonly Dictionary<PlayerControllerId, int> _lastSequenceByController = [];

    public CommandGateway(CommandGatewayOptions? options = null)
    {
        _options = options ?? new CommandGatewayOptions();
    }

    public CommandGatewayResult Submit(
        CommandGatewaySubmission submission,
        IReadOnlyList<PlayerCommand> commands,
        ICommandGatewayEntityCommandSink? entityCommandSink = null)
    {
        var results = new List<PlayerCommandResult>(commands.Count);
        foreach (var command in commands)
        {
            results.Add(SubmitOne(submission, command, entityCommandSink));
        }

        return new CommandGatewayResult(results);
    }

    private PlayerCommandResult SubmitOne(
        CommandGatewaySubmission submission,
        PlayerCommand command,
        ICommandGatewayEntityCommandSink? entityCommandSink)
    {
        if (!ValidateSubmission(submission, command, out var error, out var message))
        {
            return PlayerCommandResult.Reject(command, error, message);
        }

        _lastSequenceByController[submission.ControllerId] = command.ClientSequence;

        if (!ValidatePayload(command, out error, out message))
        {
            return PlayerCommandResult.Reject(command, error, message);
        }

        if (entityCommandSink is not null
            && !entityCommandSink.TryEnqueue(command, out _, out error, out message))
        {
            return PlayerCommandResult.Reject(
                command,
                error == CommandGatewayValidationError.None
                    ? CommandGatewayValidationError.EntityCommandSinkRejected
                    : error,
                message);
        }

        return PlayerCommandResult.Accept(command);
    }

    private bool ValidateSubmission(
        CommandGatewaySubmission submission,
        PlayerCommand command,
        out CommandGatewayValidationError error,
        out string message)
    {
        if (!submission.ControllerId.IsValid)
        {
            return Reject(CommandGatewayValidationError.InvalidController, "Controller id is required.", out error, out message);
        }

        if (command.IssuerSlotId.Value <= 0)
        {
            return Reject(CommandGatewayValidationError.InvalidIssuerSlot, "Issuer slot must be a positive slot id.", out error, out message);
        }

        if (!ControlsSlot(submission.ControlledSlots, command.IssuerSlotId))
        {
            return Reject(CommandGatewayValidationError.ControllerDoesNotOwnSlot, "Controller is not bound to the issuer slot.", out error, out message);
        }

        if (command.ClientSequence <= LastSequence(submission.ControllerId))
        {
            return Reject(CommandGatewayValidationError.NonMonotonicSequence, "Client sequence must increase per controller.", out error, out message);
        }

        if (command.TargetTick < 0)
        {
            return Reject(CommandGatewayValidationError.InvalidTargetTick, "Target tick must be non-negative.", out error, out message);
        }

        if (!command.IsIntent)
        {
            return Reject(CommandGatewayValidationError.InvalidCommandKind, "Command kind must name a player intent.", out error, out message);
        }

        if (command.Kind == PlayerCommandKind.DebugSandbox && !_options.SandboxCommandsEnabled)
        {
            return Reject(CommandGatewayValidationError.SandboxOnly, "Sandbox command rejected outside sandbox authority.", out error, out message);
        }

        error = CommandGatewayValidationError.None;
        message = string.Empty;
        return true;
    }

    private int LastSequence(PlayerControllerId controllerId)
    {
        return _lastSequenceByController.GetValueOrDefault(controllerId);
    }

    private static bool ControlsSlot(IReadOnlyList<PlayerSlotId> slots, PlayerSlotId slot)
    {
        for (var index = 0; index < slots.Count; index++)
        {
            if (slots[index] == slot)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Accept(out CommandGatewayValidationError error, out string message)
    {
        error = CommandGatewayValidationError.None;
        message = string.Empty;
        return true;
    }

    private static bool Reject(
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
