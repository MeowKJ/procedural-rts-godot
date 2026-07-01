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
    private const float UnderLoadFadeSeconds = 0.35f;
    private const float ImpactFlashLifetime = 0.32f;
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
    public int ActiveEffectCount => _unitDeaths.Count + _impactFlashes.Count + State.Projectiles.Count + State.Beams.Count;

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

    public void AddImpactFlash(
        Vector2 position,
        float radius,
        Color accent,
        UnitWeightClass weightClass = UnitWeightClass.Medium,
        MovementDomain movementDomain = MovementDomain.Land,
        float damage = 0,
        AmmoKind? ammoKind = null)
    {
        var effect = RentImpactFlashEffect();
        effect.Reset(position, radius, accent, weightClass, movementDomain, damage, ammoKind);
        _impactFlashes.Add(effect);
        ApplyImpactFlashBudget();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        for (var index = _impactFlashes.Count - 1; index >= 0; index--)
        {
            _impactFlashes[index].Age += dt;
            if (_impactFlashes[index].Age >= ImpactFlashLifetime)
            {
                ReturnAndRemoveImpactFlash(index);
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
        DrawBeams();
        DrawProjectiles();
        DrawImpactFlashes();
        DrawHitPulses();
    }
}
