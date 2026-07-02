internal static partial class SelectionStressSuite
{
    public static void Run()
    {
        var selectionCaseCount = RunSelectionQueries();
        var battlefieldPickCaseCount = RunUnitBattlefieldPickingQueries();

        RunGroupCommandScenarios();
        RunCameraCommandScenarios();
        RunEconomyCommandScenarios();
        RunPathingQueries();

        Console.WriteLine($"Selection stress passed: {selectionCaseCount + battlefieldPickCaseCount + 20} cases");
    }
}
