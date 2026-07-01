internal static partial class SelectionStressSuite
{
    public static void Run()
    {
        var selectionCaseCount = RunSelectionQueries();

        RunGroupCommandScenarios();
        RunCameraCommandScenarios();
        RunEconomyCommandScenarios();
        RunPathingQueries();

        Console.WriteLine($"Selection stress passed: {selectionCaseCount + 20} cases");
    }
}
