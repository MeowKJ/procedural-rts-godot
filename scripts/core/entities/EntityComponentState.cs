using Godot;

namespace ProceduralRts.Core;

public abstract record EntityComponentState;

public sealed record HealthComponentState(float Hp, float MaxHp) : EntityComponentState;

public sealed record SelectableComponentState(bool Selected = false, float AlertPulse = 0) : EntityComponentState;

public sealed record CommandableComponentState(
    Vector2? PlayerIntentTarget = null,
    Vector2? CommandVisualTarget = null,
    MoveCommandMode MoveMode = MoveCommandMode.Direct) : EntityComponentState;

public sealed record MovementComponentState(
    Vector2 Velocity,
    Vector2? MoveTarget = null,
    Vector2? FormationSlot = null,
    float FireAnchorRemaining = 0) : EntityComponentState;

public sealed record PatrolOrderComponentState(
    Vector2 PointA,
    Vector2 PointB,
    bool MovingToB = true) : EntityComponentState;

public sealed record GuardOrderComponentState(
    EntityId TargetEntity,
    Vector2 GuardPoint,
    float Radius) : EntityComponentState;

public sealed record AttackGroundOrderComponentState(Vector2 Target) : EntityComponentState;

public sealed record PathfindingComponentState(
    PathPoint Goal,
    IReadOnlyList<PathPoint> Waypoints,
    int NextWaypointIndex = 0) : EntityComponentState;

public sealed record CollisionComponentState(float Radius, float Mass, int PushPriority, bool BlocksMovement) : EntityComponentState;

public sealed record VisionComponentState(float SightRange) : EntityComponentState;

public sealed record AutonomyComponentState(
    float AcquireRange,
    float LeashRange,
    Vector2? AnchorPosition = null) : EntityComponentState;

public sealed record RetaliationComponentState(
    EntityId Target,
    int LastThreatTick = 0) : EntityComponentState;

public sealed record WeaponUserComponentState(
    IReadOnlyList<WeaponMountRuntimeState> Mounts,
    EntityId AttackTarget = default,
    CombatTargetKind AttackTargetKind = CombatTargetKind.Unit,
    bool AttackTargetIsManual = false,
    float AutoReacquireCooldownRemaining = 0,
    Vector2? LastKnownTargetPosition = null,
    float LastKnownTargetRemaining = 0) : EntityComponentState;

public sealed record ProjectileComponentState(
    EntityId Source,
    EntityId Target,
    string WeaponId,
    string AmmoId,
    ProjectileBehavior Behavior,
    HitRule HitRule,
    Vector2 Origin,
    Vector2 AimPoint,
    float Damage,
    Vector2 Velocity,
    float Speed,
    float TrackingStrength,
    float HitRadius,
    float Age,
    float FlightDuration,
    float LifetimeRemaining,
    bool Interceptable = false) : EntityComponentState
{
    public float FlightProgress => FlightDuration <= 0
        ? 1
        : Mathf.Clamp(Age / FlightDuration, 0, 1);
}

public sealed record VeterancyComponentState(
    int Kills = 0,
    float Experience = 0,
    int Rank = 0) : EntityComponentState;

public sealed record RegenerationComponentState(
    float HpPerSecond,
    float Progress = 0) : EntityComponentState;

public sealed record HarvesterComponentState(
    HarvesterMode Mode = HarvesterMode.Idle,
    int? FieldId = null,
    int? RefineryId = null,
    float HarvestPulse = 0,
    bool Retreating = false) : EntityComponentState;

public sealed record ResourceCargoComponentState(int Cargo = 0, int Capacity = 0) : EntityComponentState;

public sealed record ResourceNodeComponentState(
    int Amount,
    int MaxAmount,
    float GatherRateModifier = 1,
    ResourceDepletionBehavior DepletionBehavior = ResourceDepletionBehavior.DepleteToZero,
    ResourceVisibilityRule VisibilityRule = ResourceVisibilityRule.VisibleWhenExplored,
    ResourceCorruptionState CorruptionState = ResourceCorruptionState.Clean,
    float RegenerationProgress = 0) : EntityComponentState;

public sealed record ResourceRegenerationAuraComponentState(
    float Radius,
    float Multiplier = 1,
    bool RequiresPowered = true) : EntityComponentState;

public sealed record SignalNetworkComponentState(
    SignalNodeKind Kind,
    float DayControlRadius,
    float NightVisionRadius,
    float SafetyAuraMultiplier = 1.5f) : EntityComponentState;

public sealed record ProductionQueueComponentState(
    IReadOnlyList<UnitProductionQueueItem> Items,
    ProductionPauseReason PauseReason = ProductionPauseReason.None,
    string? RepeatOutputSpecId = null) : EntityComponentState;

public sealed record AbilityCooldownState(AbilityKind Kind, float CooldownRemaining);

public sealed record AbilityRuntimeComponentState(
    IReadOnlyList<AbilityCooldownState> Cooldowns) : EntityComponentState;

public sealed record ShieldComponentState(
    float AbsorbRemaining,
    float DurationRemaining) : EntityComponentState;

public sealed record ScanRevealComponentState(
    float Radius,
    float DurationRemaining) : EntityComponentState;

public sealed record DeployComponentState(
    bool IsDeployed,
    float SetupRemaining,
    float RangeMultiplier) : EntityComponentState;

public sealed record RepairOrderComponentState(
    int TargetId,
    float Range,
    float RepairPerSecond,
    float CreditCostPerHp = 1,
    float RepairProgress = 0) : EntityComponentState;

public sealed record CommandQueueComponentState(IReadOnlyList<EntityCommand> Items) : EntityComponentState;

public sealed record FootprintComponentState(Vector2 Size, MovementDomain PlacementDomain = MovementDomain.Land) : EntityComponentState
{
    public float Radius => Mathf.Max(Size.X, Size.Y) * 0.5f;
}

public sealed record BuildingIdentityComponentState(
    int BuildingId,
    string Kind,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction) : EntityComponentState;

public sealed record ConstructionIdentityComponentState(string Kind) : EntityComponentState;

public enum ConstructionPhase
{
    Building,
    Queued,
    ReadyToPlace,
    RestartCapture
}

public sealed record ConstructionComponentState(
    float Progress = 1,
    float BuildTime = 0,
    int Cost = 0,
    float RefundRatio = 0.5f,
    ConstructionPauseReason PauseReason = ConstructionPauseReason.None,
    ConstructionPhase Phase = ConstructionPhase.Building) : EntityComponentState
{
    public bool Paused => PauseReason != ConstructionPauseReason.None;

    public bool ReadyToPlace => Phase == ConstructionPhase.ReadyToPlace;
}

public sealed record PowerComponentState(int Provided, int Used, bool Powered = true) : EntityComponentState;

public sealed record RallyPointComponentState(
    Vector2? Target = null,
    int? TargetEntityId = null) : EntityComponentState;

public sealed record DockComponentState(int? ReservedByEntityId = null, int? DockedEntityId = null) : EntityComponentState;

public sealed record BuildRadiusComponentState(float Radius) : EntityComponentState;

public sealed record PresentationPulseComponentState(
    float CommandPulse = 0,
    float AlertPulse = 0,
    float HitPulse = 0) : EntityComponentState;
