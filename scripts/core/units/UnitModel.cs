using Godot;

namespace ProceduralRts.Core;

public sealed class UnitModel
{
    public required int Id { get; init; }
    public required string DesignId { get; init; }
    public string Kind => DesignId;
    public required Owner Owner { get; init; }
    public FactionId FactionId { get; init; } = FactionId.Dog;
    public required Vector2 Position { get; set; }
    public float Facing { get; set; }
    public float TurretFacing { get; set; }
    public TurretState TurretState { get; set; } = TurretState.Idle;
    public float Hp { get; set; }
    public bool Selected { get; set; }
    public UnitStance Stance { get; set; } = UnitStance.Hold;
    public Vector2 AnchorPosition { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2? PlayerIntentTarget { get; set; }
    public Vector2? FormationSlot { get; set; }
    public Vector2? CommandVisualTarget { get; set; }
    public UnitMovementState MovementState { get; set; } = UnitMovementState.Idle;
    public Vector2? MoveTarget { get; set; }
    public MoveCommandMode MoveMode { get; set; } = MoveCommandMode.Direct;
    public Queue<Vector2> Path { get; } = [];
    public List<Vector2> GlobalCorridor { get; } = [];
    public List<GridObstacle> DebugRawPathCells { get; } = [];
    public Vector2 DebugLocalAvoidanceVector { get; set; }
    public Vector2 DebugSteeringVector { get; set; }
    public float RepathCooldownRemaining { get; set; }
    public float PathStallSeconds { get; set; }
    public float LastMoveTargetDistance { get; set; } = float.PositiveInfinity;
    public int? AttackTargetId { get; set; }
    public CombatTargetKind AttackTargetKind { get; set; } = CombatTargetKind.Unit;
    public bool AttackTargetIsManual { get; set; }
    public bool AttackTargetAllowsPursuit { get; set; }
    public Vector2? AttackTargetLastKnownPosition { get; set; }
    public Vector2 AttackTargetLastKnownDirection { get; set; } = Vector2.Right;
    public float AttackTargetLostTrailRemaining { get; set; }
    public bool ReturnToAnchorAfterAttack { get; set; }
    public int? RetaliationTargetId { get; set; }
    public int? LastSharedThreatKey { get; set; }
    public HarvesterMode HarvesterMode { get; set; } = HarvesterMode.Idle;
    public int? HarvestFieldId { get; set; }
    public int? HarvestRefineryId { get; set; }
    public int Cargo { get; set; }
    public float HarvestPulse { get; set; }
    public float AttackCooldownRemaining { get; set; }
    public float ThreatShareCooldownRemaining { get; set; }
    public float CommandPulse { get; set; }
    public float HitPulse { get; set; }
    public float AlertPulse { get; set; }
    public AmmoKind? LastDamageAmmoKind { get; set; }
    public float LastDamageAmount { get; set; }
    public float DeathOverkillDamage { get; set; }
    public bool IsAnchor => MovementState is UnitMovementState.HoldingSlot or UnitMovementState.CombatAnchor;
    public int AnchorPriority => MovementState == UnitMovementState.CombatAnchor ? 2 : MovementState == UnitMovementState.HoldingSlot ? 1 : 0;
    public bool CanBeDisplaced => MovementState != UnitMovementState.CombatAnchor;
    public UnitSpec Spec => UnitDesignCatalog.Spec(DesignId);
    public UnitSpecRuntimeDescriptor RuntimeDescriptor => UnitDesignDefinitionCatalog.RuntimeDescriptors[DesignId];
}
