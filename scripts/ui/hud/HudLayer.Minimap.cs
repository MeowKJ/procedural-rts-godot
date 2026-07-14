using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class MinimapSurface : Control
    {
        private static readonly Color UnexploredSurface = new("#26313B");
        private static readonly Color FogVeil = new("#26313B", 1.0f);

        public Vector2 WorldSize { get; set; } = new(3600, 2400);
        public Rect2 CameraWorldRect { get; set; }
        public IReadOnlyList<MinimapUnit> Units { get; set; } = [];
        public IReadOnlyList<UnitMinimapPip> UnitDesignPips { get; set; } = [];
        public IReadOnlyList<MinimapBuilding> Buildings { get; set; } = [];
        public IReadOnlyList<MinimapResource> Resources { get; set; } = [];
        public IReadOnlyList<MinimapAlertPing> AlertPings { get; set; } = [];
        public Texture2D? FogMask { get; set; }
        public FactionId ViewerFaction { get; set; } = FactionId.Dog;
        public Action<Vector2>? JumpRequested { get; init; }
        private bool _draggingCamera;

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse)
            {
                _draggingCamera = mouse.Pressed;
                if (mouse.Pressed)
                {
                    JumpToLocal(mouse.Position);
                }

                AcceptEvent();
                return;
            }

            if (@event is InputEventMouseMotion motion && _draggingCamera)
            {
                JumpToLocal(motion.Position);
                AcceptEvent();
            }
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            DrawRect(rect, UnexploredSurface, true);
            DrawRect(rect, new Color(CurrentPalette.PanelBorderStrong, 0.72f), false, 1.4f);

            for (var x = 0; x <= rect.Size.X; x += 24)
            {
                DrawLine(new Vector2(x, 0), new Vector2(x, rect.Size.Y), new Color(CurrentPalette.TextDim, 0.12f), 1, true);
            }

            for (var y = 0; y <= rect.Size.Y; y += 24)
            {
                DrawLine(new Vector2(0, y), new Vector2(rect.Size.X, y), new Color(CurrentPalette.TextDim, 0.12f), 1, true);
            }

            foreach (var resource in Resources)
            {
                var alpha = Mathf.Lerp(0.18f, 0.64f, Mathf.Clamp(resource.RemainingRatio, 0, 1));
                DrawCircle(WorldToLocal(resource.Position), Mathf.Max(2.4f, resource.Radius / WorldSize.X * rect.Size.X), new Color(Amber, alpha));
            }

            foreach (var building in Buildings)
            {
                var position = WorldToLocal(building.Position);
                var size = new Vector2(
                    Mathf.Max(4, building.Size.X / WorldSize.X * rect.Size.X),
                    Mathf.Max(4, building.Size.Y / WorldSize.Y * rect.Size.Y));
                var pip = new Rect2(position - size / 2f, size);
                DrawRect(pip, EntityPipColor(ViewerFaction, building.Owner, building.FactionId, building.Selected ? 0.95f : 0.72f), true);
                DrawRect(pip, new Color(Ink, building.Selected ? 0.75f : 0.18f), false, building.Selected ? 1.5f : 1);
                DrawAlert(position, building.AlertPulse, 7);
            }

            foreach (var unit in Units)
            {
                var position = WorldToLocal(unit.Position);
                DrawCircle(position, unit.Selected ? 3.2f : 2.4f, EntityPipColor(ViewerFaction, unit.Owner, unit.FactionId, unit.Selected ? 1f : 0.86f));
                if (unit.Selected)
                {
                    DrawCircle(position, 5.4f, new Color(Ink, 0.36f), false, 1.1f, true);
                }

                DrawAlert(position, unit.AlertPulse, 5.5f);
            }

            foreach (var unit in UnitDesignPips)
            {
                var position = WorldToLocal(unit.Position);
                DrawCircle(position, unit.Selected ? 3.2f : 2.4f, UnitDesignPipColor(unit, unit.Selected ? 1f : 0.86f));
                if (unit.Selected)
                {
                    DrawCircle(position, 5.4f, new Color(Ink, 0.36f), false, 1.1f, true);
                }

                DrawAlert(position, unit.AlertPulse, 5.5f);
            }

            DrawFog(rect);
            DrawFogTacticalGrid(rect);

            foreach (var ping in AlertPings)
            {
                DrawAlertPing(WorldToLocal(ping.Position), ping.Kind, ping.RemainingRatio);
            }

            var cameraRect = WorldRectToLocal(CameraWorldRect).Intersection(rect);
            if (cameraRect.Size.X > 0 && cameraRect.Size.Y > 0)
            {
                DrawRect(cameraRect, new Color(Ink, 0.78f), false, 1.8f);
                DrawRect(cameraRect.Grow(-2), new Color(Cyan, 0.62f), false, 1.1f);
            }
        }

        private Vector2 WorldToLocal(Vector2 world)
        {
            if (WorldSize.X <= 0 || WorldSize.Y <= 0)
            {
                return Vector2.Zero;
            }

            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            return new Vector2(
                Mathf.Clamp(world.X / WorldSize.X * rect.Size.X, 0, rect.Size.X),
                Mathf.Clamp(world.Y / WorldSize.Y * rect.Size.Y, 0, rect.Size.Y));
        }

        private Vector2 LocalToWorld(Vector2 local)
        {
            var rect = new Rect2(Vector2.Zero, CustomMinimumSize);
            return new Vector2(
                Mathf.Clamp(local.X / Mathf.Max(1, rect.Size.X), 0, 1) * WorldSize.X,
                Mathf.Clamp(local.Y / Mathf.Max(1, rect.Size.Y), 0, 1) * WorldSize.Y);
        }

        private void JumpToLocal(Vector2 localPosition)
        {
            JumpRequested?.Invoke(LocalToWorld(localPosition));
        }

        private Rect2 WorldRectToLocal(Rect2 worldRect)
        {
            var topLeft = WorldToLocal(worldRect.Position);
            var bottomRight = WorldToLocal(worldRect.Position + worldRect.Size);
            return new Rect2(topLeft, bottomRight - topLeft).Abs();
        }

        private void DrawFog(Rect2 bounds)
        {
            if (FogMask is null)
            {
                return;
            }

            DrawTextureRect(FogMask, bounds, false, FogVeil);
        }

        private void DrawFogTacticalGrid(Rect2 bounds)
        {
            for (var x = 12; x < bounds.Size.X; x += 24)
            {
                DrawLine(new Vector2(x, 0), new Vector2(x, bounds.Size.Y), new Color(Cyan, 0.055f), 1, true);
            }

            for (var y = 12; y < bounds.Size.Y; y += 12)
            {
                var alpha = y % 24 == 0 ? 0.065f : 0.035f;
                DrawLine(new Vector2(0, y), new Vector2(bounds.Size.X, y), new Color(CurrentPalette.TextDim, alpha), 1, true);
            }
        }

        private void DrawAlert(Vector2 position, float pulse, float baseRadius)
        {
            if (pulse <= 0.01f)
            {
                return;
            }

            var radius = baseRadius + pulse * 8;
            DrawCircle(position, radius, new Color(Danger, 0.2f * pulse), false, 1.4f, true);
            DrawCircle(position, radius + 3, new Color(Danger, 0.12f * pulse), false, 1, true);
        }

        private void DrawAlertPing(Vector2 position, AlertKind kind, float remainingRatio)
        {
            var pulse = Mathf.Clamp(remainingRatio, 0, 1);
            if (pulse <= 0.01f)
            {
                return;
            }

            var accent = SoftOldCityTheme.AccentForAlert(kind, CurrentPalette);
            var radius = 8 + (1 - pulse) * 16;
            DrawCircle(position, 3.2f, new Color(accent, 0.9f * pulse), true);
            DrawCircle(position, radius, new Color(accent, 0.42f * pulse), false, 1.8f, true);
            DrawCircle(position, radius + 4, new Color(accent, 0.2f * pulse), false, 1.1f, true);
        }

        private static Color EntityPipColor(FactionId viewerFaction, Owner owner, FactionId factionId, float alpha)
        {
            var color = FactionVisualPolicy.MinimapPip(ProceduralRts.Core.Owner.Player, viewerFaction, owner, factionId);
            color.A = alpha;
            return color;
        }

        private static Color UnitDesignPipColor(UnitMinimapPip unit, float alpha)
        {
            var color = unit.Relation switch
            {
                PlayerRelation.Self => new Color("#68a6c8"),
                PlayerRelation.Allied => new Color("#8abf74"),
                PlayerRelation.Neutral => new Color("#b7ad9c"),
                PlayerRelation.Hostile => new Color("#c15b6c"),
                _ => new Color("#b7ad9c"),
            };
            color.A = alpha;
            return color;
        }
    }
}
