namespace ProceduralRts.Core;

[Flags]
public enum SpecialAttackHook
{
    None = 0,
    Targeting = 1 << 0,
    FireAuthorization = 1 << 1,
    ProjectileUpdate = 1 << 2,
    Impact = 1 << 3,
    Beam = 1 << 4,
    Area = 1 << 5,
    Charge = 1 << 6,
    Chain = 1 << 7
}
