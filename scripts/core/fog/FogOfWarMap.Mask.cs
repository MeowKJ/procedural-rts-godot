using Godot;

namespace ProceduralRts.Core;

public sealed partial class FogOfWarMap
{
    private void UpdateMaskImage(Image image, MaskUpdateRange range)
    {
        if (!range.HasCells)
        {
            return;
        }

        for (var y = range.MinY; y <= range.MaxY; y++)
        {
            for (var x = range.MinX; x <= range.MaxX; x++)
            {
                image.SetPixel(x, y, MaskPixel(_visibleStrength[x, y], _exploredStrength[x, y]));
            }
        }
    }

    private MaskUpdateRange FullMaskRange()
    {
        return Columns == 0 || Rows == 0
            ? MaskUpdateRange.None
            : new MaskUpdateRange(0, Columns - 1, 0, Rows - 1);
    }

    private MaskUpdateRange CellRangeFor(Rect2? updateWorldRect)
    {
        if (Columns == 0 || Rows == 0)
        {
            return MaskUpdateRange.None;
        }

        if (updateWorldRect is not { } worldRect)
        {
            return new MaskUpdateRange(0, Columns - 1, 0, Rows - 1);
        }

        if (worldRect.End.X <= 0
            || worldRect.End.Y <= 0
            || worldRect.Position.X >= WorldSize.X
            || worldRect.Position.Y >= WorldSize.Y)
        {
            return MaskUpdateRange.None;
        }

        var minX = Mathf.Clamp(Mathf.FloorToInt(worldRect.Position.X / CellSize), 0, Columns - 1);
        var maxX = Mathf.Clamp(Mathf.FloorToInt(worldRect.End.X / CellSize), 0, Columns - 1);
        var minY = Mathf.Clamp(Mathf.FloorToInt(worldRect.Position.Y / CellSize), 0, Rows - 1);
        var maxY = Mathf.Clamp(Mathf.FloorToInt(worldRect.End.Y / CellSize), 0, Rows - 1);
        if (maxX < minX || maxY < minY)
        {
            return MaskUpdateRange.None;
        }

        return new MaskUpdateRange(minX, maxX, minY, maxY);
    }

    private static Color MaskPixel(float visibleStrength, float exploredStrength)
    {
        return FogOfWarVisualPolicy.MaskPixel(visibleStrength, exploredStrength);
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        return value * value * (3 - 2 * value);
    }

    private readonly record struct MaskUpdateRange(int MinX, int MaxX, int MinY, int MaxY)
    {
        public static MaskUpdateRange None { get; } = new(0, -1, 0, -1);

        public bool HasCells => MaxX >= MinX && MaxY >= MinY;

        public bool Covers(int columns, int rows)
        {
            return MinX == 0 && MinY == 0 && MaxX == columns - 1 && MaxY == rows - 1;
        }

        public bool Covers(MaskUpdateRange other)
        {
            return HasCells
                && other.HasCells
                && MinX <= other.MinX
                && MinY <= other.MinY
                && MaxX >= other.MaxX
                && MaxY >= other.MaxY;
        }

        public MaskUpdateRange Include(int x, int y)
        {
            return HasCells
                ? new MaskUpdateRange(
                    Math.Min(MinX, x),
                    Math.Max(MaxX, x),
                    Math.Min(MinY, y),
                    Math.Max(MaxY, y))
                : new MaskUpdateRange(x, x, y, y);
        }

        public MaskUpdateRange Union(MaskUpdateRange other)
        {
            if (!HasCells)
            {
                return other;
            }

            if (!other.HasCells)
            {
                return this;
            }

            return new MaskUpdateRange(
                Math.Min(MinX, other.MinX),
                Math.Max(MaxX, other.MaxX),
                Math.Min(MinY, other.MinY),
                Math.Max(MaxY, other.MaxY));
        }

        public MaskUpdateRange Intersection(MaskUpdateRange other)
        {
            if (!HasCells || !other.HasCells)
            {
                return None;
            }

            var minX = Math.Max(MinX, other.MinX);
            var maxX = Math.Min(MaxX, other.MaxX);
            var minY = Math.Max(MinY, other.MinY);
            var maxY = Math.Min(MaxY, other.MaxY);
            return maxX < minX || maxY < minY
                ? None
                : new MaskUpdateRange(minX, maxX, minY, maxY);
        }
    }
}
