static class CommandConsoleReviewGate
{
    public static void Check(string root, GateResult result)
    {
        ReviewGateSource.RequireTextInFile(root, result, "#111820", "scripts", "ui", "SoftOldCityTheme.cs");
        ReviewGateSource.RequireTextInFile(root, result, "#C99A52", "scripts", "ui", "SoftOldCityTheme.cs");
        ReviewGateSource.RequireTextInFile(root, result, "#62C9C4", "scripts", "ui", "SoftOldCityTheme.cs");
        ReviewGateSource.RequireTextInFile(root, result, "#D75B5B", "scripts", "ui", "SoftOldCityTheme.cs");
        ReviewGateSource.RequireTextInFile(root, result, "RailWidth = 64", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "MinimumCommandHitTarget = 44", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "CommandRibbonMaxWidth = 720", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "CommandRibbonViewportFraction = 0.60f", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "CommandRibbonHeight = 56", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "CreateBottomCommandControls", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "CompactFieldText", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "CommandRibbonSurfaceMaxChars = 14", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "ProductionDrawerHeight", "scripts", "core", "presentation", "ui", "HudLayoutMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "UnexploredSurface = new(\"#26313B\")", "scripts", "ui", "hud", "HudLayer.Minimap.cs");
        ReviewGateSource.RequireTextInFile(root, result, "FogVeil = new(\"#26313B\", 1.0f)", "scripts", "ui", "hud", "HudLayer.Minimap.cs");
        ReviewGateSource.RequireTextInFile(root, result, "battle_hud_command_deck_dense.png", "scripts", "VisualQaCaptureRoot.cs");
        ReviewGateSource.RequireTextInFile(root, result, "battle_hud_command_ribbon.png", "scripts", "VisualQaCaptureRoot.cs");
        ReviewGateSource.RequireTextInFile(root, result, "battle_hud_selection_detail.png", "scripts", "VisualQaCaptureRoot.cs");
        ReviewGateSource.ForbidTextInSources(root, result, "TooltipText", "scripts/ui/UiFactory.cs");
        ReviewGateSource.ForbidTextInSources(root, result, "0.58f", "scripts/ui/hud/HudLayer.Minimap.cs");
        ReviewGateSource.ForbidTextInSources(root, result, "FogLift", "scripts/ui/hud/HudLayer.Minimap.cs");
        ReviewGateSource.ForbidTextInSources(root, result, "new Vector2(36, 34)", "scripts/ui/hud");
        ReviewGateSource.RequireTextInFile(root, result, "DrawFogTacticalGrid(rect);", "scripts", "ui", "hud", "HudLayer.Minimap.cs");
        ReviewGateSource.RequireTextInFile(root, result, "visible && !string.IsNullOrWhiteSpace(statusText)", "scripts", "ui", "hud", "provider", "HudLayer.ProviderRepeatControls.cs");
    }
}
