namespace ProceduralRts.Core;

public enum CommandGatewayValidationError
{
    None,
    InvalidController,
    InvalidIssuerSlot,
    ControllerDoesNotOwnSlot,
    NonMonotonicSequence,
    InvalidTargetTick,
    InvalidCommandKind,
    SandboxOnly,
    TooManySubjects,
    InvalidSubject,
    InvalidTarget,
    InvalidSpecId,
    InvalidPayloadShape,
    EntityCommandSinkRejected,
}

public sealed record PlayerCommandResult(
    PlayerCommand Command,
    bool Accepted,
    CommandGatewayValidationError Error,
    string Message)
{
    public static PlayerCommandResult Accept(PlayerCommand command)
    {
        return new PlayerCommandResult(command, Accepted: true, CommandGatewayValidationError.None, string.Empty);
    }

    public static PlayerCommandResult Reject(
        PlayerCommand command,
        CommandGatewayValidationError error,
        string message)
    {
        return new PlayerCommandResult(command, Accepted: false, error, message);
    }
}

public sealed record CommandGatewayResult(IReadOnlyList<PlayerCommandResult> Commands)
{
    public int AcceptedCount => Commands.Count(command => command.Accepted);
    public int RejectedCount => Commands.Count - AcceptedCount;
}
