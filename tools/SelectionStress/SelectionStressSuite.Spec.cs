using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static void AssertClose(float actual, float expected, string label)
    {
        if (MathF.Abs(actual - expected) > 0.001f)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }

    private static void AssertCloseWithin(float actual, float expected, float tolerance, string label)
    {
        if (MathF.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }

    private static void AssertRect(SelectionRect actual, SelectionRect expected, string label)
    {
        AssertClose(actual.X, expected.X, $"{label}.X");
        AssertClose(actual.Y, expected.Y, $"{label}.Y");
        AssertClose(actual.Width, expected.Width, $"{label}.Width");
        AssertClose(actual.Height, expected.Height, $"{label}.Height");
    }
}
