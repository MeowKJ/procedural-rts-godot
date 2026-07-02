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
            var fade = BeamMath.Fade(beam.Age, beam.Duration);
            var pulse = BeamMath.Pulse(beam.Age, beam.Duration);
            if (fade <= 0)
            {
                continue;
            }

            var direction = beam.End - beam.Start;
            if (direction.LengthSquared() <= 0.01f)
            {
                continue;
            }

            if (!IsSegmentVisible(beam.Start, beam.End, beam.Width * 4f))
            {
                continue;
            }

            var normal = direction.Normalized().Orthogonal();
            var jitter = normal * (1.6f + pulse * 2.4f);
            var coreWidth = Mathf.Max(1.2f, beam.Width * (0.42f + pulse * 0.2f));

            DrawLine(beam.Start, beam.End, new Color(beam.Accent, 0.18f * fade), beam.Width * 3.8f, true);
            DrawLine(beam.Start + jitter, beam.End + jitter, new Color(beam.Accent, 0.32f * fade), beam.Width * 1.4f, true);
            DrawLine(beam.Start - jitter, beam.End - jitter, new Color(beam.Accent, 0.22f * fade), beam.Width, true);
            DrawLine(beam.Start, beam.End, new Color("#ffffff", 0.84f * fade), coreWidth, true);

            DrawCircle(beam.Start, beam.Width * (0.8f + pulse), new Color("#ffffff", 0.58f * fade));
            DrawCircle(beam.End, beam.Width * (1.1f + pulse * 1.3f), new Color(beam.Accent, 0.38f * fade));
        }
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
