namespace ProceduralRts.Core;

public readonly record struct PlayerControllerId(string Value)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    public override string ToString()
    {
        return Value;
    }
}

public readonly record struct PlayerAgentId(string Value)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    public override string ToString()
    {
        return Value;
    }
}

public enum PlayerControllerKind
{
    LocalHuman,
    ScriptedBot,
    Replay,
    RemoteClient,
    ExternalAgent,
    QaAgent
}

public enum PlayerAgentKind
{
    None,
    Scripted,
    Utility,
    Replay,
    ExternalLlm,
    RlPolicy,
    Qa
}

public enum PlayerCommandKind
{
    None,
    Select,
    Move,
    Attack,
    AttackMove,
    Stop,
    HoldPosition,
    Build,
    Produce,
    Ability,
    Rally,
    Harvest,
    Repair,
    SetStance,
    ControlGroup,
    DebugSandbox
}

public readonly record struct PlayerCommand(
    PlayerSlotId IssuerSlotId,
    int ClientSequence,
    int TargetTick,
    PlayerCommandKind Kind,
    PlayerCommandPayload Payload)
{
    public PlayerCommand(
        PlayerSlotId issuerSlotId,
        int clientSequence,
        int targetTick,
        PlayerCommandKind kind)
        : this(issuerSlotId, clientSequence, targetTick, kind, PlayerCommandPayload.Empty)
    {
    }

    public bool IsIntent => Kind != PlayerCommandKind.None;
}

public readonly record struct PlayerControllerContext(
    int Tick,
    ObservationView Observation)
{
    public PlayerSlotId ViewerSlotId => Observation.ViewerSlotId;
}

public sealed record PlayerControllerResult(IReadOnlyList<PlayerCommand> Commands)
{
    public static readonly PlayerControllerResult Empty = new(Array.Empty<PlayerCommand>());
}

public interface IPlayerController
{
    PlayerControllerId Id { get; }
    PlayerControllerKind Kind { get; }
    IReadOnlyList<PlayerSlotId> ControlledSlots { get; }
    PlayerControllerResult Poll(in PlayerControllerContext context);
}

public interface IPlayerAgent
{
    PlayerAgentId Id { get; }
    PlayerAgentKind Kind { get; }
    PlayerControllerResult Think(in ObservationView observation);
}
