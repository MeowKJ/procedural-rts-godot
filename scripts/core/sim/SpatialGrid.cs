using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Deterministic uniform spatial hash for broadphase neighbor queries, extracted
/// from the copies that CombatSystem, VisionSystem, and SeparationSystem each
/// rolled by hand (M9 - Elegance &amp; Decoupling). One guarded cell quantization
/// lives here, removing the divergent `Cell()` implementations (VisionSystem's
/// lacked the divide-by-zero guard CombatSystem had).
///
/// Determinism: buckets are keyed by integer cell and iterated in sorted cell
/// order; within a cell, insertion order is preserved. Callers that need a fully
/// stable scan still compare by EntityId, exactly as before. Reusable across
/// ticks - call <see cref="Clear"/> then re-<see cref="Add"/>; bucket lists are
/// retained to avoid per-tick allocation.
/// </summary>
public sealed class SpatialGrid<T>
{
    private readonly SortedDictionary<(int X, int Y), List<T>> _buckets = [];
    private float _cellSize;

    public SpatialGrid(float cellSize = 96f)
    {
        _cellSize = MathF.Max(cellSize, 1f);
    }

    public float CellSize => _cellSize;

    /// <summary>
    /// Clears all buckets (retaining their backing lists) and optionally resizes
    /// the cell. Call once at the start of a rebuild.
    /// </summary>
    public void Reset(float cellSize)
    {
        _cellSize = MathF.Max(cellSize, 1f);
        foreach (var bucket in _buckets.Values)
        {
            bucket.Clear();
        }
    }

    /// <summary>Clears all buckets, keeping the current cell size.</summary>
    public void Clear()
    {
        foreach (var bucket in _buckets.Values)
        {
            bucket.Clear();
        }
    }

    public void Add(Vector2 position, T item)
    {
        Add(position.X, position.Y, item);
    }

    public void Add(float x, float y, T item)
    {
        var cell = Cell(x, y);
        if (!_buckets.TryGetValue(cell, out var bucket))
        {
            bucket = [];
            _buckets[cell] = bucket;
        }

        bucket.Add(item);
    }

    /// <summary>
    /// Visits every item within <paramref name="cellRadius"/> cells of the cell
    /// containing <paramref name="position"/> (inclusive box). Radius 1 is the
    /// common 3x3 neighborhood; a larger radius covers a wider query range.
    ///
    /// Returns a struct enumerator so `foreach` over neighbors allocates nothing
    /// per call - this runs per-entity-per-tick in the combat/separation hot paths.
    /// </summary>
    public NeighborEnumerator Neighbors(Vector2 position, int cellRadius = 1)
    {
        return Neighbors(position.X, position.Y, cellRadius);
    }

    public NeighborEnumerator Neighbors(float x, float y, int cellRadius = 1)
    {
        var origin = Cell(x, y);
        return new NeighborEnumerator(_buckets, origin, cellRadius);
    }

    /// <summary>
    /// Allocation-free enumerator over the items in a square cell neighborhood.
    /// Walks cells x-outer/y-inner (matching the prior hand-rolled loops) and the
    /// items within each cell in insertion order.
    /// </summary>
    public struct NeighborEnumerator
    {
        private readonly SortedDictionary<(int X, int Y), List<T>> _buckets;
        private readonly int _minX;
        private readonly int _maxX;
        private readonly int _minY;
        private readonly int _maxY;
        private int _x;
        private int _y;
        private List<T>? _bucket;
        private int _index;

        public NeighborEnumerator(SortedDictionary<(int X, int Y), List<T>> buckets, (int X, int Y) origin, int cellRadius)
        {
            _buckets = buckets;
            _minX = origin.X - cellRadius;
            _maxX = origin.X + cellRadius;
            _minY = origin.Y - cellRadius;
            _maxY = origin.Y + cellRadius;
            _x = _minX;
            _y = _minY;
            _bucket = null;
            _index = -1;
            Current = default!;
        }

        public T Current { get; private set; }

        public readonly NeighborEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            // Continue the current bucket if one is in progress.
            if (_bucket is not null)
            {
                _index++;
                if (_index < _bucket.Count)
                {
                    Current = _bucket[_index];
                    return true;
                }

                _bucket = null;
            }

            // Walk cells (x-outer, y-inner) until a non-empty bucket is found.
            while (_x <= _maxX)
            {
                var cell = (_x, _y);

                // Advance the cell cursor for next time.
                if (_y < _maxY)
                {
                    _y++;
                }
                else
                {
                    _y = _minY;
                    _x++;
                }

                if (_buckets.TryGetValue(cell, out var bucket) && bucket.Count > 0)
                {
                    _bucket = bucket;
                    _index = 0;
                    Current = bucket[0];
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Cell radius needed to cover a world-space query range at the current cell
    /// size (at least 1). Matches CombatSystem's prior ceiling computation.
    /// </summary>
    public int CellRadiusFor(float range)
    {
        return Math.Max(1, (int)MathF.Ceiling(range / _cellSize));
    }

    /// <summary>Integer cell for a world position. Single guarded quantization.</summary>
    public (int X, int Y) Cell(Vector2 position)
    {
        return Cell(position.X, position.Y);
    }

    public (int X, int Y) Cell(float x, float y)
    {
        return ((int)MathF.Floor(x / _cellSize), (int)MathF.Floor(y / _cellSize));
    }
}
