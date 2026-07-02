internal static partial class SelectionStressSuite
{
    public static void Run()
    {
        var selectionCaseCount = RunSelectionQueries();
        var battlefieldPickCaseCount = RunUnitBattlefieldPickingQueries();
        var battlefieldSelectionCaseCount = RunUnitBattlefieldSelectionCommandScenarios();

        RunGroupCommandScenarios();
        RunCameraCommandScenarios();
        RunEconomyCommandScenarios();
        RunPathingQueries();

        Console.WriteLine($"Selection stress passed: {selectionCaseCount + battlefieldPickCaseCount + battlefieldSelectionCaseCount + 20} cases");
    }
}
