using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static void PrintBuildProbe(BuildCommandProbeReport report)
    {
        Console.WriteLine("AiOpponentLoopQa construction command probe");
        Console.WriteLine($"  commands: {report.CommandsSubmitted}, all StartConstruction build commands: {report.CommandsWereBuildCommands}");
        Console.WriteLine($"  completed: {string.Join(", ", report.CompletedBuildingSpecIds)}, rejections {report.Rejections}, credits {report.RemainingCredits}, hash {report.StateHash:X16}");
    }

    private static void PrintTournamentCase(TournamentCaseResult result)
    {
        if (result.Metrics is not { } metrics)
        {
            Console.WriteLine($"case seed={result.Config.Seed} mapping={result.Config.Mapping}: ERROR {string.Join("; ", result.Failures)}");
            return;
        }

        Console.WriteLine(
            $"case seed={result.Config.Seed} mapping={result.Config.Mapping}: "
            + $"{(result.Failures.Count == 0 ? "PASS" : "FAIL")} sha256={result.FirstSha256} "
            + $"harvest={metrics.EnemyFieldDepleted} build={metrics.ConstructionOrders} "
            + $"production={metrics.ProductionCompletedEvents} engagement={metrics.DefenseBuildingHits + metrics.DefenseUnitHits + metrics.EnemyBuildingHitsOnPlayerBase} "
            + $"milestone={metrics.MilestoneCompletionTick} termination={metrics.Termination}@{metrics.FinalTick} outcome={metrics.Outcome}");
    }
}

internal sealed record BaseRuntime(
    UnitBattlefieldBuildingSnapshot Headquarters,
    UnitBattlefieldBuildingSnapshot GroundTurret);

internal sealed record OpponentLoopReport(
    int FinalTick,
    string Termination,
    int? OutcomeTick,
    double SimulationSeconds,
    string SetupFingerprint,
    int ProductionOrders,
    int ProductionQueuedEvents,
    int ProductionCompletedEvents,
    IReadOnlyList<string> ProducedDesignIds,
    IReadOnlyList<string> ProducedCombatDesignIds,
    int ProducedInfantry,
    int ProducedVehicles,
    int HarvestAssignments,
    int EnemyFieldDepleted,
    int EnemyCreditsStart,
    int EnemyCreditsPeak,
    int ResourceEvents,
    int ConstructionOrders,
    IReadOnlyList<string> BuiltEnemyBuildingSpecIds,
    int DefenseBuildingHits,
    int DefenseUnitHits,
    int RaiderDeaths,
    float RaiderHpDamage,
    int WavesLaunched,
    int FirstWaveTick,
    int SecondWaveTick,
    int MaxManualWaveAttackers,
    int LaunchedWaveUnitOrders,
    int EnemyBuildingHitsOnPlayerBase,
    float PlayerHqDamage,
    int EnemyCombatUnitsAlive,
    int MaxEnemyCombatUnitsAlive,
    int TotalAppliedCommands,
    int HarvestBridgeCommands,
    int ConstructionBridgeCommands,
    int ProductionBridgeCommands,
    int WaveBridgeCommands,
    int LeftAttackCommands,
    float LeftToRightDamage,
    float RightToLeftDamage,
    int LeftFinalUnitCount,
    int RightFinalUnitCount,
    int LeftFinalBuildingCount,
    int RightFinalBuildingCount,
    string ProductionStatus,
    string WaveStatus,
    GameOutcome Outcome,
    int FirstHarvestTick,
    int FirstConstructionTick,
    int FirstProductionTick,
    int FirstEngagementTick,
    int MilestoneCompletionTick,
    TournamentStateFailure? StateFailure);

internal sealed record TournamentStateFailure(string Code, string Subject, int Tick);

internal sealed record BuildCommandProbeReport(
    int CommandsSubmitted,
    bool CommandsWereBuildCommands,
    IReadOnlyList<string> CompletedBuildingSpecIds,
    int Rejections,
    int RemainingCredits,
    ulong StateHash);

internal sealed record TournamentCaseConfig(
    int Seed,
    string Mapping,
    FactionId LeftFaction,
    FactionId RightFaction)
{
    public string ReproductionCommand =>
        $"dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore -- --seed {Seed} --mapping {Mapping}";
}

internal sealed record TournamentCaseResult(
    TournamentCaseConfig Config,
    OpponentLoopReport? Metrics,
    string? FirstSha256,
    string? SecondSha256,
    bool Deterministic,
    string Termination,
    int FinalTick,
    int? OutcomeTick,
    IReadOnlyList<string> Failures,
    string ReproductionCommand);

internal sealed record TournamentReport(
    string SchemaVersion,
    string TournamentVersion,
    double FixedDelta,
    int SimulationTicks,
    IReadOnlyList<int> MatrixSeeds,
    IReadOnlyList<string> MatrixMappings,
    TournamentSelectedFilters SelectedFilters,
    int PassedCaseCount,
    int FailedCaseCount,
    IReadOnlyList<TournamentCaseResult> Cases,
    BuildCommandProbeReport? BuildProbe,
    IReadOnlyList<string> Failures);

internal sealed record TournamentSelectedFilters(int? Seed, string? Mapping);
