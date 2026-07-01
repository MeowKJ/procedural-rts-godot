using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class MainMenuRoot
{
    private partial class MenuBackdrop : Control
    {
        public float Elapsed { get; set; }

        public override void _Draw()
        {
            var size = Size;
            DrawRect(new Rect2(Vector2.Zero, size), new Color("#02060a"));
            DrawGrid(size, 32, new Color("#59f1ff", 0.08f), 1);
            DrawGrid(size, 160, new Color("#8fffe1", 0.14f), 1.4f);
            DrawVectorSweep(size);
            DrawRect(new Rect2(Vector2.Zero, size), new Color("#59f1ff", 0.42f), false, 2);
        }

        private void DrawGrid(Vector2 size, int cell, Color color, float width)
        {
            var drift = (Elapsed * 8) % cell;
            for (var x = -cell + drift; x <= size.X + cell; x += cell)
            {
                DrawLine(new Vector2(x, 0), new Vector2(x, size.Y), color, width, true);
            }

            for (var y = -cell + drift * 0.5f; y <= size.Y + cell; y += cell)
            {
                DrawLine(new Vector2(0, y), new Vector2(size.X, y), color, width, true);
            }
        }

        private void DrawVectorSweep(Vector2 size)
        {
            var center = new Vector2(size.X * 0.38f, size.Y * 0.42f);
            var radius = Mathf.Min(size.X, size.Y) * 0.32f;
            var sweep = Elapsed * 0.52f;

            for (var index = 0; index < 7; index++)
            {
                var angle = sweep + index * Mathf.Tau / 7f;
                var end = center + Vector2.FromAngle(angle) * radius;
                var accent = index % 3 == 0 ? Cyan : index % 3 == 1 ? Mint : Amber;
                DrawLine(center, end, new Color(accent, 0.16f), 4, true);
                DrawLine(center, end, new Color("#ffffff", 0.12f), 1.1f, true);
                DrawCircle(end, 4.5f, new Color(accent, 0.66f));
            }

            for (var ring = 0; ring < 4; ring++)
            {
                var ringRadius = radius * (0.28f + ring * 0.22f);
                DrawArc(center, ringRadius, 0, Mathf.Tau, 128, new Color("#59f1ff", 0.15f - ring * 0.018f), 1.6f, true);
            }
        }
    }

    private partial class MenuTelemetry : Control
    {
        public override void _Draw()
        {
            var width = Size.X;
            var rowHeight = Size.Y / 3f;
            var rows = new[]
            {
                (GameText.T("menu.telemetry.units"), GameText.T("menu.telemetry.units.detail"), Cyan),
                (GameText.T("menu.telemetry.economy"), GameText.T("menu.telemetry.economy.detail"), Amber),
                (GameText.T("menu.telemetry.ai"), GameText.T("menu.telemetry.ai.detail"), Danger),
            };

            for (var index = 0; index < rows.Length; index++)
            {
                var y = index * rowHeight;
                DrawRect(new Rect2(0, y, width, rowHeight - 5), new Color("#02060a", 0.54f));
                DrawRect(new Rect2(0, y, width, rowHeight - 5), new Color(rows[index].Item3, 0.34f), false, 1);
                DrawString(ThemeDB.FallbackFont, new Vector2(12, y + 17), rows[index].Item1, HorizontalAlignment.Left, 70, 12, new Color(rows[index].Item3, 0.95f));
                DrawString(ThemeDB.FallbackFont, new Vector2(92, y + 17), rows[index].Item2, HorizontalAlignment.Left, width - 100, 12, InkMuted);
            }
        }
    }
}
