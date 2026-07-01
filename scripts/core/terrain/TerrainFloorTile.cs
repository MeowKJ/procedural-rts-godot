using Godot;

namespace ProceduralRts.Core;

public readonly record struct TerrainFloorTile(
    Rect2 Rect,
    TerrainFloorKind Kind,
    float Noise,
    Color Fill,
    Color Edge);
