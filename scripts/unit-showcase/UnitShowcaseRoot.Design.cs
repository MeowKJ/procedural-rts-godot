using Godot;

namespace ProceduralRts;

public partial class UnitShowcaseRoot
{
    private void DrawUnit(UnitShape shape, Vector2 center, float scale, Color accent, Color dark, Role role)
    {
        var roleColor = RoleColor(role, accent);
        DrawCircle(center + new Vector2(0, 8 * scale), 34 * scale, new Color("#16120c", 0.10f));

        switch (shape)
        {
            case UnitShape.DogInfantry:
                DrawDogInfantry(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.CatInfantry:
                DrawCatInfantry(center, scale, accent, dark, roleColor, false);
                break;
            case UnitShape.CatRocket:
                DrawCatInfantry(center, scale, accent, dark, roleColor, true);
                break;
            case UnitShape.CatSniper:
                DrawCatSniper(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.CatSpecial:
                DrawCatSpecial(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.DogTank:
                DrawTank(center, scale, accent, dark, roleColor, 0);
                break;
            case UnitShape.CatTank:
                DrawNeedleTank(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.HeavyTank:
                DrawTank(center, scale, accent, dark, roleColor, 1);
                break;
            case UnitShape.Artillery:
                DrawArtillery(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.ShieldTank:
                DrawShieldTank(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.RepairTank:
                DrawRepairTank(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.UtilityTruck:
                DrawUtilityTruck(center, scale, accent, dark, roleColor);
                break;
            case UnitShape.Fighter:
                DrawFighter(center, scale, accent, dark, roleColor);
                break;
            default:
                DrawAircraft(center, scale, accent, dark, roleColor);
                break;
        }
    }

    private void DrawDogInfantry(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        var body = Points(c, s, [new(0, -30), new(24, -8), new(18, 22), new(0, 34), new(-18, 22), new(-24, -8)]);
        DrawToken(body, accent, dark, 0.30f, 2.7f);
        DrawLine(c + new Vector2(-11, -8) * s, c + new Vector2(11, -8) * s, new Color(role, 0.82f), 4 * s, true);
        DrawLine(c + new Vector2(0, -20) * s, c + new Vector2(0, 20) * s, new Color("#fff1d5", 0.46f), 2 * s, true);
        DrawCircle(c + new Vector2(0, 2) * s, 11 * s, new Color(role, 0.18f), false, 3 * s, true);
    }

    private void DrawCatInfantry(Vector2 c, float s, Color accent, Color dark, Color role, bool rocket)
    {
        var body = Points(c, s, [new(30, 0), new(-14, -26), new(-4, 0), new(-14, 26)]);
        DrawToken(body, accent, dark, 0.24f, 2.5f);
        DrawLine(c + new Vector2(-7, 0) * s, c + new Vector2(24, 0) * s, new Color(role, 0.82f), 2.4f * s, true);
        DrawArc(c, 27 * s, -0.7f, 0.7f, 24, new Color(role, 0.42f), 2.1f * s, true);
        if (rocket)
        {
            DrawLine(c + new Vector2(-12, -18) * s, c + new Vector2(23, -9) * s, new Color("#fff4e0", 0.76f), 4.2f * s, true);
            DrawCircle(c + new Vector2(26, -8) * s, 4.8f * s, new Color(role, 0.92f));
        }
    }

    private void DrawCatSniper(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        DrawCatInfantry(c, s, accent, dark, role, false);
        DrawLine(c + new Vector2(12, -4) * s, c + new Vector2(50, -9) * s, new Color("#201e31", 0.80f), 3.6f * s, true);
        DrawLine(c + new Vector2(17, -4) * s, c + new Vector2(52, -9) * s, new Color(role, 0.95f), 1.6f * s, true);
    }

    private void DrawCatSpecial(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        DrawCatInfantry(c, s, accent, dark, role, false);
        DrawArc(c, 39 * s, -0.25f, Mathf.Tau - 0.25f, 72, new Color(role, 0.28f), 2.2f * s, true);
        DrawDashedCircle(c, 48 * s, new Color(accent, 0.36f), 1.6f * s);
    }

    private void DrawTank(Vector2 c, float s, Color accent, Color dark, Color role, int heavy)
    {
        var body = Points(c, s, [new(-44, -22), new(26, -22), new(44, -8), new(44, 10), new(24, 22), new(-42, 22), new(-54, 10), new(-54, -10)]);
        DrawToken(body, accent, dark, 0.26f + heavy * 0.06f, 3.0f);
        DrawLine(c + new Vector2(-32, -12) * s, c + new Vector2(20, -12) * s, new Color("#fff0d2", 0.42f), 2.2f * s, true);
        DrawLine(c + new Vector2(-32, 12) * s, c + new Vector2(20, 12) * s, new Color("#fff0d2", 0.42f), 2.2f * s, true);
        DrawCircle(c, (14 + heavy * 4) * s, new Color(role, 0.20f));
        DrawCircle(c, (14 + heavy * 4) * s, new Color(role, 0.86f), false, 2.4f * s, true);
        DrawLine(c + new Vector2(6, 0) * s, c + new Vector2(56 + heavy * 8, 0) * s, new Color(role, 0.92f), (6 + heavy) * s, true);
        DrawLine(c + new Vector2(8, 0) * s, c + new Vector2(58 + heavy * 8, 0) * s, new Color("#fff7e7", 0.54f), 1.5f * s, true);
    }

    private void DrawNeedleTank(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        var body = Points(c, s, [new(50, 0), new(18, -21), new(-40, -18), new(-52, 0), new(-38, 18), new(18, 21)]);
        DrawToken(body, accent, dark, 0.23f, 2.7f);
        DrawLine(c + new Vector2(-26, -10) * s, c + new Vector2(18, -10) * s, new Color("#eee8ff", 0.38f), 2.0f * s, true);
        DrawLine(c + new Vector2(-26, 10) * s, c + new Vector2(18, 10) * s, new Color("#eee8ff", 0.38f), 2.0f * s, true);
        DrawLine(c + new Vector2(8, 0) * s, c + new Vector2(64, 0) * s, new Color(role, 0.92f), 3.2f * s, true);
    }

    private void DrawArtillery(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        DrawTank(c, s, accent, dark, role, 0);
        DrawArc(c + new Vector2(8, 0) * s, 35 * s, -0.42f, 0.42f, 24, new Color(role, 0.38f), 2.2f * s, true);
        DrawLine(c + new Vector2(8, 0) * s, c + new Vector2(78, -22) * s, new Color(role, 0.88f), 7 * s, true);
        DrawCircle(c + new Vector2(83, -24) * s, 5 * s, new Color(Red, 0.78f));
    }

    private void DrawShieldTank(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        DrawTank(c, s, accent, dark, role, 1);
        DrawArc(c, 58 * s, 0, Mathf.Tau, 96, new Color(role, 0.28f), 3.0f * s, true);
        DrawArc(c, 47 * s, 0.2f, Mathf.Tau - 0.2f, 96, new Color("#fff5dd", 0.22f), 1.6f * s, true);
    }

    private void DrawRepairTank(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        DrawTank(c, s, accent, dark, role, 0);
        DrawCircle(c, 31 * s, new Color(role, 0.18f), false, 2.3f * s, true);
        DrawLine(c + new Vector2(-13, 0) * s, c + new Vector2(13, 0) * s, new Color(role, 0.92f), 4.2f * s, true);
        DrawLine(c + new Vector2(0, -13) * s, c + new Vector2(0, 13) * s, new Color(role, 0.92f), 4.2f * s, true);
    }

    private void DrawUtilityTruck(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        var body = Points(c, s, [new(-48, -18), new(24, -18), new(48, 0), new(24, 18), new(-48, 18), new(-58, 0)]);
        DrawToken(body, accent, dark, 0.22f, 2.8f);
        DrawLine(c + new Vector2(-28, -8) * s, c + new Vector2(15, -8) * s, new Color(role, 0.84f), 5 * s, true);
        DrawLine(c + new Vector2(-28, 8) * s, c + new Vector2(15, 8) * s, new Color(role, 0.84f), 5 * s, true);
        DrawCircle(c + new Vector2(34, 0) * s, 9 * s, new Color(role, 0.28f));
    }

    private void DrawAircraft(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        var body = Points(c, s, [new(0, -50), new(16, -8), new(47, 10), new(12, 12), new(0, 42), new(-12, 12), new(-47, 10), new(-16, -8)]);
        DrawToken(body, accent, dark, 0.22f, 2.4f);
        DrawLine(c + new Vector2(0, -34) * s, c + new Vector2(0, 24) * s, new Color(role, 0.72f), 2.4f * s, true);
    }

    private void DrawFighter(Vector2 c, float s, Color accent, Color dark, Color role)
    {
        DrawAircraft(c, s, accent, dark, role);
        DrawLine(c + new Vector2(-34, 8) * s, c + new Vector2(-58, 24) * s, new Color(role, 0.88f), 3.2f * s, true);
        DrawLine(c + new Vector2(34, 8) * s, c + new Vector2(58, 24) * s, new Color(role, 0.88f), 3.2f * s, true);
    }
}
