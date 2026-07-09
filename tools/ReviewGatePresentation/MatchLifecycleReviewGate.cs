static class MatchLifecycleReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var verifyAll = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        var pauseQa = ReviewGateSource.Read(root, "scripts", "PauseQaRoot.cs");
        var skirmishQa = ReviewGateSource.Read(root, "scripts", "SkirmishFlowQaRoot.cs");
        var pauseMenu = ReviewGateSource.Read(root, "scripts", "ui", "PauseMenuLayer.cs");
        var outcome = ReviewGateSource.Read(root, "scripts", "ui", "OutcomeScreenLayer.cs");
        var settings = ReviewGateSource.Read(root, "scripts", "ui", "SettingsOverlayLayer.cs");
        var controlBindingCatalog = ReviewGateSource.Read(root, "scripts", "core", "presentation", "ui", "ControlBindingCatalog.cs");
        var englishText = ReviewGateSource.Read(root, "scripts", "core", "localization", "GameText.English.cs");
        var chineseText = ReviewGateSource.Read(root, "scripts", "core", "localization", "GameText.ChineseSimplified.cs");

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
        RequireText(controlBindingCatalog, "public static IReadOnlyList<ControlBindingSection> Sections", "Control binding sections must live in a shared catalog.", result);
        RequireText(controlBindingCatalog, "public static IReadOnlyList<string> SettingsOverviewRowKeys", "Settings controls overview must draw from the shared binding catalog.", result);
        RequireText(controlBindingCatalog, "\"hotkeys.build.4\"", "Shared binding catalog must include batch production controls.", result);
        RequireText(settings, "Name = \"ControlsBindingOverview\"", "Settings overlay must expose a stable controls binding overview node.", result);
        RequireText(settings, "Name = \"ControlsBindingSectionSelect\"", "Settings overlay must expose a stable controls section selector node.", result);
        RequireText(settings, "Name = \"ControlsBindingSectionRows\"", "Settings overlay must expose stable controls section rows.", result);
        RequireText(settings, "SettingsControlsOverviewText()", "Settings overlay controls overview must use shared binding catalog rows.", result);
        RequireText(settings, "SettingsControlsSectionText(_selectedControlsSectionIndex)", "Settings overlay controls section rows must refresh from the selected shared binding section.", result);
        RequireText(settings, "_status.Text = SettingsControlsSectionStatusText(_selectedControlsSectionIndex)", "Settings controls section selection must preview remap status without mutating bindings.", result);
        RequireText(settings, "SettingsControlsSectionStatusText(int sectionIndex)", "Settings controls remap status must be localized through a helper.", result);
        RequireText(settings, "ControlBindingCatalog.Sections[index].TitleKey", "Settings overlay controls section selector must read titles from the shared binding catalog.", result);
        RequireText(settings, "_controlsOverview.Text = SettingsControlsOverviewText()", "Settings overlay language refresh must update shared binding catalog rows.", result);
        ForbidText(settings, "\"hotkeys.camera.1\"", "Settings overlay must not duplicate binding row keys outside ControlBindingCatalog.", result);
        RequireText(englishText, "[\"settings.controls\"] = \"CONTROLS\"", "English settings controls label must exist.", result);
        RequireText(englishText, "[\"settings.controls.sectionStatus\"] = \"{0} selected; remap later\"", "English controls section remap-status feedback must exist.", result);
        RequireText(englishText, "[\"hotkeys.build.4\"] = \"Shift-click trains x5\"", "English hotkey legend must expose batch production controls.", result);
        ForbidText(englishText, "[\"settings.controlsOverview\"]", "Settings controls overview must not drift from the shared binding catalog.", result);
        RequireText(chineseText, "[\"settings.controls\"] = \"控制\"", "Chinese settings controls label must exist.", result);
        RequireText(chineseText, "[\"settings.controls.sectionStatus\"] = \"已选择 {0}；后续重绑定\"", "Chinese controls section remap-status feedback must exist.", result);
        RequireText(chineseText, "[\"hotkeys.build.4\"] = \"Shift 点击训练 x5\"", "Chinese hotkey legend must expose batch production controls.", result);
        ForbidText(chineseText, "[\"settings.controlsOverview\"]", "Settings controls overview must not drift from the shared binding catalog.", result);
    }
}
