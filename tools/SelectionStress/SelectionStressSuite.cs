internal static partial class SelectionStressSuite
{
    public static void Run()
    {
        var selectionCaseCount = RunSelectionQueries();
        var battlefieldPickCaseCount = RunUnitBattlefieldPickingQueries();
        var battlefieldSelectionCaseCount = RunUnitBattlefieldSelectionCommandScenarios();
        var battlefieldGroupCommandCaseCount = RunUnitBattlefieldGroupCommandSubjectScenarios();
        var selectionCandidateCaseCount = RunSelectionCandidateCountScenarios();

        RunGroupCommandScenarios();
        RunCameraCommandScenarios();
        RunEconomyCommandScenarios();
        RunPathingQueries();

        Console.WriteLine($"Selection stress passed: {selectionCaseCount + battlefieldPickCaseCount + battlefieldSelectionCaseCount + battlefieldGroupCommandCaseCount + selectionCandidateCaseCount + 20} cases");
    }
}
