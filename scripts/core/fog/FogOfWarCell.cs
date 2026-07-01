namespace ProceduralRts.Core;

public readonly record struct FogOfWarCell(
    int X,
    int Y,
    float WorldX,
    float WorldY,
    float Size,
    bool Visible,
    bool Explored);
