using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private void DrawImpactFlashes()
    {
        foreach (var effect in _impactFlashes)
        {
            if (!IsVisible(effect.Position, effect.Radius + 34))
            {
                continue;
            }

            var readability = ReadabilityFor(effect.Position);
            if (!readability.Draw)
            {
                continue;
            }

            var t = Mathf.Clamp(effect.Age / ImpactFlashLifetime, 0, 1);
            var fade = 1 - t;
            var pulse = Mathf.Sin(t * Mathf.Pi);
            var radius = effect.Radius + t * effect.Style.Expansion;
            var accent = effect.Accent;

            DrawCircle(effect.Position, radius * 0.55f, Readable(accent, 0.12f * fade, readability));
            DrawArc(effect.Position, radius, 0, Mathf.Tau, MediumEffectArcSegments, Readable(accent, 0.66f * fade, readability), ReadableWidth(effect.Style.LineWidth * fade + 0.7f, readability), true);
            DrawArc(effect.Position, radius * 0.68f, 0, Mathf.Tau, SmallEffectArcSegments, Readable(effect.Style.SecondaryColor, 0.38f * fade, readability), ReadableWidth(1.1f, readability), true);

            var sparkCount = effect.Style.SparkCount;
            for (var index = 0; index < sparkCount; index++)
            {
                var direction = Vector2.FromAngle(NoiseAngle(effect.Seed, index));
                var length = (6 + Noise01(effect.Seed, index + 19) * 12) * effect.Style.SparkScale;
                var start = effect.Position + direction * (effect.Radius * 0.28f + pulse * 4);
                var end = start + direction * (length + t * 12 * effect.Style.SparkScale);
                DrawLine(start, end, Readable(accent, 0.48f * fade, readability), ReadableWidth(1.4f, readability), true);
            }

            if (effect.Style.EmitsEmbers)
            {
                for (var index = 0; index < 4; index++)
                {
                    var direction = Vector2.FromAngle(NoiseAngle(effect.Seed + 73, index));
                    var center = effect.Position + direction * (effect.Radius * 0.42f + t * 18 * effect.Style.SparkScale);
                    DrawCircle(center, 1.8f + Noise01(effect.Seed, index + 91) * 2.2f, Readable(new Color("#ffb35c"), 0.34f * fade, readability));
                }
            }

            if (!effect.Style.EmitsEmpDissolve)
            {
                continue;
            }

            for (var index = 0; index < 3; index++)
            {
                var arcRadius = radius * (0.74f + index * 0.16f);
                var startAngle = NoiseAngle(effect.Seed + 137, index) + t * 1.6f;
                DrawArc(effect.Position, arcRadius, startAngle, startAngle + Mathf.Pi * 0.48f, 18, Readable(effect.Style.SecondaryColor, 0.36f * fade, readability), ReadableWidth(1.1f, readability), true);
            }
        }
    }

    private ImpactFlashEffect RentImpactFlashEffect()
    {
        if (_pooledImpactFlashes.Count == 0)
        {
            return new ImpactFlashEffect();
        }

        var last = _pooledImpactFlashes.Count - 1;
        var effect = _pooledImpactFlashes[last];
        _pooledImpactFlashes.RemoveAt(last);
        return effect;
    }

    private void ApplyImpactFlashBudget()
    {
        if (_impactFlashes.Count > ImpactFlashSoftLimit)
        {
            var overflow = _impactFlashes.Count - ImpactFlashSoftLimit;
            for (var index = 0; index < overflow && index < _impactFlashes.Count; index++)
            {
                _impactFlashes[index].FadeOutSoon(UnderLoadFadeSeconds);
            }
        }

        while (_impactFlashes.Count > ImpactFlashHardLimit)
        {
            ReturnAndRemoveImpactFlash(0);
        }
    }

    private void ReturnAndRemoveImpactFlash(int index)
    {
        var effect = _impactFlashes[index];
        _impactFlashes.RemoveAt(index);
        if (_pooledImpactFlashes.Count < ImpactFlashPoolLimit)
        {
            _pooledImpactFlashes.Add(effect);
        }
    }

    private sealed class ImpactFlashEffect
    {
        public void Reset(
            Vector2 position,
            float radius,
            Color accent,
            float damage,
            string? ammoId,
            ImpactVfxStyle style)
        {
            Position = position;
            Radius = Mathf.Max(8, radius);
            Accent = accent;
            Age = 0;
            Seed = Mathf.RoundToInt(position.X * 7 + position.Y * 11 + damage * 3 + EffectIdSeed(ammoId) * 101);
            Style = style;
        }

        public void FadeOutSoon(float remainingSeconds)
        {
            Age = Mathf.Max(Age, ImpactFlashLifetime - remainingSeconds);
        }

        public Vector2 Position { get; private set; }
        public float Radius { get; private set; }
        public Color Accent { get; private set; }
        public int Seed { get; private set; }
        public ImpactVfxStyle Style { get; private set; }
        public float Age { get; set; }
    }
}
