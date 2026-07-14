internal static partial class SelectionStressSuite
{
    public static void Run()
    {
        var selectionCaseCount = RunSelectionQueries();
        var battlefieldPickCaseCount = RunUnitBattlefieldPickingQueries();
        var battlefieldSelectionCaseCount = RunUnitBattlefieldSelectionCommandScenarios();
        var legacySelectionCaseCount = RunLegacySelectionRectCandidateScenarios();
        var battlefieldGroupCommandCaseCount = RunUnitBattlefieldGroupCommandSubjectScenarios();

        RunGroupCommandScenarios();
        RunCameraCommandScenarios();
        RunEconomyCommandScenarios();
        RunPathingQueries();

        Console.WriteLine($"Selection stress passed: {selectionCaseCount + battlefieldPickCaseCount + battlefieldSelectionCaseCount + legacySelectionCaseCount + battlefieldGroupCommandCaseCount + 20} cases");
    }
}
