namespace ProceduralRts.Core;

public sealed partial class CommandGateway
{
    private bool ValidatePayload(
        PlayerCommand command,
        out CommandGatewayValidationError error,
        out string message)
    {
        var payload = command.Payload;
        var subjects = payload.SubjectIds;
        if (subjects.Count > _options.MaxSubjectsPerCommand)
        {
            return Reject(CommandGatewayValidationError.TooManySubjects, "Command has too many subject ids.", out error, out message);
        }

        if (ContainsInvalidSubject(subjects))
        {
            return Reject(CommandGatewayValidationError.InvalidSubject, "Subject ids must be valid entity ids.", out error, out message);
        }

        return command.Kind switch
        {
            PlayerCommandKind.Select => Accept(out error, out message),
            PlayerCommandKind.Move or PlayerCommandKind.AttackMove => RequireSubjectsAndPoint(payload, out error, out message),
            PlayerCommandKind.Attack or PlayerCommandKind.Harvest or PlayerCommandKind.Repair => RequireSubjectsAndTarget(payload, out error, out message),
            PlayerCommandKind.Stop or PlayerCommandKind.HoldPosition => RequireSubjects(payload, out error, out message),
            PlayerCommandKind.Build => RequireSpecAndPoint(payload, out error, out message),
            PlayerCommandKind.Produce => RequireSubjectsAndSpec(payload, out error, out message),
            PlayerCommandKind.Rally => RequireSubjectsAndRallyTarget(payload, out error, out message),
            PlayerCommandKind.Ability or PlayerCommandKind.SetStance or PlayerCommandKind.ControlGroup => RequireSubjects(payload, out error, out message),
            PlayerCommandKind.DebugSandbox => Accept(out error, out message),
            _ => Reject(CommandGatewayValidationError.InvalidCommandKind, "Command kind is not supported.", out error, out message),
        };
    }

    private static bool RequireSubjects(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        return payload.SubjectIds.Count > 0
            ? Accept(out error, out message)
            : Reject(CommandGatewayValidationError.InvalidPayloadShape, "Command requires at least one subject.", out error, out message);
    }

    private static bool ContainsInvalidSubject(IReadOnlyList<EntityId> subjects)
    {
        for (var index = 0; index < subjects.Count; index++)
        {
            if (!subjects[index].IsValid)
            {
                return true;
            }
        }

        return false;
    }

    private static bool RequireSubjectsAndPoint(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        if (!RequireSubjects(payload, out error, out message))
        {
            return false;
        }

        return RequirePoint(payload, out error, out message);
    }

    private static bool RequireSubjectsAndTarget(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        if (!RequireSubjects(payload, out error, out message))
        {
            return false;
        }

        return payload.TargetEntity.IsValid
            ? Accept(out error, out message)
            : Reject(CommandGatewayValidationError.InvalidTarget, "Command requires a valid target entity.", out error, out message);
    }

    private bool RequireSubjectsAndSpec(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        if (!RequireSubjects(payload, out error, out message))
        {
            return false;
        }

        return RequireSpec(payload, out error, out message);
    }

    private bool RequireSpecAndPoint(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        if (!RequireSpec(payload, out error, out message))
        {
            return false;
        }

        return RequirePoint(payload, out error, out message);
    }

    private static bool RequireSubjectsAndRallyTarget(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        if (!RequireSubjects(payload, out error, out message))
        {
            return false;
        }

        return payload.TargetEntity.IsValid || (payload.HasTargetPoint && payload.TargetPoint.IsFinite)
            ? Accept(out error, out message)
            : Reject(CommandGatewayValidationError.InvalidTarget, "Rally requires a point or target entity.", out error, out message);
    }

    private bool RequireSpec(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        return !string.IsNullOrWhiteSpace(payload.SpecId) && payload.SpecId.Length <= _options.MaxSpecIdLength
            ? Accept(out error, out message)
            : Reject(CommandGatewayValidationError.InvalidSpecId, "Command requires a bounded spec id.", out error, out message);
    }

    private static bool RequirePoint(PlayerCommandPayload payload, out CommandGatewayValidationError error, out string message)
    {
        return payload.HasTargetPoint && payload.TargetPoint.IsFinite
            ? Accept(out error, out message)
            : Reject(CommandGatewayValidationError.InvalidPayloadShape, "Command requires a finite target point.", out error, out message);
    }
}
