namespace ProceduralRts.Core;

public sealed record WeaponMountRuntimeState(
    string MountId,
    string WeaponId,
    float Facing,
    float CooldownRemaining,
    WeaponMountPhase Phase = WeaponMountPhase.Acquire,
    float WarmupRemaining = 0,
    float ReloadRemaining = 0);
