using Godot;

namespace ProceduralRts.Core;

public enum EntityCommandKind
{
    Move,
    Attack,
    AttackGround,
    AttackMove,
    Select,
    Stop,
    HoldPosition,
    Build,
    QueueConstruction,
    CancelConstruction,
    Produce,
    CancelProduction,
    Ability,
    Repair,
    Rally,
    Harvest,
    SetStance,
    Patrol,
    Guard,
    DebugSandbox
}

public abstract record EntityCommand(
    EntityCommandKind Kind,
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick);

public sealed record MoveEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    Vector2 Target,
    MoveCommandMode Mode,
    PlayerCommandQueueMode QueueMode = PlayerCommandQueueMode.Replace) : EntityCommand(EntityCommandKind.Move, Issuer, Subjects, Tick);

public sealed record AttackEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    EntityId Target,
    CombatTargetKind TargetKind) : EntityCommand(EntityCommandKind.Attack, Issuer, Subjects, Tick);

public sealed record AttackGroundEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    Vector2 Target) : EntityCommand(EntityCommandKind.AttackGround, Issuer, Subjects, Tick);

public sealed record SetSelectionEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick) : EntityCommand(EntityCommandKind.Select, Issuer, Subjects, Tick);

public sealed record ProduceEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    string OutputSpecId) : EntityCommand(EntityCommandKind.Produce, Issuer, Subjects, Tick);

public sealed record StartConstructionEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    string BuildingSpecId,
    Vector2 Position,
    float Facing = 0,
    EntityId ReadyTicket = default) : EntityCommand(EntityCommandKind.Build, Issuer, Subjects, Tick);

public sealed record QueueConstructionEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    string BuildingSpecId) : EntityCommand(EntityCommandKind.QueueConstruction, Issuer, Subjects, Tick);

public sealed record CancelConstructionEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick) : EntityCommand(EntityCommandKind.CancelConstruction, Issuer, Subjects, Tick);

public sealed record CancelProductionEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    float RefundRatio = 0.5f) : EntityCommand(EntityCommandKind.CancelProduction, Issuer, Subjects, Tick);

public sealed record SetRepeatProductionEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    bool Enabled,
    string OutputSpecId = "") : EntityCommand(EntityCommandKind.Produce, Issuer, Subjects, Tick);

public sealed record SetRallyPointEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    Vector2 Target,
    EntityId TargetEntity = default) : EntityCommand(EntityCommandKind.Rally, Issuer, Subjects, Tick);

public sealed record HarvestEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    EntityId ResourceTarget) : EntityCommand(EntityCommandKind.Harvest, Issuer, Subjects, Tick);

public sealed record AutoHarvestEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick) : EntityCommand(EntityCommandKind.Harvest, Issuer, Subjects, Tick);

public sealed record AbilityEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    AbilityKind Ability,
    EntityId Target = default,
    Vector2? TargetPoint = null) : EntityCommand(EntityCommandKind.Ability, Issuer, Subjects, Tick);

public sealed record RepairEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    EntityId Target) : EntityCommand(EntityCommandKind.Repair, Issuer, Subjects, Tick);

public sealed record AttackMoveEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    Vector2 Target,
    MoveCommandMode Mode,
    PlayerCommandQueueMode QueueMode = PlayerCommandQueueMode.Replace) : EntityCommand(EntityCommandKind.AttackMove, Issuer, Subjects, Tick);

public sealed record PatrolEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    Vector2 PointA,
    Vector2 PointB) : EntityCommand(EntityCommandKind.Patrol, Issuer, Subjects, Tick);

public sealed record GuardEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    Vector2 GuardPoint,
    float Radius,
    EntityId TargetEntity = default) : EntityCommand(EntityCommandKind.Guard, Issuer, Subjects, Tick);

public sealed record StopEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick) : EntityCommand(EntityCommandKind.Stop, Issuer, Subjects, Tick);

public sealed record HoldPositionEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick) : EntityCommand(EntityCommandKind.HoldPosition, Issuer, Subjects, Tick);

public sealed record SetStanceEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    UnitStance Stance) : EntityCommand(EntityCommandKind.SetStance, Issuer, Subjects, Tick);

/// <summary>
/// One player intent for a whole selection. Decomposed by the CommandSystem into
/// per-entity formation slots, so the group arrives compact without each unit
/// racing to the same point. The visible command line points at <see cref="Target"/>
/// (the intent), never at the internal slot.
/// </summary>
public sealed record GroupMoveEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    Vector2 Target,
    MoveCommandMode Mode,
    PlayerCommandQueueMode QueueMode = PlayerCommandQueueMode.Replace) : EntityCommand(EntityCommandKind.Move, Issuer, Subjects, Tick);

/// <summary>
/// Group attack on one target. Attackers already in weapon range become firing
/// anchors and hold; the rest are assigned slots on the target's weapon-range
/// ring instead of all piling into the target center.
/// </summary>
public sealed record GroupAttackEntityCommand(
    OwnerId Issuer,
    IReadOnlyList<EntityId> Subjects,
    int Tick,
    EntityId Target,
    CombatTargetKind TargetKind) : EntityCommand(EntityCommandKind.Attack, Issuer, Subjects, Tick);
