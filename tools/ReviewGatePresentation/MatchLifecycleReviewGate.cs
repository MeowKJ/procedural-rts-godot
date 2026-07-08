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
        RequireText(settings, "Name = \"ControlsBindingDefaultsButton\"", "Settings overlay must expose a stable controls defaults affordance node.", result);
        RequireText(settings, "Name = \"ControlsBindingSectionRows\"", "Settings overlay must expose stable controls section rows.", result);
        RequireText(settings, "Name = \"ControlsBindingDefaultsStatus\"", "Settings overlay must expose stable controls defaults status text.", result);
        RequireText(settings, "SettingsControlsOverviewText()", "Settings overlay controls overview must use shared binding catalog rows.", result);
        RequireText(settings, "SettingsControlsSectionText(_selectedControlsSectionIndex)", "Settings overlay controls section rows must refresh from the selected shared binding section.", result);
        RequireText(settings, "SettingsControlsDefaultsText(_selectedControlsSectionIndex)", "Settings overlay controls defaults status must be scoped to the selected binding section.", result);
        RequireText(settings, "_controlsSectionRows.CustomMinimumSize = new Vector2(274, 52)", "Settings overlay controls section rows must leave vertical room for defaults status at 720p.", result);
        RequireText(settings, "_controlsDefaultsStatus.Position = new Vector2(204, 502)", "Settings overlay controls defaults status must stay above audio controls at 720p.", result);
        RequireText(settings, "ControlBindingCatalog.Sections[index].TitleKey", "Settings overlay controls section selector must read titles from the shared binding catalog.", result);
        RequireText(settings, "_controlsDefaults.Disabled = true", "Settings overlay controls defaults affordance must remain non-mutating until remapping persistence exists.", result);
        RequireText(settings, "_controlsOverview.Text = SettingsControlsOverviewText()", "Settings overlay language refresh must update shared binding catalog rows.", result);
        RequireText(settings, "_controlsDefaultsStatus.Text = SettingsControlsDefaultsText(_selectedControlsSectionIndex)", "Settings overlay language refresh must update controls defaults status.", result);
        ForbidText(settings, "\"hotkeys.camera.1\"", "Settings overlay must not duplicate binding row keys outside ControlBindingCatalog.", result);
        RequireText(englishText, "[\"settings.controls\"] = \"CONTROLS\"", "English settings controls label must exist.", result);
        RequireText(englishText, "[\"settings.controls.defaults\"] = \"DEFAULTS\"", "English settings controls defaults affordance text must exist.", result);
        RequireText(englishText, "[\"settings.controls.defaultsStatus\"] = \"{0} defaults active; editing later\"", "English settings controls defaults status must explain the non-persistent staging state.", result);
        RequireText(englishText, "[\"hotkeys.build.4\"] = \"Shift-click trains x5\"", "English hotkey legend must expose batch production controls.", result);
        ForbidText(englishText, "[\"settings.controlsOverview\"]", "Settings controls overview must not drift from the shared binding catalog.", result);
        RequireText(chineseText, "[\"settings.controls\"] = \"控制\"", "Chinese settings controls label must exist.", result);
        RequireText(chineseText, "[\"settings.controls.defaults\"] = \"默认\"", "Chinese settings controls defaults affordance text must exist.", result);
        RequireText(chineseText, "[\"settings.controls.defaultsStatus\"] = \"{0} 默认已生效；编辑稍后接入\"", "Chinese settings controls defaults status must explain the non-persistent staging state.", result);
        RequireText(chineseText, "[\"hotkeys.build.4\"] = \"Shift 点击训练 x5\"", "Chinese hotkey legend must expose batch production controls.", result);
        ForbidText(chineseText, "[\"settings.controlsOverview\"]", "Settings controls overview must not drift from the shared binding catalog.", result);
    }
}
