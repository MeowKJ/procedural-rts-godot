using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private void DrawUnitDeaths()
    {
        foreach (var effect in _unitDeaths)
        {
            if (!IsVisible(effect.Position, effect.Radius + 80))
            {
                continue;
            }

            var t = Mathf.Clamp(effect.Age / effect.Style.Lifetime, 0, 1);
            var fade = 1 - t;
            var burst = Mathf.Sin(t * Mathf.Pi);
            var radius = effect.Radius;
            var outer = radius + 10 + t * 54 * effect.Style.BurstScale;
            var inner = Mathf.Max(2, radius * (0.42f + t * 0.75f));

            DrawDeathScorch(effect, t, fade);
            DrawCircle(effect.Position, radius * (1.25f + burst * 0.65f * effect.Style.BurstScale), new Color("#ffffff", 0.2f * fade));
            DrawCircle(effect.Position, radius * (1.7f + t * 1.1f * effect.Style.BurstScale), new Color(effect.Accent, 0.2f * fade));
            DrawArc(effect.Position, outer, 0, Mathf.Tau, LargeEffectArcSegments, new Color(effect.Accent, 0.78f * fade), effect.Style.RingWidth * fade + 0.7f, true);
            DrawArc(effect.Position, inner, 0, Mathf.Tau, MediumEffectArcSegments, new Color("#ffffff", 0.62f * fade), 1.3f, true);

            DrawDeathFragments(effect, t, fade);
            DrawDeathSmoke(effect, t, fade);
            DrawDeathSpecials(effect, t, fade);
        }
    }

    private void DrawDeathScorch(UnitDeathEffect effect, float t, float fade)
    {
        var scorchFade = Mathf.Clamp(1 - t * 1.18f, 0, 1) * fade;
        if (scorchFade <= 0)
        {
            return;
        }

        var radius = effect.Radius * (0.74f + effect.Style.ScorchScale * 0.42f);
        DrawCircle(effect.Position, radius, new Color("#05070a", effect.Style.ScorchAlpha * scorchFade));
        DrawArc(effect.Position, radius * 1.12f, 0, Mathf.Tau, MediumEffectArcSegments, new Color("#10151a", effect.Style.ScorchAlpha * 0.7f * scorchFade), 1.5f, true);

        for (var index = 0; index < 4; index++)
        {
            var angle = NoiseAngle(effect.Seed + 503, index);
            var direction = Vector2.FromAngle(angle);
            var normal = direction.Orthogonal();
            var length = radius * (0.34f + Noise01(effect.Seed, index + 521) * 0.34f);
            var center = effect.Position + direction * radius * (0.12f + index * 0.055f);
            DrawLine(center - direction * length - normal * 2.2f, center + direction * length + normal * 2.2f, new Color("#02060a", 0.18f * scorchFade), 1.4f, true);
        }
    }

    private void DrawDeathFragments(UnitDeathEffect effect, float t, float fade)
    {
        var count = effect.Style.FragmentCount;
        for (var index = 0; index < count; index++)
        {
            var angle = NoiseAngle(effect.Seed, index);
            var direction = Vector2.FromAngle(angle);
            var distance = effect.Radius * 0.42f + t * (28 + Noise01(effect.Seed, index + 29) * 42) * effect.Style.BurstScale;
            var length = 5 + Noise01(effect.Seed, index + 47) * 12 * effect.Style.BurstScale;
            var center = effect.Position + direction * distance;
            var side = direction.Orthogonal() * (1.5f + Noise01(effect.Seed, index + 71) * 3.5f);
            var shardAlpha = fade * (0.38f + Noise01(effect.Seed, index + 83) * 0.28f);

            DrawLine(center - direction * length * 0.35f - side * 0.28f, center + direction * length + side * 0.28f, new Color(effect.Accent, shardAlpha), 1.5f, true);
            DrawLine(center - side * 0.55f, center + side * 0.55f, new Color("#ffffff", shardAlpha * 0.62f), 0.9f, true);
        }
    }

    private void DrawDeathSmoke(UnitDeathEffect effect, float t, float fade)
    {
        for (var index = 0; index < effect.Style.SmokeCount; index++)
        {
            var angle = NoiseAngle(effect.Seed + 97, index);
            var direction = Vector2.FromAngle(angle);
            var distance = effect.Radius * 0.34f + t * (16 + Noise01(effect.Seed, index + 113) * 20) * effect.Style.SmokeScale;
            var center = effect.Position + direction * distance;
            var radius = effect.Radius * (0.28f + Noise01(effect.Seed, index + 131) * 0.32f) * effect.Style.SmokeScale + t * 12;
            DrawCircle(center, radius, new Color("#02070d", 0.22f * fade));
        }
    }

    private void DrawDeathSpecials(UnitDeathEffect effect, float t, float fade)
    {
        if (effect.Style.EmitsEmbers)
        {
            for (var index = 0; index < 6; index++)
            {
                var direction = Vector2.FromAngle(NoiseAngle(effect.Seed + 211, index));
                var center = effect.Position + direction * (effect.Radius * 0.55f + t * (24 + index * 4) * effect.Style.BurstScale);
                DrawCircle(center, 2.2f + Noise01(effect.Seed, index + 229) * 2.4f, new Color("#ffb35c", 0.36f * fade));
            }
        }

        if (!effect.Style.EmitsEmpDissolve)
        {
            return;
        }

        for (var index = 0; index < 4; index++)
        {
            var radius = effect.Radius * (0.72f + index * 0.18f) + t * (18 + index * 8);
            var start = NoiseAngle(effect.Seed + 307, index) + t * 1.8f;
            DrawArc(effect.Position, radius, start, start + Mathf.Pi * 0.62f, 32, new Color(effect.Style.SecondaryColor, 0.46f * fade), 1.3f, true);
        }
    }

    private UnitDeathEffect RentUnitDeathEffect()
    {
        if (_pooledUnitDeaths.Count == 0)
        {
            return new UnitDeathEffect();
        }

        var last = _pooledUnitDeaths.Count - 1;
        var effect = _pooledUnitDeaths[last];
        _pooledUnitDeaths.RemoveAt(last);
        return effect;
    }

    private void ApplyDeathEffectBudget()
    {
        if (_unitDeaths.Count > DeathEffectSoftLimit)
        {
            var overflow = _unitDeaths.Count - DeathEffectSoftLimit;
            for (var index = 0; index < overflow && index < _unitDeaths.Count; index++)
            {
                _unitDeaths[index].FadeOutSoon(UnderLoadFadeSeconds);
            }
        }

        while (_unitDeaths.Count > DeathEffectHardLimit)
        {
            ReturnAndRemoveDeathEffect(0);
        }
    }

    private void ReturnAndRemoveDeathEffect(int index)
    {
        var effect = _unitDeaths[index];
        _unitDeaths.RemoveAt(index);
        if (_pooledUnitDeaths.Count < DeathEffectPoolLimit)
        {
            _pooledUnitDeaths.Add(effect);
        }
    }

    private sealed class UnitDeathEffect
    {
        public void Reset(UnitDeathInfo death, Color accent)
        {
            Reset(
                death.Id,
                death.Position,
                death.Radius,
                death.WeightClass,
                death.MovementDomain,
                death.KillingAmmoKind,
                death.OverkillDamage,
                accent);
        }

        public void Reset(UnitInstanceDeathInfo death, Color accent)
        {
            Reset(
                death.Id,
                death.Position,
                death.Radius,
                death.WeightClass,
                death.MovementDomain,
                death.KillingAmmoKind,
                death.OverkillDamage,
                accent);
        }

        private void Reset(
            int id,
            Vector2 position,
            float radius,
            UnitWeightClass weightClass,
            MovementDomain movementDomain,
            AmmoKind? killingAmmoKind,
            float overkillDamage,
            Color accent)
        {
            Position = position;
            Radius = radius;
            Accent = accent;
            Seed = id * 17 + (killingAmmoKind is null ? 0 : (int)killingAmmoKind.Value * 101);
            Style = DeathVfxMath.StyleFor(weightClass, movementDomain, killingAmmoKind, overkillDamage);
            Age = 0;
        }

        public void FadeOutSoon(float remainingSeconds)
        {
            Age = Mathf.Max(Age, Style.Lifetime - remainingSeconds);
        }

        public Vector2 Position { get; private set; }
        public float Radius { get; private set; }
        public Color Accent { get; private set; }
        public int Seed { get; private set; }
        public DeathVfxStyle Style { get; private set; }
        public float Age { get; set; }
    }
}
