using Godot;

namespace ProceduralRts.Core;

public readonly record struct TerrainFloorTileLayout(
    Rect2 Rect,
    TerrainFloorKind Kind,
    float Noise);
