using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class FootprintLayer
{
    private void DrawMark(FootprintMark mark)
    {
        var fade = 1 - Mathf.Clamp(mark.Age / mark.Lifetime, 0, 1);
        var color = new Color(mark.Color, mark.Color.A * fade * fade);
        if (color.A <= 0.002f)
        {
            return;
        }

        switch (mark.Kind)
        {
            case FootprintMarkKind.Step:
                DrawStep(mark, color);
                break;
            case FootprintMarkKind.TrackPlate:
                DrawTrackPlate(mark, color);
                break;
            case FootprintMarkKind.Wake:
                DrawWake(mark, color);
                break;
            case FootprintMarkKind.Contrail:
                DrawContrail(mark, color);
                break;
            default:
                DrawTwinTread(mark, color);
                break;
        }
    }

    private void DrawStep(FootprintMark mark, Color color)
    {
        var sideSign = mark.Alternate ? 1 : -1;
        var center = mark.Position + mark.Side * mark.LateralOffset * sideSign;
        var diagonal = (mark.Direction * 0.52f + mark.Side * sideSign * 0.48f).Normalized();
        DrawLine(center - diagonal * mark.Length * 0.5f, center + diagonal * mark.Length * 0.5f, color, mark.Width, true);
    }

    private void DrawTwinTread(FootprintMark mark, Color color)
    {
        var left = mark.Position + mark.Side * mark.LateralOffset;
        var right = mark.Position - mark.Side * mark.LateralOffset;
        DrawLine(left - mark.Direction * mark.Length * 0.5f, left + mark.Direction * mark.Length * 0.5f, color, mark.Width, true);
        DrawLine(right - mark.Direction * mark.Length * 0.5f, right + mark.Direction * mark.Length * 0.5f, color, mark.Width, true);
    }

    private void DrawTrackPlate(FootprintMark mark, Color color)
    {
        var left = mark.Position + mark.Side * mark.LateralOffset;
        var right = mark.Position - mark.Side * mark.LateralOffset;
        var width = mark.Width;
        DrawLine(left - mark.Direction * mark.Length * 0.38f, left + mark.Direction * mark.Length * 0.38f, color, width, true);
        DrawLine(right - mark.Direction * mark.Length * 0.38f, right + mark.Direction * mark.Length * 0.38f, color, width, true);
        DrawLine(mark.Position - mark.Side * mark.LateralOffset * 0.72f, mark.Position + mark.Side * mark.LateralOffset * 0.72f, new Color(color, color.A * 0.38f), width * 0.42f, true);
    }

    private void DrawWake(FootprintMark mark, Color color)
    {
        DrawArc(mark.Position, mark.Length, mark.Direction.Angle() + Mathf.Pi * 0.68f, mark.Direction.Angle() + Mathf.Pi * 1.32f, 24, color, mark.Width, true);
    }

    private void DrawContrail(FootprintMark mark, Color color)
    {
        DrawLine(mark.Position - mark.Direction * mark.Length, mark.Position, color, mark.Width, true);
    }
}
