using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class ResourceFieldView : Node2D
{
    private const float RedrawIntervalSeconds = 1f / 20f;
    private const int OuterArcSegments = 48;
    private const int InnerArcSegments = 40;
    private const int AmountArcSegments = 48;
    private float _redrawTimer;

    public required ResourceFieldModel Field { get; init; }

    public override void _Process(double delta)
    {
        Position = Field.Position;
        _redrawTimer -= (float)delta;
        if (_redrawTimer > 0)
        {
            return;
        }

        _redrawTimer = RedrawIntervalSeconds;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var fullness = Field.MaxAmount <= 0 ? 0 : Mathf.Clamp((float)Field.Amount / Field.MaxAmount, 0, 1);
        var pulse = 0.58f + Mathf.Sin((float)Time.GetTicksMsec() / 360f + Field.Id * 1.7f) * 0.18f;
        var active = fullness > 0 ? 1 : 0.22f;

        DrawArc(Vector2.Zero, Field.Radius, 0, Mathf.Tau, OuterArcSegments, new Color(Field.Accent, (0.26f + Field.Pulse * 0.2f) * active), 2.2f, true);
        DrawArc(Vector2.Zero, Field.Radius * (0.64f + pulse * 0.025f), 0, Mathf.Tau, InnerArcSegments, new Color("#ffffff", (0.11f + Field.Pulse * 0.12f) * active), 1.2f, true);
        DrawCircle(Vector2.Zero, Field.Radius * (0.48f + Field.Pulse * 0.03f), new Color(Field.Accent, (0.045f + Field.Pulse * 0.035f) * active));

        DrawMineralNodes(fullness, pulse);
        DrawAmountRing(fullness);
    }

    private void DrawMineralNodes(float fullness, float pulse)
    {
        var nodeCount = Mathf.Max(8, Mathf.RoundToInt(42 * fullness));
        for (var i = 0; i < nodeCount; i++)
        {
            var angle = i * 2.39996f + Field.Id * 0.31f;
            var band = 0.18f + ((i * 37) % 100) / 100f * 0.74f;
            var wobble = Mathf.Sin((float)Time.GetTicksMsec() / 520f + i * 0.77f) * 3.8f;
            var radius = Field.Radius * band + wobble;
            var position = Vector2.FromAngle(angle) * radius;
            var size = 3.5f + i % 5 + pulse * 1.2f;
            var alpha = (0.2f + fullness * 0.58f) * (0.74f + (i % 4) * 0.08f);

            DrawCircle(position, size + 5, new Color(Field.Accent, alpha * 0.12f));
            DrawCircle(position, size, new Color(Field.Accent, alpha));
            DrawCircle(position + new Vector2(size * 0.28f, -size * 0.22f), Mathf.Max(1.2f, size * 0.28f), new Color("#ffffff", alpha * 0.42f));
        }
    }

    private void DrawAmountRing(float fullness)
    {
        var start = -Mathf.Pi / 2f;
        var end = start + Mathf.Tau * fullness;
        DrawArc(Vector2.Zero, Field.Radius + 12, start, end, AmountArcSegments, new Color(Field.Accent, 0.82f), 3.4f, true);
        DrawArc(Vector2.Zero, Field.Radius + 18, 0, Mathf.Tau, OuterArcSegments, new Color("#05080f", 0.55f), 1.4f, true);
    }
}
