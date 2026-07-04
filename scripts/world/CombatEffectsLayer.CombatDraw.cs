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

            var readability = ReadabilityFor(unit.Position);
            if (!readability.Draw)
            {
                continue;
            }

            DrawArc(unit.Position, shareRadius, 0, Mathf.Tau, LargeEffectArcSegments, Readable(style.Accent, unit.AlertPulse * 0.12f, readability), ReadableWidth(1.6f, readability), true);
            DrawArc(unit.Position, shareRadius * 0.24f, 0, Mathf.Tau, MediumEffectArcSegments, Readable(new Color("#ffffff"), unit.AlertPulse * 0.24f, readability), ReadableWidth(1.2f, readability), true);
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

        var readability = ReadabilityForSegment(start, end);
        if (!readability.Draw)
        {
            return;
        }

        var normal = direction.Normalized().Orthogonal();
        var jitter = normal * (1.6f + pulse * 2.4f);
        var coreWidth = Mathf.Max(1.2f, width * (0.42f + pulse * 0.2f));

        DrawLine(start, end, Readable(accent, 0.18f * fade, readability), ReadableWidth(width * 3.8f, readability), true);
        DrawLine(start + jitter, end + jitter, Readable(accent, 0.32f * fade, readability), ReadableWidth(width * 1.4f, readability), true);
        DrawLine(start - jitter, end - jitter, Readable(accent, 0.22f * fade, readability), ReadableWidth(width, readability), true);
        DrawLine(start, end, Readable(new Color("#ffffff"), 0.84f * fade, readability), ReadableWidth(coreWidth, readability), true);

        DrawCircle(start, width * (0.8f + pulse), Readable(new Color("#ffffff"), 0.58f * fade, readability));
        DrawCircle(end, width * (1.1f + pulse * 1.3f), Readable(accent, 0.38f * fade, readability));
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

        var readability = ReadabilityForSegment(tail, position);
        if (!readability.Draw)
        {
            return;
        }

        DrawLine(tail, position, Readable(accent, style.TrailAlpha, readability), ReadableWidth(style.TrailWidth, readability), true);
        DrawLine(tail, position, Readable(new Color("#ffffff"), style.CoreAlpha, readability), ReadableWidth(style.CoreWidth, readability), true);
        DrawCircle(position, style.HeadRadius, Readable(accent, style.HeadAlpha, readability));
        if (isSeekerRocket)
        {
            DrawCircle(tail, style.HeadRadius * 0.74f, Readable(style.TailFlare, style.TailFlare.A, readability));
        }
    }
}
