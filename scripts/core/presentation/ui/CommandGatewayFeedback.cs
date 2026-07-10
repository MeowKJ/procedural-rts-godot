namespace ProceduralRts.Core;

public static class CommandGatewayFeedback
{
    public static string Status(CommandGatewayResult result, string acceptedStatus)
    {
        var rejection = FirstRejection(result);
        return rejection == CommandGatewayValidationError.None
            ? acceptedStatus
            : RejectionStatus(rejection);
    }

    public static CommandGatewayValidationError FirstRejection(CommandGatewayResult result)
    {
        for (var index = 0; index < result.Commands.Count; index++)
        {
            var command = result.Commands[index];
            if (!command.Accepted)
            {
                return command.Error;
            }
        }

        return CommandGatewayValidationError.None;
    }

    public static string RejectionStatus(CommandGatewayValidationError error)
    {
        var key = error switch
        {
            CommandGatewayValidationError.InvalidTarget => "ui.commandFailure.invalidTarget",
            CommandGatewayValidationError.InvalidSubject => "ui.commandFailure.invalidSelection",
            CommandGatewayValidationError.TooManySubjects => "ui.commandFailure.tooManyUnits",
            CommandGatewayValidationError.NonMonotonicSequence or CommandGatewayValidationError.InvalidTargetTick => "ui.commandFailure.stale",
            CommandGatewayValidationError.InvalidPayloadShape => "ui.commandFailure.invalidCommand",
            CommandGatewayValidationError.InvalidController
                or CommandGatewayValidationError.InvalidIssuerSlot
                or CommandGatewayValidationError.ControllerDoesNotOwnSlot => "ui.commandFailure.notAuthorized",
            _ => "ui.commandFailure.unavailable",
        };
        return GameText.T(key);
    }
}
