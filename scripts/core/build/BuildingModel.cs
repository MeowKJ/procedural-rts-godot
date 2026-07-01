using Godot;

namespace ProceduralRts.Core;

public sealed class BuildingModel
{
    public required int Id { get; init; }
    public required string Kind { get; init; }
    public required Owner Owner { get; init; }
    public FactionId FactionId { get; init; } = FactionId.Dog;
    public required Vector2 Position { get; set; }
    public float Facing { get; set; }
    public float TurretFacing { get; set; }
    public float Hp { get; set; }
    public bool Selected { get; set; }
    public int? AttackTargetId { get; set; }
    public CombatTargetKind AttackTargetKind { get; set; } = CombatTargetKind.Unit;
    public float AttackCooldownRemaining { get; set; }
    public TurretState TurretState { get; set; } = TurretState.Idle;
    public Vector2? RallyPoint { get; set; }
    public float RallyPulse { get; set; }
    public int? DockReservedByHarvesterId { get; set; }
    public int? DockedHarvesterId { get; set; }
    public float DeliveryPulse { get; set; }
    public float HitPulse { get; set; }
    public float BuildProgress { get; set; } = 1;
    public bool Powered { get; set; } = true;
    public List<ProductionQueueItem> ProductionQueue { get; } = [];
}
