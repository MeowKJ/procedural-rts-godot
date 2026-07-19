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

public enum BattleHudRuntimeControlId
{
    ResourceStrip,
    MinimapCluster,
    RightRail,
    CommandRibbon,
    UnitDetailPanel,
    UnitStanceStrip,
    StanceHold,
    ProductionPanel,
    ProductionProviderLane0,
    ProductionCard,
    QueueMiniStack,
    CancelProduction,
    AlertRow0,
}

public enum BattleHudRuntimeSignalId
{
    NoSelection,
    Status,
    SelectionDetail,
    UniformHoldStance,
    BuildingDetail,
    ProductionReady,
    ProductionBlocked,
    QueueProgress,
    QueueCancel,
    AlertPayload,
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
    int SettleFrames,
    int RenderFlushFrames);

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
    UnitStanceStripProjection StanceStrip,
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
    BattleHudRuntimeProjection Projection,
    IReadOnlyList<BattleHudRuntimeControlId> CriticalControls,
    IReadOnlyList<BattleHudRuntimeSignalId> CriticalSignals)
{
    public string CaptureFileName(BattleHudCaptureResolution resolution) =>
        $"battle_hud_runtime_{CaptureId}_{resolution.Suffix}.png";
}

public sealed record BattleHudRuntimeControlEvidence(
    string ControlId,
    float X,
    float Y,
    float Width,
    float Height,
    float EffectiveAlpha);

public sealed record BattleHudRuntimeStructuralEvidence(
    string Scenario,
    string State,
    string CaptureId,
    string FileName,
    int Width,
    int Height,
    bool Passed,
    IReadOnlyList<string> Checks,
    IReadOnlyList<BattleHudRuntimeControlEvidence> Controls);

public static class BattleHudRuntimeStateCatalog
{
    public const string Scenario = "normal-skirmish-battle-hud-runtime-v1";
    public const string StructuralEvidenceFileName = "battle-hud-runtime-structural-evidence.json";
    public const string ArtifactManifestFileName = "battle-hud-runtime-artifact-manifest.json";

    public static BattleHudRuntimeCaptureConfig CaptureConfig { get; } = new(
        GameLanguage.English,
        2400,
        1729,
        EnemyDifficulty.Normal,
        LaunchMode.Skirmish,
        WorldVisualTheme.DayCommand,
        8,
        6);

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
                    UnitStanceStripProjection.None,
                    400,
                    BattleHudProductionProjection.None,
                    null,
                    false,
                    "READY"),
                CriticalControls(),
                Signals(BattleHudRuntimeSignalId.NoSelection, BattleHudRuntimeSignalId.Status)),
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
                    UnitStanceStripProjection.FromSelection(UnitStance.Hold, selectedUnitCount: 1),
                    400,
                    BattleHudProductionProjection.None,
                    null,
                    false,
                    "UNIT SELECTED"),
                CriticalControls(
                    BattleHudRuntimeControlId.UnitDetailPanel,
                    BattleHudRuntimeControlId.UnitStanceStrip,
                    BattleHudRuntimeControlId.StanceHold),
                Signals(
                    BattleHudRuntimeSignalId.SelectionDetail,
                    BattleHudRuntimeSignalId.UniformHoldStance,
                    BattleHudRuntimeSignalId.Status)),
            new(
                BattleHudRuntimeStateKind.ProductionBuildingSelected,
                "production_building_selected",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    ProductionBuildingSelection(),
                    UnitStanceStripProjection.None,
                    640,
                    AvailableProduction(),
                    null,
                    true,
                    "PROD READY"),
                ProductionControls(),
                Signals(
                    BattleHudRuntimeSignalId.BuildingDetail,
                    BattleHudRuntimeSignalId.ProductionReady,
                    BattleHudRuntimeSignalId.Status)),
            new(
                BattleHudRuntimeStateKind.UnavailableLowResources,
                "unavailable_low_resources",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    ProductionBuildingSelection(),
                    UnitStanceStripProjection.None,
                    40,
                    AvailableProduction() with
                    {
                        EnoughCredits = false,
                        DisabledReasonKey = "ui.needCredits",
                    },
                    null,
                    true,
                    "LOW CREDITS"),
                ProductionControls(),
                Signals(
                    BattleHudRuntimeSignalId.BuildingDetail,
                    BattleHudRuntimeSignalId.ProductionBlocked,
                    BattleHudRuntimeSignalId.Status)),
            new(
                BattleHudRuntimeStateKind.QueueProgress,
                "queue_progress",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    ProductionBuildingSelection(),
                    UnitStanceStripProjection.None,
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
                    "QUEUE ACTIVE"),
                ProductionControls(),
                Signals(
                    BattleHudRuntimeSignalId.BuildingDetail,
                    BattleHudRuntimeSignalId.QueueProgress,
                    BattleHudRuntimeSignalId.QueueCancel,
                    BattleHudRuntimeSignalId.Status)),
            new(
                BattleHudRuntimeStateKind.Alert,
                "alert",
                BattleHudRuntimeSourceKind.ReadOnlyProjection,
                BattleHudCommandIntentKind.None,
                new BattleHudRuntimeProjection(
                    BattleHudSelectionProjection.Empty,
                    UnitStanceStripProjection.None,
                    80,
                    BattleHudProductionProjection.None,
                    new BattleHudAlertProjection(AlertKind.Economy, "INSUFFICIENT CREDITS", 1),
                    false,
                    "CREDIT ALERT"),
                CriticalControls(BattleHudRuntimeControlId.AlertRow0),
                Signals(BattleHudRuntimeSignalId.AlertPayload, BattleHudRuntimeSignalId.Status)),
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
        "SELL REFUND 300",
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

    private static IReadOnlyList<BattleHudRuntimeControlId> CriticalControls(
        params BattleHudRuntimeControlId[] contextualControls)
    {
        var controls = new List<BattleHudRuntimeControlId>
        {
            BattleHudRuntimeControlId.ResourceStrip,
            BattleHudRuntimeControlId.MinimapCluster,
            BattleHudRuntimeControlId.RightRail,
            BattleHudRuntimeControlId.CommandRibbon,
        };
        controls.AddRange(contextualControls);
        return controls.AsReadOnly();
    }

    private static IReadOnlyList<BattleHudRuntimeControlId> ProductionControls() =>
        CriticalControls(
            BattleHudRuntimeControlId.UnitDetailPanel,
            BattleHudRuntimeControlId.ProductionPanel,
            BattleHudRuntimeControlId.ProductionProviderLane0,
            BattleHudRuntimeControlId.ProductionCard,
            BattleHudRuntimeControlId.QueueMiniStack,
            BattleHudRuntimeControlId.CancelProduction);

    private static IReadOnlyList<BattleHudRuntimeSignalId> Signals(params BattleHudRuntimeSignalId[] signals) =>
        Array.AsReadOnly(signals);
}
