using Godot;

namespace ProceduralRts.Core;

public sealed partial class FogOfWarMap(float cellSize = FogOfWarMap.DefaultCellSize)
{
    public const float DefaultCellSize = FogOfWarVisualPolicy.DefaultCellSize;

    private bool[,] _visible = new bool[0, 0];
    private bool[,] _explored = new bool[0, 0];
    private float[,] _visibleStrength = new float[0, 0];
    private float[,] _exploredStrength = new float[0, 0];
    private float[,] _previousVisibleStrength = new float[0, 0];
    private float[,] _previousExploredStrength = new float[0, 0];
    private Image? _maskImage;
    private ImageTexture? _maskTexture;
    private bool _maskTextureDirty = true;
    private MaskUpdateRange _dirtyMaskRange = MaskUpdateRange.None;
    private FogOfWarStats? _cachedStats;
    private bool _statsDirty = true;
    private bool _hasVisionSourceSignature;
    private ulong _lastVisionSourceSignature;
    private int _lastVisionSourceCount;

    public float CellSize { get; } = cellSize;
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public int MaskRevision { get; private set; }
    public int MaskTextureUploadCount { get; private set; }
    public Vector2 WorldSize { get; private set; }

    public FogOfWarMap(FogQualityTier quality)
        : this(FogOfWarVisualPolicy.CellSizeFor(quality))
    {
    }

    public bool HasPendingMaskTextureUpload(Rect2? updateWorldRect = null)
    {
        if (!_maskTextureDirty || Columns == 0 || Rows == 0)
        {
            return false;
        }

        if (_maskTexture is null)
        {
            return true;
        }

        var requestedRange = CellRangeFor(updateWorldRect);
        var dirtyRange = _dirtyMaskRange.HasCells ? _dirtyMaskRange : FullMaskRange();
        return requestedRange.Intersection(dirtyRange).HasCells;
    }

    public void Update(
        Vector2 worldSize,
        IEnumerable<(Vector2 Position, float SightRange)> visionSources)
    {
        var sources = visionSources as IReadOnlyList<(Vector2 Position, float SightRange)> ?? visionSources.ToArray();
        var sourceSignature = VisionSourceSignature(worldSize, sources);
        var canSkipUnchangedSources = _hasVisionSourceSignature
            && _lastVisionSourceSignature == sourceSignature
            && _lastVisionSourceCount == sources.Count
            && WorldSize == worldSize
            && Columns > 0
            && Rows > 0;
        if (canSkipUnchangedSources)
        {
            return;
        }

        EnsureSize(worldSize);
        Array.Copy(_visibleStrength, _previousVisibleStrength, _visibleStrength.Length);
        Array.Copy(_exploredStrength, _previousExploredStrength, _exploredStrength.Length);
        Array.Clear(_visible, 0, _visible.Length);
        Array.Clear(_visibleStrength, 0, _visibleStrength.Length);

        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            Reveal(source.Position, source.SightRange);
        }

        var changedRange = MaskChangedSincePreviousUpdate();
        if (changedRange.HasCells)
        {
            MaskRevision++;
            _maskTextureDirty = true;
            _dirtyMaskRange = _dirtyMaskRange.HasCells
                ? _dirtyMaskRange.Union(changedRange)
                : changedRange;
            _statsDirty = true;
        }

        _lastVisionSourceSignature = sourceSignature;
        _lastVisionSourceCount = sources.Count;
        _hasVisionSourceSignature = true;
    }

    public bool IsVisible(Vector2 worldPosition)
    {
        return TryCell(worldPosition, out var x, out var y) && _visible[x, y];
    }

    public bool IsExplored(Vector2 worldPosition)
    {
        return TryCell(worldPosition, out var x, out var y) && _explored[x, y];
    }

    public bool AnyVisible(Rect2 worldRect)
    {
        return AnyCell(worldRect, visible: true);
    }

    public bool AnyExplored(Rect2 worldRect)
    {
        return AnyCell(worldRect, visible: false);
    }

    public IReadOnlyList<FogOfWarCell> Snapshot()
    {
        var cells = new List<FogOfWarCell>(Columns * Rows);
        for (var y = 0; y < Rows; y++)
        {
            for (var x = 0; x < Columns; x++)
            {
                cells.Add(new FogOfWarCell(
                    x,
                    y,
                    x * CellSize,
                    y * CellSize,
                    CellSize,
                    _visible[x, y],
                    _explored[x, y]));
            }
        }

        return cells;
    }

    public FogOfWarStats Stats()
    {
        if (!_statsDirty && _cachedStats is { } cached)
        {
            return cached;
        }

        var visibleCells = 0;
        var exploredCells = 0;
        for (var y = 0; y < Rows; y++)
        {
            for (var x = 0; x < Columns; x++)
            {
                if (_visible[x, y])
                {
                    visibleCells++;
                }

                if (_explored[x, y])
                {
                    exploredCells++;
                }
            }
        }

        _cachedStats = new FogOfWarStats(
            Columns,
            Rows,
            visibleCells,
            exploredCells,
            Columns * Rows - exploredCells);
        _statsDirty = false;
        return _cachedStats.Value;
    }

    public Color DebugMaskPixel(Vector2 worldPosition)
    {
        return TryCell(worldPosition, out var x, out var y)
            ? MaskPixel(_visibleStrength[x, y], _exploredStrength[x, y])
            : MaskPixel(visibleStrength: 0, exploredStrength: 0);
    }

    public Texture2D? MaskTexture(Rect2? updateWorldRect = null)
    {
        if (Columns == 0 || Rows == 0)
        {
            return null;
        }

        if (_maskImage is null || _maskImage.GetWidth() != Columns || _maskImage.GetHeight() != Rows)
        {
            _maskImage = Image.CreateEmpty(Columns, Rows, false, Image.Format.Rgba8);
            _maskTexture = null;
            _maskTextureDirty = true;
            _dirtyMaskRange = FullMaskRange();
        }

        if (_maskTextureDirty)
        {
            var requestedRange = _maskTexture is null ? FullMaskRange() : CellRangeFor(updateWorldRect);
            var dirtyRange = _dirtyMaskRange.HasCells ? _dirtyMaskRange : FullMaskRange();
            var range = requestedRange.Intersection(dirtyRange);
            if (!range.HasCells)
            {
                return _maskTexture;
            }

            UpdateMaskImage(_maskImage, range);
            if (_maskTexture is null)
            {
                _maskTexture = ImageTexture.CreateFromImage(_maskImage);
            }
            else
            {
                _maskTexture.Update(_maskImage);
            }

            MaskTextureUploadCount++;
            var rangeCoversDirty = range.Covers(dirtyRange);
            if (rangeCoversDirty)
            {
                _dirtyMaskRange = MaskUpdateRange.None;
            }

            _maskTextureDirty = !range.Covers(Columns, Rows);
            if (rangeCoversDirty)
            {
                _maskTextureDirty = false;
            }
        }

        return _maskTexture;
    }

    public void ClearMemory()
    {
        Array.Clear(_visible, 0, _visible.Length);
        Array.Clear(_explored, 0, _explored.Length);
        Array.Clear(_visibleStrength, 0, _visibleStrength.Length);
        Array.Clear(_exploredStrength, 0, _exploredStrength.Length);
        Array.Clear(_previousVisibleStrength, 0, _previousVisibleStrength.Length);
        Array.Clear(_previousExploredStrength, 0, _previousExploredStrength.Length);
        MaskRevision++;
        _maskTextureDirty = true;
        _dirtyMaskRange = FullMaskRange();
        _statsDirty = true;
        _hasVisionSourceSignature = false;
    }

    public void ReleaseManagedResources()
    {
        _maskTexture?.Dispose();
        _maskTexture = null;
        _maskImage?.Dispose();
        _maskImage = null;
        _maskTextureDirty = true;
        _dirtyMaskRange = FullMaskRange();
    }

}
