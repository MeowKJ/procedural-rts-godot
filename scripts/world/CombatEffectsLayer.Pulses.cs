using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private void DrawHitPulses()
    {
        foreach (var unit in State.Units)
        {
            if (unit.HitPulse <= 0)
            {
                continue;
            }

            var style = UnitEffectStyleFor(unit);
            var radius = style.Radius + 34;
            if (!IsVisible(unit.Position, radius))
            {
                continue;
            }

            var pulseRadius = style.Radius + 8 + (1 - unit.HitPulse) * 22;
            DrawArc(unit.Position, pulseRadius, 0, Mathf.Tau, MediumEffectArcSegments, new Color(style.Accent, unit.HitPulse * 0.85f), 3, true);
            DrawCircle(unit.Position, style.Radius + 7, new Color("#ffffff", unit.HitPulse * 0.16f));
        }

        if (UnitBattlefield is not null && UnitBattlefield.LiveBuildingCount() > 0)
        {
            DrawUnitBattlefieldBuildingHitPulses();
            return;
        }

        foreach (var building in State.Buildings)
        {
            if (building.HitPulse <= 0)
            {
                continue;
            }

            var spec = BuildSpecCatalog.For(building.Kind);
            var accent = State.VisualAccent(building.Owner, building.FactionId, spec.Accent);
            var radius = Mathf.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f + 12 + (1 - building.HitPulse) * 30;
            if (!IsVisible(building.Position, radius))
            {
                continue;
            }

            DrawArc(building.Position, radius, 0, Mathf.Tau, LargeEffectArcSegments, new Color(accent, building.HitPulse * 0.75f), 4, true);
            DrawCircle(building.Position, radius * 0.72f, new Color("#ffffff", building.HitPulse * 0.08f));
        }
    }

    private (float Radius, Color Accent) UnitEffectStyleFor(UnitModel unit)
    {
        var descriptor = unit.RuntimeDescriptor;
        return (descriptor.Radius, State.VisualAccent(unit.Owner, unit.FactionId, descriptor.Accent));
    }

    private void DrawUnitBattlefieldBuildingHitPulses()
    {
        foreach (var building in UnitBattlefield!.BuildingHitPulseProjections())
        {
            var radius = building.Radius + 12 + (1 - building.HitPulse) * 30;
            if (!IsVisible(building.Position, radius))
            {
                continue;
            }

            DrawArc(building.Position, radius, 0, Mathf.Tau, LargeEffectArcSegments, new Color(building.Accent, building.HitPulse * 0.75f), 4, true);
            DrawCircle(building.Position, radius * 0.72f, new Color("#ffffff", building.HitPulse * 0.08f));
        }
    }
}
