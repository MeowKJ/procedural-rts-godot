static class PresentationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireHudAndTheme(root, result);
        RequireWorldPresentation(root, result);
        RequireVisualQa(root, result);
        BattleRootHudAllocationReviewGate.Check(root, result);
        HoverTooltipReviewGate.Check(root, result);
        CombatReadabilityReviewGate.Check(root, result);
        ClassSilhouetteReviewGate.Check(root, result);
        TacticalAudioReviewGate.Check(root, result); MatchLifecycleReviewGate.Check(root, result);
        ControlGroupAllocationReviewGate.Check(root, result);
        SelectionControllerAllocationReviewGate.Check(root, result);
        UnitRenderingAllocationReviewGate.Check(root, result);
    }
    private static void RequireHudAndTheme(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "HudLayer.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "UiFactory.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "UiFontProfile.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "SoftOldCityTheme.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "DynamicUnitIcon.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "presentation", "theme", "WorldThemeMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "Owner", "scripts", "core", "entities", "EntityRenderPalette.cs");
        ReviewGateSource.RequireTextInFile(root, result, "WorldVisualThemeState", "scripts", "core", "presentation", "theme", "WorldVisualThemeState.cs");
        var hudTheme = ReviewGateSource.Read(root, "scripts", "ui", "SoftOldCityTheme.cs");
        RequireText(hudTheme, "public static readonly SoftOldCityHudPalette NightRadar", "NightRadar HUD theme must have a dedicated radar-terminal palette.", result);
        RequireText(hudTheme, "PanelBorderStrong: new Color(SoftOldCityPalette.NightRadar", "NightRadar HUD theme must use the radar accent for strong borders.", result);
        RequireText(hudTheme, "Text: new Color(SoftOldCityPalette.NightRadarSoft", "NightRadar HUD theme must use the soft radar text color.", result);
        RequireText(hudTheme, "WorldVisualTheme.NightRadar => NightRadar", "NightRadar visual theme must map to its dedicated HUD palette.", result);
        ForbidText(hudTheme, "WorldVisualTheme.DuskDefense or WorldVisualTheme.NightRadar => Dusk", "NightRadar HUD theme must not alias the DuskDefense palette.", result);
    }

    private static void RequireWorldPresentation(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "world", "UnitInstanceView.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "world", "BuildingView.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "world", "FogOfWarLayer.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "world", "GridLayer.cs");
        ReviewGateSource.RequireAnyText(root, result, "EntityProjection", "scripts/world", "scripts/BattleRoot.EntityWorld.cs");
        ReviewGateSource.RequireAnyText(root, result, "RedrawSignature", "scripts/world");
    }

    private static void RequireVisualQa(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "tools", "DesktopHudQa", "DesktopHudQa.csproj");
        ReviewGateSource.RequireFile(root, result, "scenes", "UiFontQa.tscn");
        ReviewGateSource.RequireTextInFile(root, result, "godot-ui-font-qa", "tools", "VerifyAll", "Program.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "VisualQaCaptureRoot.cs");
        ReviewGateSource.RequireFile(root, result, "scenes", "VisualQaCapture.tscn");
        ReviewGateSource.RequireTextInFile(root, result, "desktop-hud-qa", "tools", "VerifyAll", "Program.cs");
        ReviewGateSource.RequireTextInFile(root, result, "battle_hud_style1c_dusk.png", "scripts", "VisualQaCaptureRoot.cs");
    }
}
