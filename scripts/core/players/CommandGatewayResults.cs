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
    private readonly int _acceptedCount = CountAccepted(Commands);

    public int AcceptedCount => _acceptedCount;
    public int RejectedCount => Commands.Count - AcceptedCount;

    private static int CountAccepted(IReadOnlyList<PlayerCommandResult> commands)
    {
        var accepted = 0;
        for (var index = 0; index < commands.Count; index++)
        {
            if (commands[index].Accepted)
            {
                accepted++;
            }
        }

        return accepted;
    }
}
