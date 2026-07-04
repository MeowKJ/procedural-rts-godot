using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private void DrawMuzzleFlashes()
    {
        foreach (var effect in _muzzleFlashes)
        {
            if (!IsVisible(effect.Position, effect.Length + 20))
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

            DrawCircle(effect.Position, effect.Width * 0.72f, new Color("#ffffff", 0.72f * fade));
            DrawLine(effect.Position, tip, new Color("#ffffff", 0.74f * fade), effect.CoreWidth * fade + 0.6f, true);
            DrawLine(left, right, new Color(effect.Accent, 0.20f * fade), effect.Width * 1.1f * fade + 0.8f, true);
            DrawLine(left + direction * effect.Length * 0.38f, right + direction * effect.Length * 0.38f, new Color(effect.Accent, 0.18f * fade), effect.Width * 0.74f * fade + 0.6f, true);
            DrawLine(left, tip, new Color(effect.Accent, 0.52f * fade), 1.1f, true);
            DrawLine(right, tip, new Color(effect.Accent, 0.44f * fade), 1.1f, true);
        }
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
}
