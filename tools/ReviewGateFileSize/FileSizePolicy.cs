static class FileSizePolicy
{
    public const int HealthyMax = 200;
    public const int NormalMax = 400;
    public const int YellowMax = 600;
    public const int ReviewGateFileMax = HealthyMax;
    public const int ReviewGateRunnerMax = 1000;
    public const int ValidationToolSuiteMax = 1000;
    public const int BridgeLegacyCompatibilityBaseline = 8;

    public static readonly Dictionary<string, int> KnownRedLineCeilings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["scripts/ui/hud/HudLayer.State.cs"] = 625,
        ["tools/DesktopHudQa/Program.cs"] = 607,
    };

    public static readonly string[] StableEntrypoints =
    [
        "scripts/core/sim/systems/CombatSystem.cs",
        "scripts/core/units/runtime/UnitBattlefield.cs",
        "scripts/core/GameState.cs",
        "scripts/BattleRoot.cs",
        "scripts/ui/HudLayer.cs",
        "tools/ReviewGate/Program.cs",
    ];

    public static readonly string[] ReviewGateSourceRoots =
    [
        "tools/ReviewGate/",
        "tools/ReviewGateCombat/",
        "tools/ReviewGateCore/",
        "tools/ReviewGateDomains/",
        "tools/ReviewGateFileSize/",
        "tools/ReviewGateReservations/",
    ];

    public static readonly string[] MainProjectCompileExclusions =
    [
        ".godot\\**\\*.cs",
        "artifacts\\**\\*.cs",
        "tools\\**\\*.cs",
    ];

    public static readonly string[] ForbiddenFileNames =
    [
        "Helper",
        "Helpers",
        "Utils",
        "Utility",
        "Misc",
        "Common",
        "Manager",
    ];

    public static bool IsReviewGateSource(string path)
    {
        return ReviewGateSourceRoots.Any(root => path.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }
}
