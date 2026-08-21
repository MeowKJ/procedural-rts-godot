using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private Vector2 ScreenToWorld(Vector2 screenPoint)
    {
        var viewportSize = GetViewportRect().Size;
        var zoom = Mathf.Max(Camera.Zoom.X, 0.001f);
        return Camera.GetScreenCenterPosition() + (screenPoint - viewportSize / 2f) / zoom;
    }

    private Rect2 VisibleWorldRect()
    {
        var size = GetViewportRect().Size / Mathf.Max(Camera.Zoom.X, 0.001f);
        return new Rect2(Camera.GetScreenCenterPosition() - size / 2f, size);
    }

    private float PickPaddingWorld()
    {
        const float desiredScreenPixels = 13;
        return SelectionMath.ScreenPixelsToWorld(desiredScreenPixels, Camera.Zoom.X);
    }

    private void DrawSelectionBox(Rect2 rect)
    {
        var pulse = 0.62f + Mathf.Sin(Time.GetTicksMsec() / 120f) * 0.18f;
        var bright = new Color("#d8f7ff", 0.86f + pulse * 0.12f);
        var cyan = new Color("#59f1ff", 0.68f);
        var fill = new Color("#59f1ff", 0.09f);
        var corner = Mathf.Clamp(Mathf.Min(rect.Size.X, rect.Size.Y) * 0.18f, 10, 28);

        DrawRect(rect, fill, true);
        DrawRect(rect, new Color("#05080f", 0.62f), false, 4.6f);
        DrawRect(rect, bright, false, 1.6f);

        DrawLine(rect.Position, rect.Position + new Vector2(corner, 0), cyan, 3, true);
        DrawLine(rect.Position, rect.Position + new Vector2(0, corner), cyan, 3, true);

        DrawLine(new Vector2(rect.End.X, rect.Position.Y), new Vector2(rect.End.X - corner, rect.Position.Y), cyan, 3, true);
        DrawLine(new Vector2(rect.End.X, rect.Position.Y), new Vector2(rect.End.X, rect.Position.Y + corner), cyan, 3, true);

        DrawLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.Position.X + corner, rect.End.Y), cyan, 3, true);
        DrawLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.Position.X, rect.End.Y - corner), cyan, 3, true);

        DrawLine(rect.End, rect.End - new Vector2(corner, 0), cyan, 3, true);
        DrawLine(rect.End, rect.End - new Vector2(0, corner), cyan, 3, true);

        var midY = rect.Position.Y + rect.Size.Y / 2f;
        DrawLine(new Vector2(rect.Position.X, midY), new Vector2(rect.End.X, midY), new Color("#59f1ff", 0.18f), 1, true);
    }

    private static Rect2 RectFromPoints(Vector2 a, Vector2 b)
    {
        var rect = SelectionMath.RectFromPoints(a.X, a.Y, b.X, b.Y);
        return new Rect2(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private UnitBattlefieldResourceNodeProjection? PickResourceNode(Vector2 worldPosition)
    {
        return UnitBattlefield.PickResourceNode(worldPosition, PickPaddingWorld());
    }

    private bool HasSelectedHarvester()
    {
        return HasSelectedRuntimeHarvester();
    }

    private bool HasSelectedBuildingForPreview()
    {
        return UnitBattlefield.HasSelectedBuildings(LocalPlayerSlotId);
    }

    private bool HasSelectedRuntimeHarvester()
    {
        foreach (var unit in UnitBattlefield.Units)
        {
            if (unit.PlayerSlotId == LocalPlayerSlotId && unit.Selected && IsHarvester(unit))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHarvester(UnitInstance unit)
    {
        return IsHarvesterSpec(unit.Spec);
    }

    private static bool IsHarvesterSpec(UnitSpec spec)
    {
        return (spec.RoleTags.Contains(UnitRoleTag.Economy) || spec.RoleTags.Contains(UnitRoleTag.Worker))
            && spec.HasAbility(AbilityKind.Harvest);
    }

    private static Color UnitRelationAccent(PlayerRelation relation)
    {
        return relation switch
        {
            PlayerRelation.Self => new Color("#68a6c8"),
            PlayerRelation.Allied => new Color("#8abf74"),
            PlayerRelation.Neutral => new Color("#b7ad9c"),
            PlayerRelation.Hostile => new Color("#c15b6c"),
            _ => new Color("#b7ad9c"),
        };
    }
}
