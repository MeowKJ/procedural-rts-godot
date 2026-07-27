using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class BuildingView : Node2D
{
    private void DrawHeadquarters(Rect2 rect, Color accent, BuildingArtColors art, float pulse)
    {
        var core = new Rect2(rect.Position + rect.Size * 0.25f, rect.Size * 0.5f);
        DrawRect(core, new Color(art.Body, 0.82f), true);
        DrawRect(core, new Color(art.Highlight, 0.45f), false, 1.6f);
        DrawLine(new Vector2(0, rect.Position.Y + 12), new Vector2(0, rect.End.Y - 12), new Color(art.Effect, 0.55f), 3, true);
        DrawLine(new Vector2(rect.Position.X + 14, 0), new Vector2(rect.End.X - 14, 0), new Color(art.Effect, 0.38f), 2, true);
        DrawArc(Vector2.Zero, Mathf.Min(rect.Size.X, rect.Size.Y) * (0.22f + pulse * 0.02f), 0, Mathf.Tau, LargeArcSegments, new Color(art.Effect, 0.82f), 2.6f, true);
        DrawCircle(Vector2.Zero, 8 + pulse * 5, new Color(art.Highlight, 0.22f));
    }

    private void DrawPowerPlant(Rect2 rect, Color accent, BuildingArtColors art, float pulse)
    {
        var radius = Mathf.Min(rect.Size.X, rect.Size.Y) * 0.29f;
        DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, LargeArcSegments, new Color(art.Effect, 0.94f), 4.2f, true);
        DrawArc(Vector2.Zero, radius * 0.62f, 0, Mathf.Tau, MediumArcSegments, new Color(art.Highlight, 0.55f), 1.8f, true);
        DrawCircle(Vector2.Zero, radius * (0.26f + pulse * 0.08f), new Color(art.Effect, 0.23f));
        DrawLine(new Vector2(rect.Position.X + 12, -radius), new Vector2(rect.End.X - 12, -radius), new Color(art.Effect, 0.34f), 2, true);
        DrawLine(new Vector2(rect.Position.X + 12, radius), new Vector2(rect.End.X - 12, radius), new Color(art.Effect, 0.34f), 2, true);
    }

    private void DrawBarracks(Rect2 rect, Color accent, BuildingArtColors art)
    {
        var bayWidth = rect.Size.X / 4.8f;
        for (var index = -1; index <= 1; index++)
        {
            var bay = new Rect2(new Vector2(index * bayWidth - bayWidth / 2f, rect.Position.Y + 18), new Vector2(bayWidth, rect.Size.Y - 36));
            DrawRect(bay, new Color(art.Shadow, 0.10f), true);
            DrawRect(bay, new Color(art.Effect, 0.54f), false, 1.6f);
        }

        DrawLine(new Vector2(rect.Position.X + 16, rect.End.Y - 20), new Vector2(rect.End.X - 16, rect.End.Y - 20), new Color(art.Highlight, 0.42f), 2.2f, true);
    }

    private void DrawVehicleFactory(Rect2 rect, Color accent, BuildingArtColors art)
    {
        var ramp = new Rect2(rect.Position + new Vector2(22, rect.Size.Y * 0.48f), new Vector2(rect.Size.X - 44, rect.Size.Y * 0.34f));
        DrawRect(ramp, new Color(art.Shadow, 0.14f), true);
        DrawRect(ramp, new Color(art.Effect, 0.62f), false, 2.2f);
        DrawLine(new Vector2(ramp.Position.X, ramp.Position.Y + ramp.Size.Y / 2f), new Vector2(ramp.End.X, ramp.Position.Y + ramp.Size.Y / 2f), new Color(art.Highlight, 0.34f), 1.4f, true);
        DrawLine(new Vector2(rect.Position.X + 24, rect.Position.Y + 24), new Vector2(rect.End.X - 24, rect.Position.Y + 24), new Color(art.Effect, 0.44f), 3.2f, true);
        DrawLine(new Vector2(rect.Position.X + 24, rect.Position.Y + 42), new Vector2(rect.End.X - 24, rect.Position.Y + 42), new Color(art.Effect, 0.26f), 1.6f, true);
    }

    private void DrawRefinery(Rect2 rect, Color accent, BuildingArtColors art, float pulse)
    {
        var siloRadius = Mathf.Min(rect.Size.X, rect.Size.Y) * 0.18f;
        var left = new Vector2(rect.Position.X + rect.Size.X * 0.3f, -6);
        var right = new Vector2(rect.End.X - rect.Size.X * 0.3f, -6);
        DrawCircle(left, siloRadius, new Color(art.Shadow, 0.18f));
        DrawCircle(right, siloRadius, new Color(art.Shadow, 0.18f));
        DrawArc(left, siloRadius, 0, Mathf.Tau, 60, new Color(art.Effect, 0.82f), 2.3f, true);
        DrawArc(right, siloRadius, 0, Mathf.Tau, 60, new Color(art.Effect, 0.82f), 2.3f, true);
        DrawLine(left, right, new Color(art.Effect, 0.42f), 3, true);
        DrawRect(new Rect2(new Vector2(rect.Position.X + 18, rect.End.Y - 34), new Vector2(rect.Size.X - 36, 18)), new Color(art.Effect, 0.18f + pulse * 0.08f), true);
        DrawRefineryDock(rect, accent, art);
    }

    private void DrawRefineryDock(Rect2 rect, Color accent, BuildingArtColors art)
    {
        var dockCenter = new Vector2(rect.End.X + 52, 0);
        var pad = new Rect2(dockCenter - new Vector2(34, 18), new Vector2(68, 36));
        var deliveryPulse = _buildingProjection!.Value.DeliveryPulse;
        var dockOccupied = _buildingProjection.Value.DockOccupied;
        DrawRect(pad, new Color(art.Body, 0.64f), true);
        DrawRect(pad, new Color(art.Effect, 0.48f + deliveryPulse * 0.28f), false, 2.2f);
        DrawLine(pad.Position + new Vector2(10, pad.Size.Y / 2f), pad.End - new Vector2(10, pad.Size.Y / 2f), new Color(art.Highlight, 0.32f + deliveryPulse * 0.36f), 1.8f, true);

        if (dockOccupied)
        {
            DrawArc(dockCenter, 28 + deliveryPulse * 10, 0, Mathf.Tau, MediumArcSegments, new Color(SoftOldCityPalette.Cargo, 0.44f + deliveryPulse * 0.36f), 2.2f, true);
        }
    }

    private void DrawAirfield(Rect2 rect, Color accent, BuildingArtColors art)
    {
        var runway = new Rect2(rect.Position + new Vector2(20, rect.Size.Y * 0.38f), new Vector2(rect.Size.X - 40, rect.Size.Y * 0.24f));
        DrawRect(runway, new Color(art.Shadow, 0.12f), true);
        DrawRect(runway, new Color(art.Effect, 0.58f), false, 2.0f);
        DrawLine(new Vector2(runway.Position.X + 14, 0), new Vector2(runway.End.X - 14, 0), new Color(art.Highlight, 0.36f), 1.4f, true);
        DrawLine(new Vector2(rect.Position.X + 28, rect.Position.Y + 22), new Vector2(rect.End.X - 28, rect.Position.Y + 22), new Color(art.Effect, 0.42f), 2.4f, true);
        DrawLine(new Vector2(rect.Position.X + 28, rect.End.Y - 22), new Vector2(rect.End.X - 28, rect.End.Y - 22), new Color(art.Effect, 0.30f), 1.5f, true);
        DrawArc(Vector2.Zero, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.34f, -Mathf.Pi * 0.12f, Mathf.Pi * 1.12f, MediumArcSegments, new Color(art.Effect, 0.34f), 1.4f, true);
    }

    private void DrawTurretPlatform(Rect2 rect, BuildingArtColors art, float pulse, bool antiAir)
    {
        var radius = Mathf.Min(rect.Size.X, rect.Size.Y) * 0.33f;
        DrawCircle(Vector2.Zero, radius + 5, new Color(art.Shadow, 0.16f));
        DrawArc(Vector2.Zero, radius + 4, 0, Mathf.Tau, LargeArcSegments, new Color(art.Ink, 0.54f), 3.0f, true);
        DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, LargeArcSegments, new Color(art.Effect, 0.74f), 2.0f, true);
        DrawArc(Vector2.Zero, radius * 0.62f, 0, Mathf.Tau, MediumArcSegments, new Color(art.Highlight, 0.34f + pulse * 0.08f), 1.4f, true);
        DrawLine(new Vector2(-radius, -radius * 0.72f), new Vector2(radius, -radius * 0.72f), new Color(art.Effect, 0.28f), 1.4f, true);
        DrawLine(new Vector2(-radius, radius * 0.72f), new Vector2(radius, radius * 0.72f), new Color(art.Effect, 0.28f), 1.4f, true);

        var bodyFacing = _projection!.Value.Facing;
        DrawSetTransform(Vector2.Zero, _buildingProjection!.Value.TurretFacing - bodyFacing, Vector2.One);
        DrawCircle(Vector2.Zero, radius * 0.36f, new Color(art.Body, 0.78f));
        DrawArc(Vector2.Zero, radius * 0.36f, 0, Mathf.Tau, MediumArcSegments, new Color(art.Ink, 0.76f), 1.8f, true);
        DrawArc(Vector2.Zero, radius * 0.25f, 0, Mathf.Tau, SmallArcSegments, new Color(art.Owner, 0.46f), 1.2f, true);

        if (antiAir)
        {
            DrawLine(new Vector2(4, -4), new Vector2(radius + 17, -11), new Color(art.Ink, 0.90f), 3.4f, true);
            DrawLine(new Vector2(4, 4), new Vector2(radius + 17, 11), new Color(art.Ink, 0.90f), 3.4f, true);
            DrawLine(new Vector2(radius + 4, -10), new Vector2(radius + 17, -11), new Color(art.Highlight, 0.54f), 1.0f, true);
            DrawLine(new Vector2(radius + 4, 10), new Vector2(radius + 17, 11), new Color(art.Highlight, 0.54f), 1.0f, true);
        }
        else
        {
            DrawLine(new Vector2(4, 0), new Vector2(radius + 22, 0), new Color(art.Ink, 0.92f), 5.0f, true);
            DrawLine(new Vector2(12, 0), new Vector2(radius + 23, 0), new Color(art.Highlight, 0.42f), 1.1f, true);
            DrawLine(new Vector2(radius + 9, -6), new Vector2(radius + 21, 0), new Color(art.Effect, 0.46f), 1.1f, true);
            DrawLine(new Vector2(radius + 9, 6), new Vector2(radius + 21, 0), new Color(art.Effect, 0.46f), 1.1f, true);
        }

        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
