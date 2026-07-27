using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class CombatEffectsLayer : Node2D
{
    private bool IsVisible(Vector2 position, float radius)
    {
        return CullingWorldRect is not { } rect
            || rect.Intersects(new Rect2(position - Vector2.One * radius, Vector2.One * radius * 2f));
    }

    private bool IsSegmentVisible(Vector2 start, Vector2 end, float padding)
    {
        if (CullingWorldRect is not { } rect)
        {
            return true;
        }

        var min = new Vector2(Mathf.Min(start.X, end.X), Mathf.Min(start.Y, end.Y)) - Vector2.One * padding;
        var max = new Vector2(Mathf.Max(start.X, end.X), Mathf.Max(start.Y, end.Y)) + Vector2.One * padding;
        return rect.Intersects(new Rect2(min, max - min));
    }

    private bool IsProjectileVisibleToPlayer(Vector2 tail, Vector2 head)
    {
        return IsVisibleToPlayer(head)
            || IsVisibleToPlayer(tail)
            || IsVisibleToPlayer((tail + head) * 0.5f);
    }

    private CombatReadabilityStyle ReadabilityFor(Vector2 position)
    {
        var visible = IsVisibleToPlayer(position);
        var explored = visible || IsExploredByPlayer(position);
        return CombatReadabilityMath.StyleFor(visible, explored, ActiveEffectCount, CommandMarkerCount);
    }

    private CombatReadabilityStyle ReadabilityForSegment(Vector2 start, Vector2 end)
    {
        var midpoint = (start + end) * 0.5f;
        var visible = IsVisibleToPlayer(start) || IsVisibleToPlayer(end) || IsVisibleToPlayer(midpoint);
        var explored = visible || IsExploredByPlayer(start) || IsExploredByPlayer(end) || IsExploredByPlayer(midpoint);
        return CombatReadabilityMath.StyleFor(visible, explored, ActiveEffectCount, CommandMarkerCount);
    }

    private static Color Readable(Color color, float alpha, CombatReadabilityStyle style)
    {
        return new Color(color, alpha * style.AlphaScale);
    }

    private static float ReadableWidth(float width, CombatReadabilityStyle style)
    {
        return Mathf.Max(0.7f, width * style.LineWidthScale);
    }

    private static float NoiseAngle(int seed, int index)
    {
        return Noise01(seed, index) * Mathf.Tau;
    }

    private static float Noise01(int seed, int index)
    {
        var value = unchecked((uint)(seed * 73856093 ^ index * 19349663));
        value ^= value >> 13;
        value *= 1274126177;
        return (value & 1023) / 1023f;
    }
}
