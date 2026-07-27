using Godot;

namespace ProceduralRts.Core;

public sealed class UnitInstance
{
    public required int Id { get; init; }
    public required EntityId EntityId { get; init; }
    public required UnitSpec Spec { get; init; }
    public required PlayerSlotId PlayerSlotId { get; init; }
    public required Vector2 Position { get; set; }
    public float Facing { get; set; }
    public float Hp { get; set; }
    public bool Selected { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2? PlayerIntentTarget { get; set; }
    public Vector2? FormationSlot { get; set; }
    public Vector2? CommandVisualTarget { get; set; }
    public Vector2? MoveTarget { get; set; }
    public MoveCommandMode MoveMode { get; set; } = MoveCommandMode.Direct;
    public UnitStance Stance { get; set; } = UnitStance.Aggressive;
    public int? AttackTargetId { get; set; }
    public CombatTargetKind AttackTargetKind { get; set; } = CombatTargetKind.Unit;
    public bool AttackTargetIsManual { get; set; }
    public float AttackCooldownRemaining { get; set; }
    public string? LastDamageAmmoId { get; set; }
    public float LastDamageAmount { get; set; }
    public float DeathOverkillDamage { get; set; }
    public float CommandPulse { get; set; }
    public float AlertPulse { get; set; }
    public float HitPulse { get; set; }
    public HarvesterMode HarvesterMode { get; set; } = HarvesterMode.Idle;
    public int? HarvestFieldId { get; set; }
    public int? HarvestRefineryId { get; set; }
    public float HarvestPulse { get; set; }
    public bool HarvesterRetreating { get; set; }
    public int Cargo { get; set; }
    public List<WeaponMountRuntimeState> WeaponMounts { get; init; } = [];
    public bool IsMoving => MoveTarget is not null;

}
