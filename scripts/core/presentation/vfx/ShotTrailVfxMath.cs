namespace ProceduralRts.Core;

public readonly record struct ShotTrailVfxStyle(
    float Length,
    float Width,
    float CoreWidth,
    float HeadRadius)
{
    public bool Draw => Length > 0 && Width > 0 && CoreWidth > 0 && HeadRadius > 0;
}

public static class ShotTrailVfxMath
{
    public static bool ShouldCreate(WeaponKind? weaponKind)
    {
        return weaponKind == WeaponKind.VectorCannon;
    }

    public static ShotTrailVfxStyle StyleFor(WeaponKind? weaponKind, float distance)
    {
        if (!ShouldCreate(weaponKind) || distance <= 0)
        {
            return default;
        }

        var width = weaponKind == WeaponKind.VectorCannon ? 4.8f : 3.6f;
        return new ShotTrailVfxStyle(
            MathF.Min(distance, weaponKind == WeaponKind.VectorCannon ? 78f : 54f),
            width,
            MathF.Max(1.4f, width * 0.42f),
            weaponKind == WeaponKind.VectorCannon ? 4.2f : 3f);
    }
}
