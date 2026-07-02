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
            DrawProjectile(
                projectile.Position,
                projectile.Velocity,
                projectile.AmmoKind == AmmoKind.SeekerRocket ? 34 : projectile.AmmoKind == AmmoKind.NeedleDart ? 28 : 22,
                projectile.TrailWidth,
                projectile.CoreWidth,
                projectile.HeadRadius,
                projectile.Accent,
                projectile.AmmoKind == AmmoKind.SeekerRocket);
        }

        if (UnitBattlefield is null)
        {
            return;
        }

        foreach (var projectile in UnitBattlefield.ProjectileProjections())
        {
            DrawProjectile(
                projectile.Position,
                projectile.Velocity,
                projectile.TailLength,
                projectile.TrailWidth,
                projectile.CoreWidth,
                projectile.HeadRadius,
                projectile.Accent,
                projectile.IsSeekerRocket);
        }
    }

    private void DrawProjectile(
        Vector2 position,
        Vector2 velocity,
        float tailLength,
        float trailWidth,
        float coreWidth,
        float headRadius,
        Color accent,
        bool isSeekerRocket)
    {
        if (!IsVisible(position, headRadius + 42))
        {
            return;
        }

        var direction = velocity.LengthSquared() <= 0.01f
            ? Vector2.Right
            : velocity.Normalized();

        var tail = position - direction * tailLength;
        DrawLine(tail, position, new Color(accent, 0.38f), trailWidth, true);
        DrawLine(tail, position, new Color("#ffffff", 0.72f), coreWidth, true);
        DrawCircle(position, headRadius, new Color(accent, 0.94f));
        if (isSeekerRocket)
        {
            DrawCircle(tail, headRadius * 0.74f, new Color("#ffefad", 0.34f));
        }
    }
}
