namespace ProceduralRts.Core;

public enum BattleHudRuntimeStateKind
{
    Empty,
    UnitSelected,
    ProductionBuildingSelected,
    UnavailableLowResources,
    QueueProgress,
    Alert,
}

public enum BattleHudRuntimeSourceKind
{
    ReadOnlyProjection,
    CommandIntent,
}

public enum BattleHudCommandIntentKind
{
    None,
    QueueProduction,
}

public enum BattleHudSelectionKind
{
    None,
    Unit,
    ProductionBuilding,
}

public readonly record struct BattleHudCaptureResolution(int Width, int Height)
{
    public string Suffix => $"{Width}x{Height}";
}

public readonly record struct BattleHudRuntimeCaptureConfig(
    GameLanguage Language,
    int StartingCredits,
    int MapSeed,
    EnemyDifficulty EnemyDifficulty,
    LaunchMode LaunchMode,
    WorldVisualTheme Theme,
    int SettleFrames);

public readonly record struct BattleHudSelectionProjection(
    BattleHudSelectionKind Kind,
    string Title,
    string Meta,
    string Stats,
    string Detail,
    string PortraitMode,
    IconGlyph Icon)
{
    public static BattleHudSelectionProjection Empty { get; } = new(
        BattleHudSelectionKind.None,
        "NO SELECTION",
        "READY",
        "SELECT A UNIT OR STRUCTURE",
        "NORMAL SKIRMISH",
        "none",
        IconGlyph.None);
}

public readonly record struct BattleHudProductionProjection(
    bool Visible,
    int Cost,
    bool EnoughCredits,
    int QueuedCount,
    float ActiveProgress,
    string DisabledReasonKey,
    string QueueSummary,
    bool CanCancel)
{
    public static BattleHudProductionProjection None { get; } = new(
        false,
        0,
        true,
        0,
        0,
        "",
        "QUEUE EMPTY",
        false);
}

public readonly record struct BattleHudAlertProjection(
    AlertKind Kind,
    string Text,
    float RemainingRatio);

public sealed record BattleHudRuntimeProjection(
    BattleHudSelectionProjection Selection,
    int Credits,
    BattleHudProductionProjection Production,
    BattleHudAlertProjection? Alert,
    bool CommandDeckOpen,
    string Status);

public sealed record BattleHudRuntimeStateSpec(
    BattleHudRuntimeStateKind Kind,
    string CaptureId,
    BattleHudRuntimeSourceKind SourceKind,
    BattleHudCommandIntentKind CommandIntent,
    BattleHudRuntimeProjection Projection)
{
    public string CaptureFileName(BattleHudCaptureResolution resolution) =>
        $"battle_hud_runtime_{CaptureId}_{resolution.Suffix}.png";
}

public static class BattleHudRuntimeStateCatalog
{
    public static BattleHudRuntimeCaptureConfig CaptureConfig { get; } = new(
        GameLanguage.English,
        2400,
        1729,
        EnemyDifficulty.Normal,
        LaunchMode.Skirmish,
        WorldVisualTheme.DayCommand,
        8);

    private static readonly IReadOnlyList<BattleHudCaptureResolution> CaptureResolutions =
        Array.AsReadOnly<BattleHudCaptureResolution>(
        [
            new(1280, 720),
            new(1600, 900),
            new(1920, 1080),
        ]);

    private static readonly IReadOnlyList<BattleHudRuntimeStateSpec> RuntimeStates =
        Array.AsReadOnly<BattleHudRuntimeStateSpec>(
        [
            new(
                BattleHudRuntimeStateKind.Empty,
                "empty",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    BattleHudSelectionProjection.Empty,
                    400,
                    BattleHudProductionProjection.None,
                    null,
                    false,
                    "READY")),
            new(
                BattleHudRuntimeStateKind.UnitSelected,
                "unit_selected",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    new BattleHudSelectionProjection(
                        BattleHudSelectionKind.Unit,
                        "ALLEY RUNNER",
                        "READY",
                        "HP 180  SPD 92  RNG 140",
                        "RECON INFANTRY / DIRECT FIRE",
                        "unit",
                        IconGlyph.Infantry),
                    400,
                    BattleHudProductionProjection.None,
                    null,
                    false,
                    "UNIT SELECTED")),
            new(
                BattleHudRuntimeStateKind.ProductionBuildingSelected,
                "production_building_selected",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    ProductionBuildingSelection(),
                    640,
                    AvailableProduction(),
                    null,
                    true,
                    "PRODUCTION READY")),
            new(
                BattleHudRuntimeStateKind.UnavailableLowResources,
                "unavailable_low_resources",
                BattleHudRuntimeSourceKind.CommandIntent,
                BattleHudCommandIntentKind.QueueProduction,
                new BattleHudRuntimeProjection(
                    ProductionBuildingSelection(),
                    40,
                    AvailableProduction() with
                    {
                        EnoughCredits = false,
                        DisabledReasonKey = "ui.needCredits",
                    },
                    null,
                    true,
                    "INSUFFICIENT CREDITS")),
            new(
                BattleHudRuntimeStateKind.QueueProgress,
                "queue_progress",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    ProductionBuildingSelection(),
                    640,
                    AvailableProduction() with
                    {
                        QueuedCount = 4,
                        ActiveProgress = 0.56f,
                        QueueSummary = "ALLEY RUNNER  56%  QUEUED 4",
                        CanCancel = true,
                    },
                    null,
                    true,
                    "PRODUCTION ACTIVE")),
            new(
                BattleHudRuntimeStateKind.Alert,
                "alert",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    BattleHudSelectionProjection.Empty,
                    80,
                    BattleHudProductionProjection.None,
                    new BattleHudAlertProjection(AlertKind.Economy, "INSUFFICIENT CREDITS", 1),
                    false,
                    "ECONOMY ALERT")),
        ]);

    public static IReadOnlyList<BattleHudCaptureResolution> Resolutions => CaptureResolutions;

    public static IReadOnlyList<BattleHudRuntimeStateSpec> States => RuntimeStates;

    public static BattleHudRuntimeStateSpec For(BattleHudRuntimeStateKind kind) =>
        RuntimeStates.First(state => state.Kind == kind);

    private static BattleHudSelectionProjection ProductionBuildingSelection() => new(
        BattleHudSelectionKind.ProductionBuilding,
        "CAT BARRACKS",
        "OPERATIONAL",
        "HP 1200 / 1200  POWER +0",
        "INFANTRY PRODUCER / SELL REFUND 300",
        "building",
        IconGlyph.Building);

    private static BattleHudProductionProjection AvailableProduction() => new(
        true,
        120,
        true,
        0,
        0,
        "",
        "QUEUE EMPTY",
        false);
}
