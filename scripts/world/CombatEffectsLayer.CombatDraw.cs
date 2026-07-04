using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private void DrawThreatAlerts()
    {
        foreach (var unit in State.Units)
        {
            if (unit.AlertPulse <= 0)
            {
                continue;
            }

            var style = UnitEffectStyleFor(unit);
            var shareRadius = unit.Stance == UnitStance.PassiveRetaliate
                ? GameState.PassiveAllyCallRadius
                : GameState.AllyThreatShareRadius;
            if (!IsVisible(unit.Position, shareRadius))
            {
                continue;
            }

            DrawArc(unit.Position, shareRadius, 0, Mathf.Tau, LargeEffectArcSegments, new Color(style.Accent, unit.AlertPulse * 0.12f), 1.6f, true);
            DrawArc(unit.Position, shareRadius * 0.24f, 0, Mathf.Tau, MediumEffectArcSegments, new Color("#ffffff", unit.AlertPulse * 0.24f), 1.2f, true);
        }
    }

    private void DrawBeams()
    {
        foreach (var beam in State.Beams)
        {
            DrawBeam(beam.Start, beam.End, beam.Age, beam.Duration, beam.Width, beam.Accent);
        }

        foreach (var beam in _beamEffects)
        {
            DrawBeam(beam.Start, beam.End, beam.Age, beam.Duration, beam.Width, beam.Accent);
        }
    }

    private void DrawBeam(Vector2 start, Vector2 end, float age, float duration, float width, Color accent)
    {
        var fade = BeamMath.Fade(age, duration);
        var pulse = BeamMath.Pulse(age, duration);
        if (fade <= 0)
        {
            return;
        }

        var direction = end - start;
        if (direction.LengthSquared() <= 0.01f)
        {
            return;
        }

        if (!IsSegmentVisible(start, end, width * 4f))
        {
            return;
        }

        var normal = direction.Normalized().Orthogonal();
        var jitter = normal * (1.6f + pulse * 2.4f);
        var coreWidth = Mathf.Max(1.2f, width * (0.42f + pulse * 0.2f));

        DrawLine(start, end, new Color(accent, 0.18f * fade), width * 3.8f, true);
        DrawLine(start + jitter, end + jitter, new Color(accent, 0.32f * fade), width * 1.4f, true);
        DrawLine(start - jitter, end - jitter, new Color(accent, 0.22f * fade), width, true);
        DrawLine(start, end, new Color("#ffffff", 0.84f * fade), coreWidth, true);

        DrawCircle(start, width * (0.8f + pulse), new Color("#ffffff", 0.58f * fade));
        DrawCircle(end, width * (1.1f + pulse * 1.3f), new Color(accent, 0.38f * fade));
    }

    private void DrawProjectiles()
    {
        foreach (var projectile in State.Projectiles)
        {
            var style = ProjectileVfxMath.StyleFor(projectile.AmmoKind);
            DrawProjectile(
                projectile.Position,
                projectile.Velocity,
                style,
                projectile.Accent,
                projectile.AmmoKind == AmmoKind.SeekerRocket);
        }

        if (UnitBattlefield is null)
        {
            return;
        }

        UnitBattlefield.ProjectileProjections(_projectileProjections);
        foreach (var projectile in _projectileProjections)
        {
            DrawProjectile(
                projectile.Position,
                projectile.Velocity,
                projectile.Style,
                projectile.Accent,
                projectile.IsSeekerRocket);
        }
    }

    private void DrawProjectile(
        Vector2 position,
        Vector2 velocity,
        ProjectileVfxStyle style,
        Color accent,
        bool isSeekerRocket)
    {
        var direction = velocity.LengthSquared() <= 0.01f
            ? Vector2.Right
            : velocity.Normalized();

        var tail = position - direction * style.TailLength;
        if (!IsSegmentVisible(tail, position, style.CullingPadding)
            || !IsProjectileVisibleToPlayer(tail, position))
        {
            return;
        }

        DrawLine(tail, position, new Color(accent, style.TrailAlpha), style.TrailWidth, true);
        DrawLine(tail, position, new Color("#ffffff", style.CoreAlpha), style.CoreWidth, true);
        DrawCircle(position, style.HeadRadius, new Color(accent, style.HeadAlpha));
        if (isSeekerRocket)
        {
            DrawCircle(tail, style.HeadRadius * 0.74f, style.TailFlare);
        }
    }
}
