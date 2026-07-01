using Godot;

namespace ProceduralRts.Core;

public sealed partial class FogOfWarMap
{
    private bool AnyCell(Rect2 worldRect, bool visible)
    {
        if (Columns == 0 || Rows == 0)
        {
            return false;
        }

        var minX = Mathf.Clamp(Mathf.FloorToInt(worldRect.Position.X / CellSize), 0, Columns - 1);
        var maxX = Mathf.Clamp(Mathf.FloorToInt(worldRect.End.X / CellSize), 0, Columns - 1);
        var minY = Mathf.Clamp(Mathf.FloorToInt(worldRect.Position.Y / CellSize), 0, Rows - 1);
        var maxY = Mathf.Clamp(Mathf.FloorToInt(worldRect.End.Y / CellSize), 0, Rows - 1);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (visible ? _visible[x, y] : _explored[x, y])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryCell(Vector2 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt(worldPosition.X / CellSize);
        y = Mathf.FloorToInt(worldPosition.Y / CellSize);
        return x >= 0 && y >= 0 && x < Columns && y < Rows;
    }

    private Vector2 CellCenter(int x, int y)
    {
        return new Vector2((x + 0.5f) * CellSize, (y + 0.5f) * CellSize);
    }
}
