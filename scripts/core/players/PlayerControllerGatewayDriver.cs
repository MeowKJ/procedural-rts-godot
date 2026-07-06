namespace ProceduralRts.Core;

public static class PlayerControllerGatewayDriver
{
    public static CommandGatewayResult PollAndSubmit(
        CommandGateway gateway,
        IPlayerController controller,
        in PlayerControllerContext context,
        ICommandGatewayEntityCommandSink sink)
    {
        var result = controller.Poll(context);
        if (result.Commands.Count == 0)
        {
            return new CommandGatewayResult(Array.Empty<PlayerCommandResult>());
        }

        var submission = new CommandGatewaySubmission(
            controller.Id,
            controller.Kind,
            controller.ControlledSlots,
            context.Tick);
        return gateway.Submit(submission, result.Commands, sink);
    }
}

public sealed class BufferedLocalPlayerController : IPlayerController
{
    private readonly PlayerSlotId[] _controlledSlots;
    private readonly List<PlayerCommand> _pending = [];

    public BufferedLocalPlayerController(PlayerControllerId id, IReadOnlyList<PlayerSlotId> controlledSlots)
    {
        Id = id;
        _controlledSlots = controlledSlots.Count == 0
            ? [PlayerSlotId.One]
            : controlledSlots.ToArray();
    }

    public PlayerControllerId Id { get; }
    public PlayerControllerKind Kind => PlayerControllerKind.LocalHuman;
    public IReadOnlyList<PlayerSlotId> ControlledSlots => _controlledSlots;

    public void Enqueue(PlayerCommand command)
    {
        _pending.Add(command);
    }

    public PlayerControllerResult Poll(in PlayerControllerContext context)
    {
        if (_pending.Count == 0)
        {
            return PlayerControllerResult.Empty;
        }

        var commands = _pending.ToArray();
        _pending.Clear();
        return new PlayerControllerResult(commands);
    }
}

public sealed class AgentPlayerController : IPlayerController
{
    private readonly IPlayerAgent _agent;
    private readonly PlayerSlotId[] _controlledSlots;

    public AgentPlayerController(PlayerControllerId id, IPlayerAgent agent, IReadOnlyList<PlayerSlotId> controlledSlots)
    {
        Id = id;
        _agent = agent;
        _controlledSlots = controlledSlots.Count == 0
            ? [PlayerSlotId.One]
            : controlledSlots.ToArray();
    }

    public PlayerControllerId Id { get; }
    public PlayerControllerKind Kind => PlayerControllerKind.ScriptedBot;
    public IReadOnlyList<PlayerSlotId> ControlledSlots => _controlledSlots;

    public PlayerControllerResult Poll(in PlayerControllerContext context)
    {
        return _agent.Think(context.Observation);
    }
}
