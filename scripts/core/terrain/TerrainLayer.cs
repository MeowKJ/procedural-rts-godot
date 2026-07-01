namespace ProceduralRts.Core;

[Flags]
public enum TerrainLayer
{
    None = 0,
    Ground = 1 << 0,
    Water = 1 << 1,
    Coast = 1 << 2,
    Air = 1 << 3
}
