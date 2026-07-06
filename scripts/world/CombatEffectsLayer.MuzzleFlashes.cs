using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private void DrawMuzzleFlashes()
    {
        DrawShotTrails();
        foreach (var effect in _muzzleFlashes)
        {
            if (!IsVisible(effect.Position, effect.Length + 20))
            {
                continue;
            }

            var readability = ReadabilityFor(effect.Position);
            if (!readability.Draw)
            {
                continue;
            }

            var t = Mathf.Clamp(effect.Age / MuzzleFlashLifetime, 0, 1);
            var fade = 1 - t;
            var direction = effect.Direction;
            var normal = direction.Orthogonal();
            var tip = effect.Position + direction * effect.Length * (0.8f + t * 0.28f);
            var left = effect.Position + normal * effect.Width * fade;
            var right = effect.Position - normal * effect.Width * fade;

            DrawCircle(effect.Position, effect.Width * 0.72f, Readable(new Color("#ffffff"), 0.72f * fade, readability));
            DrawLine(effect.Position, tip, Readable(new Color("#ffffff"), 0.74f * fade, readability), ReadableWidth(effect.CoreWidth * fade + 0.6f, readability), true);
            DrawLine(left, right, Readable(effect.Accent, 0.20f * fade, readability), ReadableWidth(effect.Width * 1.1f * fade + 0.8f, readability), true);
            DrawLine(left + direction * effect.Length * 0.38f, right + direction * effect.Length * 0.38f, Readable(effect.Accent, 0.18f * fade, readability), ReadableWidth(effect.Width * 0.74f * fade + 0.6f, readability), true);
            DrawLine(left, tip, Readable(effect.Accent, 0.52f * fade, readability), ReadableWidth(1.1f, readability), true);
            DrawLine(right, tip, Readable(effect.Accent, 0.44f * fade, readability), ReadableWidth(1.1f, readability), true);
        }
    }

    private void DrawShotTrails()
    {
        foreach (var effect in _shotTrails)
        {
            if (!IsSegmentVisible(effect.Start, effect.End, effect.Width * 4f))
            {
                continue;
            }

            var readability = ReadabilityForSegment(effect.Start, effect.End);
            if (!readability.Draw)
            {
                continue;
            }

            var t = Mathf.Clamp(effect.Age / ShotTrailLifetime, 0, 1);
            var fade = 1 - t;
            DrawLine(effect.Start, effect.End, Readable(effect.Accent, 0.44f * fade, readability), ReadableWidth(effect.Width, readability), true);
            DrawLine(effect.Start, effect.End, Readable(new Color("#ffffff"), 0.82f * fade, readability), ReadableWidth(effect.CoreWidth, readability), true);
            DrawCircle(effect.End, effect.HeadRadius * fade, Readable(effect.Accent, 0.62f * fade, readability));
        }
    }

    private void AddShotTrailIfNeeded(Vector2 position, Vector2 targetPosition, Color accent, WeaponKind? weaponKind)
    {
        if (!ShotTrailVfxMath.ShouldCreate(weaponKind))
        {
            return;
        }

        var effect = RentShotTrailEffect();
        effect.Reset(position, targetPosition, accent, weaponKind);
        _shotTrails.Add(effect);
        ApplyShotTrailBudget();
    }

    private MuzzleFlashEffect RentMuzzleFlashEffect()
    {
        if (_pooledMuzzleFlashes.Count == 0)
        {
            return new MuzzleFlashEffect();
        }

        var last = _pooledMuzzleFlashes.Count - 1;
        var effect = _pooledMuzzleFlashes[last];
        _pooledMuzzleFlashes.RemoveAt(last);
        return effect;
    }

    private void ApplyMuzzleFlashBudget()
    {
        if (_muzzleFlashes.Count > MuzzleFlashSoftLimit)
        {
            var overflow = _muzzleFlashes.Count - MuzzleFlashSoftLimit;
            for (var index = 0; index < overflow && index < _muzzleFlashes.Count; index++)
            {
                _muzzleFlashes[index].FadeOutSoon(UnderLoadFadeSeconds);
            }
        }

        while (_muzzleFlashes.Count > MuzzleFlashHardLimit)
        {
            ReturnAndRemoveMuzzleFlash(0);
        }
    }

    private void ReturnAndRemoveMuzzleFlash(int index)
    {
        var effect = _muzzleFlashes[index];
        _muzzleFlashes.RemoveAt(index);
        if (_pooledMuzzleFlashes.Count < MuzzleFlashPoolLimit)
        {
            _pooledMuzzleFlashes.Add(effect);
        }
    }

    private ShotTrailEffect RentShotTrailEffect()
    {
        if (_pooledShotTrails.Count == 0)
        {
            return new ShotTrailEffect();
        }

        var last = _pooledShotTrails.Count - 1;
        var effect = _pooledShotTrails[last];
        _pooledShotTrails.RemoveAt(last);
        return effect;
    }

    private void ApplyShotTrailBudget()
    {
        if (_shotTrails.Count > ShotTrailSoftLimit)
        {
            var overflow = _shotTrails.Count - ShotTrailSoftLimit;
            for (var index = 0; index < overflow && index < _shotTrails.Count; index++)
            {
                _shotTrails[index].FadeOutSoon(UnderLoadFadeSeconds);
            }
        }

        while (_shotTrails.Count > ShotTrailHardLimit)
        {
            ReturnAndRemoveShotTrail(0);
        }
    }

    private void ReturnAndRemoveShotTrail(int index)
    {
        var effect = _shotTrails[index];
        _shotTrails.RemoveAt(index);
        if (_pooledShotTrails.Count < ShotTrailPoolLimit)
        {
            _pooledShotTrails.Add(effect);
        }
    }

    private sealed class MuzzleFlashEffect
    {
        public void Reset(Vector2 position, Vector2 targetPosition, Color accent, WeaponKind? weaponKind)
        {
            var toTarget = targetPosition - position;
            Position = position;
            Direction = toTarget.LengthSquared() <= 0.01f ? Vector2.Right : toTarget.Normalized();
            Accent = accent;
            Age = 0;

            var scale = WeaponScale(weaponKind);
            Length = 14 * scale;
            Width = 4.8f * scale;
            CoreWidth = 2.1f * scale;
        }

        public void FadeOutSoon(float remainingSeconds)
        {
            Age = Mathf.Max(Age, MuzzleFlashLifetime - remainingSeconds);
        }

        private static float WeaponScale(WeaponKind? weaponKind)
        {
            return weaponKind switch
            {
                WeaponKind.RocketPod or WeaponKind.VectorCannon => 1.25f,
                WeaponKind.IonEmitter or WeaponKind.ElectromagneticEmitter => 1.12f,
                WeaponKind.LightRepeater => 0.82f,
                _ => 1f,
            };
        }

        public Vector2 Position { get; private set; }
        public Vector2 Direction { get; private set; }
        public Color Accent { get; private set; }
        public float Length { get; private set; }
        public float Width { get; private set; }
        public float CoreWidth { get; private set; }
        public float Age { get; set; }
    }

    private sealed class ShotTrailEffect
    {
        public void Reset(Vector2 position, Vector2 targetPosition, Color accent, WeaponKind? weaponKind)
        {
            var toTarget = targetPosition - position;
            var distance = toTarget.Length();
            var direction = distance <= 0.01f ? Vector2.Right : toTarget / distance;
            var style = ShotTrailVfxMath.StyleFor(weaponKind, distance);
            Start = position;
            End = position + direction * style.Length;
            Accent = accent;
            Age = 0;
            Width = style.Width;
            CoreWidth = style.CoreWidth;
            HeadRadius = style.HeadRadius;
        }

        public void FadeOutSoon(float remainingSeconds)
        {
            Age = Mathf.Max(Age, ShotTrailLifetime - remainingSeconds);
        }

        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }
        public Color Accent { get; private set; }
        public float Width { get; private set; }
        public float CoreWidth { get; private set; }
        public float HeadRadius { get; private set; }
        public float Age { get; set; }
    }
}
