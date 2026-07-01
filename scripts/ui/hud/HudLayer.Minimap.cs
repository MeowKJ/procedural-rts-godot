using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class MinimapSurface : Control
    {
        public Vector2 WorldSize { get; set; } = new(3600, 2400);
        public Rect2 CameraWorldRect { get; set; }
        public IReadOnlyList<MinimapUnit> Units { get; set; } = [];
        public IReadOnlyList<UnitMinimapPip> UnitDesignPips { get; set; } = [];
        public IReadOnlyList<MinimapBuilding> Buildings { get; set; } = [];
        public IReadOnlyList<MinimapResource> Resources { get; set; } = [];
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
            DrawRect(rect, CurrentPalette.Dark ? new Color("#121816", 0.96f) : new Color("#efe3cf", 0.78f), true);
            DrawRect(rect, new Color(CurrentPalette.PanelBorderStrong, 0.44f), false, 1.2f);

            for (var x = 0; x <= rect.Size.X; x += 24)
            {
                DrawLine(new Vector2(x, 0), new Vector2(x, rect.Size.Y), new Color(CurrentPalette.TextDim, CurrentPalette.Dark ? 0.08f : 0.10f), 1, true);
            }

            for (var y = 0; y <= rect.Size.Y; y += 24)
            {
                DrawLine(new Vector2(0, y), new Vector2(rect.Size.X, y), new Color(CurrentPalette.TextDim, CurrentPalette.Dark ? 0.08f : 0.10f), 1, true);
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

            var cameraRect = WorldRectToLocal(CameraWorldRect).Intersection(rect);
            if (cameraRect.Size.X > 0 && cameraRect.Size.Y > 0)
            {
                DrawRect(cameraRect, new Color(Ink, 0.18f), false, 1.6f);
                DrawRect(cameraRect.Grow(-2), new Color(Cyan, 0.30f), false, 1);
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

            DrawTextureRect(FogMask, bounds, false, new Color("#000000", 1.0f));
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
