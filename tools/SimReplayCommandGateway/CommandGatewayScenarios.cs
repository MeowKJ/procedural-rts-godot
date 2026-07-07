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

        AssertCommandGatewayPayloadVariants(submission, subjects);

        Console.WriteLine("OK [command-gateway]: slot rights, sequence monotonicity, payload shape variants, sandbox gate, and sink forwarding validated.");
    }

    private static void AssertCommandGatewayPayloadVariants(
        CommandGatewaySubmission submission,
        IReadOnlyList<EntityId> subjects)
    {
        var gateway = new CommandGateway();
        var sink = new RecordingGatewaySink();
        var build = new PlayerCommand(
            PlayerSlotId.One,
            1,
            12,
            PlayerCommandKind.Build,
            PlayerCommandPayload.ForSpec("building.powerplant") with
            {
                HasTargetPoint = true,
                TargetPoint = new PlayerCommandPoint(64, 96),
            });
        AssertAccepted(gateway.Submit(submission, new[] { build }, sink), "gateway should accept build payloads with spec and point");

        var produceMissingSpec = new PlayerCommand(
            PlayerSlotId.One,
            2,
            12,
            PlayerCommandKind.Produce,
            PlayerCommandPayload.ForSubjects(subjects));
        AssertRejected(
            gateway.Submit(submission, new[] { produceMissingSpec }, sink),
            CommandGatewayValidationError.InvalidSpecId,
            "gateway should reject produce payloads without a spec id");

        var rallyPoint = new PlayerCommand(
            PlayerSlotId.One,
            3,
            12,
            PlayerCommandKind.Rally,
            PlayerCommandPayload.ForPoint(subjects, 512, 448));
        AssertAccepted(gateway.Submit(submission, new[] { rallyPoint }, sink), "gateway should accept rally payloads with a point target");

        var attackGround = new PlayerCommand(
            PlayerSlotId.One,
            4,
            12,
            PlayerCommandKind.AttackGround,
            PlayerCommandPayload.ForPoint(subjects, 544, 352, MoveCommandMode.Attack));
        AssertAccepted(gateway.Submit(submission, new[] { attackGround }, sink), "gateway should accept attack-ground payloads with subjects and a point target");

        var attackGroundMissingPoint = new PlayerCommand(
            PlayerSlotId.One,
            5,
            12,
            PlayerCommandKind.AttackGround,
            PlayerCommandPayload.ForSubjects(subjects));
        AssertRejected(
            gateway.Submit(submission, new[] { attackGroundMissingPoint }, sink),
            CommandGatewayValidationError.InvalidPayloadShape,
            "gateway should reject attack-ground payloads without a point target");

        var invalidSubject = new PlayerCommand(
            PlayerSlotId.One,
            6,
            12,
            PlayerCommandKind.Stop,
            PlayerCommandPayload.ForSubjects(new[] { default(EntityId) }));
        AssertRejected(
            gateway.Submit(submission, new[] { invalidSubject }, sink),
            CommandGatewayValidationError.InvalidSubject,
            "gateway should reject payloads with invalid subject ids");

        var rejectingSink = new RecordingGatewaySink(reject: true);
        var sinkRejected = new PlayerCommand(
            PlayerSlotId.One,
            7,
            12,
            PlayerCommandKind.Stop,
            PlayerCommandPayload.ForSubjects(subjects));
        AssertRejected(
            gateway.Submit(submission, new[] { sinkRejected }, rejectingSink),
            CommandGatewayValidationError.EntityCommandSinkRejected,
            "gateway should return structured errors from a rejecting sink");
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

    private static void AssertAccepted(CommandGatewayResult result, string message)
    {
        Assert(result.AcceptedCount == 1 && result.RejectedCount == 0, message);
    }

    private sealed class RecordingGatewaySink : ICommandGatewayEntityCommandSink
    {
        private readonly bool _reject;

        public RecordingGatewaySink(bool reject = false)
        {
            _reject = reject;
        }

        public List<PlayerCommand> Accepted { get; } = [];

        public bool TryEnqueue(
            PlayerCommand command,
            out SequencedCommandEnvelope? envelope,
            out CommandGatewayValidationError error,
            out string message)
        {
            if (_reject)
            {
                envelope = null;
                error = CommandGatewayValidationError.None;
                message = "test sink rejected command";
                return false;
            }

            Accepted.Add(command);
            envelope = null;
            error = CommandGatewayValidationError.None;
            message = string.Empty;
            return true;
        }
    }
}
