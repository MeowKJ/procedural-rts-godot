using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static void PrintLoop(OpponentLoopReport report)
    {
        Console.WriteLine("AiOpponentLoopQa runtime loop");
        Console.WriteLine($"  duration: {report.SimulationSeconds}s, outcome: {report.Outcome}");
        Console.WriteLine($"  harvest: assignments {report.HarvestAssignments}, depleted {report.EnemyFieldDepleted}, credits start/peak {report.EnemyCreditsStart}/{report.EnemyCreditsPeak}, bridge commands {report.HarvestBridgeCommands}");
        Console.WriteLine($"  builds present: {string.Join(", ", report.BuiltEnemyBuildingSpecIds)}");
        Console.WriteLine($"  production: orders {report.ProductionOrders}, queued {report.ProductionQueuedEvents}, completed {report.ProductionCompletedEvents}, bridge commands {report.ProductionBridgeCommands}");
        Console.WriteLine($"  produced designs: {string.Join(", ", report.ProducedDesignIds)}");
        Console.WriteLine($"  mixed combat: infantry {report.ProducedInfantry}, vehicles {report.ProducedVehicles}, max combat alive {report.MaxEnemyCombatUnitsAlive}");
        Console.WriteLine($"  defense: building hits {report.DefenseBuildingHits}, unit hits {report.DefenseUnitHits}, raider deaths {report.RaiderDeaths}, raider damage {report.RaiderHpDamage:0}");
        Console.WriteLine($"  attack waves: waves {report.WavesLaunched}, first tick {report.FirstWaveTick}, second tick {report.SecondWaveTick}, max manual attackers {report.MaxManualWaveAttackers}, HQ damage {report.PlayerHqDamage:0}, bridge commands {report.WaveBridgeCommands}");
        Console.WriteLine($"  command proof: total applied {report.TotalAppliedCommands}, production status '{report.ProductionStatus}', wave status '{report.WaveStatus}'");
    }

    private static void PrintBuildProbe(BuildCommandProbeReport report)
    {
        Console.WriteLine("AiOpponentLoopQa construction command probe");
        Console.WriteLine($"  commands: {report.CommandsSubmitted}, all StartConstruction build commands: {report.CommandsWereBuildCommands}");
        Console.WriteLine($"  completed: {string.Join(", ", report.CompletedBuildingSpecIds)}, rejections {report.Rejections}, credits {report.RemainingCredits}, hash {report.StateHash:X16}");
    }
}

internal sealed record BaseRuntime(
    UnitBattlefieldBuildingSnapshot Headquarters,
    UnitBattlefieldBuildingSnapshot GroundTurret);

internal sealed record OpponentLoopReport(
    int SimulationSeconds,
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
    int ProductionBridgeCommands,
    int WaveBridgeCommands,
    string ProductionStatus,
    string WaveStatus,
    GameOutcome Outcome);

internal sealed record BuildCommandProbeReport(
    int CommandsSubmitted,
    bool CommandsWereBuildCommands,
    IReadOnlyList<string> CompletedBuildingSpecIds,
    int Rejections,
    int RemainingCredits,
    ulong StateHash);
