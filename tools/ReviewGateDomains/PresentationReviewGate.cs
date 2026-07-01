static class PresentationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireHudAndTheme(root, result);
        RequireWorldPresentation(root, result);
        RequireVisualQa(root, result);
    }

    private static void RequireHudAndTheme(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "HudLayer.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "UiFactory.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "SoftOldCityTheme.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "ui", "DynamicUnitIcon.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "presentation", "theme", "WorldThemeMath.cs");
        ReviewGateSource.RequireTextInFile(root, result, "Owner", "scripts", "core", "entities", "EntityRenderPalette.cs");
        ReviewGateSource.RequireTextInFile(root, result, "WorldVisualThemeState", "scripts", "core", "presentation", "theme", "WorldVisualThemeState.cs");
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
        ReviewGateSource.RequireFile(root, result, "scripts", "VisualQaCaptureRoot.cs");
        ReviewGateSource.RequireAnyText(root, result, "battle_hud_1920x1080", "TODO.md", "docs/reviews");
        ReviewGateSource.RequireAnyText(root, result, "battle_hud_style1c_dusk", "TODO.md", "docs/reviews");
    }
}
