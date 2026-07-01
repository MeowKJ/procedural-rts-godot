namespace ProceduralRts.Core;

public readonly record struct SimSystemTiming(
    int Samples,
    double TotalMs,
    double LastMs,
    double MaxMs)
{
    public double AverageMs => Samples <= 0 ? 0 : TotalMs / Samples;
}
