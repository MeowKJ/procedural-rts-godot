static class MatchLifecycleReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var verifyAll = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        var pauseQa = ReviewGateSource.Read(root, "scripts", "PauseQaRoot.cs");
        var skirmishQa = ReviewGateSource.Read(root, "scripts", "SkirmishFlowQaRoot.cs");
        var pauseMenu = ReviewGateSource.Read(root, "scripts", "ui", "PauseMenuLayer.cs");
        var outcome = ReviewGateSource.Read(root, "scripts", "ui", "OutcomeScreenLayer.cs");

        RequireText(verifyAll, "godot-skirmish-flow-qa", "VerifyAll must cover menu setup into Battle.", result);
        RequireText(verifyAll, "godot-pause-qa", "VerifyAll must cover pause/restart/menu lifecycle.", result);
        RequireText(skirmishQa, "ChangeSceneToFile(\"res://scenes/MainMenu.tscn\")", "SkirmishFlowQa must start from MainMenu.", result);
        RequireText(skirmishQa, "StartSkirmishButton", "SkirmishFlowQa must launch Battle through the real menu button.", result);
        RequireText(skirmishQa, "CountNodes<BattleRoot>", "SkirmishFlowQa must assert battle root cleanup after setup flow.", result);
        RequireText(pauseQa, "PauseRestartButton", "PauseQa must exercise the real restart button.", result);
        RequireText(pauseQa, "PauseMainMenuButton", "PauseQa must exercise the real main-menu button.", result);
        RequireText(pauseQa, "CountNodes<BattleRoot>", "PauseQa must assert battle roots do not leak across lifecycle transitions.", result);
        RequireText(pauseMenu, "GetTree().Paused = false;", "Pause menu scene changes must clear paused state.", result);
        RequireText(outcome, "GetTree().Paused = false;", "Outcome scene changes must clear paused state.", result);
    }
}
