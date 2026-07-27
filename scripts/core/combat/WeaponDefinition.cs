namespace ProceduralRts.Core;

public sealed record WeaponDefinition(
    string Id,
    string Label,
    string AmmoId,
    WeaponMountKind MountKind,
    float Range,
    float Cooldown,
    float FireArcRadians,
    bool CanFireWhileMoving,
    WeaponTargetProfile TargetProfile,
    SpecialAttackHook Hooks,
    float MinRange = 0,
    bool CanInterceptProjectiles = false,
    float Warmup = 0,
    float Reload = 0);
