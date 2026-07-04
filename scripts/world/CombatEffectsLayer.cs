using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private const int DeathEffectSoftLimit = 56;
    private const int DeathEffectHardLimit = 72;
    private const int DeathEffectPoolLimit = 96;
    private const int ImpactFlashSoftLimit = 96;
    private const int ImpactFlashHardLimit = 128;
    private const int ImpactFlashPoolLimit = 160;
    private const int MuzzleFlashSoftLimit = 96;
    private const int MuzzleFlashHardLimit = 128;
    private const int MuzzleFlashPoolLimit = 160;
    private const int BeamEffectSoftLimit = 48;
    private const int BeamEffectHardLimit = 64;
    private const float UnderLoadFadeSeconds = 0.35f;
    private const float ImpactFlashLifetime = 0.32f;
    private const float MuzzleFlashLifetime = 0.16f;
    private const int LargeEffectArcSegments = 48;
    private const int MediumEffectArcSegments = 36;
    private const int SmallEffectArcSegments = 24;

    public required GameState State { get; init; }
    public UnitBattlefield? UnitBattlefield { get; init; }
    public Rect2? CullingWorldRect { get; set; }

    private readonly List<UnitDeathEffect> _unitDeaths = [];
    private readonly List<UnitDeathEffect> _pooledUnitDeaths = [];
    private readonly List<ImpactFlashEffect> _impactFlashes = [];
    private readonly List<ImpactFlashEffect> _pooledImpactFlashes = [];
    private readonly List<MuzzleFlashEffect> _muzzleFlashes = [];
    private readonly List<MuzzleFlashEffect> _pooledMuzzleFlashes = [];
    private readonly List<BeamEffect> _beamEffects = [];
    private readonly List<ProjectilePresentationProjection> _projectileProjections = [];
    public int ActiveEffectCount =>
        _unitDeaths.Count
        + _impactFlashes.Count
        + _muzzleFlashes.Count
        + _beamEffects.Count
        + State.Projectiles.Count
        + State.Beams.Count
        + (UnitBattlefield?.ProjectileProjectionCount() ?? 0);

    public void AddUnitDeath(UnitDeathInfo death, Color accent)
    {
        var effect = RentUnitDeathEffect();
        effect.Reset(death, accent);
        _unitDeaths.Add(effect);
        ApplyDeathEffectBudget();
    }

    public void AddUnitDeath(UnitInstanceDeathInfo death, Color accent)
    {
        var effect = RentUnitDeathEffect();
        effect.Reset(death, accent);
        _unitDeaths.Add(effect);
        ApplyDeathEffectBudget();
    }

    public ImpactVfxStyle AddImpactFlash(
        Vector2 position,
        float radius,
        Color accent,
        UnitWeightClass weightClass = UnitWeightClass.Medium,
        MovementDomain movementDomain = MovementDomain.Land,
        float damage = 0,
        AmmoKind? ammoKind = null)
    {
        var style = ImpactVfxMath.StyleFor(weightClass, movementDomain, ammoKind, damage);
        var effect = RentImpactFlashEffect();
        effect.Reset(position, radius, accent, damage, ammoKind, style);
        _impactFlashes.Add(effect);
        ApplyImpactFlashBudget();
        return style;
    }

    public void AddMuzzleFlash(Vector2 position, Vector2 targetPosition, Color accent, WeaponKind? weaponKind = null)
    {
        var effect = RentMuzzleFlashEffect();
        effect.Reset(position, targetPosition, accent, weaponKind);
        _muzzleFlashes.Add(effect);
        ApplyMuzzleFlashBudget();
    }

    public void AddBeam(Vector2 start, Vector2 end, float duration, float width, Color accent)
    {
        if (duration <= 0 || width <= 0 || start.DistanceSquaredTo(end) <= 0.01f)
        {
            return;
        }

        _beamEffects.Add(new BeamEffect(start, end, duration, width, accent));
        ApplyBeamEffectBudget();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        for (var index = _beamEffects.Count - 1; index >= 0; index--)
        {
            var effect = _beamEffects[index];
            effect.Age += dt;
            if (effect.Age >= effect.Duration)
            {
                _beamEffects.RemoveAt(index);
            }
        }

        for (var index = _impactFlashes.Count - 1; index >= 0; index--)
        {
            _impactFlashes[index].Age += dt;
            if (_impactFlashes[index].Age >= ImpactFlashLifetime)
            {
                ReturnAndRemoveImpactFlash(index);
            }
        }

        for (var index = _muzzleFlashes.Count - 1; index >= 0; index--)
        {
            _muzzleFlashes[index].Age += dt;
            if (_muzzleFlashes[index].Age >= MuzzleFlashLifetime)
            {
                ReturnAndRemoveMuzzleFlash(index);
            }
        }

        for (var index = _unitDeaths.Count - 1; index >= 0; index--)
        {
            _unitDeaths[index].Age += dt;
            if (_unitDeaths[index].Age >= _unitDeaths[index].Style.Lifetime)
            {
                ReturnAndRemoveDeathEffect(index);
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawThreatAlerts();
        DrawUnitDeaths();
        DrawMuzzleFlashes();
        DrawBeams();
        DrawProjectiles();
        DrawImpactFlashes();
        DrawHitPulses();
    }

    private void ApplyBeamEffectBudget()
    {
        if (_beamEffects.Count > BeamEffectSoftLimit)
        {
            var overflow = _beamEffects.Count - BeamEffectSoftLimit;
            for (var index = 0; index < overflow && index < _beamEffects.Count; index++)
            {
                _beamEffects[index].FadeOutSoon(UnderLoadFadeSeconds);
            }
        }

        while (_beamEffects.Count > BeamEffectHardLimit)
        {
            _beamEffects.RemoveAt(0);
        }
    }

    private sealed class BeamEffect(Vector2 start, Vector2 end, float duration, float width, Color accent)
    {
        public Vector2 Start { get; } = start;
        public Vector2 End { get; } = end;
        public float Duration { get; } = duration;
        public float Width { get; } = width;
        public Color Accent { get; } = accent;
        public float Age { get; set; }

        public void FadeOutSoon(float remainingSeconds)
        {
            Age = Mathf.Max(Age, Duration - remainingSeconds);
        }
    }
}
