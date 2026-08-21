using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private const float DragFeedbackMinWidthPixels = 92f;
    private const float DragFeedbackHeightPixels = 26f;
    private const float DragFeedbackInsetPixels = 8f;
    private const int DragFeedbackFontPixels = 10;

    private void DrawDragSelectionFeedback(Rect2 worldRect, bool additive)
    {
        var candidateCount = CountDragSelectionCandidates(worldRect);
        var label = GameText.Format(
            additive ? "selection.dragFeedback.add" : "selection.dragFeedback.select",
            candidateCount);
        var zoom = Mathf.Max(Camera.Zoom.X, 0.001f);
        var labelRect = DragSelectionFeedbackRect(worldRect, label, zoom);
        var accent = additive ? new Color("#8abf74") : new Color("#59f1ff");
        var fill = new Color("#05080f", 0.78f);
        var textColor = new Color("#eefaff", candidateCount > 0 ? 0.96f : 0.62f);
        var textInset = SelectionMath.ScreenPixelsToWorld(9f, zoom);
        var baseline = SelectionMath.ScreenPixelsToWorld(17f, zoom);
        var borderWidth = SelectionMath.ScreenPixelsToWorld(1.4f, zoom);
        var fontSize = Mathf.Max(1, Mathf.RoundToInt(DragFeedbackFontPixels / zoom));

        DrawRect(labelRect, fill, true);
        DrawRect(labelRect, new Color(accent, additive ? 0.86f : 0.72f), false, borderWidth, true);
        DrawString(
            UiFontProfile.DrawFont(UiFontRole.Compact),
            labelRect.Position + new Vector2(textInset, baseline),
            label,
            HorizontalAlignment.Left,
            labelRect.Size.X - textInset * 2f,
            fontSize,
            textColor);
    }

    private int CountDragSelectionCandidates(Rect2 worldRect)
    {
        return UnitBattlefield.CountSelectionRectCandidates(LocalPlayerSlotId, worldRect);
    }

    private Rect2 DragSelectionFeedbackRect(Rect2 worldRect, string label, float zoom)
    {
        var visibleRect = VisibleWorldRect();
        var widthPixels = Mathf.Max(DragFeedbackMinWidthPixels, 18f + label.Length * 7f);
        var width = SelectionMath.ScreenPixelsToWorld(widthPixels, zoom);
        var height = SelectionMath.ScreenPixelsToWorld(DragFeedbackHeightPixels, zoom);
        var inset = SelectionMath.ScreenPixelsToWorld(DragFeedbackInsetPixels, zoom);
        var position = new Vector2(worldRect.Position.X, worldRect.Position.Y - height - inset);
        var minX = visibleRect.Position.X + inset;
        var maxX = Mathf.Max(minX, visibleRect.End.X - width - inset);
        position.X = Mathf.Clamp(position.X, minX, maxX);
        if (position.Y < visibleRect.Position.Y + inset)
        {
            position.Y = worldRect.End.Y + inset;
        }

        return new Rect2(position, new Vector2(width, height));
    }
}
