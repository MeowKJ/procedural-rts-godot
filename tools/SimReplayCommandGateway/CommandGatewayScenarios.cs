static partial class Program
{
    static void AssertCommandGatewayValidationShell()
    {
        var controller = new PlayerControllerId("qa-controller");
        var submission = new CommandGatewaySubmission(
            controller,
            PlayerControllerKind.QaAgent,
            new[] { PlayerSlotId.One },
            CurrentTick: 10);
        var subjects = new[] { new EntityId(7) };
        var acceptedMove = new PlayerCommand(
            PlayerSlotId.One,
            1,
            11,
            PlayerCommandKind.Move,
            PlayerCommandPayload.ForPoint(subjects, 320, 128));

        var sink = new RecordingGatewaySink();
        var gateway = new CommandGateway();
        var first = gateway.Submit(submission, new[] { acceptedMove }, sink);
        Assert(first.AcceptedCount == 1 && first.RejectedCount == 0, "gateway should accept a valid move command");
        Assert(sink.Accepted.Count == 1 && sink.Accepted[0] == acceptedMove, "gateway should forward accepted commands only through the sink");

        var duplicate = gateway.Submit(submission, new[] { acceptedMove }, sink);
        AssertRejected(duplicate, CommandGatewayValidationError.NonMonotonicSequence, "gateway should reject duplicate controller sequence");
        Assert(sink.Accepted.Count == 1, "duplicate sequence must not reach the sink");

        var unauthorized = new PlayerCommand(
            PlayerSlotId.Two,
            2,
            11,
            PlayerCommandKind.Stop,
            PlayerCommandPayload.ForSubjects(subjects));
        AssertRejected(
            gateway.Submit(submission, new[] { unauthorized }, sink),
            CommandGatewayValidationError.ControllerDoesNotOwnSlot,
            "gateway should reject commands for slots not bound to the controller");

        var missingPoint = new PlayerCommand(
            PlayerSlotId.One,
            2,
            11,
            PlayerCommandKind.Move,
            PlayerCommandPayload.ForSubjects(subjects));
        AssertRejected(
            gateway.Submit(submission, new[] { missingPoint }, sink),
            CommandGatewayValidationError.InvalidPayloadShape,
            "gateway should reject malformed move payloads");

        var sandbox = new PlayerCommand(
            PlayerSlotId.One,
            3,
            11,
            PlayerCommandKind.DebugSandbox);
        AssertRejected(
            gateway.Submit(submission, new[] { sandbox }, sink),
            CommandGatewayValidationError.SandboxOnly,
            "gateway should reject sandbox commands in standard authority");

        var sandboxGateway = new CommandGateway(new CommandGatewayOptions(SandboxCommandsEnabled: true));
        var sandboxResult = sandboxGateway.Submit(submission, new[] { sandbox }, sink);
        Assert(sandboxResult.AcceptedCount == 1, "gateway should accept sandbox commands when sandbox authority is enabled");

        Console.WriteLine("OK [command-gateway]: slot rights, sequence monotonicity, payload shape, sandbox gate, and sink forwarding validated.");
    }

    private static void AssertRejected(
        CommandGatewayResult result,
        CommandGatewayValidationError expected,
        string message)
    {
        Assert(result.RejectedCount == 1, message);
        Assert(result.Commands[0].Error == expected, $"{message}: expected {expected}, got {result.Commands[0].Error}");
        Assert(!string.IsNullOrWhiteSpace(result.Commands[0].Message), $"{message}: rejection should include feedback text");
    }

    private sealed class RecordingGatewaySink : ICommandGatewayEntityCommandSink
    {
        public List<PlayerCommand> Accepted { get; } = [];

        public bool TryEnqueue(
            PlayerCommand command,
            out SequencedCommandEnvelope? envelope,
            out CommandGatewayValidationError error,
            out string message)
        {
            Accepted.Add(command);
            envelope = null;
            error = CommandGatewayValidationError.None;
            message = string.Empty;
            return true;
        }
    }
}
