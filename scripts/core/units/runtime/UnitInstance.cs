using Godot;

namespace ProceduralRts.Core;

public sealed class UnitInstance
{
    private readonly List<WeaponMountRuntimeState> _weaponMounts = [];

    internal UnitInstance()
    {
    }

    public required int Id { get; init; }
    public required EntityId EntityId { get; init; }
    public required UnitSpec Spec { get; init; }
    public required PlayerSlotId PlayerSlotId { get; init; }
    public Vector2 Position { get; internal set; }
    public float Facing { get; internal set; }
    public float Hp { get; internal set; }
    public bool Selected { get; internal set; }
    public Vector2 Velocity { get; internal set; }
    public Vector2? PlayerIntentTarget { get; internal set; }
    public Vector2? FormationSlot { get; internal set; }
    public Vector2? CommandVisualTarget { get; internal set; }
    public Vector2? MoveTarget { get; internal set; }
    public MoveCommandMode MoveMode { get; internal set; } = MoveCommandMode.Direct;
    public UnitStance Stance { get; internal set; } = UnitStance.Aggressive;
    public int? AttackTargetId { get; internal set; }
    public CombatTargetKind AttackTargetKind { get; internal set; } = CombatTargetKind.Unit;
    public bool AttackTargetIsManual { get; internal set; }
    public float AttackCooldownRemaining { get; internal set; }
    public string? LastDamageAmmoId { get; internal set; }
    public float LastDamageAmount { get; internal set; }
    public float DeathOverkillDamage { get; internal set; }
    public float CommandPulse { get; internal set; }
    public float AlertPulse { get; internal set; }
    public float HitPulse { get; internal set; }
    public HarvesterMode HarvesterMode { get; internal set; } = HarvesterMode.Idle;
    public EntityId? HarvestResourceEntityId { get; internal set; }
    public int? HarvestRefineryId { get; internal set; }
    public float HarvestPulse { get; internal set; }
    public bool HarvesterRetreating { get; internal set; }
    public int Cargo { get; internal set; }
    public IReadOnlyList<WeaponMountRuntimeState> WeaponMounts => _weaponMounts;
    internal List<WeaponMountRuntimeState> MutableWeaponMounts => _weaponMounts;
    public bool IsMoving => MoveTarget is not null;

}
