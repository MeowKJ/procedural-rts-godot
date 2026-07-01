namespace ProceduralRts.Core;

public readonly record struct FogOfWarStats(
    int Columns,
    int Rows,
    int VisibleCells,
    int ExploredCells,
    int ConcealedCells)
{
    public int TotalCells => Columns * Rows;
}
