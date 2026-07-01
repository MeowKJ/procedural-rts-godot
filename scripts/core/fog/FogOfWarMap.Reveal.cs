using Godot;

namespace ProceduralRts.Core;

public sealed partial class FogOfWarMap
{
    private void EnsureSize(Vector2 worldSize)
    {
        var maskSize = FogOfWarVisualPolicy.MaskSize(worldSize, CellSize);
        var columns = maskSize.X;
        var rows = maskSize.Y;
        if (columns == Columns && rows == Rows)
        {
            WorldSize = worldSize;
            return;
        }

        Columns = columns;
        Rows = rows;
        WorldSize = worldSize;
        _visible = new bool[Columns, Rows];
        _explored = new bool[Columns, Rows];
        _visibleStrength = new float[Columns, Rows];
        _exploredStrength = new float[Columns, Rows];
        _previousVisibleStrength = new float[Columns, Rows];
        _previousExploredStrength = new float[Columns, Rows];
        _maskImage = null;
        _maskTexture = null;
        MaskRevision++;
        _maskTextureDirty = true;
        _dirtyMaskRange = FullMaskRange();
        _statsDirty = true;
        _hasVisionSourceSignature = false;
    }

    private MaskUpdateRange MaskChangedSincePreviousUpdate()
    {
        var changedRange = MaskUpdateRange.None;
        for (var y = 0; y < Rows; y++)
        {
            for (var x = 0; x < Columns; x++)
            {
                if (!Mathf.IsEqualApprox(_visibleStrength[x, y], _previousVisibleStrength[x, y])
                    || !Mathf.IsEqualApprox(_exploredStrength[x, y], _previousExploredStrength[x, y]))
                {
                    changedRange = changedRange.Include(x, y);
                }
            }
        }

        return changedRange;
    }

    private void Reveal(Vector2 position, float sightRange)
    {
        var radius = Math.Max(CellSize * 0.65f, sightRange);
        var radiusWithCellEdge = radius + CellSize * 0.62f;
        var visualFeather = Math.Max(CellSize * 2.25f, radius * 0.18f);
        var visualRadius = radiusWithCellEdge + visualFeather;
        var radiusSquared = radiusWithCellEdge * radiusWithCellEdge;
        var visualRadiusSquared = visualRadius * visualRadius;
        var minX = Mathf.Clamp(Mathf.FloorToInt((position.X - visualRadius) / CellSize), 0, Columns - 1);
        var maxX = Mathf.Clamp(Mathf.FloorToInt((position.X + visualRadius) / CellSize), 0, Columns - 1);
        var minY = Mathf.Clamp(Mathf.FloorToInt((position.Y - visualRadius) / CellSize), 0, Rows - 1);
        var maxY = Mathf.Clamp(Mathf.FloorToInt((position.Y + visualRadius) / CellSize), 0, Rows - 1);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var center = CellCenter(x, y);
                var distanceSquared = center.DistanceSquaredTo(position);
                if (distanceSquared > visualRadiusSquared)
                {
                    continue;
                }

                if (distanceSquared <= radiusSquared)
                {
                    _visibleStrength[x, y] = 1;
                    _exploredStrength[x, y] = 1;
                    _visible[x, y] = true;
                    _explored[x, y] = true;
                    continue;
                }

                var distance = MathF.Sqrt(distanceSquared);
                var strength = Smooth01((visualRadius - distance) / visualFeather);
                _visibleStrength[x, y] = Math.Max(_visibleStrength[x, y], strength);
                _exploredStrength[x, y] = Math.Max(_exploredStrength[x, y], strength);
            }
        }
    }
}
